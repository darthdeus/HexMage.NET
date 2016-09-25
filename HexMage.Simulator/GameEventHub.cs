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

        public async Task<bool> MainLoop() {
            var turnManager = _gameInstance.TurnManager;

            Utils.Log(LogSeverity.Info, nameof(GameEventHub), "Starting Main Loop");
            while (!_gameInstance.IsFinished()) {
                Utils.Log(LogSeverity.Info, nameof(GameEventHub), "Main Loop Iteration");
                var action = turnManager.CurrentController.PlayTurn(this);
                await action;

                turnManager.NextMobOrNewTurn(_gameInstance.Pathfinder);

                while (IsPaused) {
                    await Task.Delay(TimeSpan.FromMilliseconds(200));
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            Utils.Log(LogSeverity.Info, nameof(GameEventHub), "Main Loop DONE");

            return true;
        }

        public void AddSubscriber(IGameEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
        }

        public async Task BroadcastMobMoved(Mob mob, AxialCoord pos) {
            await Task.WhenAll(_subscribers.Select(x => x.EventMobMoved(mob, pos)));

            int distance = mob.Coord.Distance(pos);

            Debug.Assert(distance <= mob.Ap, "Trying to move a mob that doesn't have enough AP.");
            Debug.Assert(_gameInstance.Map[pos] == HexType.Empty, "Trying to move a mob into a wall.");
            Debug.Assert(_gameInstance.MobManager.AtCoord(pos) == null, "Trying to move into a mob.");

            mob.Ap -= distance;
            mob.Coord = pos;
            _gameInstance.Pathfinder.PathfindFrom(pos);
        }

        public async Task BroadcastAbilityUsed(Mob mob, Mob target, UsableAbility ability) {
            Utils.Log(LogSeverity.Info, nameof(GameEventHub), $"waiting for {_subscribers.Count} subscribers");

#warning TODO - nepredavat UsableAbility ale Ability
            await Task.WhenAll(_subscribers.Select(x => x.EventAbilityUsed(mob, target, ability)));

            var defenseDesireResult = await ability.Use(_gameInstance.Map, _gameInstance.MobManager);

            await BroadcastDefenseDesire(target, defenseDesireResult);
        }

        public async Task BroadcastAbilityUsedWithDefense(Mob mob, Mob target, UsableAbility ability,
                                                          DefenseDesire defenseDesire) {
#warning TODO - nepredavat UsableAbility ale Ability
            await Task.WhenAll(_subscribers.Select(x => x.EventAbilityUsed(mob, target, ability)));

            ability.UseWithDefenseResult(_gameInstance.Map, defenseDesire);

            await BroadcastDefenseDesire(target, defenseDesire);
        }

        public async Task BroadcastDefenseDesire(Mob mob, DefenseDesire defenseDesireResult) {
            await Task.WhenAll(_subscribers.Select(x => x.EventDefenseDesireAcquired(mob, defenseDesireResult)));
        }
    }
}