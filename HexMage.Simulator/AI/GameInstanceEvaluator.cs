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
        public static readonly Dictionary<string, int> GlobalControllerStatistics = new Dictionary<string, int>();

        public GameInstanceEvaluator(GameInstance gameInstance, TextWriter writer) {
            _gameInstance = gameInstance;
            _writer = writer;
        }

        public EvaluationResult Evaluate() {
            var factories = new IAiFactory[] {
                new RuleBasedFactory(),
                new MctsFactory(1),
                //new MctsFactory(10),
                new RandomFactory(),
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

                    // TODO !!!!!!!!!!!!!!!! muze nastat remiza
                    if (game.IsFinished && game.VictoryTeam.HasValue) {
                        Debug.Assert(game.VictoryTeam.HasValue);
                        Debug.Assert(game.VictoryController != null);                        

                        _writer.Write($"{game.VictoryController}:{game.LoserController}: {maxIterations - iterations}({game.VictoryTeam.ToString()[0]}), ");
                        if (game.VictoryTeam == TeamColor.Red) {
                            result.RedWins++;
                        } else {
                            result.BlueWins++;
                        }

                        var victoryControllerName = game.VictoryController.ToString();

                        if (GlobalControllerStatistics.ContainsKey(victoryControllerName)) {
                            GlobalControllerStatistics[victoryControllerName]++;
                        } else {
                            GlobalControllerStatistics[victoryControllerName] = 1;
                        }
                    } else {
                        result.Timeouts++;
                        _writer.Write("Timeout\t");
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
            const int mapSize = 4;

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

            for (int i = 0; i < mapSize; i += 20) {
                PreparePositions(game, mobIds, i, mapSize - i);

                var result = new GameInstanceEvaluator(game, writer).Evaluate();

                results.Add(result);
            }

            writer.WriteLine($"\n***MCTS avg: {ExponentialMovingAverage.Instance.CurrentValue}ms/iter\n\n");

            return results;
        }
    }
}