using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace HexMage.Simulator.AI
{
    public class GameInstanceEvaluator
    {
        private readonly GameInstance _gameInstance;

        public GameInstanceEvaluator(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public EvaluationResult Evaluate() {
            var factories = new IAiFactory[] {
                new RuleBasedFactory(),
                new MctsFactory(8),
                new MctsFactory(20)
            };

            foreach (var factory1 in factories) {
                foreach (var factory2 in factories) {
                    var game = _gameInstance.DeepCopy();
                    var hub = new GameEventHub(game);

                    var ai1 = factory1.Build(game);
                    var ai2 = factory2.Build(game);

                    int iterations = 500;

                    while (!game.IsFinished && iterations-- > 0) {
                        game.TurnManager.CurrentController.FastPlayTurn(hub);
                        game.TurnManager.NextMobOrNewTurn(game.Pathfinder, game.State);
                    }

                    if (game.IsFinished) {
                        Debug.Assert(game.VictoryTeam.HasValue);
                        Debug.Assert(game.VictoryController != null);

                        Console.WriteLine($"Won {game.VictoryTeam.Value} - {game.VictoryController}");
                    } else {
                        Console.WriteLine("DRAW");
                    }
                }
            }

            // TODO - figure out a format for the evaluation result
            return new EvaluationResult();
        }
    }

    public class RuleBasedFactory : IAiFactory {
        public IMobController Build(GameInstance gameInstance) {
            // TODO - rename to RuleBasedController
            return new AiRandomController(gameInstance);
        }
    }

    public class MctsFactory : IAiFactory {
        private readonly int _time;

        public MctsFactory(int time) {
            _time = time;
        }

        public IMobController Build(GameInstance gameInstance) {
            // TODO - actually set the time property of MCTS
            return new MctsController(gameInstance);
        }
    }

    public interface IAiFactory {
        IMobController Build(GameInstance gameInstance);
    }

    public struct EvaluationResult {
        
    }
}
