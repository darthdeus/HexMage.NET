using System;
using System.Threading.Tasks;

namespace HexMage.Simulator {
    public class MctsController : IMobController {
        private readonly GameInstance _gameInstance;
        private readonly int _thinkTime;

        public MctsController(GameInstance gameInstance, int thinkTime = 10) {
            _gameInstance = gameInstance;
            _thinkTime = thinkTime;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            UctAction action;

            do {
                var uct = new UctAlgorithm(_thinkTime);
                var node = uct.UctSearch(_gameInstance);
                action = node.Action;

                // TODO - pokud nemam AP, tak MCTS stejne pocita dlouho, co muze udelat

                float endRatio = (float) UctAlgorithm.ActionCounts[UctActionType.EndTurn] /
                                 UctAlgorithm.ActionCounts[UctActionType.AbilityUse];

                // TODO: temporarily disabled logging
                //Console.WriteLine($"action: {node.Action}, total: {UctAlgorithm.actions} [end ratio: {endRatio}]\t{UctAlgorithm.ActionCountString()}");
                
                UctAlgorithm.FNoCopy(_gameInstance, action);
            } while (action.Type != UctActionType.EndTurn);
        }

        public async Task SlowPlayTurn(GameEventHub eventHub) {
            UctAction action;
            do {
                var node = await Task.Run(() => new UctAlgorithm(_thinkTime).UctSearch(_gameInstance));
                action = node.Action;

                await eventHub.SlowPlayAction(_gameInstance, action);
            } while (action.Type != UctActionType.EndTurn);
        }

        public string Name => "MctsController";

        public override string ToString() {
            return $"MCTS[{_thinkTime}]";
        }
    }
}