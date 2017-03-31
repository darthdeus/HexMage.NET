using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;

namespace HexMage.Simulator {
    public static class ActionGenerator {
        public static void Shuffle<T>(IList<T> list) {
            // TODO - rewrite this to be better
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = Generator.Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static UctAction DefaultPolicyAction(GameInstance state) {
            var mobId = state.TurnManager.CurrentMob;

            if (mobId == null)
                throw new InvalidOperationException("Requesting mob action when there is no current mob.");

            var mob = state.CachedMob(mobId.Value);

            var mobInfo = mob.MobInfo;

            int? abilityId = null;
            Ability ability = null;
            foreach (var possibleAbilityId in mobInfo.Abilities) {
                if (state.IsAbilityUsableNoTarget(mobId.Value, possibleAbilityId)) {
                    abilityId = possibleAbilityId;
                }
            }

            int spellTarget = MobInstance.InvalidId;
            int moveTargetId = MobInstance.InvalidId;

            foreach (var possibleTargetId in state.MobManager.Mobs) {
                var possibleTarget = state.CachedMob(possibleTargetId);

                const bool allowCorpseTargetting = false;
                // TODO - porovnat, co kdyz dovolim utocit na dead cile
                if (!allowCorpseTargetting && !state.IsTargetable(mob, possibleTarget)) continue;

                moveTargetId = possibleTargetId;

                if (!abilityId.HasValue) continue;

                if (state.IsAbilityUsableApRangeCheck(mob, possibleTarget, abilityId.Value)) {
                    spellTarget = possibleTargetId;
                    break;
                }
            }

            if (spellTarget != MobInstance.InvalidId) {
                Debug.Assert(abilityId.HasValue);
                return UctAction.AbilityUseAction(abilityId.Value, mobId.Value, spellTarget);
            } else if (moveTargetId != MobInstance.InvalidId) {
                var action = FastMoveTowardsEnemy(state, mobId.Value, moveTargetId);

                if (action.Type == UctActionType.Null) {
                    return UctAction.EndTurnAction();
                } else {
                    return action;
                }
            } else {
                throw new InvalidOperationException("No targets, game should be over.");
            }
        }

        public static UctAction FastMoveTowardsEnemy(GameInstance state, int mobId, int targetId) {
            var pathfinder = state.Pathfinder;
            var mobInstance = state.State.MobInstances[mobId];
            var targetInstance = state.State.MobInstances[targetId];

            var moveTarget = pathfinder.FurthestPointToTarget(mobInstance, targetInstance);

            if (moveTarget != null && pathfinder.Distance(mobInstance.Coord, moveTarget.Value) <= mobInstance.Ap) {
                return UctAction.MoveAction(mobId, moveTarget.Value);
            } else if (moveTarget == null) {
                // TODO - intentionally doing nothing
                return UctAction.EndTurnAction();
            } else {
                Console.WriteLine("Move failed!");

                Utils.Log(LogSeverity.Debug, nameof(AiRuleBasedController),
                          $"Move failed since target is too close, source {mobInstance.Coord}, target {targetInstance.Coord}");
                return UctAction.EndTurnAction();
            }
        }


        public static void GenerateDefensiveMoveActions(GameInstance state, MobInstance mobInstance, int mobId,
                                                        List<UctAction> result) {
            var heatmap = state.BuildHeatmap();
            var coords = new List<AxialCoord>();

            foreach (var coord in heatmap.Map.AllCoords) {
                if (heatmap.Map[coord] != heatmap.MinValue) continue;
                if (state.Map[coord] == HexType.Wall) continue;
                if (state.State.AtCoord(coord).HasValue) continue;

                bool canMoveTo = state.Pathfinder.Distance(mobInstance.Coord, coord) <= mobInstance.Ap;

                if (!canMoveTo) continue;

                // TODO - samplovat po sektorech
                coords.Add(coord);

                // TODO - tohle je asi overkill, ale nemame lepsi zpusob jak iterovat
                //int value = heatmap.Map[coord];

                //if (usedValues.Contains(value)) continue;

                //usedValues.Add(value);
                //result.Add(UctAction.MoveAction(mobId, coord));
            }

            Shuffle(coords);
            for (int i = 0; i < Math.Min(coords.Count, 3); i++) {
                result.Add(UctAction.DefensiveMoveAction(mobId, coords[i]));
            }
        }

        public static void GenerateAttackMoveActions(GameInstance state, MobInstance mobInstance, int mobId,
                                                     List<UctAction> result) {
            var mobInfo = state.MobManager.MobInfos[mobId];
            var enemyDistances = new HexMap<int>(state.Size);

            // TODO - preferovat blizsi policka pri vyberu akci?
            foreach (var enemyId in state.MobManager.Mobs) {
                MobInstance enemyInstance = state.State.MobInstances[enemyId];
                // TODO - zkontrolovat vsude, ze netargetuju dead opponenty :)
                //if (enemyInstance.Hp <= 0) continue;

                AxialCoord myCoord = mobInstance.Coord;
                AxialCoord? closestCoord = null;
                int? distance = null;
                int? chosenAbilityId = null;
                int? targetId = null;

                foreach (var coord in enemyDistances.AllCoords) {
                    if (state.Map[coord] == HexType.Wall) continue;
                    if (!state.Map.IsVisible(coord, enemyInstance.Coord)) continue;
                    if (state.State.AtCoord(coord).HasValue) continue;

                    int remainingAp = mobInstance.Ap - state.Pathfinder.Distance(mobInstance.Coord, coord);

                    foreach (var abilityId in mobInfo.Abilities) {
                        var ability = state.MobManager.Abilities[abilityId];
                        bool withinRange = ability.Range >= coord.Distance(enemyInstance.Coord);
                        bool enoughAp = remainingAp >= ability.Cost;

                        if (withinRange && enoughAp) {
                            int myDistance = state.Pathfinder.Distance(myCoord, coord);

                            chosenAbilityId = abilityId;
                            targetId = enemyId;

                            if (!closestCoord.HasValue) {
                                closestCoord = coord;
                                distance = myDistance;
                            } else if (distance.Value > myDistance) {
                                closestCoord = coord;
                                distance = myDistance;
                            }
                        }
                    }
                }

                if (closestCoord.HasValue) {
                    const bool attackMoveEnabled = true;
                    if (attackMoveEnabled) {
                        result.Add(UctAction.AttackMoveAction(mobId,
                                                              closestCoord.Value,
                                                              chosenAbilityId.Value,
                                                              targetId.Value));
                    } else {
                        result.Add(UctAction.MoveAction(mobId, closestCoord.Value));
                    }
                }
            }

            //    //// TODO - properly define max actions
            //    int count = 2;
            //    foreach (var coord in state.Map.AllCoords) {
            //        if (coord == mobInstance.Coord) continue;

            //        if (state.Pathfinder.Distance(mobInstance.Coord, coord) <= mobInstance.Ap) {
            //            if (state.State.AtCoord(coord) == null && count-- > 0) {
            //                moveActions.Add(UctAction.MoveAction(mobId, coord));
            //                //result.Add(UctAction.MoveAction(mobId, coord));
            //            }
            //        }
            //    }

            //    if (count == 0) {
            //        //Console.WriteLine("More than 100 possible move actions.");
            //    }

            //    Shuffle(moveActions);

            //    result.AddRange(moveActions.Take(20));
        }

        public static bool GenerateDirectAbilityUse(GameInstance state,
                                                    int mobId,
                                                    MobInfo mobInfo,
                                                    MobInstance mobInstance,
                                                    List<UctAction> result) {
            bool foundAbilityUse = false;

            foreach (var abilityId in mobInfo.Abilities) {
                var abilityInfo = state.MobManager.Abilities[abilityId];

                // Skip abilities which are on cooldown
                if (state.State.Cooldowns[abilityId] > 0) continue;

                if (abilityInfo.Cost <= mobInstance.Ap) {
                    foreach (var targetId in state.MobManager.Mobs) {
                        var targetInfo = state.MobManager.MobInfos[targetId];
                        var targetInstance = state.State.MobInstances[targetId];
                        int enemyDistance = state.Map.AxialDistance(mobInstance.Coord, targetInstance.Coord);

                        // TODO - nahradit za isAbilityUsable
                        bool isVisible = state.Map.IsVisible(mobInstance.Coord, targetInstance.Coord);
                        bool isEnemy = targetInfo.Team != mobInfo.Team;
                        bool withinRange = enemyDistance <= abilityInfo.Range;
                        bool targetAlive = targetInstance.Hp > 0;

                        if (isEnemy && withinRange && targetAlive && isVisible) {
                            foundAbilityUse = true;
                            result.Add(UctAction.AbilityUseAction(abilityId, mobId, targetId));
                        }
                    }
                }
            }

            return foundAbilityUse;
        }


        public static List<UctAction> PossibleActions(GameInstance state, bool allowMove, bool allowEndTurn) {
            // TODO - zmerit poradne, jestli tohle vubec pomaha, a kolik to ma byt
            var result = new List<UctAction>(10);

            var currentMob = state.TurnManager.CurrentMob;
            if (currentMob.HasValue) {
                var mobId = currentMob.Value;

                var mobInstance = state.State.MobInstances[mobId];
                var mobInfo = state.MobManager.MobInfos[mobId];

                bool foundAbilityUse = ActionGenerator.GenerateDirectAbilityUse(state, mobId, mobInfo, mobInstance,
                                                                                result);

                const bool alwaysAttackMove = false;

                // We disable movement if there is a possibility to cast abilities.
                if (allowMove && (alwaysAttackMove || !foundAbilityUse)) {
                    ActionGenerator.GenerateAttackMoveActions(state, mobInstance, mobId, result);
                }

                if (allowMove) {
                    ActionGenerator.GenerateDefensiveMoveActions(state, mobInstance, mobId, result);
                }
            } else {
                throw new InvalidOperationException();
                Utils.Log(LogSeverity.Warning, nameof(UctNode),
                          "Final state reached while trying to compute possible actions.");
            }

            const bool endTurnAsLastResort = true;

            if (allowEndTurn) {
                // We would skip end turn if there are not enough actions.
                // TODO - generate more move actions if we don't have enough?
                if (!endTurnAsLastResort || result.Count <= 1) {
                    result.Add(UctAction.EndTurnAction());
                }
            }

            return result;
        }
    }
}