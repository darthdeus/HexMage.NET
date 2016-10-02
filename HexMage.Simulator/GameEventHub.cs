using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            Utils.Log(LogSeverity.Info, nameof(GameEventHub), "FAST Starting Main Loop");

            int totalTurns = 0;

            while (!_gameInstance.IsFinished()) {
                totalTurns++;

                turnManager.CurrentController.FastPlayTurn(this);
                turnManager.NextMobOrNewTurn(_gameInstance.Pathfinder);
            }

            return totalTurns;
        }

        public async Task<int> MainLoop(TimeSpan turnDelay) {
            var turnManager = _gameInstance.TurnManager;
            turnManager.StartNextTurn(_gameInstance.Pathfinder);

            Utils.Log(LogSeverity.Info, nameof(GameEventHub), "Starting Main Loop");

            int totalTurns = 0;

            while (!_gameInstance.IsFinished()) {
                totalTurns++;
                Utils.Log(LogSeverity.Info, nameof(GameEventHub), "Main Loop Iteration");

                await turnManager.CurrentController.PlayTurn(this);

                turnManager.NextMobOrNewTurn(_gameInstance.Pathfinder);

                while (IsPaused) {
                    await Task.Delay(_pauseDelay);
                }

                if (turnDelay != TimeSpan.Zero) {
                    await Task.Delay(turnDelay);
                }
            }

            Utils.Log(LogSeverity.Info, nameof(GameEventHub), "Main Loop DONE");

            return totalTurns;
        }

        public void AddSubscriber(IGameEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
        }

        public async Task BroadcastMobMoved(Mob mob, AxialCoord pos) {
            await Task.WhenAll(_subscribers.Select(x => x.EventMobMoved(mob, pos)));
            _gameInstance.MobManager.FastMoveMob(_gameInstance.Map, _gameInstance.Pathfinder, mob, pos);
        }

        public async Task BroadcastAbilityUsed(Mob mob, Mob target, UsableAbility ability) {
            await Task.WhenAll(_subscribers.Select(x => x.EventAbilityUsed(mob, target, ability)));

            var defenseDesireResult = await ability.Use(_gameInstance.Map, _gameInstance.MobManager);

            await BroadcastDefenseDesire(target, defenseDesireResult);
        }

        public async Task BroadcastAbilityUsedWithDefense(Mob mob, Mob target, UsableAbility ability,
                                                          DefenseDesire defenseDesire) {
            await Task.WhenAll(_subscribers.Select(x => x.EventAbilityUsed(mob, target, ability)));

            ability.UseWithDefenseResult(_gameInstance.Map, defenseDesire);

            await BroadcastDefenseDesire(target, defenseDesire);
        }

        public async Task BroadcastDefenseDesire(Mob mob, DefenseDesire defenseDesireResult) {
            await Task.WhenAll(_subscribers.Select(x => x.EventDefenseDesireAcquired(mob, defenseDesireResult)));
        }
    }
}