using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HexMage.Simulator {
    public class GameEventHub {
        private readonly GameInstance _gameInstance;
        private readonly List<IGameEventSubscriber> _subscribers = new List<IGameEventSubscriber>();

        public GameEventHub(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public async Task<bool> MainLoop() {
            var turnManager = _gameInstance.TurnManager;

            Utils.ThreadLog("[EventHub] Starting Main Loop");
            while (!_gameInstance.IsFinished()) {
                Utils.ThreadLog("[EventHub] Main Loop Iteration");
                var action = turnManager.CurrentMob.Team.Controller.PlayTurn(this);
                await action;

                turnManager.NextMobOrNewTurn(_gameInstance.Pathfinder);
            }
            Utils.ThreadLog("[EventHub] Main Loop DONE");

            return true;
        }

        public void AddSubscriber(IGameEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
        }

        public async Task BroadcastMobMoved(Mob mob, AxialCoord pos) {
            await Task.WhenAll(_subscribers.Select(x => x.EventMobMoved(mob, pos)));

            int distance = mob.Coord.ModifiedDistance(mob, pos);

            Debug.Assert(distance <= mob.Ap, "Trying to move a mob that doesn't have enough AP.");
            mob.Ap -= distance;
            mob.Coord = pos;
            _gameInstance.Pathfinder.PathfindFrom(pos);
        }

        public async Task BoardcastAbilityUsed(Mob mob, Mob target, UsableAbility ability) {
            Utils.ThreadLog($"[EventHub] waiting for {_subscribers.Count} subscribers");
            await Task.WhenAll(_subscribers.Select(x => x.EventAbilityUsed(mob, target, ability)));

            await ability.Use(_gameInstance.Map);
        }
    }
}