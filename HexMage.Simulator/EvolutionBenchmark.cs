using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HexMage.Benchmarks;
using HexMage.Simulator.AI;
using HexMage.Simulator.Pathfinding;
using HexMage.Simulator.PCG;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace HexMage.Simulator {
    public class EvolutionBenchmark {
        private readonly GameInstance _game;
        private DNA _initialDna;
        private int _restartCount = 0;

        public EvolutionBenchmark() {
            if (!Directory.Exists(Constants.SaveDir)) {
                Directory.CreateDirectory(Constants.SaveDir);
            }

            string content = File.ReadAllText("team-1.json");
            var team = JsonLoader.LoadTeam(content);
            team.mobs.RemoveAt(0);

            _initialDna = GenomeLoader.FromTeam(team);
            //if (Constants.RandomizeInitialTeam) {
            //    _initialDna.Randomize();
            //}

            _initialDna = new DNA(2, 2);
            _initialDna.Randomize();

            string initialDnaString = _initialDna.ToDnaString();
            Console.WriteLine($"Initial ({_initialDna.Data.Count}): {initialDnaString}\n\n");

            var map = Map.Load("data/map.json");

            _game = GameSetup.GenerateFromDna(_initialDna, _initialDna.Clone(), map);
        }

        public void RunSimulatedAnnealing() {
            var copy = _initialDna.Clone();
            copy.Randomize();

            var current = new GenerationMember {
                dna = copy,
                result = CalculateFitness(_game, _initialDna, copy)
            };

            float T = Constants.InitialT;

            List<double> plotT = new List<double>();
            List<double> plotFit = new List<double>();
            List<double> plotHpPercentage = new List<double>();
            List<double> plotLength = new List<double>();

            int goodCount = 0;
            bool goodEnough = false;

            var gameCopies = Enumerable.Range(0, Constants.TeamsPerGeneration)
                                       .AsParallel()
                                       .Select(_ => _game.DeepCopy())
                                       .ToList();

            var initialDnaCopies = Enumerable.Range(0, Constants.TeamsPerGeneration)
                                             .Select(_ => _initialDna.Clone())
                                             .ToList();

            for (int i = 0; i < Constants.NumGenerations; i++) {
                var tpercentage = Math.Max(0, 1.0f - (float) i / Constants.NumGenerations);

                T = Constants.InitialT * tpercentage;

                var genWatch = new Stopwatch();
                genWatch.Start();

                var teamWatch = new Stopwatch();
                teamWatch.Start();

                if (Constants.RestartFailures && current.result.SimpleFitness() < Constants.FitnessThreshold) {
                    current.dna.Randomize();
                    _restartCount++;
                }

                //_initialDna = Mutate(_initialDna, T);

                var tmp = T;
                var current1 = current;
                var generation = Enumerable.Range(0, Constants.TeamsPerGeneration)
                                           .AsParallel()
                                           .Select(j => {
                                               var newDna = Mutate(current1.dna, (float) tmp);
                                               var newFitness =
                                                   CalculateFitness(gameCopies[j], initialDnaCopies[j], newDna);

                                               return new GenerationMember(newDna, newFitness);
                                           })
                                           .ToList();


                var newMax = PickBestMember(_game, _initialDna, generation);

                HandleGoodEnough(ref goodEnough, newMax.result, current, ref goodCount);

                // TODO: tohle pak budu mozna chtit vratit
                //if (goodEnough) break;

                //if (Constants.ForbidTimeouts && newMax.result.Timeouted) continue;

                float e = current.result.SimpleFitness();
                float ep = newMax.result.SimpleFitness();

                double probability = 1;

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
                plotFit.Add(current.result.SimpleFitness());
                plotHpPercentage.Add(1 - current.result.HpPercentage);
                //plotLength.Add(current.result.TotalTurns);
                plotLength.Add(PlayoutResult.LengthSample(current.result.TotalTurns));

                PrintEvaluationResult(i, e, ep, probability, T, current);
            }

            Console.WriteLine($"Restarts: {_restartCount}");

            if (Constants.GnuPlot) {
                var gnuplotConfigString = $"title '{Constants.NumGenerations} generations," +
                                          $"T_s = {Constants.InitialT}'";

                GnuPlot.HoldOn();
                GnuPlot.Set($"xrange [{Constants.InitialT}:{T}] reverse",
                            $"title '{Constants.NumGenerations} generations, T_s = {Constants.InitialT}",
                            //"yrange [0:1]",
                            //"style data lines",
                            "key tmargin center horizontal");
                GnuPlot.Plot(plotT.ToArray(), plotFit.ToArray(), $"title 'Fitness {Constants.NumGenerations}gen'");
                GnuPlot.Plot(plotT.ToArray(), plotHpPercentage.ToArray(), $"title 'HP percentage'");
                GnuPlot.Plot(plotT.ToArray(), plotLength.ToArray(), "title 'Game length'");
                //GnuPlot.Plot(plotT.ToArray(), plotProb.ToArray(), gnuplotConfigString);
                Console.ReadKey();
            }
        }

        private static GenerationMember PickBestMember(GameInstance game, DNA initialDna,
                                                       List<GenerationMember> generation) {
            float totalFitness = generation.Sum(g => g.CombinedFitness(initialDna));

            var first = generation[0];
            Vector<float> sum = first.dna.Data * (first.CombinedFitness(initialDna) / totalFitness);

            for (int i = 1; i < generation.Count; i++) {
                sum += generation[i].dna.Data * (generation[i].CombinedFitness(initialDna) / totalFitness);
            }

            var dna = new DNA(first.dna.MobCount, first.dna.AbilityCount);
            dna.Data = sum;

            var resultFitness = CalculateFitness(game, initialDna, dna);

            return new GenerationMember(dna, resultFitness);

            //generation.Sort((a, b) => a.result.Fitness.CompareTo(b.result.Fitness));

            //GenerationMember newMax = generation[0];

            //for (int j = 1; j < Constants.TeamsPerGeneration; j++) {
            //    var potentialMax = generation[j];

            //    if (potentialMax.result.Fitness > newMax.result.Fitness) {
            //        newMax = potentialMax;
            //    }
            //}
            //return newMax;
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

        private void HandleGoodEnough(ref bool goodEnough, PlayoutResult newFitness, GenerationMember member,
                                      ref int goodCount) {
            if (Constants.SaveGoodOnes && !goodEnough && newFitness.SimpleFitness() > 0.93) {
                goodCount++;
                if (goodCount > 50) goodEnough = true;
                Console.WriteLine($"Found extra good {newFitness.SimpleFitness()}");

                SaveDna(goodCount, member.dna);
            }
        }

        public static PlayoutResult CalculateFitness(GameInstance game, DNA initialDna, DNA dna) {
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
                copy.Data[i] = (float) Mathf.Clamp(0.01f, dna.Data[i] * change, 1);

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
            var path = Constants.BuildEvoSavePath(savefileIndex);
            using (var writer = new StreamWriter(path)) {
                Console.WriteLine($"Saved to {path}");
                writer.WriteLine(_initialDna.ToSerializableString());
                writer.WriteLine(dna.ToSerializableString());
            }
        }

        public static void SaveDna(int fileIndex, DNA d1, DNA d2) {
            string path = Constants.BuildEvoSavePath(fileIndex);
            using (var writer = new StreamWriter(path)) {
                Console.WriteLine($"Saved to {path}");
                writer.WriteLine(d1.ToSerializableString());
                writer.WriteLine(d2.ToSerializableString());
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