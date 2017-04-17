using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Benchmarks;

namespace HexMage.Simulator.AI {
    public class Evolution<T> {
        private readonly Func<T, float> _fitnessFunc;
        public static readonly RollingAverage AverageGenerationTime = new RollingAverage();

        public Evolution(Func<T, float> fitnessFunc) {
            _fitnessFunc = fitnessFunc;
        }

        public void RunSimulatedAnnealing() {
            var plotT = new List<double>();
            var plotFit = new List<double>();

            GameInstance game = null;
            DNA initialDna = null;

            int restartCount = 0;

#warning TODO: initialize
            var current = new GenerationMember();

            var generationStopwatch = new Stopwatch();
            for (int i = 0; i < Constants.NumGenerations; i++) {
                generationStopwatch.Restart();

                float tpercentage = Math.Max(0, 1 - (float) i / Constants.NumGenerations);
                float T = Constants.InitialT * tpercentage;

                if (Constants.RestartFailures && current.result.SimpleFitness() < Constants.FitnessThreshold) {
                    current.dna.Randomize();
                    restartCount++;
                }

                var mutated = EvolutionBenchmark.Mutate(current.dna, T);
                var evaluated = EvolutionBenchmark.CalculateFitness(game, initialDna, mutated);

                generationStopwatch.Stop();
                AverageGenerationTime.Add(generationStopwatch.Elapsed.TotalMilliseconds);

            }
        }
    }
}