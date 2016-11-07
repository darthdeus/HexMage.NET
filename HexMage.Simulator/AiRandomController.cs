using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public DefenseDesire FastRequestDesireToDefend(int mobId, int ability) {
            return DefenseDesire.Pass;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            FastRandomAction(eventHub);
        }

        public void FastRandomAction(GameEventHub eventHub) {
            var mobId = _gameInstance.TurnManager.CurrentMob;

            if (mobId == null)
                throw new InvalidOperationException("Requesting mob action when there is no current mob.");

            var pathfinder = _gameInstance.Pathfinder;

            var mobInfo = _gameInstance.MobManager.MobInfos[mobId.Value];
            var mobInstance = _gameInstance.State.MobInstances[mobId.Value];

            Ability ability = null;
            foreach (var possibleAbilityId in mobInfo.Abilities) {
                var possibleAbility = _gameInstance.MobManager.AbilityForId(possibleAbilityId);

                if (possibleAbility.Cost <= mobInstance.Ap &&
                    _gameInstance.State.Cooldowns[possibleAbilityId] == 0) {
                    ability = possibleAbility;
                }
            }

            int spellTarget = MobInstance.InvalidId;
            int moveTarget = MobInstance.InvalidId;

            foreach (var possibleTarget in _gameInstance.MobManager.Mobs) {
                var possibleTargetInstance = _gameInstance.State.MobInstances[possibleTarget];
                var possibleTargetInfo = _gameInstance.MobManager.MobInfos[possibleTarget];
                if (possibleTargetInstance.Hp <= 0) continue;

                // TODO - mela by to byt viditelna vzdalenost
                if (possibleTargetInfo.Team != mobInfo.Team) {
                    if (pathfinder.Distance(mobInstance.Coord, possibleTargetInstance.Coord) <= ability.Range) {
                        spellTarget = possibleTarget;
                        break;
                    }

                    moveTarget = possibleTarget;
                }
            }

            if (spellTarget != MobInstance.InvalidId) {
                _gameInstance.FastUse(ability.Id, mobId.Value, spellTarget);
            } else if (moveTarget != MobInstance.InvalidId) {
                FastMoveTowardsEnemy(mobId.Value, moveTarget);
            } else {
                throw new InvalidOperationException("No targets, game should be over.");
            }
        }

        private void FastMoveTowardsEnemy(int mobId, int targetId) {
            var pathfinder = _gameInstance.Pathfinder;
            var mobInstance = _gameInstance.State.MobInstances[mobId];
            var targetInstance = _gameInstance.State.MobInstances[targetId];

            var moveTarget = pathfinder.FurthestPointToTarget(mobInstance, targetInstance);

            if (moveTarget != null && pathfinder.Distance(mobInstance.Coord, moveTarget.Value) <= mobInstance.Ap) {
                _gameInstance.State.FastMoveMob(_gameInstance.Map, _gameInstance.Pathfinder, mobId,
                                                moveTarget.Value);
            } else {
                Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                          $"Move failed since target is too close, source {mobInstance.Coord}, target {targetInstance.Coord}");
            }
        }

        public Task<DefenseDesire> SlowRequestDesireToDefend(int targetId, int abilityId) {
            return Task.FromResult(DefenseDesire.Pass);
        }

        public Task SlowPlayTurn(GameEventHub eventHub) {
            FastRandomAction(eventHub);
            return Task.CompletedTask;
        }

        public string Name => nameof(AiRandomController);
    }
}