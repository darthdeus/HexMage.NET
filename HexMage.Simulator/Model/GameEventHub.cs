using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HexMage.Simulator.AI;

namespace HexMage.Simulator.Model {
    public enum GameEventState {
        NotStarted,
        TurnInProgress,
        SettingUpTurn
    }

    public class GameEventHub {
        private readonly GameInstance _gameInstance;
        private readonly List<IGameEventSubscriber> _subscribers = new List<IGameEventSubscriber>();
        public bool IsPaused { get; set; } = false;

        private TimeSpan _pauseDelay = TimeSpan.FromMilliseconds(200);

        public GameEventState State { get; set; } = GameEventState.NotStarted;

        public GameEventHub(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public async Task<int> PlayReplay(List<UctAction> actions) {
            using (new TemporarilySuspendReplayRecording()) {
                foreach (var action in actions) {
                    while (IsPaused) {
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }

                    if (action.Type == UctActionType.EndTurn) {
                        ActionEvaluator.FNoCopy(_gameInstance, action);
                    } else {
                        await SlowPlayAction(_gameInstance, action);
                    }
                    Console.WriteLine($"Replaying {action}");
                }
            }

            return actions.Count;
        }

        public async Task<int> SlowMainLoop(TimeSpan turnDelay) {
            State = GameEventState.SettingUpTurn;

            var state = _gameInstance.State;

            _gameInstance.Reset();

            int totalTurns = 0;
            state.SlowUpdateIsFinished(_gameInstance.MobManager);

            while (!_gameInstance.IsFinished) {
                while (IsPaused) {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }

                totalTurns++;

                await _gameInstance.CurrentController.SlowPlayTurn(this);

                // TODO - try to use this to find some more race conditions :)
                // Delay used to find random race conditions
                //await Task.Delay(TimeSpan.FromMilliseconds(1000));
                ActionEvaluator.FNoCopy(_gameInstance, UctAction.EndTurnAction());
            }

            ReplayRecorder.Instance.SaveAndClear(_gameInstance);
            Console.WriteLine(Constants.GetLogBuffer());

            return totalTurns;
        }

        public async Task SlowPlayAction(GameInstance game, UctAction action) {
            // TODDO - game vs _gameInstance?
            Debug.Assert(game == _gameInstance, "instance == _gameInstance");

            foreach (var subscriber in _subscribers) {
                subscriber.ActionApplied(action);
            }

            switch (action.Type) {
                case UctActionType.AbilityUse:
                    await SlowBroadcastAbilityUsed(action.MobId, action.TargetId, action.AbilityId);
                    break;

                case UctActionType.EndTurn:
                    // TODO - nastavit nejakej stav?                    
                    break;

                case UctActionType.AttackMove:
                    // TODO - tohle se deje uz jinde
                    GameInvariants.AssertValidMoveAction(_gameInstance, action);

                    await SlowBroadcastMobMoved(action.MobId, action.Coord);
                    await SlowBroadcastAbilityUsed(action.MobId, action.TargetId, action.AbilityId);
                    break;

                case UctActionType.DefensiveMove:
                case UctActionType.Move:
                    // TODO - tohle se deje uz jinde
                    GameInvariants.AssertValidMoveAction(_gameInstance, action);

                    await SlowBroadcastMobMoved(action.MobId, action.Coord);
                    break;

                case UctActionType.Null:
                    break;
            }
        }

        public int FastMainLoop() {
            var turnManager = _gameInstance.TurnManager;

            throw new NotImplementedException();
            _gameInstance.Reset();
            //turnManager.StartNextTurn(_gameInstance.Pathfinder, _gameInstance.State);

            int totalTurns = 0;
            _gameInstance.State.SlowUpdateIsFinished(_gameInstance.MobManager);

            while (!_gameInstance.IsFinished) {
                totalTurns++;

                _gameInstance.CurrentController.FastPlayTurn(this);
                ActionEvaluator.FNoCopy(_gameInstance, UctAction.EndTurnAction());
            }

            return totalTurns;
        }


        public void AddSubscriber(IGameEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
        }

        public async Task SlowBroadcastMobMoved(int mob, AxialCoord pos) {
            await Task.WhenAll(_subscribers.Select(x => x.SlowEventMobMoved(mob, pos)));

            ActionEvaluator.FNoCopy(_gameInstance, UctAction.MoveAction(mob, pos));
        }

        public async Task SlowBroadcastAbilityUsed(int mobId, int targetId, int abilityId) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);
            await Task.WhenAll(_subscribers.Select(x => x.SlowEventAbilityUsed(mobId, targetId, ability)));

            ActionEvaluator.FNoCopy(_gameInstance, UctAction.AbilityUseAction(abilityId, mobId, targetId));
        }
    }
}