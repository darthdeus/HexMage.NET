using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HexMage.Simulator {
    public class MctsController : IMobController {
        public static bool EnableLogging = true;
        private readonly GameInstance _gameInstance;
        private readonly int _thinkTime;

        public MctsController(GameInstance gameInstance, int thinkTime = 10) {
            _gameInstance = gameInstance;
            _thinkTime = thinkTime;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            var uct = new UctAlgorithm(_thinkTime);
            var result = uct.UctSearch(_gameInstance);

            foreach (var action in result.Actions) {
                Debug.Assert(action.Type != UctActionType.EndTurn, "node.Action.Type != UctActionType.EndTurn");

                UctAlgorithm.FNoCopy(_gameInstance, action);
            }

            float endRatio = (float) UctAlgorithm.ActionCounts[UctActionType.EndTurn] /
                             UctAlgorithm.ActionCounts[UctActionType.AbilityUse];

            ExponentialMovingAverage.Instance.Average(result.MillisecondsPerIteration);

            if (EnableLogging) {
                Console.WriteLine($"*** MCTS SPEED: {result.MillisecondsPerIteration}ms/iter***");

                foreach (var action in result.Actions) {
                    Console.WriteLine($"action: {action}");
                }                

                Console.WriteLine(
                    $"total: {UctAlgorithm.actions} [end ratio: {endRatio}]\t{UctAlgorithm.ActionCountString()}");
            }
        }

        public async Task SlowPlayTurn(GameEventHub eventHub) {
            var result = await Task.Run(() => new UctAlgorithm(_thinkTime).UctSearch(_gameInstance));

            foreach (var action in result.Actions) {
                Debug.Assert(action.Type != UctActionType.EndTurn, "node.Action.Type != UctActionType.EndTurn");

                await eventHub.SlowPlayAction(_gameInstance, action);
            }

            //UctAction action;
            //do {
            //    var node = await Task.Run(() => new UctAlgorithm(_thinkTime).UctSearch(_gameInstance));
            //    action = node.Action;

            //    await eventHub.SlowPlayAction(_gameInstance, action);
            //} while (action.Type != UctActionType.EndTurn);
        }

        public string Name => "MctsController";

        public override string ToString() {
            return $"MCTS#{_thinkTime}";
        }
    }
}