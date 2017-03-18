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
            UctAction action;
            do {
                action = UctAlgorithm.DefaultPolicyAction(_gameInstance);

                if (action.Type == UctActionType.EndTurn) {
                    break;
                }

                UctAlgorithm.FNoCopy(_gameInstance, action);
            } while (!_gameInstance.IsFinished);            
        }

        public async Task SlowPlayTurn(GameEventHub eventHub) {
            UctAction action;

            do {
                action = UctAlgorithm.DefaultPolicyAction(_gameInstance);

                if (action.Type == UctActionType.EndTurn) {
                    break;
                }

                await eventHub.SlowPlayAction(_gameInstance, action);
            } while (!_gameInstance.IsFinished);
        }

        public string Name => nameof(AiRandomController);

        public override string ToString() {
            return "AI_Rule";
        }
    }
}