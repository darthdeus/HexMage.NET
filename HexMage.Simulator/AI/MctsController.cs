using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class MctsController : IMobController {
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

                ActionEvaluator.FNoCopy(_gameInstance, action);
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
#warning TODO - FUUUUUUUUUUUJ tohle sem nepatri
            // TODO - FUUUUUUUUUUUJ tohle sem nepatri
            // TODO - FUUUUUUUUUUUJ tohle sem nepatri
            // TODO - FUUUUUUUUUUUJ tohle sem nepatri
            // TODO - FUUUUUUUUUUUJ tohle sem nepatri
            // TODO - FUUUUUUUUUUUJ tohle sem nepatri
            // TODO - FUUUUUUUUUUUJ tohle sem nepatri
            // TODO - FUUUUUUUUUUUJ tohle sem nepatri
            // TODO - FUUUUUUUUUUUJ tohle sem nepatri
            // TODO - FUUUUUUUUUUUJ tohle sem nepatri
            // TODO - FUUUUUUUUUUUJ tohle sem nepatri
            float abilityUse = ActionEvaluator.ActionCounts[UctActionType.AbilityUse];
            float attackMove = ActionEvaluator.ActionCounts[UctActionType.AttackMove];
            float defensiveMove = ActionEvaluator.ActionCounts[UctActionType.DefensiveMove];
            float move = ActionEvaluator.ActionCounts[UctActionType.Move];
            float endTurn = ActionEvaluator.ActionCounts[UctActionType.EndTurn];

            float useCounts = abilityUse + attackMove + defensiveMove + move;
            string endRatio = (endTurn / useCounts).ToString("0.00");
            string abilityMoveRatio = ((abilityUse + attackMove) / (move + defensiveMove)).ToString("0.00");

            if (Constants.MctsLogging) {
                //Console.WriteLine($"*** MCTS SPEED: {result.MillisecondsPerIteration}ms/iter***");

                foreach (var action in result.Actions) {
                    Console.WriteLine($"action: {action}");
                }

                Console.WriteLine(
                    $"#sum: {ActionEvaluator.Actions}[ability/move ratio: {abilityMoveRatio}] [end ratio: {endRatio}] {ActionEvaluator.ActionCountString()}");
            }
        }


        public string Name => "MctsController";

        public override string ToString() {
            return $"MCTS#{_thinkTime}";
        }

        public static IMobController Build(GameInstance game, int thinkTime) {
            return new MctsController(game, thinkTime);
        }
    }
}