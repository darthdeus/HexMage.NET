using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    /// <summary>
    /// Wraps all the helpers for generating game actions.
    /// </summary>
    public static class ActionGenerator {
        /// <summary>
        /// Stochastically picks an ability based on its damage/cost. Can be optionally
        /// run with a deterministic result simply returning the maximum value.
        /// </summary>
        public static UctAction MaxAbilityRatio(GameInstance game, List<UctAction> actions,
                                                bool deterministic = false) {
            if (deterministic) {
                return DeterministicMaxAbilityRatio(game, actions);
            } else {
                var pairs = actions.Select(action => {
                                       return Tuple.Create(action, game.MobManager.Abilities[action.AbilityId]);
                                   })
                                   .ToList();

                pairs.Sort((a, b) => b.Item2.DmgCostRatio.CompareTo(a.Item2.DmgCostRatio));

                var totalRatio = pairs.Sum(p => p.Item2.DmgCostRatio);

                var probabilities = pairs.Select(p => (double) p.Item2.DmgCostRatio / totalRatio);

                var pick = Probability.UniformPick(pairs, probabilities.ToList());
                return pick.Item1;
            }
        }

        private static UctAction DeterministicMaxAbilityRatio(GameInstance game, List<UctAction> actions) {
            UctAction max = actions[0];
            var maxAbilityInfo = game.MobManager.Abilities[max.AbilityId];

            for (int i = 1; i < actions.Count; i++) {
                var abilityInfo = game.MobManager.Abilities[actions[i].AbilityId];

                if (abilityInfo.DmgCostRatio > maxAbilityInfo.DmgCostRatio) {
                    max = actions[i];
                    maxAbilityInfo = abilityInfo;
                }
            }

            return max;
        }

        /// <summary>
        /// Calculates an action based on a simple set of rules.
        /// </summary>
        public static UctAction RuleBasedAction(GameInstance game) {
            if (Constants.FastActionGeneration) return DefaultPolicyAction(game);

            var result = new List<UctAction>();

            var currentMob = game.CurrentMob;
            if (!currentMob.HasValue) return UctAction.EndTurnAction();

            var mob = game.CachedMob(currentMob.Value);

            GenerateDirectAbilityUse(game, mob, result);
            if (result.Count > 0) return MaxAbilityRatio(game, result);

            GenerateAttackMoveActions(game, mob, result);
            if (result.Count > 0) return MaxAbilityRatio(game, result);

            GenerateDefensiveMoveActions(game, mob, result);
            if (result.Count > 0) return result[0];

            return UctAction.EndTurnAction();
        }

        /// <summary>
        /// Calculates an action according to a simple default policy. Used mainly
        /// in MCTS playouts.
        /// </summary>
        public static UctAction DefaultPolicyAction(GameInstance state) {
            var mobId = state.CurrentMob;

            if (mobId == null) {
                throw new InvalidOperationException("Requesting mob action when there is no current mob.");
            }

            Debug.Assert(state.State.MobInstances[mobId.Value].Hp > 0, "Current mob is dead");

            var mob = state.CachedMob(mobId.Value);

            if (mob.MobInstance.Ap == 0) return UctAction.EndTurnAction();

            var abilityIds = new List<int>();

            foreach (var possibleAbilityId in mob.MobInfo.Abilities) {
                if (GameInvariants.IsAbilityUsableNoTarget(state, mobId.Value, possibleAbilityId)) {
                    abilityIds.Add(possibleAbilityId);
                }
            }

            int moveTargetId = MobInstance.InvalidId;

            var actions = new List<UctAction>();

            foreach (var possibleTargetId in state.MobManager.Mobs) {
                var possibleTarget = state.CachedMob(possibleTargetId);

                moveTargetId = possibleTargetId;

                if (!GameInvariants.IsTargetable(state, mob, possibleTarget)) continue;

                if (abilityIds.Count == 0) continue;

                foreach (var abilityId in abilityIds) {
                    if (GameInvariants.IsAbilityUsableApRangeCheck(state, mob, possibleTarget, abilityId)) {
                        actions.Add(UctAction.AbilityUseAction(abilityId, mob.MobId, possibleTargetId));
                    }
                }
            }

            if (actions.Count > 0) {
                return MaxAbilityRatio(state, actions);
            }

            if (moveTargetId != MobInstance.InvalidId) {
                return PickMoveTowardsEnemyAction(state, state.CachedMob(mobId.Value),
                                                  state.CachedMob(moveTargetId));
            } else {
                Utils.Log(LogSeverity.Error, nameof(ActionGenerator), "No targets, game should be over");

                throw new InvalidOperationException("No targets, game should be over.");
            }
        }

        /// <summary>
        /// Calculates a move towards an enemy, or ends a turn if no such move is possible.
        /// </summary>
        private static UctAction PickMoveTowardsEnemyAction(GameInstance game, CachedMob mob,
                                                            CachedMob possibleTarget) {
            foreach (var targetId in game.MobManager.Mobs) {
                var target = game.CachedMob(targetId);

                if (!GameInvariants.IsTargetableNoSource(game, mob, target)) continue;

                var action = FastMoveTowardsEnemy(game, mob, target);

                if (action.Type == UctActionType.Move) return action;
            }

            return UctAction.EndTurnAction();
        }

        /// <summary>
        /// Returns an action that moves towards an enemy as fast as possible.
        /// </summary>
        public static UctAction FastMoveTowardsEnemy(GameInstance state, CachedMob mob, CachedMob target) {
            var pathfinder = state.Pathfinder;

            var moveTarget = pathfinder.FurthestPointToTarget(mob, target);

            if (moveTarget != null && pathfinder.Distance(mob.MobInstance.Coord, moveTarget.Value) <=
                mob.MobInstance.Ap) {
                return UctAction.MoveAction(mob.MobId, moveTarget.Value);
            } else if (moveTarget == null) {
                // Intentionally doing nothing
                return UctAction.EndTurnAction();
            } else {
                Utils.Log(LogSeverity.Debug, nameof(AiRuleBasedController),
                          $"Move failed since target is too close, source {mob.MobInstance.Coord}, target {target.MobInstance.Coord}");
                return UctAction.EndTurnAction();
            }
        }

        /// <summary>
        /// Generates a number of defensive move actions based on a heatmap.
        /// </summary>
        public static void GenerateDefensiveMoveActions(GameInstance state, CachedMob mob, List<UctAction> result) {
            var heatmap = Heatmap.BuildHeatmap(state, null, false);
            var coords = new List<AxialCoord>();

            var mobInstance = mob.MobInstance;
            var mobId = mob.MobId;

            foreach (var coord in heatmap.Map.AllCoords) {
                if (heatmap.Map[coord] != heatmap.MinValue) continue;
                if (state.Map[coord] == HexType.Wall) continue;
                if (state.State.AtCoord(coord, true).HasValue) continue;

                bool canMoveTo = state.Pathfinder.Distance(mobInstance.Coord, coord) <= mobInstance.Ap;

                if (!canMoveTo) continue;

                coords.Add(coord);
            }

            coords.Shuffle();

            int maximumMoveActions = Math.Max(0, 3 - result.Count);
            for (int i = 0; i < Math.Min(coords.Count, maximumMoveActions); i++) {
                var action = UctAction.DefensiveMoveAction(mobId, coords[i]);
                GameInvariants.AssertValidAction(state, action);

                result.Add(action);
            }
        }

        /// <summary>
        /// Generates possible attack move actions.
        /// </summary>
        public static void GenerateAttackMoveActions(GameInstance state, CachedMob mob, List<UctAction> result) {
            var mobInfo = mob.MobInfo;
            var mobInstance = mob.MobInstance;

            foreach (var enemyId in state.MobManager.Mobs) {
                var target = state.CachedMob(enemyId);

                if (!GameInvariants.IsTargetableNoSource(state, mob, target)) continue;

                AxialCoord myCoord = mobInstance.Coord;
                AxialCoord? closestCoord = null;
                int? distance = null;
                int? chosenAbilityId = null;

                foreach (var coord in state.Map.EmptyCoords) {
                    if (!state.Map.IsVisible(coord, target.MobInstance.Coord)) continue;

                    var possibleMoveAction = GameInvariants.CanMoveTo(state, mob, coord);

                    if (possibleMoveAction.Type == UctActionType.Null) continue;
                    Debug.Assert(possibleMoveAction.Type == UctActionType.Move);

                    foreach (var abilityId in mobInfo.Abilities) {
                        if (!GameInvariants.IsAbilityUsableFrom(state, mob, coord, target, abilityId)) continue;

                        int myDistance = state.Pathfinder.Distance(myCoord, coord);

                        if (!closestCoord.HasValue) {
                            chosenAbilityId = abilityId;
                            closestCoord = coord;
                            distance = myDistance;
                        } else if (distance.Value > myDistance) {
                            chosenAbilityId = abilityId;
                            closestCoord = coord;
                            distance = myDistance;
                        }
                    }
                }

                if (closestCoord.HasValue) {
                    if (Constants.AttackMoveEnabled) {
                        var action = UctAction.AttackMoveAction(mob.MobId,
                                                                closestCoord.Value,
                                                                chosenAbilityId.Value,
                                                                target.MobId);

                        var after = ActionEvaluator.F(state, action.ToPureMove());
                        GameInvariants.AssertValidAbilityUseAction(after, action.ToPureAbilityUse());

                        GameInvariants.AssertValidAction(state, action);

                        result.Add(action);
                    } else {
                        var action = UctAction.MoveAction(mob.MobId, closestCoord.Value);
                        GameInvariants.AssertValidAction(state, action);

                        result.Add(action);
                    }
                }
            }
        }

        /// <summary>
        /// Generates possible direct ability use actions.
        /// </summary>
        public static bool GenerateDirectAbilityUse(GameInstance state,
                                                    CachedMob mob,
                                                    List<UctAction> result) {
            bool foundAbilityUse = false;
            var mobInfo = mob.MobInfo;
            var mobId = mob.MobId;

            foreach (var abilityId in mobInfo.Abilities) {
                if (!GameInvariants.IsAbilityUsableNoTarget(state, mobId, abilityId)) continue;

                foreach (var targetId in state.MobManager.Mobs) {
                    if (GameInvariants.IsAbilityUsable(state, mob, state.CachedMob(targetId), abilityId)) {
                        foundAbilityUse = true;

                        var action = UctAction.AbilityUseAction(abilityId, mobId, targetId);
                        GameInvariants.AssertValidAction(state, action);

                        result.Add(action);
                    }
                }
            }

            return foundAbilityUse;
        }

        /// <summary>
        /// Generates a list of possible actions, truncated for the purposes of MCTS.
        /// </summary>
        public static List<UctAction> PossibleActions(GameInstance game, UctNode parent, bool allowMove,
                                                      bool allowEndTurn) {
            var result = new List<UctAction>(10);

            var currentMob = game.CurrentMob;
            if (currentMob.HasValue) {
                var mob = game.CachedMob(currentMob.Value);

                GameInvariants.AssertMobPlayable(game, mob);

                bool foundAbilityUse = GenerateDirectAbilityUse(game, mob,
                                                                result);

                // We disable movement if there is a possibility to cast abilities.
                if (allowMove && (Constants.AlwaysAttackMove || !foundAbilityUse)) {
                    GenerateAttackMoveActions(game, game.CachedMob(mob.MobId), result);
                }

                if (allowMove) {
                    if (parent == null || parent.Action.Type != UctActionType.DefensiveMove) {
                        GenerateDefensiveMoveActions(game, mob, result);
                    }
                }
            } else {
                Utils.Log(LogSeverity.Warning, nameof(UctNode),
                          "Final state reached while trying to compute possible actions.");
                throw new InvalidOperationException();
            }

            if (allowEndTurn) {
                // We would skip end turn if there are not enough actions.
                if (!Constants.EndTurnAsLastResort || result.Count <= 1) {
                    result.Add(UctAction.EndTurnAction());
                }
            }

            GameInvariants.AssertValidActions(game, result);

            return result;
        }
    }
}