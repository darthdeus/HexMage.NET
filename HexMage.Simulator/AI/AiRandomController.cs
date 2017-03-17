using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class AiRandomController : IMobController {
        private readonly GameInstance _gameInstance;

        public AiRandomController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            var action = UctAlgorithm.DefaultPolicyAction(_gameInstance);

            // TODO - is this what we want?
            UctAlgorithm.FNoCopy(_gameInstance, action);
        }

        public async Task SlowPlayTurn(GameEventHub eventHub) {
            var action = UctAlgorithm.DefaultPolicyAction(_gameInstance);

            await eventHub.SlowPlayAction(_gameInstance, action);
            //FastPlayTurn(eventHub);
        }

        public string Name => nameof(AiRandomController);

        public override string ToString() {
            return "AI_Rule";
        }
    }
}