using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class AiRandomController : IMobController {
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
#warning TODO - tohle je extremne spatne
                _gameInstance.FastUse(ref ability, mob, spellTarget);
            } else if (moveTarget != null) {
                Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                          $"There are no targets, moving towards a random enemy at {moveTarget.Coord}");
                FastMoveTowardsEnemy(mob, moveTarget);
            } else {
                throw new InvalidOperationException("No targets, game should be over.");
            }
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
    }
}