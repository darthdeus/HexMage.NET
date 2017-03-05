using System;
using System.Threading.Tasks;

namespace HexMage.Simulator {
    public class MctsController : IMobController {
        private readonly GameInstance _gameInstance;

        public MctsController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            var uct = new UctAlgorithm();
            var node = uct.UctSearch(_gameInstance);

            float endRatio = (float) UctAlgorithm.ActionCounts[UctActionType.EndTurn] /
                             UctAlgorithm.ActionCounts[UctActionType.AbilityUse];

            // TODO: temporarily disabled logging
            //Console.WriteLine($"action: {node.Action}, total: {UctAlgorithm.actions} [end ratio: {endRatio}]\t{UctAlgorithm.ActionCountString()}");

            switch (node.Action.Type) {
                case UctActionType.AbilityUse:
                    _gameInstance.FastUse(node.Action.AbilityId, node.Action.MobId, node.Action.TargetId);
                    break;
                case UctActionType.Move:
                    _gameInstance.FastMove(node.Action.MobId, node.Action.Coord);
                    break;
                default:
                    // TODO - check out if there is a need to explicitly end the turn
                    // intentionally doing nothing
                    break;
            }
        }

        public Task SlowPlayTurn(GameEventHub eventHub) {
            FastPlayTurn(eventHub);
            return Task.CompletedTask;
        }

        public string Name => "MctsController";

        public override string ToString() {
            return "MCTS";
        }
    }
}