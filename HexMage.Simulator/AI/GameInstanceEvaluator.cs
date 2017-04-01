﻿using System;
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
            result.Tainted = false;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            int gameCount = 0;
            float gameHpPercentageTotal = 0;

            foreach (var factory1 in factories) {
                foreach (var factory2 in factories) {
                    result.TotalGames++;

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

                        Constants.WriteLogLine(UctAction.EndTurnAction());
                    }

                    result.TotalIterations += maxIterations - iterations;

                    //float totalPercentage = 0;

                    //foreach (var mobId in game.MobManager.Mobs) {
                    //    float mobPercentage = (float) game.State.MobInstances[mobId].Hp /
                    //                          (float) game.MobManager.MobInfos[mobId].MaxHp;

                    //    mobPercentage = Math.Min(Math.Max(0, mobPercentage), 1);

                    //    Debug.Assert(mobPercentage <= 1);
                    //    totalPercentage += mobPercentage;

                    //    result.TotalHp += game.State.MobInstances[mobId].Hp;
                    //}

                    //float gamePercentage = totalPercentage / game.MobManager.Mobs.Count;
                    float gamePercentage =
                        game.MobManager.Mobs.Average(mobId => {
                            float currHp = Math.Max(0, game.State.MobInstances[mobId].Hp);
                            float maxHp = game.MobManager.MobInfos[mobId].MaxHp;

                            float avg = currHp / maxHp;

                            Debug.Assert(avg >= 0);
                            //Debug.Assert(avg <= 1);

                            if (avg > 1) {
                                result.Tainted = true;
                                avg = 1;
                            }

                            // TODO - temporary hack


                            return avg;
                        });
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

            //_writer.WriteLine();

            return result;
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

        public static void UnpackTeamsIntoGame(GameInstance game, DNA team1, DNA team2) {
            var red = team1.ToTeam();
            var blue = team2.ToTeam();

            foreach (var mob in red.mobs) {
                var ids = mob.abilities.Select(ab => game.AddAbilityWithInfo(ab.ToAbility()));
                game.AddMobWithInfo(mob.ToMobInfo(TeamColor.Red, ids));
            }

            foreach (var mob in blue.mobs) {
                var ids = mob.abilities.Select(ab => game.AddAbilityWithInfo(ab.ToAbility()));
                game.AddMobWithInfo(mob.ToMobInfo(TeamColor.Blue, ids));
            }
        }

        public static void ResetPositions(GameInstance game) {
            int x = 0;
            int y = game.Size - 1;
            var mobIds = game.MobManager.Mobs;
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