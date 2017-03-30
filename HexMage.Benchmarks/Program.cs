using System;
using System.IO;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.PCG;

namespace HexMage.Benchmarks {
    internal class Program {
        private static void Main(string[] args) {
            Console.WriteLine("Choose:");
            Console.WriteLine();
            Console.WriteLine("\t1) Benchmark");
            Console.WriteLine("\t2) Team evaluation");
            Console.WriteLine("\t3) Generate new team");

            Generator.Random = new Random(3);
            MctsController.EnableLogging = false;

            const bool evaluateAis = false;

            if (evaluateAis) {
                new AiEvaluator().Run();
            } else {
                new Evolution().Run();
            }

            return;

            var key = Console.ReadKey();

            Console.WriteLine();

            if (key.Key == ConsoleKey.D2) {
                MctsController.EnableLogging = false;
                RunEvaluator();
            } else if (key.Key == ConsoleKey.D3) {
                MctsController.EnableLogging = false;
                new Evolution().Run();
            } else {
                for (int i = 0; i < 10; i++) {
                    new Benchmarks().Run();
                }
            }
        }

        private static void RunEvaluator() {
            string content = File.ReadAllText(@"simple.json");
            var setup = JsonLoader.Load(content);

            var results = GameInstanceEvaluator.EvaluateSetup(setup, Console.Out);
            Console.WriteLine("*************************");
            foreach (var result in results) {
                Console.WriteLine(result);
            }
            Console.WriteLine("*************************\n\n");
        }
    }
}
