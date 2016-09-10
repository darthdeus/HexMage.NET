using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HexMage.Simulator;

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
            }
            Utils.ThreadLog("[EventHub] Main Loop DONE");

            return true;
        }

        public void AddSubscriber(IGameEventSubscriber subscriber) {
            _subscribers.Add(subscriber);
        }

        public async Task BroadcastMobMoved(Mob mob, AxialCoord coord) {
            await Task.WhenAll(_subscribers.Select(x => x.EventMobMoved(mob, coord)));

#warning TODO - assert
            currentMob.Ap -= distance;
            currentMob.Coord = pos;
            _gameInstance.Pathfinder.PathfindFrom(pos);
        }

        public async Task BoardcastAbilityUsed(Mob mob, Mob target, UsableAbility ability) {
            Utils.ThreadLog($"[EventHub] waiting for {_subscribers.Count} subscribers");
            await Task.WhenAll(_subscribers.Select(x => x.EventAbilityUsed(mob, target, ability)));

            await ability.Use(_gameInstance.Map);
        }
    }

    public interface IGameEventSubscriber {
        Task<bool> EventAbilityUsed(Mob mob, Mob target, UsableAbility ability);
        Task<bool> EventMobMoved(Mob mob, AxialCoord pos);
    }


    public class AiRandomController : IMobController, IGameEventSubscriber {
        private readonly GameInstance _gameInstance;

        public AiRandomController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public Task<DefenseDesire> RequestDesireToDefend(Mob mob, Ability ability) {
            return Task.FromResult(DefenseDesire.Pass);
        }

        public Task<bool> PlayTurn(GameEventHub eventHub) {
            return RandomAction(eventHub);
        }

        public async Task<bool> RandomAction(GameEventHub eventHub) {
            var mob = _gameInstance.TurnManager.CurrentMob;
            var targets = _gameInstance.PossibleTargets(mob);
            var pathfinder = _gameInstance.Pathfinder;

            if (targets.Count > 0) {
                var target = targets.OrderBy(t => t.Coord.Distance(mob.Coord)).First();

                var usableAbilities = _gameInstance.UsableAbilities(mob, target);
                if (usableAbilities.Count > 0) {
                    var ua = usableAbilities.First();

                    Utils.ThreadLog("Broadcasting used ability");
                    await eventHub.BoardcastAbilityUsed(mob, target, ua);

                    Utils.ThreadLog("Using ability");
                    await ua.Use(_gameInstance.Map);
                } else {
                    var path = pathfinder.PathTo(target.Coord);
                    pathfinder.MoveAsFarAsPossible(mob, path);
                }
            } else {
                var enemies = _gameInstance.Enemies(mob);
                if (enemies.Count > 0) {
                    var path = pathfinder.PathTo(enemies.First().Coord);
                    pathfinder.MoveAsFarAsPossible(mob, path);
                } else {
                    // Do nothing
#warning FIX ME
                    Console.WriteLine("No possible action");
                }
            }

            throw new NotImplementedException();
        }

        public Task<bool> EventAbilityUsed(Mob mob, Mob target, UsableAbility ability) {
            throw new NotImplementedException();
            return Task<bool>.FromResult(true);
        }

        public Task<bool> EventMobMoved(Mob mob, AxialCoord pos) {
            throw new NotImplementedException();
            return Task<bool>.FromResult(true);
        }
    }
}