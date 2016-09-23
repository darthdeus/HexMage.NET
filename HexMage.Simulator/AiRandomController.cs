using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using HexMage.Simulator;
using HexMage.Simulator.Model;

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
                    await eventHub.BroadcastAbilityUsed(mob, target, ua);
                } else {
                    var moveTarget = pathfinder.FurthestPointToTarget(mob, target);

                    if (pathfinder.Distance(moveTarget) > 0) {
                        await eventHub.BroadcastMobMoved(mob, moveTarget);
                    } else {
                        Utils.Log(LogSeverity.Debug, nameof(AiRandomController), "Move failed since target is too close");
                    }
                }
            } else {
                var enemies = _gameInstance.Enemies(mob);
                if (enemies.Count > 0) {
                    var target = enemies.First();
                    var moveTarget = pathfinder.FurthestPointToTarget(mob, target);

                    if (pathfinder.Distance(moveTarget) > 0) {
                        await eventHub.BroadcastMobMoved(mob, moveTarget);
                    } else {
                        Utils.Log(LogSeverity.Debug, nameof(AiRandomController), "Move failed since target is too close");
                    }
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

        public Task<bool> EventDefenseDesireAcquired(Mob mob, DefenseDesire defenseDesireResult) {
            return Task.FromResult(true);
        }
    }
}