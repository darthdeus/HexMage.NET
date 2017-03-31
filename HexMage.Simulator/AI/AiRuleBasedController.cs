using System.Threading.Tasks;

namespace HexMage.Simulator.AI {
    public class AiRuleBasedController : IMobController {
        private readonly GameInstance _gameInstance;

        public AiRuleBasedController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            do {
                var action = ActionGenerator.DefaultPolicyAction(_gameInstance);

                if (action.Type == UctActionType.EndTurn)
                    break;

                UctAlgorithm.FNoCopy(_gameInstance, action);
            } while (!_gameInstance.IsFinished);
        }

        public async Task SlowPlayTurn(GameEventHub eventHub) {
            do {
                var action = ActionGenerator.DefaultPolicyAction(_gameInstance);

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