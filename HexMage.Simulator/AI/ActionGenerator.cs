using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public static class ActionGenerator {
        public static UctAction MaxAbilityRatio(GameInstance game, List<UctAction> actions) {
#warning TODO: vyzkouset tohle s i bez methodimpl aggressiveinlining
            //return actions.FastMax(action => game.MobManager.Abilities[action.AbilityId].DmgCostRatio);

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

        public static UctAction RuleBasedAction(GameInstance game) {
            if (Constants.FastActionGeneration) return ActionGenerator.DefaultPolicyAction(game);

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

        public static UctAction DefaultPolicyAction(GameInstance state) {
            var mobId = state.CurrentMob;

            // TODO - shortcut pokud nemam zadny AP, tak rovnou end turn :)
            if (mobId == null)
                throw new InvalidOperationException("Requesting mob action when there is no current mob.");

            Debug.Assert(state.State.MobInstances[mobId.Value].Hp > 0, "Current mob is dead");

            var mob = state.CachedMob(mobId.Value);

            if (mob.MobInstance.Ap == 0) return UctAction.EndTurnAction();

            var mobInfo = mob.MobInfo;

            int? abilityId = null;
            AbilityInfo abilityInfo = null;
            foreach (var possibleAbilityId in mobInfo.Abilities) {
                if (GameInvariants.IsAbilityUsableNoTarget(state, mobId.Value, possibleAbilityId)) {
                    abilityId = possibleAbilityId;
                }
            }

            int spellTarget = MobInstance.InvalidId;
            int moveTargetId = MobInstance.InvalidId;

            foreach (var possibleTargetId in state.MobManager.Mobs) {
                var possibleTarget = state.CachedMob(possibleTargetId);

                moveTargetId = possibleTargetId;

                // TODO - remove corpse targetting
                if (!Constants.AllowCorpseTargetting &&
                    !GameInvariants.IsTargetable(state, mob, possibleTarget)) continue;
                if (!abilityId.HasValue) continue;

                if (GameInvariants.IsAbilityUsableApRangeCheck(state, mob, possibleTarget, abilityId.Value)) {
                    spellTarget = possibleTargetId;
                    break;
                }
            }

            if (spellTarget != MobInstance.InvalidId) {
                Debug.Assert(abilityId.HasValue);
                return UctAction.AbilityUseAction(abilityId.Value, mobId.Value, spellTarget);
            } else if (moveTargetId != MobInstance.InvalidId) {
                return PickMoveTowardsEnemyAction(state, state.CachedMob(mobId.Value),
                                                  state.CachedMob(moveTargetId));
            } else {
                throw new InvalidOperationException("No targets, game should be over.");
            }
        }

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

        public static UctAction FastMoveTowardsEnemy(GameInstance state, CachedMob mob, CachedMob target) {
            var pathfinder = state.Pathfinder;

            var moveTarget = pathfinder.FurthestPointToTarget(mob, target);

            if (moveTarget != null && pathfinder.Distance(mob.MobInstance.Coord, moveTarget.Value) <=
                mob.MobInstance.Ap) {
                return UctAction.MoveAction(mob.MobId, moveTarget.Value);
            } else if (moveTarget == null) {
                //Console.WriteLine("Move target is null");
                // TODO - intentionally doing nothing
                return UctAction.EndTurnAction();
            } else {
                //Console.WriteLine("Move failed!");

                Utils.Log(LogSeverity.Debug, nameof(AiRuleBasedController),
                          $"Move failed since target is too close, source {mob.MobInstance.Coord}, target {target.MobInstance.Coord}");
                return UctAction.EndTurnAction();
            }
        }

        public static void GenerateDefensiveMoveActions(GameInstance state, CachedMob mob, List<UctAction> result) {
            var heatmap = Heatmap.BuildHeatmap(state);
            var coords = new List<AxialCoord>();

            var mobInstance = mob.MobInstance;
            var mobId = mob.MobId;

            foreach (var coord in heatmap.Map.AllCoords) {
                if (heatmap.Map[coord] != heatmap.MinValue) continue;
                if (state.Map[coord] == HexType.Wall) continue;
                if (state.State.AtCoord(coord, true).HasValue) continue;

                bool canMoveTo = state.Pathfinder.Distance(mobInstance.Coord, coord) <= mobInstance.Ap;

                if (!canMoveTo) continue;

                // TODO - samplovat po sektorech
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

        public static void GenerateAttackMoveActions(GameInstance state, CachedMob mob, List<UctAction> result) {
            var mobInfo = mob.MobInfo;
            var mobInstance = mob.MobInstance;

            // TODO - preferovat blizsi policka pri vyberu akci?
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

        public static List<UctAction> PossibleActions(GameInstance state, UctNode parent, bool allowMove,
                                                      bool allowEndTurn) {
            // TODO - zmerit poradne, jestli tohle vubec pomaha, a kolik to ma byt
            var result = new List<UctAction>(10);

            var currentMob = state.CurrentMob;
            if (currentMob.HasValue) {
                var mob = state.CachedMob(currentMob.Value);

                bool foundAbilityUse = GenerateDirectAbilityUse(state, mob,
                                                                result);

                // We disable movement if there is a possibility to cast abilities.
                if (allowMove && (Constants.AlwaysAttackMove || !foundAbilityUse)) {
                    GenerateAttackMoveActions(state, state.CachedMob(mob.MobId), result);
                }

                if (allowMove) {
                    if (parent == null || parent.Action.Type != UctActionType.DefensiveMove) {
                        GenerateDefensiveMoveActions(state, mob, result);
                    }
                }
            } else {
                throw new InvalidOperationException();
                Utils.Log(LogSeverity.Warning, nameof(UctNode),
                          "Final state reached while trying to compute possible actions.");
            }

            if (allowEndTurn) {
                // We would skip end turn if there are not enough actions.
                // TODO - generate more move actions if we don't have enough?
                if (!Constants.EndTurnAsLastResort || result.Count <= 1) {
                    result.Add(UctAction.EndTurnAction());
                }
            }

            GameInvariants.AssertValidActions(state, result);

            return result;
        }
    }
}