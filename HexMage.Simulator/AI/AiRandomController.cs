using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator
{
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

        public Task SlowPlayTurn(GameEventHub eventHub) {
            FastPlayTurn(eventHub);
            return Task.CompletedTask;
        }

        public string Name => nameof(AiRandomController);

        public override string ToString() {
            return "AI_Rule";
        }
    }
}