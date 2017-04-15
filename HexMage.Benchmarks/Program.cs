using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.PCG;
using MathNet.Numerics;
using MathNet.Numerics.Random;
using Constants = HexMage.Simulator.Constants;

namespace HexMage.Benchmarks {
    internal class Program {
        private static void Main(string[] args) {
            // TODO - proc to ale s timhle nekonverguje?!?!?!?!??!
            //Generator.Random = new Random(3);

            //if (!ProcessArguments(args)) return;

            //new Benchmarks().Run();
            //return;

            Constants.MctsBenchmark = true;
            Benchmarks.BenchmarkAllAisAgainstMcts();
            return;

            //Constants.MctsBenchmark = true;
            //Benchmarks.CompareAi();
            //return;


            if ((args.Length > 0 && args[0] == "mcts-benchmark") || Constants.MctsBenchmark) {
                MctsBenchmark();
                return;
            }

            if ((args.Length > 0 && args[0] == "stats") || Constants.MeasureSearchSpaceStats) {
                MeasureSearchSpaceStats();
            }

            if (Constants.EvaluateAis) {
                //new AiEvaluator().Run();
            } else {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                new EvolutionBenchmark().RunSimulatedAnnealing();
                stopwatch.Stop();

                Console.WriteLine(
                    $"Total evolution time: {stopwatch.ElapsedMilliseconds}ms, {Constants.NumGenerations} generations");
            }

            return;

            var key = Console.ReadKey();

            Console.WriteLine();

            if (key.Key == ConsoleKey.D2) {
                Constants.MctsLogging = false;
                //RunEvaluator();
            } else if (key.Key == ConsoleKey.D3) {
                Constants.MctsLogging = false;
                new EvolutionBenchmark().RunSimulatedAnnealing();
            } else {
                for (int i = 0; i < 10; i++) {
                    new Benchmarks().Run();
                }
            }
        }

        private static void MeasureSearchSpaceStats() {
            var d1 = new DNA(2, 2);
            var d2 = new DNA(2, 2);

            int iterations = 0;

            int down = 0;
            int up = 0;
            double avgDown = 0;
            double avgUp = 0;

            Action printStats = () => Console.WriteLine($"I: {iterations.ToString("000000000")}\t" +
                                                        $"D: {down.ToString("00000000")} " +
                                                        $"({(avgDown / down).ToString("0.0000")})\t\t" +
                                                        $"U: {up.ToString("00000000")} " +
                                                        $"({(avgUp / up).ToString("0.0000")})\t\t" +
                                                        $"Ratio D/U: {((float) down / (float) up).ToString("0.0000")}");

            var game = GameSetup.GenerateFromDna(d1, d2);


            for (int i = 0; i < Constants.MeasureSamples; i++) {
                d1.Randomize();
                d2.Randomize();

                var fitness = EvolutionBenchmark.CalculateFitness(game, d1, d2);

                for (int j = 0; j < Constants.MeasureNeighboursPerSample; j++) {
                    iterations++;
                    if (iterations % 1000 == 0) printStats();
                    var neighbour = EvolutionBenchmark.Mutate(d2, 0);

                    var neighbourFitness = EvolutionBenchmark.CalculateFitness(game, d1, neighbour);

                    float delta = neighbourFitness.Fitness - fitness.Fitness;

                    if (delta > 0) {
                        down++;
                        avgDown += Math.Abs(delta);
                    } else {
                        up++;
                        avgUp += Math.Abs(delta);
                    }
                }
            }
            Console.WriteLine("TOTAL:");
            printStats();
        }

        private static bool ProcessArguments(string[] args) {
            const string mctsFactoryPrefix = "--factory=Mcts";

            foreach (var arg in args) {
                if (arg == "--factory=Rule") {
                    GameEvaluator.GlobalFactories.Add(new RuleBasedFactory());
                    continue;
                } else if (arg == "--factory=Random") {
                    GameEvaluator.GlobalFactories.Add(new RandomFactory());
                    continue;
                } else if (arg.StartsWith(mctsFactoryPrefix)) {
                    string mctsIterationsStr = arg.Replace(mctsFactoryPrefix, "");

                    int mctsIterations;
                    if (int.TryParse(mctsIterationsStr, out mctsIterations)) {
                        GameEvaluator.GlobalFactories.Add(new MctsFactory(mctsIterations));
                    } else {
                        Console.WriteLine(
                            $"Invalid format of {arg}, use --factory=MctsN instead (N can be multiple digits).");
                        return false;
                    }

                    continue;
                }

                if (arg.StartsWith("--") && arg.Contains("=")) {
                    var newarg = arg.Replace("--", "").Split('=');

                    if (newarg.Length != 2) {
                        Console.WriteLine($"Invalid argument format of {arg}");
                        return false;
                    }

                    var value = newarg[1];
                    var name = newarg[0];

                    var fieldInfo = typeof(Constants).GetField(name);

                    if (fieldInfo.FieldType == typeof(bool)) {
                        fieldInfo.SetValue(null, bool.Parse(value));
                    } else if (fieldInfo.FieldType == typeof(double)) {
                        fieldInfo.SetValue(null, double.Parse(value));
                    } else if (fieldInfo.FieldType == typeof(float)) {
                        fieldInfo.SetValue(null, float.Parse(value));
                    } else if (fieldInfo.FieldType == typeof(int)) {
                        fieldInfo.SetValue(null, int.Parse(value));
                    } else {
                        Console.WriteLine($"Unsupported field type {fieldInfo.FieldType}");
                    }
                }
            }
            return true;
        }

        public static void MctsBenchmark() {
            var d1 = new DNA(3, 2);
            var game = GameSetup.GenerateFromDna(d1, d1);

            List<double> xs = new List<double>();
            List<double> ys = new List<double>();

            for (int i = 1; i < 10000; i += 20) {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var controller = new MctsController(game, i);

                //controller = new AiRuleBasedController(game);

                int iterations = GameEvaluator.Playout(game, d1, d1, controller, controller);

                stopwatch.Stop();

                Console.WriteLine("************************************");
                Console.WriteLine(
                    $"I:{i} Took ${Constants.MaxPlayoutEvaluationIterations - iterations} iterations, time {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine();

                xs.Add(i);
                ys.Add(stopwatch.ElapsedMilliseconds);
            }

            if (Constants.GnuPlot) {
                GnuPlot.Plot(xs.ToArray(), ys.ToArray(), "with linespoints");
                Console.ReadKey();
            }
        }
    }
}