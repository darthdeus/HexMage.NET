//#define COPY_BENCH

#define FAST
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;

namespace HexMage.Benchmarks {
    public class Benchmarks {
        public static void CompareAi() {
            var dna = new DNA(2, 2);
            dna.Randomize();

            var map = Map.Load(@"data/map.json");

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
                    c1 = new AiRuleBasedController(game);
                    c2 = new AiRandomController(game);
                    break;                
                default:
                    throw new ArgumentException($"Invalid value of {Constants.MctsBenchType} for --MctsBenchType");
            }

            var iterationStopwatch = new Stopwatch();

            int c1Wins = 0;
            int c2Wins = 0;

            for (int i = 0; i < 1000; i++) {
                dna.Randomize();
                iterationStopwatch.Restart();

                GameSetup.OverrideGameDna(game, dna, dna);
                var r1 = GameEvaluator.Playout(game, c1, c2);

                GameSetup.OverrideGameDna(game, dna, dna);
                var r2 = GameEvaluator.Playout(game, c2, c1);

                iterationStopwatch.Stop();

                //Console.WriteLine($"Iteration: {iterationStopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine(Accounting.GetStats());

                c1Wins += r1.RedWins + r2.BlueWins;
                c2Wins += r1.BlueWins + r2.RedWins;

                Console.WriteLine($"{i.ToString("0000")} STATS: M2: {c1Wins}, M5: {c2Wins}, winrate: {((double)c1Wins/(c1Wins+c2Wins)).ToString("0.000")}");
            }
        }

        public void Run() {
            var s = Stopwatch.StartNew();
            CoordRadiusCache.Instance.PrecomputeUpto(50);
            Console.WriteLine($"Cache precomputed in {s.Elapsed.TotalMilliseconds}ms");

            var game = GameSetup.GenerateForDnaSettings(2, 2);
            var c = new MctsController(game, 500);

            {
                var totalStopwatch = Stopwatch.StartNew();
                var stopwatch = new Stopwatch();
                var iterations = 0;
                const int dumpIterations = 1;
                const int totalIterations = 500000;

                while (iterations < totalIterations) {
                    iterations++;

                    stopwatch.Start();
                    GameEvaluator.Playout(game, c, c);
                    stopwatch.Stop();

                    if (iterations % dumpIterations == 0) {
                        double perThousandMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);
                        double perGame = Math.Round(perThousandMs / dumpIterations, 2);

                        Console.WriteLine(
                            $"TOTAL: {UctAlgorithm.TotalIterationCount}, " +
                            $"Actions: {ActionEvaluator.Actions}, " +
                            $"IterAVG: {UctAlgorithm.MillisecondsPerIterationAverage.Average:0.000000}ms\t" +
                            $"IPS: {1 / UctAlgorithm.MillisecondsPerIterationAverage.Average * 1000}\t" +
                            $"per game: {perGame:00.00}ms");

                        stopwatch.Reset();
                    }

                    game.Reset();
                }

                Console.WriteLine("Took {0}ms", totalStopwatch.ElapsedMilliseconds);
            }
        }
    }
}