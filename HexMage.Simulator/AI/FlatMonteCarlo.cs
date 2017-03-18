using System;
using System.Collections.Generic;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class FlatMonteCarlo {
        public UctAction Run(GameInstance initialState) {
            var actions = UctAlgorithm.PossibleActions(initialState, true, true);

            float max = float.MinValue;
            UctAction bestAction = UctAction.EndTurnAction();

            foreach (var action in actions) {
                var copy = UctAlgorithm.F(initialState, action);

                const int iterations = 1000;

                float sum = 0;

                for (int i = 0; i < iterations; i++) {
                    float result = UctAlgorithm.DefaultPolicy(copy, initialState.CurrentTeam.Value);
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
                var moveAction = UctAlgorithm.FastMoveTowardsEnemy(moveForwardCopy, mobId, moveTarget);
                UctAlgorithm.FNoCopy(moveForwardCopy, moveAction);

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
    }
}