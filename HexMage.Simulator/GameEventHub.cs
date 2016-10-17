using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class GameEventHub {
        private readonly GameInstance _gameInstance;
        private readonly List<IGameEventSubscriber> _subscribers = new List<IGameEventSubscriber>();
        public bool IsPaused { get; set; } = false;

        public GameEventHub(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        private TimeSpan _pauseDelay = TimeSpan.FromMilliseconds(200);

        public async Task<int> SlowMainLoop(TimeSpan turnDelay) {
            var turnManager = _gameInstance.TurnManager;
            turnManager.StartNextTurn(_gameInstance.Pathfinder);

            int totalTurns = 0;
            _gameInstance.SlowUpdateIsFinished();

            while (!_gameInstance.IsFinished) {
                totalTurns++;

                await turnManager.CurrentController.SlowPlayTurn(this);
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

        public async Task SlowBroadcastMobMoved(MobId mob, AxialCoord pos) {
            await Task.WhenAll(_subscribers.Select(x => x.SlowEventMobMoved(mob, pos)));

            _gameInstance.MobManager.FastMoveMob(_gameInstance.Map, _gameInstance.Pathfinder, mob, pos);
        }

        public void FastBroadcastMobMoved(MobId mob, AxialCoord pos) {
            foreach (var subscriber in _subscribers) {
                subscriber.EventMobMoved(mob, pos);
            }

            _gameInstance.MobManager.FastMoveMob(_gameInstance.Map, _gameInstance.Pathfinder, mob, pos);
        }

        public async Task SlowBroadcastAbilityUsed(MobId mobId, MobId targetId, AbilityId abilityId) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);
            await Task.WhenAll(_subscribers.Select(x => x.SlowEventAbilityUsed(mobId, targetId, ability)));

            var defenseDesireResult = await _gameInstance.SlowUse(abilityId, mobId, targetId);

            BroadcastDefenseDesire(targetId, defenseDesireResult);
        }

        public void FastBroadcastAbilityUsed(MobId mobId, MobId targetId, AbilityId abilityId) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);
            foreach (var subscriber in _subscribers) {
                subscriber.EventAbilityUsed(mobId, targetId, ability);
            }

            var defenseDesireResult = _gameInstance.FastUse(abilityId, mobId, targetId);

            BroadcastDefenseDesire(targetId, defenseDesireResult);
        }

        public void BroadcastAbilityUsedWithDefense(MobId mob, MobId target, AbilityId abilityId,
                                                    DefenseDesire defenseDesire) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);

            foreach (var subscriber in _subscribers) {
                subscriber.EventAbilityUsed(mob, target, ability);
            }

            _gameInstance.FastUseWithDefenseDesire(mob, target, abilityId, defenseDesire);

            BroadcastDefenseDesire(target, defenseDesire);
        }

        public void BroadcastDefenseDesire(MobId mob, DefenseDesire defenseDesireResult) {
            foreach (var subscriber in _subscribers) {
                subscriber.EventDefenseDesireAcquired(mob, defenseDesireResult);
            }
        }
    }
}