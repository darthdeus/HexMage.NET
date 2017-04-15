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

            var dna = new DNA(3, 2);
            var game = GameSetup.GenerateFromDna(dna, dna, map);

            var dnas = new List<DNA>();

            for (int i = 0; i < 20; i++) {
                var copy = dna.Clone();
                copy.Randomize();
                dnas.Add(copy);
            }

            const int step = 20;
            using (var writer = new StreamWriter($@"data/results-rule.txt")) {
                for (int i = step; i < 1000; i += step) {
                    var c1 = new MctsController(game, i);
                    var c2 = new AiRuleBasedController(game);

                    double result = GameEvaluator.CompareAiControllers(game,
                                                                       dnas,
                                                                       c1,
                                                                       c2);

#warning TODO: tohle je hodne fuj !!!!!!!!!!!!!!!!!!!!!!!!!! XXXXXXXXXXXX
                    Console.WriteLine(Accounting.GetStats());
                    Console.WriteLine($"{i} {result}");
                    Console.WriteLine();
                    Accounting.Reset();

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
                GameEvaluator.Playout(game, c1, c2);
                iterationStopwatch.Stop();
                Console.WriteLine($"Iteration: {iterationStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine(Accounting.GetStats());

                GameSetup.OverrideGameDna(game, dna, dna);
                iterationStopwatch.Restart();
                GameEvaluator.Playout(game, c2, c1);
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
                const int dumpIterations = 10;

                var copyStopwatch = Stopwatch.StartNew();
                var totalCopyStopwatch = Stopwatch.StartNew();

                var avgMsPerCopy = new RollingAverage();

                while (iterations < totalIterations) {
                    iterations++;

                    copyStopwatch.Restart();
                    game.DeepCopy();
                    copyStopwatch.Stop();

                    avgMsPerCopy.Add(copyStopwatch.Elapsed.TotalMilliseconds);

                    if (iterations % dumpIterations == 0) {
                        Console.WriteLine($"Copy {avgMsPerCopy.Average:0.00}ms");
                        copyStopwatch.Reset();
                    }
                }

                Console.WriteLine($"Total copy time for {totalIterations}: {totalCopyStopwatch.ElapsedMilliseconds}ms");
            }

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
#endif

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
                    var result = GameEvaluator.Playout(game, c, c);
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

                        //Console.WriteLine(ActionEvaluator.ActionCountString());

                        stopwatch.Reset();
                    }

                    game.Reset();
                }

                Console.WriteLine("Took {0}ms", totalStopwatch.ElapsedMilliseconds);
            }
        }
    }
}