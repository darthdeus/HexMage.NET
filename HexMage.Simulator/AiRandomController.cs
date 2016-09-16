using System;
using System.Linq;
using System.Threading.Tasks;
using HexMage.Simulator;

namespace HexMage.Simulator {
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

                    Utils.Log(LogSeverity.Info, nameof(AiRandomController), "Broadcasting used ability");
                    await eventHub.BroadcastAbilityUsed(mob, target, ua);
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
                    Utils.Log(LogSeverity.Info, nameof(AiRandomController), "No possible action");
                }
            }

#warning TODO - is there any cleanup necessary?
            return true;
        }

        public Task<bool> EventAbilityUsed(Mob mob, Mob target, UsableAbility ability) {
            return Task.FromResult(true);
        }

        public Task<bool> EventMobMoved(Mob mob, AxialCoord pos) {
            return Task.FromResult(true);
        }
    }
}