using System;
using System.Collections.Generic;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class FlatMonteCarlo {
        public UctAction Run(GameInstance initialState) {
            var actions = UctAlgorithm.PossibleActions(initialState);

            float max = float.MinValue;
            UctAction bestAction = UctAction.EndTurnAction();

            foreach (var action in actions) {
                var copy = UctAlgorithm.F(initialState, action);

                const int iterations = 1000;

                float sum = 0;

                for (int i = 0; i < iterations; i++) {
                    float result = UctAlgorithm.DefaultPolicy(copy);
                    sum += result;
                }

                if (sum > max) {
                    max = sum;
                    bestAction = action;
                }
            }

            return bestAction;
        }

        public UctAction Run(GameInstance initialState, int xxdontuse) {
            var possibleStates = new List<GameInstance>();
            if (!initialState.TurnManager.CurrentMob.HasValue) {
                throw new NotImplementedException();
            }

            var mobId = initialState.TurnManager.CurrentMob.Value;
            var mobInfo = initialState.MobManager.MobInfos[mobId];
            var mobInstance = initialState.State.MobInstances[mobId];

            {
                var pathfinder = initialState.Pathfinder;

                int moveTarget = MobInstance.InvalidId;
                MobInstance moveTargetInstance = new MobInstance();

                foreach (var possibleTargetId in initialState.MobManager.Mobs) {
                    var possibleTargetInstance = initialState.State.MobInstances[possibleTargetId];

                    if (possibleTargetInstance.Hp <= 0) continue;

                    foreach (var abilityId in mobInfo.Abilities) {
                        if (initialState.IsAbilityUsable(mobId, abilityId, possibleTargetId)) {
                            var stateWithUsedAbility = initialState.DeepCopy();

                            stateWithUsedAbility.FastUse(abilityId, mobId, possibleTargetId);
                            //stateWithUsedAbility.FastUse(mobId, possibleTargetId, abilityId);

                            possibleStates.Add(stateWithUsedAbility);
                        }
                    }

                    if (moveTarget == MobInstance.InvalidId) {
                        moveTarget = possibleTargetId;
                        moveTargetInstance = possibleTargetInstance;
                    }

                    if (pathfinder.Distance(mobInstance.Coord, moveTargetInstance.Coord) >
                        pathfinder.Distance(mobInstance.Coord, possibleTargetInstance.Coord)) {
                        moveTarget = possibleTargetId;
                        moveTargetInstance = possibleTargetInstance;
                    }
                }

                var moveForwardCopy = initialState.DeepCopy();
                FastMoveTowardsEnemy(moveForwardCopy, mobId, moveTarget);

                possibleStates.Add(moveForwardCopy);
            }

            foreach (var state in possibleStates) {
                var hub = new GameEventHub(state);
                state.MobManager.Teams[TeamColor.Red] = new AiRandomController(state);
                state.MobManager.Teams[TeamColor.Blue] = new AiRandomController(state);

                var rounds = hub.FastMainLoop(TimeSpan.Zero);
                Console.WriteLine($"Took {rounds} rounds");
            }

            return UctAction.NullAction();
        }

        private void FastMoveTowardsEnemy(GameInstance state, int mobId, int targetId) {
            var pathfinder = state.Pathfinder;
            var mobInstance = state.State.MobInstances[mobId];
            var targetInstance = state.State.MobInstances[targetId];

            var moveTarget = pathfinder.FurthestPointToTarget(mobInstance, targetInstance);

            if (moveTarget != null && pathfinder.Distance(mobInstance.Coord, moveTarget.Value) <= mobInstance.Ap) {
                state.State.FastMoveMob(state.Map, state.Pathfinder, mobId,
                                        moveTarget.Value);
            } else if (moveTarget == null) {
                // do nothing intentionally
            } else {
                Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                          $"Move failed since target is too close, source {mobInstance.Coord}, target {targetInstance.Coord}");
            }
        }
    }
}