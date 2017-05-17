using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    /// <summary>
    /// Implementation of the Rule Based AI.
    /// </summary>
    public class AiRuleBasedController : IMobController {
        private readonly GameInstance _gameInstance;

        public AiRuleBasedController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            do {
                var action = ActionGenerator.RuleBasedAction(_gameInstance);

                if (action.Type == UctActionType.EndTurn)
                    break;

                ActionEvaluator.FNoCopy(_gameInstance, action);

                if (action.Type == UctActionType.DefensiveMove) break;
            } while (!_gameInstance.IsFinished);
        }

        public async Task SlowPlayTurn(GameEventHub eventHub) {
            do {
                var action = ActionGenerator.RuleBasedAction(_gameInstance);

                if (action.Type == UctActionType.EndTurn)
                    break;

                await eventHub.SlowPlayAction(_gameInstance, action);

                if (action.Type == UctActionType.DefensiveMove) break;
            } while (!_gameInstance.IsFinished);
        }

        public string Name => nameof(AiRuleBasedController);

        public override string ToString() {
            return "Rule";
        }

        public static IMobController Build(GameInstance game) {
            return new AiRuleBasedController(game);
        }
    }
}