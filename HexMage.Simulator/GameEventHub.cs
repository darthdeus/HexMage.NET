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

        public int FastMainLoop(TimeSpan turnDelay) {
            var turnManager = _gameInstance.TurnManager;
            turnManager.StartNextTurn(_gameInstance.Pathfinder);

            _gameInstance.SlowUpdateIsFinished();

            Utils.Log(LogSeverity.Info, nameof(GameEventHub), "FAST Starting Main Loop");

            int totalTurns = 0;

            //while (!_gameInstance.SlowIsFinished()) {
            while (!_gameInstance.IsFinished) {
                Utils.Log(LogSeverity.Info, nameof(GameEventHub), "FAST Main loop iterations");
                totalTurns++;

                turnManager.CurrentController.FastPlayTurn(this);
                turnManager.NextMobOrNewTurn(_gameInstance.Pathfinder);

                if (turnDelay != TimeSpan.Zero) {
                    Thread.Sleep(turnDelay);
                }
            }

            Utils.Log(LogSeverity.Info, nameof(GameEventHub), "FAST Main loop DONE");

            return totalTurns;
        }


        public void AddSubscriber(IGameEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
        }

        public void BroadcastMobMoved(Mob mob, AxialCoord pos) {
            foreach (var subscriber in _subscribers) {
                subscriber.EventMobMoved(mob, pos);
            }

            _gameInstance.MobManager.FastMoveMob(_gameInstance.Map, _gameInstance.Pathfinder, mob, pos);
        }

        public void BroadcastAbilityUsed(Mob mob, Mob target, AbilityId abilityId) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);
            foreach (var subscriber in _subscribers) {
                subscriber.EventAbilityUsed(mob, target, ability);
            }

            var defenseDesireResult = _gameInstance.FastUse(abilityId, mob, target);

            BroadcastDefenseDesire(target, defenseDesireResult);
        }

        public void BroadcastAbilityUsedWithDefense(Mob mob, Mob target, AbilityId abilityId,
            DefenseDesire defenseDesire) {
            var ability = _gameInstance.MobManager.AbilityForId(abilityId);

            foreach (var subscriber in _subscribers) {
                subscriber.EventAbilityUsed(mob, target, ability);
            }

            _gameInstance.FastUseWithDefenseDesire(mob, target, abilityId, defenseDesire);

            BroadcastDefenseDesire(target, defenseDesire);
        }

        public void BroadcastDefenseDesire(Mob mob, DefenseDesire defenseDesireResult) {
            foreach (var subscriber in _subscribers) {
                subscriber.EventDefenseDesireAcquired(mob, defenseDesireResult);
            }
        }
    }
}