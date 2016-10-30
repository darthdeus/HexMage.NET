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
            turnManager.StartNextTurn(_gameInstance.Pathfinder);

            int totalTurns = 0;
            _gameInstance.SlowUpdateIsFinished();

            while (!_gameInstance.IsFinished) {
                totalTurns++;

                await turnManager.CurrentController.SlowPlayTurn(this);
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
                turnManager.NextMobOrNewTurn(_gameInstance.Pathfinder);
            }

            return totalTurns;
        }

        public int FastMainLoop(TimeSpan turnDelay) {
            var turnManager = _gameInstance.TurnManager;
            turnManager.StartNextTurn(_gameInstance.Pathfinder);

            int totalTurns = 0;
            _gameInstance.SlowUpdateIsFinished();

            while (!_gameInstance.IsFinished) {
                totalTurns++;

                turnManager.CurrentController.FastPlayTurn(this);
                turnManager.NextMobOrNewTurn(_gameInstance.Pathfinder);

                if (turnDelay != TimeSpan.Zero) {
                    Thread.Sleep(turnDelay);
                }

                //_gameInstance.SlowUpdateIsFinished();
            }

            return totalTurns;
        }


        public void AddSubscriber(IGameEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
        }

        public async Task SlowBroadcastMobMoved(int mob, AxialCoord pos) {
            await Task.WhenAll(_subscribers.Select(x => x.SlowEventMobMoved(mob, pos)));

            _gameInstance.MobManager.FastMoveMob(_gameInstance.Map, _gameInstance.Pathfinder, mob, pos);
        }

        public void FastBroadcastMobMoved(int mob, AxialCoord pos) {
            foreach (var subscriber in _subscribers) {
                subscriber.EventMobMoved(mob, pos);
            }

            _gameInstance.MobManager.FastMoveMob(_gameInstance.Map, _gameInstance.Pathfinder, mob, pos);
        }

        public async Task SlowBroadcastAbilityUsed(int mobId, int targetId, int abilityId) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);
            await Task.WhenAll(_subscribers.Select(x => x.SlowEventAbilityUsed(mobId, targetId, ability)));

            var defenseDesireResult = await _gameInstance.SlowUse(abilityId, mobId, targetId);

            BroadcastDefenseDesire(targetId, defenseDesireResult);
        }

        public void FastBroadcastAbilityUsed(int mobId, int targetId, int abilityId) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);
            foreach (var subscriber in _subscribers) {
                subscriber.EventAbilityUsed(mobId, targetId, ability);
            }

            var defenseDesireResult = _gameInstance.FastUse(abilityId, mobId, targetId);

            BroadcastDefenseDesire(targetId, defenseDesireResult);
        }

        public void BroadcastAbilityUsedWithDefense(int mob, int target, int abilityId,
                                                    DefenseDesire defenseDesire) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);

            foreach (var subscriber in _subscribers) {
                subscriber.EventAbilityUsed(mob, target, ability);
            }

            _gameInstance.FastUseWithDefenseDesire(mob, target, abilityId, defenseDesire);

            BroadcastDefenseDesire(target, defenseDesire);
        }

        public void BroadcastDefenseDesire(int mob, DefenseDesire defenseDesireResult) {
            foreach (var subscriber in _subscribers) {
                subscriber.EventDefenseDesireAcquired(mob, defenseDesireResult);
            }
        }
    }
}