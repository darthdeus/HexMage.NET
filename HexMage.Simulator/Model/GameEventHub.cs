using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
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

                // Delay used to find random race conditions
                //await Task.Delay(TimeSpan.FromMilliseconds(1000));
                turnManager.NextMobOrNewTurn(_gameInstance.Pathfinder, state);
            }

            return totalTurns;
        }

        public int FastMainLoop(TimeSpan turnDelay) {
            var turnManager = _gameInstance.TurnManager;
            turnManager.StartNextTurn(_gameInstance.Pathfinder, _gameInstance.State);

            int totalTurns = 0;
            _gameInstance.State.SlowUpdateIsFinished(_gameInstance.MobManager);

            while (!_gameInstance.IsFinished) {
                totalTurns++;

                turnManager.CurrentController.FastPlayTurn(this);
                turnManager.NextMobOrNewTurn(_gameInstance.Pathfinder, _gameInstance.State);
            }

            return totalTurns;
        }


        public void AddSubscriber(IGameEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
        }

        public async Task SlowBroadcastMobMoved(int mob, AxialCoord pos) {
            await Task.WhenAll(_subscribers.Select(x => x.SlowEventMobMoved(mob, pos)));

            _gameInstance.State.FastMoveMob(_gameInstance.Map, _gameInstance.Pathfinder, mob, pos);
        }

        public void FastBroadcastMobMoved(int mob, AxialCoord pos) {
            foreach (var subscriber in _subscribers) {
                subscriber.EventMobMoved(mob, pos);
            }

            _gameInstance.State.FastMoveMob(_gameInstance.Map, _gameInstance.Pathfinder, mob, pos);
        }

        public async Task SlowBroadcastAbilityUsed(int mobId, int targetId, int abilityId) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);
            await Task.WhenAll(_subscribers.Select(x => x.SlowEventAbilityUsed(mobId, targetId, ability)));

            _gameInstance.FastUse(abilityId, mobId, targetId);
        }

        public void FastBroadcastAbilityUsed(int mobId, int targetId, int abilityId) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);
            foreach (var subscriber in _subscribers) {
                subscriber.EventAbilityUsed(mobId, targetId, ability);
            }

            _gameInstance.FastUse(abilityId, mobId, targetId);
        }
    }
}