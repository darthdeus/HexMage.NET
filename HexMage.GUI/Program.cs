using System;
using System.Globalization;
using System.Threading;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.PCG;

namespace HexMage.GUI {
    public static class Program {
        public static CancellationToken CancellationToken;

        [STAThread]
        static void Main(string[] args) {
            if (!ProcessArguments(args)) return;

            Constants.MctsLogging = true;
            var cts = new CancellationTokenSource();
            CoordRadiusCache.Instance.PrecomputeUpto(50);

            using (var game = new HexMageGame())
                game.Run();

            cts.Cancel();
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
    }
}