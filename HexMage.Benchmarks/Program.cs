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

            Generator.Random = new Random(3);
            MctsController.EnableLogging = false;

            const bool mctsBenchmark = false;
            const bool evaluateAis = false;

            if (evaluateAis) {
                //new AiEvaluator().Run();
            } else if (mctsBenchmark) {
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
            var game = GameSetup.PrepareForSettings(3, 2);
            
            var d1 = new DNA(3, 2);
            d1.Randomize();


            List<double> xs = new List<double>();
            List<double> ys = new List<double>();

            for (int i = 0; i < 100; i++) {                
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                GameInstanceEvaluator.Playout(game, d1, d1, new MctsController(game), new MctsController(game));

                stopwatch.Stop();

                xs.Add(i);
                ys.Add(stopwatch.ElapsedMilliseconds);
            }

            GnuPlot.Plot(xs.ToArray(), ys.ToArray());

            Console.ReadKey();
        }
    }
}