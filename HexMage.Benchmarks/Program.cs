using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.PCG;

namespace HexMage.Benchmarks {
    internal class Program {
        private static void Main(string[] args) {
            Generator.Random = new Random(3);

            Constants.EnableLogging = true;

            if (!ProcessArguments(args)) return;

            if ((args.Length > 0 && args[0] == "mcts-benchmark") || Constants.MctsBenchmark) {
                MctsBenchmark();
                return;
            }


            if (Constants.EvaluateAis) {
                //new AiEvaluator().Run();
            } else {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                new Evolution().Run();
                stopwatch.Stop();

                Console.WriteLine($"Total evolution time: {stopwatch.ElapsedMilliseconds}ms");
            }

            return;

            var key = Console.ReadKey();

            Console.WriteLine();

            if (key.Key == ConsoleKey.D2) {
                Constants.EnableLogging = false;
                //RunEvaluator();
            } else if (key.Key == ConsoleKey.D3) {
                Constants.EnableLogging = false;
                new Evolution().Run();
            } else {
                for (int i = 0; i < 10; i++) {
                    new Benchmarks().Run();
                }
            }
        }

        private static bool ProcessArguments(string[] args) {
            const string mctsFactoryPrefix = "--factory=Mcts";

            foreach (var arg in args) {
                if (arg == "--factory=Rule") {
                    GameInstanceEvaluator.GlobalFactories.Add(new RuleBasedFactory());
                    continue;
                } else if (arg == "--factory=Random") {
                    GameInstanceEvaluator.GlobalFactories.Add(new RandomFactory());
                    continue;
                } else if (arg.StartsWith(mctsFactoryPrefix)) {
                    string mctsIterationsStr = arg.Replace(mctsFactoryPrefix, "");

                    int mctsIterations;
                    if (int.TryParse(mctsIterationsStr, out mctsIterations)) {
                        GameInstanceEvaluator.GlobalFactories.Add(new MctsFactory(mctsIterations));
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
            var game = GameSetup.FromDNAs(d1, d1);

            List<double> xs = new List<double>();
            List<double> ys = new List<double>();

            for (int i = 1; i < 10000; i += 20) {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var controller = new MctsController(game, i);

                //controller = new AiRuleBasedController(game);

                int iterations = GameInstanceEvaluator.Playout(game, d1, d1, controller, controller);

                stopwatch.Stop();

                Console.WriteLine("************************************");
                Console.WriteLine(
                    $"I:{i} Took ${Constants.MaxPlayoutEvaluationIterations - iterations} iterations, time {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine();

                xs.Add(i);
                ys.Add(stopwatch.ElapsedMilliseconds);
            }

            if (Constants.EnableGnuPlot) {
                GnuPlot.Plot(xs.ToArray(), ys.ToArray(), "with linespoints");
                Console.ReadKey();
            }
        }
    }
}