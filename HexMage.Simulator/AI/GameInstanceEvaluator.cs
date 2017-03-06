using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    public class GameInstanceEvaluator {
        private readonly GameInstance _gameInstance;

        public GameInstanceEvaluator(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public EvaluationResult Evaluate() {
            var factories = new IAiFactory[] {
                new RuleBasedFactory(),
                new MctsFactory(10),
                new MctsFactory(1)
            };

            var result = new EvaluationResult();

            foreach (var factory1 in factories) {
                foreach (var factory2 in factories) {
                    var game = _gameInstance.DeepCopy();
                    var hub = new GameEventHub(game);

                    var ai1 = factory1.Build(game);
                    var ai2 = factory2.Build(game);

                    game.MobManager.Teams[TeamColor.Red] = ai1;
                    game.MobManager.Teams[TeamColor.Blue] = ai2;

                    const int maxIterations = 500;
                    int iterations = maxIterations;

                    while (!game.IsFinished && iterations-- > 0) {
                        game.TurnManager.CurrentController.FastPlayTurn(hub);
                        game.TurnManager.NextMobOrNewTurn(game.Pathfinder, game.State);
                    }

                    if (game.IsFinished) {
                        Debug.Assert(game.VictoryTeam.HasValue);
                        Debug.Assert(game.VictoryController != null);

                        //Console.WriteLine($"Won {game.VictoryTeam.Value} - {game.VictoryController} vs {game.LoserController} in {500 - iterations}");
                        Console.Write($"{game.VictoryController}:{game.LoserController}...{maxIterations - iterations}, ");
                        if (game.VictoryTeam == TeamColor.Red) {
                            result.RedWins++;
                        } else {
                            result.BlueWins++;
                        }
                    } else {
                        result.Draws++;
                        Console.Write("DRAW\t");
                    }

                    result.Total++;
                }
            }

            Console.WriteLine();

            return result;
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
            return new MctsController(gameInstance, _time);
        }
    }

    public interface IAiFactory {
        IMobController Build(GameInstance gameInstance);
    }

    public struct EvaluationResult {
        public int RedWins;
        public int BlueWins;
        public int Draws;
        public int Total;

        public double WinPercentage => ((double) RedWins) / (double) Total;

        public override string ToString() {
            return $"{RedWins}/{BlueWins} (draws: {Draws}), total: {Total}";
        }
    }
}