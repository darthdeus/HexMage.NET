using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.PCG;

namespace HexMage.Benchmarks {
    public class EvolutionBenchmark {
        private readonly GameInstance game;
        private readonly DNA initialDna;
        private int _restartCount = 0;

        public EvolutionBenchmark() {
            if (!Directory.Exists(Constants.SaveDir)) {
                Directory.CreateDirectory(Constants.SaveDir);
            }

            string content = File.ReadAllText("team-1.json");
            var team = JsonLoader.LoadTeam(content);
            team.mobs.RemoveAt(0);

            initialDna = GenomeLoader.FromTeam(team);
            if (Constants.RandomizeInitialTeam) {
                initialDna.Randomize();
            }

            Console.WriteLine($"Initial: {initialDna.ToDnaString()}\n\n");

            game = GameSetup.GenerateFromDna(initialDna, initialDna);
        }

        public void Run() {
            var generation = new List<GenerationMember>(Constants.TeamsPerGeneration);

            var copy = initialDna.Clone();
            copy.Randomize();

            var current = new GenerationMember {
                dna = copy,
                result = CalculateFitness(game, initialDna, copy)
            };

            double Tpercentage = 1;
            double T = Constants.InitialT;

            List<double> plotT = new List<double>();
            List<double> plotFit = new List<double>();
            List<double> plotProb = new List<double>();

            // TODO: exponencialni rozdeleni otestovat
            //var xs = new List<double>();
            //var ys = new List<double>();

            //for (int i = 0; i < 1000; i++) {
            //    double x = 1.0 / 1000.0 * i;
            //    double y = Probability.Exponential(1, x);
            //    y = Math.Exp(-x);

            //    xs.Add(x);
            //    ys.Add(y);
            //}

            //GnuPlot.Plot(xs.ToArray(), ys.ToArray());
            //Console.ReadKey();

            int extraIterations = Constants.ExtraIterations;
            int maxTotalHp = 0;

            int goodCount = 0;
            bool goodEnough = false;

            for (int i = 0; i < Constants.NumGenerations; i++) {
                Tpercentage = Math.Max(0, Tpercentage - 1.0 / Constants.NumGenerations);

                T = Constants.InitialT * Tpercentage;

                if (Tpercentage < 0.01) {
                    i -= extraIterations;
                    extraIterations = 0;
                    T = 0.01;
                }

                var genWatch = new Stopwatch();
                genWatch.Start();

                var teamWatch = new Stopwatch();
                teamWatch.Start();

                if (Constants.RestartFailures && current.result.Fitness < Constants.FitnessThreshold) {
                    current.dna.Randomize();
                    _restartCount++;
                }

                generation.Clear();

                for (int j = 0; j < Constants.TeamsPerGeneration; j++) {
                    var newDna = Mutate(current.dna, (float)T);
                    var newFitness = CalculateFitness(game, initialDna, newDna);

                    generation.Add(new GenerationMember(newDna, newFitness));
                }

                var newMax = PickBestMember(generation);

                HandleGoodEnough(ref goodEnough, newMax.result, current, ref goodCount);

                // TODO: tohle pak budu mozna chtit vratit
                //if (goodEnough) break;

                if (newMax.result.Tainted) {
                    SaveTainted(newMax.dna);
                    i = Constants.NumGenerations;
                }

                if (Constants.ForbidTimeouts && newMax.result.Timeouted) continue;

                float e = current.result.Fitness;
                float ep = newMax.result.Fitness;
                                        
                double probability;

                float delta = ep - e;

                if (delta > 0) {
                    probability = 1;
                    current = newMax;
                } else {
                    probability = Math.Exp(-Math.Abs(delta) / T);
                    if (Constants.HillClimbing ^ Probability.Uniform(probability)) {
                        newMax.failCount = current.failCount;
                        current = newMax;
                    }
                    current.failCount++;
                }


                plotT.Add(T);
                plotFit.Add(current.result.Fitness);
                plotProb.Add(probability);

                PrintEvaluationResult(i, e, ep, probability, T, current);

                //Console.WriteLine("****************************************************");
                //Console.WriteLine($"Generation {i}. done in {genWatch.ElapsedMilliseconds}ms");
                //Console.WriteLine("****************************************************");
            }

            Console.WriteLine($"Restarts: {_restartCount}");

            if (Constants.GnuPlot) {
                var gnuplotConfigString = $"title '{Constants.NumGenerations} generations," +
                                          $"T_s = {Constants.InitialT}'";

                GnuPlot.HoldOn();
                GnuPlot.Set($"xrange [{Constants.InitialT}:{T}] reverse",
                            "yrange [0:1]",
                            "style data lines",
                            "key tmargin center horizontal");
                //GnuPlot.Set($"yrange [0:1] ");
                //GnuPlot.Set($"style data lines");
                GnuPlot.Plot(plotT.ToArray(), plotFit.ToArray(), gnuplotConfigString);
                //GnuPlot.Plot(plotT.ToArray(), plotProb.ToArray(), gnuplotConfigString);
                Console.ReadKey();
            }
        }

        private static GenerationMember PickBestMember(List<GenerationMember> generation) {
            generation.Sort((a, b) => a.result.Fitness.CompareTo(b.result.Fitness));

            GenerationMember newMax = generation[0];

            for (int j = 1; j < Constants.TeamsPerGeneration; j++) {
                var potentialMax = generation[j];

                if (potentialMax.result.Fitness > newMax.result.Fitness) {
                    newMax = potentialMax;
                }
            }
            return newMax;
        }

        private static void PrintEvaluationResult(int i, float e, float ep, double probability, double T,
                                                  GenerationMember member) {
            if (i % Constants.EvolutionPrintModulo == 0) {
                string ps = probability.ToString("0.00");
                string ts = T.ToString("0.0000");

                if (Constants.PrintFitnessDiff) {
                    string se = e.ToString("0.00000");
                    string sep = ep.ToString("0.00000");

                    Console.WriteLine(
                        $"P: {ps}, d/T: {((ep - e) / T).ToString("0.000")}\tT:{ts}\t{se} -> {sep} = {Math.Abs(ep - e).ToString("0.0000000")}");
                } else {
                    Console.WriteLine($"P: {ps}\tT:{ts}\t{member}");
                }
            }
        }

        private void HandleGoodEnough(ref bool goodEnough, EvaluationResult newFitness, GenerationMember member,
                                      ref int goodCount) {
            if (Constants.SaveGoodOnes && !goodEnough && newFitness.Fitness > 0.995) {
                goodCount++;
                if (goodCount > 50) goodEnough = true;
                Console.WriteLine($"Found extra good {newFitness.Fitness}");

                SaveDna(goodCount, member.dna);
            }
        }

        public static EvaluationResult CalculateFitness(GameInstance game, DNA initialDna, DNA dna) {
            Constants.ResetLogBuffer();
            GameSetup.OverrideGameDna(game, initialDna, dna);

            var result = new GameEvaluator(game, Console.Out).Evaluate();

            return result;
        }

        public static DNA Mutate(DNA dna, float T) {
            var copy = dna.Clone();

            bool redo = false;

            do {
                redo = false;

                double delta = Generator.Random.NextDouble() * Constants.MutationDelta;
                delta = Constants.MutationDelta;
                double change = Probability.Uniform(0.5) ? 1 + delta : 1 - delta;

                int i = Generator.Random.Next(0, dna.Data.Count);
                if (copy.IsElementIndex(i)) {
                    copy.Data[i] = Generator.Random.Next(0, 4) / (float)4;
                } else {
                    copy.Data[i] = (float)Mathf.Clamp(0.01f, dna.Data[i] * change, 1);
                }

                if (!copy.ToTeam().IsValid()) {
                    copy = dna.Clone();
                    redo = true;
                }

                // TODO - zkusit ruzny pravdepodobnosti - ovlivnovat to teplotou?
            } while (redo || Probability.Uniform(Constants.SecondMutationProb));

            return copy;
        }


        private void SaveTainted(DNA dna) {
            using (var logWriter = new StreamWriter(Constants.SaveDir + "tainted-log.txt")) {
                logWriter.Write(Constants.GetLogBuffer().ToString());
            }

            SaveDna(666, dna);
        }

        private void SaveDna(int savefileIndex, DNA dna) {
            using (var writer = new StreamWriter(Constants.BuildEvoSavePath(savefileIndex))) {
                writer.WriteLine(initialDna.ToSerializableString());
                writer.WriteLine(dna.ToSerializableString());
            }
        }

        private static void LogStats() {
            //writer.WriteLine(
            //    $"Total:\t\t{teamWatch.ElapsedMilliseconds}ms\nPer turn:\t{mpt}ms\nPer iteration:\t{mpi}ms");
            //writer.WriteLine();
            //writer.WriteLine();

            //Console.WriteLine("Win stats:");
            //foreach (var pair in GameInstanceEvaluator.GlobalControllerStatistics) {
            //    Console.WriteLine($"{pair.Key}: {pair.Value}");
            //}
            //Console.WriteLine(
            //    $"Expand: {UctAlgorithm.ExpandCount}, BestChild: {UctAlgorithm.BestChildCount}, Ratio: {(float) UctAlgorithm.ExpandCount / (float) UctAlgorithm.BestChildCount}");
            //Console.WriteLine("\n\n");
        }
    }
}