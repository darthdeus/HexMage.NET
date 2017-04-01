using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class MctsController : IMobController {
        public static bool EnableLogging = true;
        private readonly GameInstance _gameInstance;
        private readonly int _thinkTime;

        public MctsController(GameInstance gameInstance, int thinkTime = 100) {
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

            ExponentialMovingAverage.Instance.Average(result.MillisecondsPerIteration);

            LogActions(result);
        }

        public async Task SlowPlayTurn(GameEventHub eventHub) {
            var result = await Task.Run(() => new UctAlgorithm(_thinkTime).UctSearch(_gameInstance));

            foreach (var action in result.Actions) {
                Debug.Assert(action.Type != UctActionType.EndTurn, "node.Action.Type != UctActionType.EndTurn");

                await eventHub.SlowPlayAction(_gameInstance, action);
            }

            LogActions(result);
        }

        private void LogActions(UctSearchResult result) {
            float endRatio = (float) UctAlgorithm.ActionCounts[UctActionType.EndTurn] /
                             UctAlgorithm.ActionCounts[UctActionType.AbilityUse];
            if (EnableLogging) {
                Console.WriteLine($"*** MCTS SPEED: {result.MillisecondsPerIteration}ms/iter***");

                foreach (var action in result.Actions) {
                    Console.WriteLine($"action: {action}");
                }

                Console.WriteLine(
                    $"total: {UctAlgorithm.Actions} [end ratio: {endRatio}]\t{UctAlgorithm.ActionCountString()}");
            }
        }


        public string Name => "MctsController";

        public override string ToString() {
            return $"MCTS#{_thinkTime}";
        }
    }
}