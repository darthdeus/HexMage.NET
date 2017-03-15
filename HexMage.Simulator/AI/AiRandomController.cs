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

            switch (action.Type) {
                case UctActionType.AbilityUse:
                    await eventHub.SlowBroadcastAbilityUsed(action.MobId, action.TargetId, action.AbilityId);
                    break;

                case UctActionType.EndTurn:
                    // TODO - nastavit nejakej stav?                    
                    break;

                case UctActionType.Move:
                    await eventHub.SlowBroadcastMobMoved(action.MobId, action.Coord);
                    break;

                case UctActionType.Null:
                    break;
            }

            UctAlgorithm.FNoCopy(_gameInstance, action);
            //FastPlayTurn(eventHub);
        }

        public string Name => nameof(AiRandomController);

        public override string ToString() {
            return "AI_Rule";
        }
    }
}