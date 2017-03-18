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
            var uct = new UctAlgorithm(_thinkTime);
            var node = uct.UctSearch(_gameInstance);

            float endRatio = (float) UctAlgorithm.ActionCounts[UctActionType.EndTurn] /
                             UctAlgorithm.ActionCounts[UctActionType.AbilityUse];

            // TODO: temporarily disabled logging
            //Console.WriteLine($"action: {node.Action}, total: {UctAlgorithm.actions} [end ratio: {endRatio}]\t{UctAlgorithm.ActionCountString()}");

            // TODO - hrat vic akci za kolo
            UctAlgorithm.FNoCopy(_gameInstance, node.Action);
        }

        public async Task SlowPlayTurn(GameEventHub eventHub) {
            // TODO - ujistit se, ze continuation bezi na spravnym threadu?
            var node = await Task.Run(() => new UctAlgorithm(_thinkTime).UctSearch(_gameInstance));
            var action = node.Action;

            await eventHub.SlowPlayAction(_gameInstance, action);
        }

        public string Name => "MctsController";

        public override string ToString() {
            return $"MCTS[{_thinkTime}]";
        }
    }
}