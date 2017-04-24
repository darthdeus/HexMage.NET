using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Pathfinding;
using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;

namespace HexMage.Simulator.AI {
    public class Evolution {
        private readonly bool _keepCounter;
        private readonly int _maxGoodCount;
        private readonly bool _breakWhenFound;
        public int GoodCount = 0;

        public Evolution(bool keepCounter, int maxGoodCount, bool breakWhenFound) {
            _keepCounter = keepCounter;
            _maxGoodCount = maxGoodCount;
            _breakWhenFound = breakWhenFound;
        }

        public void RunEvolutionStrategies(DNA initialDna, bool evolveTeam1 = true) {
            if (!_keepCounter) {
                GoodCount = 0;
            }

            var t1 = initialDna;
            var t2 = t1.Clone();
            if (evolveTeam1) {
                t1.Randomize();
            }
            t2.Randomize();

            var map = Map.Load("data/map.json");
            Console.WriteLine($"Initial ({t1.Data.Count}): {t1.ToDnaString()}\n\n");

            var game = GameSetup.GenerateFromDna(t1, t2, map);

            int restartCount = 0;

            var current = new Individual(t1.Clone(),
                                         t2.Clone(),
                                         EvolutionBenchmark.CalculateFitness(game, t1, t2));

            List<double> plotT = new List<double>();
            List<double> plotFit = new List<double>();
            List<double> plotHpPercentage = new List<double>();
            List<double> plotLength = new List<double>();
            List<double> plotDistance = new List<double>();

            var gameCopies = Enumerable.Range(0, Constants.TeamsPerGeneration)
                                       .AsParallel()
                                       .Select(_ => game.DeepCopy())
                                       .ToList();

            int i;
            for (i = 0; i < Constants.NumGenerations; i++) {
                var genWatch = new Stopwatch();
                genWatch.Start();

                var teamWatch = new Stopwatch();
                teamWatch.Start();

                if (Constants.RestartFailures && current.CombinedFitness() < Constants.FitnessThreshold) {
                    if (evolveTeam1) {
                        current.Team1.Randomize();
                    }
                    current.Team2.Randomize();
                    restartCount++;
                }

                var generation = Enumerable.Range(0, Constants.TeamsPerGeneration)
                                           .AsParallel()
                                           .Select(j => {
                                               var newTeam1 =
                                                   evolveTeam1
                                                       ? EvolutionBenchmark.Mutate(current.Team1)
                                                       : current.Team1;
                                               var newTeam2 = EvolutionBenchmark.Mutate(current.Team2);

                                               var newFitness =
                                                   EvolutionBenchmark.CalculateFitness(
                                                       gameCopies[j], newTeam1, newTeam2);

                                               return new Individual(newTeam1, newTeam2, newFitness);
                                           })
                                           .ToList();

                var previous = current;
                current = EvolutionStrategy(game, previous, generation, evolveTeam1);


                plotT.Add(i);
                plotFit.Add(current.CombinedFitness());
                plotHpPercentage.Add(1 - current.Result.HpPercentage);
                plotLength.Add(PlayoutResult.LengthSample(current.Result.TotalTurns));
                plotDistance.Add(current.Team1.DistanceFitness(current.Team2));

                if (i % Constants.EvolutionPrintModulo == 0) {
                    Console.WriteLine($"T: {i}\t\t" +
                                      $"F: {previous.CombinedFitness().ToString("0.0000")}" +
                                      $" -> {current.CombinedFitness().ToString("0.0000")}");
                }

                if (Constants.SaveGoodOnes && current.CombinedFitness() > 0.95) {
                    GoodCount++;
                    EvolutionBenchmark.SaveDna(GoodCount, current.Team1, current.Team2);

                    Console.WriteLine($"Found extra good {current.CombinedFitness()}, restarting");

                    if (evolveTeam1) {
                        current.Team1.Randomize();
                    }
                    current.Team2.Randomize();

                    if (GoodCount >= _maxGoodCount) {
                        Console.WriteLine($"Stopping evolution early, reached target {_maxGoodCount} good matches.");
                        break;
                    }
                }
            }

            Console.WriteLine($"Restarts: {restartCount}");

            if (Constants.GnuPlot) {
                GnuPlot.HoldOn();
                GnuPlot.Set($"xrange [0:{i}] reverse",
                            $"title '{Constants.NumGenerations} generations, T_s = {Constants.InitialT}",
                            //"yrange [0:1]",
                            //"style data lines",
                            "key tmargin center horizontal");
                GnuPlot.Plot(plotT.ToArray(), plotFit.ToArray(), $"title 'Fitness {Constants.NumGenerations}gen'");
                GnuPlot.Plot(plotT.ToArray(), plotHpPercentage.ToArray(), $"title 'HP percentage'");
                GnuPlot.Plot(plotT.ToArray(), plotLength.ToArray(), "title 'Game length'");
                GnuPlot.Plot(plotT.ToArray(), plotDistance.ToArray(), "title 'Team difference'");
                Console.ReadKey();
            }
        }

        public static Individual EvolutionStrategy(GameInstance game, Individual previous, List<Individual> generation,
                                                   bool evolveTeam1) {
            float totalFitness = generation.Sum(i => i.CombinedFitness());

            var first = generation[0];

            Vector<float> t1 = first.Team1.Data * (first.CombinedFitness() / totalFitness);
            Vector<float> t2 = first.Team2.Data * (first.CombinedFitness() / totalFitness);

            for (int i = 1; i < generation.Count; i++) {
                var ratio = generation[i].CombinedFitness() / totalFitness;
                t1 += generation[i].Team1.Data * ratio;
                t2 += generation[i].Team2.Data * ratio;
            }

            var d1 = first.Team1.Clone();
            d1.Data = t1;
            var d2 = first.Team2.Clone();
            d2.Data = t2;

            if (!evolveTeam1) {
                d1.Data = previous.Team1.Data;
            }

            var newFitness = EvolutionBenchmark.CalculateFitness(game, d1, d2);

            return new Individual(d1, d2, newFitness);
        }
    }
}