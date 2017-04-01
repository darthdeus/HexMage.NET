using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HexMage.Simulator.AI {
    public class AiRuleBasedController : IMobController {
        private readonly GameInstance _gameInstance;

        public AiRuleBasedController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public static UctAction MaxAbilityRatio(GameInstance game, List<UctAction> actions) {
            UctAction max = actions[0];
            var maxAbilityInfo = game.MobManager.Abilities[max.AbilityId];

            for (int i = 1; i < actions.Count; i++) {
                var abilityInfo = game.MobManager.Abilities[actions[i].AbilityId];

                if (abilityInfo.DmgCostRatio > maxAbilityInfo.DmgCostRatio) {
                    max = actions[i];
                    maxAbilityInfo = abilityInfo;
                }
            }

            return max;
        }

        public static UctAction GenerateAction(GameInstance game) {
            const bool fastActionGeneration = false;

            if (fastActionGeneration) return ActionGenerator.DefaultPolicyAction(game);

            var result = new List<UctAction>();

            var currentMob = game.TurnManager.CurrentMob;
            if (!currentMob.HasValue) return UctAction.EndTurnAction();

            var mob = game.CachedMob(currentMob.Value);
            ActionGenerator.GenerateDirectAbilityUse(game, mob, result);

            if (result.Count > 0) {
                return MaxAbilityRatio(game, result);
            }

            ActionGenerator.GenerateAttackMoveActions(game, mob, result);

            if (result.Count > 0) {
                return MaxAbilityRatio(game, result);
            }

            ActionGenerator.GenerateDefensiveMoveActions(game, mob, result);

            if (result.Count > 0) {
                return result[0];
            }

            return UctAction.EndTurnAction();
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            do {
                var action = GenerateAction(_gameInstance);

                if (action.Type == UctActionType.EndTurn)
                    break;

                UctAlgorithm.FNoCopy(_gameInstance, action);
            } while (!_gameInstance.IsFinished);
        }

        public async Task SlowPlayTurn(GameEventHub eventHub) {
            do {
                var action = GenerateAction(_gameInstance);

                if (action.Type == UctActionType.EndTurn)
                    break;

                await eventHub.SlowPlayAction(_gameInstance, action);
            } while (!_gameInstance.IsFinished);
        }

        public string Name => nameof(AiRuleBasedController);

        public override string ToString() {
            return "AI";
        }
    }
}