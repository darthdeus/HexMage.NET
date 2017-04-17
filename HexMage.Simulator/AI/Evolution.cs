using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Benchmarks;
using HexMage.Simulator.Pathfinding;
using MathNet.Numerics.LinearAlgebra;

namespace HexMage.Simulator.AI {
    public class Evolution<T> {
        private readonly Func<T, float> _fitnessFunc;
        public static readonly RollingAverage AverageGenerationTime = new RollingAverage();

        public Evolution(Func<T, float> fitnessFunc) {
            _fitnessFunc = fitnessFunc;
        }

        public void RunEvolutionStrategies() {
            var t1 = new DNA(2, 2);
            var t2 = t1.Clone();
            t1.Randomize();
            t2.Randomize();

            var map = Map.Load("data/map.json");
            Console.WriteLine($"Initial ({t1.Data.Count}): {t1.ToDnaString()}\n\n");

            var game = GameSetup.GenerateFromDna(t1, t2, map);

            int restartCount = 0;

            var current = new Individual(t1.Clone(),
                                         t1.Clone(),
                                         EvolutionBenchmark.CalculateFitness(game, t1, t2));

            List<double> plotT = new List<double>();
            List<double> plotFit = new List<double>();
            List<double> plotHpPercentage = new List<double>();
            List<double> plotLength = new List<double>();
            List<double> plotDistance = new List<double>();

            int goodCount = 0;
            bool goodEnough = false;

            var gameCopies = Enumerable.Range(0, Constants.TeamsPerGeneration)
                                       .AsParallel()
                                       .Select(_ => game.DeepCopy())
                                       .ToList();

            for (int i = 0; i < Constants.NumGenerations; i++) {
                float tpercentage = Math.Max(0, 1.0f - (float) i / Constants.NumGenerations);
                float T = Constants.InitialT * tpercentage;

                var genWatch = new Stopwatch();
                genWatch.Start();

                var teamWatch = new Stopwatch();
                teamWatch.Start();

                if (Constants.RestartFailures && current.Result.SimpleFitness() < Constants.FitnessThreshold) {
                    current.Team1.Randomize();
                    current.Team2.Randomize();
                    restartCount++;
                }

                //_initialDna = Mutate(_initialDna, T);

                var generation = Enumerable.Range(0, Constants.TeamsPerGeneration)
                                           .AsParallel()
                                           .Select(j => {
                                               var newTeam1 = EvolutionBenchmark.Mutate(current.Team1, (float) T);
                                               var newTeam2 = EvolutionBenchmark.Mutate(current.Team2, (float) T);

                                               var newFitness =
                                                   EvolutionBenchmark.CalculateFitness(
                                                       gameCopies[j], newTeam1, newTeam2);

                                               return new Individual(newTeam1, newTeam2, newFitness);
                                           })
                                           .ToList();

                var previous = current;
                current = EvolutionStrategy(game, generation);

                if (Constants.SaveGoodOnes && !goodEnough && current.CombinedFitness() > 0.99) {
                    goodCount++;
                    if (goodCount > 50) goodEnough = true;
                    Console.WriteLine($"Found extra good {current.CombinedFitness()}");

                    EvolutionBenchmark.SaveDna(0, current.Team1, current.Team2);
                }

                // TODO: zakazat timeouty v playoutu
                //if (Constants.ForbidTimeouts && newMax.result.Timeouted) continue;


                plotT.Add(T);
                plotFit.Add(current.CombinedFitness());
                plotHpPercentage.Add(1 - current.Result.HpPercentage);
                plotLength.Add(PlayoutResult.LengthSample(current.Result.TotalTurns));
                plotDistance.Add(current.Team1.DistanceFitness(current.Team2));


                if (i % Constants.EvolutionPrintModulo == 0) {
                    Console.WriteLine($"T: {T.ToString("0.0000")}\t\t" +
                                      $"F: {previous.CombinedFitness().ToString("0.0000")}" +
                                      $" -> {current.CombinedFitness().ToString("0.0000")}");
                }
            }

            Console.WriteLine($"Restarts: {restartCount}");

            if (Constants.GnuPlot) {
                var gnuplotConfigString = $"title '{Constants.NumGenerations} generations," +
                                          $"T_s = {Constants.InitialT}'";

                GnuPlot.HoldOn();
                GnuPlot.Set($"xrange [{Constants.InitialT}:0] reverse",
                            $"title '{Constants.NumGenerations} generations, T_s = {Constants.InitialT}",
                            //"yrange [0:1]",
                            //"style data lines",
                            "key tmargin center horizontal");
                GnuPlot.Plot(plotT.ToArray(), plotFit.ToArray(), $"title 'Fitness {Constants.NumGenerations}gen'");
                GnuPlot.Plot(plotT.ToArray(), plotHpPercentage.ToArray(), $"title 'HP percentage'");
                GnuPlot.Plot(plotT.ToArray(), plotLength.ToArray(), "title 'Game length'");
                GnuPlot.Plot(plotT.ToArray(), plotDistance.ToArray(), "title 'Team difference'");
                //GnuPlot.Plot(plotT.ToArray(), plotProb.ToArray(), gnuplotConfigString);
                Console.ReadKey();
            }
        }

        public static Individual EvolutionStrategy(GameInstance game, List<Individual> generation) {
            float totalFitness = generation.Sum(i => i.CombinedFitness());

            var first = generation[0];

            Vector<float> t1 = first.Team1.Data * (first.CombinedFitness() / totalFitness);
            Vector<float> t2 = first.Team2.Data * (first.CombinedFitness() / totalFitness);

            for (int i = 1; i < generation.Count; i++) {
                var ratio = generation[i].CombinedFitness() / totalFitness;
                t1 += generation[i].Team1.Data * ratio;
                t2 += generation[i].Team2.Data * ratio;
            }

            for (int i = 0; i < t1.Count; i++) {
                if (first.Team1.IsElementIndex(i)) {
                    t1[i] = (float) (Math.Round(t1[i] * 4) / 4);
                    t2[i] = (float) (Math.Round(t2[i] * 4) / 4);
                }
            }

            var d1 = first.Team1.Clone();
            d1.Data = t1;
            var d2 = first.Team2.Clone();
            d2.Data = t2;
            var newFitness = EvolutionBenchmark.CalculateFitness(game, d1, d2);

            return new Individual(d1, d2, newFitness);
        }
    }
}