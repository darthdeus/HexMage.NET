using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class AiRandomController : IMobController, IGameEventSubscriber {
        private readonly GameInstance _gameInstance;

        public AiRandomController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public DefenseDesire FastRequestDesireToDefend(Mob mob, Ability ability) {
            return DefenseDesire.Pass;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            FastRandomAction(eventHub);
        }

        public void FastRandomAction(GameEventHub eventHub) {
            var mob = _gameInstance.TurnManager.CurrentMob;
            var pathfinder = _gameInstance.Pathfinder;

            var ability = mob.UsableMaxRange();

            Mob spellTarget = null;
            Mob moveTarget = null;
            foreach (var possibleTarget in _gameInstance.MobManager.Mobs) {
                if (possibleTarget.Hp <= 0) continue;

                // TODO - mela by to byt viditelna vzdalenost
                if (possibleTarget.Team != mob.Team) {
                    if (pathfinder.Distance(possibleTarget.Coord) <= ability.Range) {
                        spellTarget = possibleTarget;
                        break;
                    }

                    moveTarget = possibleTarget;
                }
            }

            if (spellTarget != null) {
                // TODO - tohle tu neni vubec potreba
                var usableAbility = new UsableAbility(mob, spellTarget, ability, 0);
                usableAbility.FastUse(_gameInstance.Map, _gameInstance.MobManager);
            } else if (moveTarget != null) {
                Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                          $"There are no targets, moving towards a random enemy at {moveTarget.Coord}");
                FastMoveTowardsEnemy(mob, moveTarget);
            } else {
                throw new InvalidOperationException("No targets, game should be over.");
            }
        }

        public Task<DefenseDesire> RequestDesireToDefend(Mob mob, Ability ability) {
            return Task.FromResult(DefenseDesire.Pass);
        }

        public Task<bool> PlayTurn(GameEventHub eventHub) {
            return RandomAction(eventHub);
        }

        public async Task<bool> RandomAction(GameEventHub eventHub) {
            var mob = _gameInstance.TurnManager.CurrentMob;
            Debug.Assert(mob != null, "Requesting action while there's no current mob.");
            var targets = _gameInstance.PossibleTargets(mob);

            if (targets.Count > 0) {
                var target = targets.OrderBy(t => t.Coord.Distance(mob.Coord)).First();

                var usableAbilities = _gameInstance.UsableAbilities(mob, target);
                if (usableAbilities.Count > 0) {
                    var ua = usableAbilities.First();
                    await eventHub.BroadcastAbilityUsed(mob, target, ua);
                } else {
                    Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                              $"No usable abilities, moving towards target at {target.Coord}");

                    await MoveTowardsEnemy(mob, target, eventHub);
                }
            } else {
                var enemies = _gameInstance.Enemies(mob);
                if (enemies.Count > 0) {
                    var target = enemies.First();

                    Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                              $"There are no targets, moving towards a random enemy at {target.Coord}");

                    await MoveTowardsEnemy(mob, target, eventHub);
                } else {
                    Utils.Log(LogSeverity.Info, nameof(AiRandomController), "No possible action");
                }
            }

            return true;
        }

        private void FastMoveTowardsEnemy(Mob mob, Mob target) {
            var pathfinder = _gameInstance.Pathfinder;

            var moveTarget = pathfinder.FurthestPointToTarget(mob, target);

            if (moveTarget != null && pathfinder.Distance(moveTarget.Value) <= mob.Ap) {
                _gameInstance.MobManager.FastMoveMob(_gameInstance.Map, _gameInstance.Pathfinder, mob, moveTarget.Value);
            } else {
                Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                          $"Move failed since target is too close, source {mob.Coord}, target {target.Coord}");
            }
        }

        private async Task MoveTowardsEnemy(Mob mob, Mob target, GameEventHub eventHub) {
            var pathfinder = _gameInstance.Pathfinder;

            var moveTarget = pathfinder.FurthestPointToTarget(mob, target);

            if (moveTarget != null && pathfinder.Distance(moveTarget.Value) <= mob.Ap) {
                await eventHub.BroadcastMobMoved(mob, moveTarget.Value);
            } else {
                Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                          $"Move failed since target is too close, source {mob.Coord}, target {target.Coord}");
            }
        }

        public string Name => nameof(AiRandomController);

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