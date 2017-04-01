using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    public class GameInstanceEvaluator {
        public static bool CountGlobalStats = true;

        private readonly GameInstance _gameInstance;
        private readonly TextWriter _writer;

        public static int MctsWins = 0;
        public static int RandomAiWins = 0;
        public static int RuleBasedAiWins = 0;

        public static readonly Dictionary<string, int> GlobalControllerStatistics = new Dictionary<string, int>();

        public GameInstanceEvaluator(GameInstance gameInstance, TextWriter writer) {
            _gameInstance = gameInstance;
            _writer = writer;
        }

        public EvaluationResult Evaluate() {
            var factories = new IAiFactory[] {
                new RuleBasedFactory(),
                //new MctsFactory(1),
                //new MctsFactory(10),
                //new RandomFactory(),
            };

            var result = new EvaluationResult();

            result.TotalHp = 0;
            result.Timeouted = false;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            int gameCount = 0;
            float gameHpPercentageTotal = 0;

            foreach (var factory1 in factories) {
                foreach (var factory2 in factories) {
                    gameCount++;

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

                        result.TotalTurns++;
                    }

                    result.TotalIterations += maxIterations - iterations;

                    float totalPercentage = 0;

                    foreach (var mobId in game.MobManager.Mobs) {
                        float mobPercentage = (float) game.State.MobInstances[mobId].Hp /
                                              (float) game.MobManager.MobInfos[mobId].MaxHp;

                        mobPercentage = Math.Min(Math.Max(0, mobPercentage), 1);

                        Debug.Assert(mobPercentage <= 1);
                        totalPercentage += mobPercentage;

                        result.TotalHp += game.State.MobInstances[mobId].Hp;
                    }

                    float gamePercentage = totalPercentage / game.MobManager.Mobs.Count;
                    Debug.Assert(gamePercentage >= 0);

                    gameHpPercentageTotal += gamePercentage;

                    Debug.Assert(gameHpPercentageTotal / gameCount <= 1);

                    // TODO !!!!!!!!!!!!!!!! muze nastat remiza
                    if (game.IsFinished && game.VictoryTeam.HasValue) {
                        Debug.Assert(game.VictoryTeam.HasValue);
                        Debug.Assert(game.VictoryController != null);

                        //_writer.Write($"{game.VictoryController}:{game.LoserController}: {maxIterations - iterations}({game.VictoryTeam.ToString()[0]}), ");
                        if (game.VictoryTeam == TeamColor.Red) {
                            result.RedWins++;
                        } else {
                            result.BlueWins++;
                        }

                        var victoryControllerName = game.VictoryController.ToString();
                        var victoryControllerType = game.VictoryController.GetType();

                        if (victoryControllerType == typeof(MctsController)) {
                            Interlocked.Increment(ref MctsWins);
                        } else if (victoryControllerType == typeof(AiRandomController)) {
                            Interlocked.Increment(ref RandomAiWins);
                        } else if (victoryControllerType == typeof(AiRuleBasedController)) {
                            Interlocked.Increment(ref RuleBasedAiWins);
                        }

                        if (CountGlobalStats) {
                            if (GlobalControllerStatistics.ContainsKey(victoryControllerName)) {
                                GlobalControllerStatistics[victoryControllerName]++;
                            } else {
                                GlobalControllerStatistics[victoryControllerName] = 1;
                            }
                        }
                    } else {
                        result.Timeouts++;
                        result.Timeouted = true;
                        //_writer.Write("Timeout\t");
                    }

                    result.TotalGames++;
                }
            }

            stopwatch.Stop();

            result.TotalHpPercentage = gameHpPercentageTotal / gameCount;
            result.TotalElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            Debug.Assert(result.TotalHpPercentage >= 0);
            Debug.Assert(result.TotalHpPercentage <= 1);

            //_writer.WriteLine();

            return result;
        }

        public static void PreparePositions(GameInstance game, List<int> mobIds, int x, int y) {
            game.State.SetMobPosition(mobIds[0], new AxialCoord(x, y));
            game.State.SetMobPosition(mobIds[1], new AxialCoord(y, x));

            game.State.SetMobPosition(mobIds[2], new AxialCoord(-x, -y));
            game.State.SetMobPosition(mobIds[3], new AxialCoord(-y, -x));

        }

        //public static EvaluationResult EvaluateSetup(Setup setup, TextWriter writer) {
        //    const int mapSize = 4;

        //    var game = new GameInstance(new Map(mapSize));

        //    setup.UnpackIntoGame(game);

        //        PreparePositions(game, game.MobManager.Mobs, 0, mapSize);


        //        results.Add(result);

        //    //writer.WriteLine($"\n***MCTS avg: {ExponentialMovingAverage.Instance.CurrentValue}ms/iter\n\n");

        //    return results;
        //}
    }
}