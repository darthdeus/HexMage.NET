using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class GameEventHub {
        private readonly GameInstance _gameInstance;
        private readonly List<IGameEventSubscriber> _subscribers = new List<IGameEventSubscriber>();

        public GameEventHub(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public async Task<bool> MainLoop() {
            var turnManager = _gameInstance.TurnManager;

            turnManager.StartNextTurn(_gameInstance.Pathfinder);
            
            Utils.Log(LogSeverity.Info, nameof(GameEventHub), "Starting Main Loop");
            while (!_gameInstance.IsFinished()) {
                Utils.Log(LogSeverity.Info, nameof(GameEventHub), "Main Loop Iteration");
                var action = turnManager.CurrentMob.Team.Controller.PlayTurn(this);
                await action;

                turnManager.NextMobOrNewTurn(_gameInstance.Pathfinder);
            }
            Utils.Log(LogSeverity.Info, nameof(GameEventHub), "Main Loop DONE");

            return true;
        }

        public void AddSubscriber(IGameEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
        }

        public async Task BroadcastMobMoved(Mob mob, AxialCoord pos) {
            await Task.WhenAll(_subscribers.Select(x => x.EventMobMoved(mob, pos)));

            int distance = mob.Coord.ModifiedDistance(mob, pos);

            Debug.Assert(distance <= mob.Ap, "Trying to move a mob that doesn't have enough AP.");
            Debug.Assert(_gameInstance.Map[pos] == HexType.Empty, "Trying to move a mob into a wall.");
            mob.Ap -= distance;
            mob.Coord = pos;
            _gameInstance.Pathfinder.PathfindFrom(pos);
        }

        public async Task BroadcastAbilityUsed(Mob mob, Mob target, UsableAbility ability) {
            Utils.Log(LogSeverity.Info, nameof(GameEventHub), $"waiting for {_subscribers.Count} subscribers");
            await Task.WhenAll(_subscribers.Select(x => x.EventAbilityUsed(mob, target, ability)));

            await ability.Use(_gameInstance.Map);
        }
    }
}