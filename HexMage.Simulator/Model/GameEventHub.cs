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
            foreach (var action in actions) {
                Console.WriteLine($"Replaying {action}");
                await SlowPlayAction(_gameInstance, action);
            }

            return actions.Count;
        }

        public async Task<int> SlowMainLoop(TimeSpan turnDelay) {
            State = GameEventState.SettingUpTurn;

            var turnManager = _gameInstance.TurnManager;
            var state = _gameInstance.State;
            turnManager.StartNextTurn(_gameInstance.Pathfinder, state);

            int totalTurns = 0;
            state.SlowUpdateIsFinished(_gameInstance.MobManager);
            

            while (!_gameInstance.IsFinished) {
                totalTurns++;

                await turnManager.CurrentController.SlowPlayTurn(this);

                // TODO - try to use this to find some more race conditions :)
                // Delay used to find random race conditions
                //await Task.Delay(TimeSpan.FromMilliseconds(1000));
                UctAlgorithm.FNoCopy(_gameInstance, UctAction.EndTurnAction());
            }

            ReplayRecorder.Instance.SaveAndClear(_gameInstance);
            Console.WriteLine(Constants.GetLogBuffer());

            return totalTurns;
        }

        public async Task SlowPlayAction(GameInstance game, UctAction action) {
            // TODDO - game vs _gameInstance?
            Debug.Assert(game == _gameInstance, "instance == _gameInstance");

            switch (action.Type) {
                case UctActionType.AbilityUse:
                    await SlowBroadcastAbilityUsed(action.MobId, action.TargetId, action.AbilityId);
                    break;

                case UctActionType.EndTurn:
                    // TODO - nastavit nejakej stav?                    
                    break;

                case UctActionType.AttackMove:
                    await SlowBroadcastMobMoved(action.MobId, action.Coord);
                    await SlowBroadcastAbilityUsed(action.MobId, action.TargetId, action.AbilityId);
                    break;

                case UctActionType.DefensiveMove:
                case UctActionType.Move:
                    // TODO - assert jenom na jednom miste?
                    Debug.Assert(game.State.AtCoord(action.Coord) == null, "Trying to move into a mob.");
                    await SlowBroadcastMobMoved(action.MobId, action.Coord);
                    break;

                case UctActionType.Null:
                    break;
            }
        }

        public int FastMainLoop() {
            var turnManager = _gameInstance.TurnManager;
            turnManager.StartNextTurn(_gameInstance.Pathfinder, _gameInstance.State);

            int totalTurns = 0;
            _gameInstance.State.SlowUpdateIsFinished(_gameInstance.MobManager);

            while (!_gameInstance.IsFinished) {
                totalTurns++;

                turnManager.CurrentController.FastPlayTurn(this);
                UctAlgorithm.FNoCopy(_gameInstance, UctAction.EndTurnAction());
            }

            return totalTurns;
        }


        public void AddSubscriber(IGameEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
        }

        public async Task SlowBroadcastMobMoved(int mob, AxialCoord pos) {
            await Task.WhenAll(_subscribers.Select(x => x.SlowEventMobMoved(mob, pos)));

            UctAlgorithm.FNoCopy(_gameInstance, UctAction.MoveAction(mob, pos));
        }

        public void FastBroadcastMobMoved(int mob, AxialCoord pos) {
            foreach (var subscriber in _subscribers) {
                subscriber.EventMobMoved(mob, pos);
            }

            UctAlgorithm.FNoCopy(_gameInstance, UctAction.MoveAction(mob, pos));
        }

        public async Task SlowBroadcastAbilityUsed(int mobId, int targetId, int abilityId) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);
            await Task.WhenAll(_subscribers.Select(x => x.SlowEventAbilityUsed(mobId, targetId, ability)));

            UctAlgorithm.FNoCopy(_gameInstance, UctAction.AbilityUseAction(abilityId, mobId, targetId));
        }

        public void FastBroadcastAbilityUsed(int mobId, int targetId, int abilityId) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);
            foreach (var subscriber in _subscribers) {
                subscriber.EventAbilityUsed(mobId, targetId, ability);
            }

            UctAlgorithm.FNoCopy(_gameInstance, UctAction.AbilityUseAction(abilityId, mobId, targetId));
        }
    }
}