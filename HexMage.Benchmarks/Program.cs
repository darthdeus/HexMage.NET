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
            //Console.WriteLine("Choose:");
            //Console.WriteLine();
            //Console.WriteLine("\t1) Benchmark");
            //Console.WriteLine("\t2) Team evaluation");
            //Console.WriteLine("\t3) Generate new team");

            foreach (var arg in args) {
                if (arg.StartsWith("--") && arg.Contains("=")) {
                    var newarg = arg.Replace("--", "").Split('=');

                    if (newarg.Length != 2) {
                        Console.WriteLine($"Invalid argument format of {arg}");
                        return;
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

            Generator.Random = new Random(3);
            MctsController.EnableLogging = false;

            if (Constants.EvaluateAis) {
                //new AiEvaluator().Run();
            } else if (Constants.MctsBenchmark) {
                MctsBenchmark();
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
                MctsController.EnableLogging = false;
                //RunEvaluator();
            } else if (key.Key == ConsoleKey.D3) {
                MctsController.EnableLogging = false;
                new Evolution().Run();
            } else {
                for (int i = 0; i < 10; i++) {
                    new Benchmarks().Run();
                }
            }
        }

        public static void MctsBenchmark() {
            var d1 = new DNA(3, 2);
            var game = GameSetup.FromDNAs(d1, d1);

            List<double> xs = new List<double>();
            List<double> ys = new List<double>();

            for (int i = 1; i < 5; i++) {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var controller = new MctsController(game, i);

                //controller = new AiRuleBasedController(game);

                GameInstanceEvaluator.Playout(game, d1, d1, controller, controller);

                stopwatch.Stop();

                xs.Add(i);
                ys.Add(stopwatch.ElapsedMilliseconds);
            }

            //GnuPlot.Plot(xs.ToArray(), ys.ToArray(), "with linespoints");

            //Console.ReadKey();
        }
    }
}