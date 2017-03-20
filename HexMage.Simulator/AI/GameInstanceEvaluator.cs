using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    public class GameInstanceEvaluator {
        private readonly GameInstance _gameInstance;
        private readonly TextWriter _writer;

        public GameInstanceEvaluator(GameInstance gameInstance, TextWriter writer) {
            _gameInstance = gameInstance;
            _writer = writer;
        }

        public EvaluationResult Evaluate() {
            var factories = new IAiFactory[] {
                new RuleBasedFactory(),
                //new MctsFactory(10),
                new MctsFactory(1)
            };

            var result = new EvaluationResult();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var factory1 in factories) {
                foreach (var factory2 in factories) {
                    var game = _gameInstance.CopyStateOnly();
                    var hub = new GameEventHub(game);

                    var ai1 = factory1.Build(game);
                    var ai2 = factory2.Build(game);

                    game.MobManager.Teams[TeamColor.Red] = ai1;
                    game.MobManager.Teams[TeamColor.Blue] = ai2;

                    const int maxIterations = 100;
                    int iterations = maxIterations;

                    while (!game.IsFinished && iterations-- > 0) {
                        game.TurnManager.CurrentController.FastPlayTurn(hub);
                        game.TurnManager.NextMobOrNewTurn(game.Pathfinder, game.State);
                    }

                    result.TotalIterations += maxIterations - iterations;

                    if (game.IsFinished) {
                        Debug.Assert(game.VictoryTeam.HasValue);
                        Debug.Assert(game.VictoryController != null);

                        //Console.WriteLine($"Won {game.VictoryTeam.Value} - {game.VictoryController} vs {game.LoserController} in {500 - iterations}");
                        _writer.Write(
                            $"{game.VictoryController}:{game.LoserController}...{maxIterations - iterations}, ");
                        if (game.VictoryTeam == TeamColor.Red) {
                            result.RedWins++;
                        } else {
                            result.BlueWins++;
                        }
                    } else {
                        result.Draws++;
                        _writer.Write("DRAW\t");
                    }

                    result.TotalTurns++;
                }
            }

            stopwatch.Stop();

            result.TotalElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            _writer.WriteLine();

            return result;
        }

        private static void PreparePositions(GameInstance game, List<int> mobIds, int x, int y) {
            game.State.SetMobPosition(mobIds[0], new AxialCoord(x, y));
            game.State.SetMobPosition(mobIds[1], new AxialCoord(y, x));

            game.State.SetMobPosition(mobIds[2], new AxialCoord(-x, -y));
            game.State.SetMobPosition(mobIds[3], new AxialCoord(-y, -x));

            game.PrepareEverything();
        }

        public static List<EvaluationResult> EvaluateSetup(Setup setup, TextWriter writer) {
            const int mapSize = 10;

            var game = new GameInstance(new Map(mapSize));
            var mobIds = new List<int>();

            foreach (var mob in setup.red) {
                var ids = mob.abilities.Select(ab => game.AddAbilityWithInfo(ab.ToAbility()));
                mobIds.Add(game.AddMobWithInfo(mob.ToMobInfo(TeamColor.Red, ids)));
            }

            foreach (var mob in setup.blue) {
                var ids = mob.abilities.Select(ab => game.AddAbilityWithInfo(ab.ToAbility()));
                mobIds.Add(game.AddMobWithInfo(mob.ToMobInfo(TeamColor.Blue, ids)));
            }

            var results = new List<EvaluationResult>();

            for (int i = 0; i < mapSize; i += 3) {
                PreparePositions(game, mobIds, i, mapSize - i);

                var result = new GameInstanceEvaluator(game, writer).Evaluate();

                results.Add(result);
            }

            return results;
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
        public int TotalTurns;
        public long TotalElapsedMilliseconds;
        public int TotalIterations;

        public double MillisecondsPerIteration => (double) TotalElapsedMilliseconds / (double) TotalIterations;
        public double MillisecondsPerTurn => (double) TotalElapsedMilliseconds / (double) TotalTurns;
        public double WinPercentage => ((double) RedWins) / (double) TotalTurns;

        public override string ToString() {
            return $"{RedWins}/{BlueWins} (draws: {Draws}), total: {TotalTurns}";
        }
    }
}