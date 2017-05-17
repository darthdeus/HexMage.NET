using System;
using System.Threading.Tasks;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;

namespace HexMage.Simulator.AI {
    /// <summary>
    /// Implementation of the random AI.
    /// </summary>
    public class AiRandomController : IMobController {
        private readonly GameInstance _gameInstance;

        public AiRandomController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            do {
                var possibleActions = ActionGenerator.PossibleActions(_gameInstance, null, true, true);
                var chosenAction = possibleActions[Generator.Random.Next(possibleActions.Count)];

                if (chosenAction.Type == UctActionType.EndTurn)
                    break;

                ActionEvaluator.FNoCopy(_gameInstance, chosenAction);
            } while (!_gameInstance.IsFinished);
        }

        public async Task SlowPlayTurn(GameEventHub eventHub) {
            do {
                var possibleActions = ActionGenerator.PossibleActions(_gameInstance, null, true, true);
                var chosenAction = possibleActions[Generator.Random.Next(possibleActions.Count)];

                if (chosenAction.Type == UctActionType.EndTurn)
                    break;

                await eventHub.SlowPlayAction(_gameInstance, chosenAction);
            } while (!_gameInstance.IsFinished);
        }


        public string Name => nameof(AiRandomController);

        public override string ToString() {
            return "RND";
        }

        public static IMobController Build(GameInstance game) {
            return new AiRandomController(game);
        }
    }
}