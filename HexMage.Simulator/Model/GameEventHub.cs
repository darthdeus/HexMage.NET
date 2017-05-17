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

    /// <summary>
    /// Encapsulates the logic of the main game loop. Subscribers can subscribe themselves
    /// to the game loop to receive notifications of the actions in progress.
    /// </summary>
    public class GameEventHub {
        private readonly GameInstance _gameInstance;
        private readonly List<IGameEventSubscriber> _subscribers = new List<IGameEventSubscriber>();
        public bool IsPaused { get; set; } = false;

        private TimeSpan _pauseDelay = TimeSpan.FromMilliseconds(200);

        public GameEventState State { get; set; } = GameEventState.NotStarted;

        public GameEventHub(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public async Task<int> PlayReplay(List<UctAction> actions, Func<bool> endTurnFunc) {
            // Wait for GUI initialization
            await Task.Delay(TimeSpan.FromSeconds(1));

            using (new TemporarilySuspendReplayRecording()) {
                foreach (var action in actions) {
                    while (IsPaused) {
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }

                    if (action.Type == UctActionType.EndTurn) {
                        ActionEvaluator.FNoCopy(_gameInstance, action);
                        endTurnFunc();
                        await Task.Delay(200);
                    } else {
                        await SlowPlayAction(_gameInstance, action);
                    }
                    Console.WriteLine($"Replaying {action}");
                }
            }

            return actions.Count;
        }

        public async Task<int> SlowMainLoop(Func<bool> turnEndFunc, Action gameFinishedFunc) {
            // Wait for GUI initialization
            await Task.Delay(TimeSpan.FromSeconds(1));

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

                ActionEvaluator.FNoCopy(_gameInstance, UctAction.EndTurnAction());

                turnEndFunc();
                await Task.Delay(200);
            }

            ReplayRecorder.Instance.SaveAndClear(_gameInstance);
            Console.WriteLine(Constants.GetLogBuffer());

            gameFinishedFunc();

            return totalTurns;
        }
        
        /// <summary>
        /// Asnychronously run a given action, notifying all of the subscribers.
        /// </summary>
        public async Task SlowPlayAction(GameInstance game, UctAction action) {
            Debug.Assert(game == _gameInstance, "instance == _gameInstance");

            foreach (var subscriber in _subscribers) {
                subscriber.ActionApplied(action);
            }

            switch (action.Type) {
                case UctActionType.AbilityUse:
                    await SlowBroadcastAction(action);
                    break;

                case UctActionType.EndTurn:
                    break;

                case UctActionType.AttackMove:
                    GameInvariants.AssertValidMoveAction(_gameInstance, action);

                    await SlowBroadcastAction(action.ToPureMove());
                    await SlowBroadcastAction(action.ToPureAbilityUse());
                    break;

                case UctActionType.DefensiveMove:
                case UctActionType.Move:
                    GameInvariants.AssertValidMoveAction(_gameInstance, action);

                    await SlowBroadcastAction(action.ToPureMove());
                    break;

                case UctActionType.Null:
                    break;
            }
        }

        public void FastPlayAction(UctAction action) {
            ActionEvaluator.FNoCopy(_gameInstance, action);
        }

        public void AddSubscriber(IGameEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
        }

        private async Task SlowBroadcastAction(UctAction action) {
            await Task.WhenAll(_subscribers.Select(x => x.SlowActionApplied(action)));

            ActionEvaluator.FNoCopy(_gameInstance, action);
        }
    }
}