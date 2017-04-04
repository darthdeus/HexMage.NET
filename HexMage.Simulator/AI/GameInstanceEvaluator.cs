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

        public static List<IAiFactory> GlobalFactories = new List<IAiFactory>();

        public EvaluationResult Evaluate() {
            if (GlobalFactories.Count == 0) {
                GlobalFactories.Add(new RuleBasedFactory());
            }

            var result = new EvaluationResult();

            result.TotalHp = 0;
            result.Timeouted = false;
            result.Tainted = false;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            float gameHpPercentageTotal = 0;

            foreach (var factory1 in GlobalFactories) {
                foreach (var factory2 in GlobalFactories) {
                    result.TotalGames++;

                    var game = _gameInstance.CopyStateOnly();
                    var hub = new GameEventHub(game);

                    var ai1 = factory1.Build(game);
                    var ai2 = factory2.Build(game);

                    game.MobManager.Teams[TeamColor.Red] = ai1;
                    game.MobManager.Teams[TeamColor.Blue] = ai2;

                    const int maxIterations = 100;
                    int i = 0;

                    for (; i < maxIterations && !game.IsFinished; i++) {
                        game.TurnManager.CurrentController.FastPlayTurn(hub);
                        game.TurnManager.NextMobOrNewTurn(game.Pathfinder, game.State);

                        result.TotalTurns++;

                        Constants.WriteLogLine(UctAction.EndTurnAction());
                    }

                    result.TotalIterations += i;

                    float gamePercentage;

                    if (Constants.AverageHpTotals) {
                        float totalMaxHp = 0;
                        float totalCurrentHp = 0;

                        foreach (var mobId in game.MobManager.Mobs) {
                            totalMaxHp += game.MobManager.MobInfos[mobId].MaxHp;
                            totalCurrentHp += Math.Max(0, game.State.MobInstances[mobId].Hp);
                        }

                        gamePercentage = totalCurrentHp / totalMaxHp;
                    } else {
                        gamePercentage =
                            game.MobManager.Mobs.Average(mobId => {
                                float currHp = Math.Max(0, game.State.MobInstances[mobId].Hp);
                                float maxHp = game.MobManager.MobInfos[mobId].MaxHp;

                                float avg = currHp / maxHp;

                                if (avg > 1) {
                                    result.Tainted = true;
                                    avg = 1;
                                }

                                return avg;
                            });
                    }


                    result.TotalHp = game.State.MobInstances.Sum(mobInstance => mobInstance.Hp);
                    Debug.Assert(gamePercentage >= 0);

                    gameHpPercentageTotal += gamePercentage;

                    Debug.Assert(gameHpPercentageTotal / result.TotalGames <= 1);

                    EvaluationResult(game, ref result);
                }
            }

            stopwatch.Stop();

            result.TotalHpPercentage = gameHpPercentageTotal / result.TotalGames;
            result.TotalElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            Debug.Assert(result.TotalHpPercentage >= 0);
            Debug.Assert(result.TotalHpPercentage <= 1);

            return result;
        }

        // TODO - na co tohle vlastne je?
        public static int Playout(GameInstance game, DNA d1, DNA d2, IMobController c1, IMobController c2) {
            GameSetup.OverrideGameDNA(game, d1, d2);

            game.MobManager.Teams[TeamColor.Red] = c1;
            game.MobManager.Teams[TeamColor.Blue] = c2;

            int iterations = Constants.MaxPlayoutEvaluationIterations;

            var hub = new GameEventHub(game);

            while (!game.IsFinished && iterations-- > 0) {
                game.TurnManager.CurrentController.FastPlayTurn(hub);
                game.TurnManager.NextMobOrNewTurn(game.Pathfinder, game.State);

                // TODO - extract these
                Constants.WriteLogLine(UctAction.EndTurnAction());
            }
            if (Constants.GetLogBuffer().ToString().Length != 0) {
                Console.WriteLine(Constants.GetLogBuffer());
            }
            Constants.ResetLogBuffer();

            return Constants.MaxPlayoutEvaluationIterations - iterations;
        }

        private static void EvaluationResult(GameInstance game, ref EvaluationResult result) {
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