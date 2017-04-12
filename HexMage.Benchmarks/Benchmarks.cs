//#define COPY_BENCH

#define FAST
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;
using HexMage.Simulator.PCG;
using Newtonsoft.Json;

namespace HexMage.Benchmarks {
    public class Benchmarks {
        public static void BenchmarkAllAisAgainstMcts() {
            var map = Map.Load(@"data/map.json");

            var opponentAis = new Func<GameInstance, IMobController>[] {
                AiRandomController.Build,
                AiRuleBasedController.Build,
                FlatMonteCarloController.Build
            };

            var dna = new DNA(2, 2);
            var game = GameSetup.GenerateFromDna(dna, dna, map);

            using (var writer = new StreamWriter($@"data/results-rule-200.txt")) {
                for (int i = 10; i < 10000; i += 20) {
                    double result = GameEvaluator.CompareAiControllers(game,
                                                                       dna,
                                                                       new MctsController(game, i),
                                                                       new AiRuleBasedController(game));

                    Console.WriteLine($"{i} {result}");
                    writer.WriteLine($"{i} {result}");
                }
            }
        }

        public static void CompareAi() {
            var dna = new DNA(2, 2);
            dna.Randomize();

            var map = Map.Load(@"data/map.json");
            //var map = new Map(5);

            var game = GameSetup.GenerateFromDna(dna, dna, map);

            game.PrepareEverything();

            GameInvariants.AssertMobsNotStandingOnEachother(game);

            IMobController c1, c2;

            switch (Constants.MctsBenchType) {
                case 0:
                    c1 = new MctsController(game, Constants.MctsBenchIterations);
                    c2 = new AiRandomController(game);
                    break;
                case 1:
                    c1 = new MctsController(game, Constants.MctsBenchIterations);
                    c2 = new AiRuleBasedController(game);
                    break;
                case 2:
                    c1 = new AiRandomController(game);
                    c2 = new AiRuleBasedController(game);
                    break;
                case 3:
                    c1 = new FlatMonteCarloController(game);
                    c2 = new AiRandomController(game);
                    break;
                case 4:
                    c1 = new FlatMonteCarloController(game);
                    c2 = new AiRuleBasedController(game);
                    break;
                case 5:
                    c1 = new FlatMonteCarloController(game);
                    c2 = new MctsController(game, Constants.MctsBenchIterations);
                    break;
                default:
                    throw new ArgumentException($"Invalid value of {Constants.MctsBenchType} for --MctsBenchType");
            }

            //c1 = new MctsController(game, 3);
            //c2 = new FlatMonteCarloController(game);
            //c2 = new MctsController(game, 100000);

            var iterationStopwatch = new Stopwatch();

            for (int i = 0; i < 100; i++) {
                dna.Randomize();

                GameSetup.OverrideGameDna(game, dna, dna);
                iterationStopwatch.Restart();
                GameEvaluator.PlayoutSingleGame(game, c1, c2);
                iterationStopwatch.Stop();
                Console.WriteLine($"Iteration: {iterationStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine(Accounting.GetStats());

                GameSetup.OverrideGameDna(game, dna, dna);
                iterationStopwatch.Restart();
                GameEvaluator.PlayoutSingleGame(game, c2, c1);
                iterationStopwatch.Stop();

                Console.WriteLine($"Iteration: {iterationStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine(Accounting.GetStats());
            }
        }

        public void Run() {
            var size = 5;

            var s = Stopwatch.StartNew();
            CoordRadiusCache.Instance.PrecomputeUpto(50);
            Console.WriteLine($"Cache precomputed in {s.Elapsed.TotalMilliseconds}ms");

            var game = GameSetup.GenerateForDnaSettings(2, 2);
            var c = new MctsController(game, 500);

            //Utils.RegisterLogger(new StdoutLogger());
            //Utils.MainThreadId = Thread.CurrentThread.ManagedThreadId;

#if COPY_BENCH
            {
                Console.WriteLine("---- STATE COPY BENCHMARK ----");

                int totalIterations = 1000000;
                int iterations = 0;
                const int dumpIterations = 1000;

                var copyStopwatch = Stopwatch.StartNew();
                var totalCopyStopwatch = Stopwatch.StartNew();

                while (iterations < totalIterations) {
                    iterations++;

                    copyStopwatch.Start();
                    gameInstance.DeepCopy();
                    copyStopwatch.Stop();

                    if (iterations % dumpIterations == 0) {
                        double secondsPerThousand = (double) copyStopwatch.ElapsedTicks / Stopwatch.Frequency;
                        double msPerCopy = secondsPerThousand;
                        Console.WriteLine($"Copy {msPerCopy:0.00}ms, 1M in: {secondsPerThousand * 1000}s");
                        copyStopwatch.Reset();
                    }
                }

                Console.WriteLine($"Total copy time for {totalIterations}: {totalCopyStopwatch.ElapsedMilliseconds}ms");
            }

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();

#endif

            Console.WriteLine("Precomputing cubes");
            game.Map.PrecomputeCubeLinedraw();
            Console.WriteLine("Cubes precomputed");

            {
                var totalStopwatch = Stopwatch.StartNew();
                var stopwatch = new Stopwatch();
                var iterations = 0;
                int roundsPerThousand = 0;
                const int dumpIterations = 1;
                const int totalIterations = 500000;

                while (iterations < totalIterations) {
                    iterations++;

                    // TODO - fuj
                    //turnManager.StartNextTurn(pathfinder, gameInstance.State);

                    //Console.WriteLine($"Starting, actions: {UctAlgorithm.Actions}");
                    stopwatch.Start();
#if FAST
                    var result = new EvaluationResult();
                    GameEvaluator.Playout(game, c, c, ref result);
                    stopwatch.Stop();

                    roundsPerThousand += result.TotalTurns;
#else
                var rounds = hub.MainLoop(TimeSpan.Zero);
                stopwatch.Stop();

                rounds.Wait();
                roundsPerThousand += rounds.Result;
#endif

                    if (iterations % dumpIterations == 0) {
                        double perThousandMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);
                        double perGame = Math.Round(perThousandMs / dumpIterations, 2);

                        Console.WriteLine(
                            $"TOTAL: {UctAlgorithm.TotalIterationCount}, " +
                            $"Actions: {ActionEvaluator.Actions}, " +
                            $"IterAVG: {UctAlgorithm.MillisecondsPerIterationAverage.Average:0.000000}ms\t" +
                            $"IPS: {1 / UctAlgorithm.MillisecondsPerIterationAverage.Average * 1000}\t" +
                            $"per game: {perGame:00.00}ms");
                        roundsPerThousand = 0;

                        Console.WriteLine(ActionEvaluator.ActionCountString());

                        stopwatch.Reset();
                    }

                    game.Reset();
                }

                Console.WriteLine("Took {0}ms", totalStopwatch.ElapsedMilliseconds);
            }
        }
    }
}