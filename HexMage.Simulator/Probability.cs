using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.PCG;

namespace HexMage.Simulator {
    /// <summary>
    /// A collection of probability helpers.
    /// </summary>
    public static class Probability {
        /// <summary>
        /// Coin flip of a given percentage.
        /// </summary>
        public static bool Uniform(double percentage) {
            return Generator.Random.NextDouble() < percentage;
        }

        /// <summary>
        /// Calculating the value of a normal distribution.
        /// </summary>
        public static double Norm(double x) {
            return Math.Exp(-(x - Constants.Mu) * (x - Constants.Mu)
                            / (2 * Constants.Sigma * Constants.Sigma))
                   / Math.Sqrt(2 * Constants.Sigma * Constants.Sigma);
        }

        public static double Exponential(double lambda, double u) {
            return Math.Log(1 - u) / -lambda;
        }

        public static double Exponential(double lambda) {
            double u = Generator.Random.NextDouble();
            return Exponential(lambda, u);
        }

        /// <summary>
        /// Picks from a list of elements where each element has a defined probability.
        /// </summary>
        public static T UniformPick<T>(List<T> items, IList<double> probabilities) {
            Debug.Assert(items.Count > 0, "items.Count > 0");
            Debug.Assert(items.Count == probabilities.Count, "items.Count == probabilities.Count");
            Debug.Assert(Math.Abs(probabilities.Sum() - 1) < 0.001, "Math.Abs(probabilities.Sum() - 1) < 0.001");

            double value = Generator.Random.NextDouble();

            double min = 0;
            double max = 0;
            for (int i = 0; i < probabilities.Count; i++) {
                min = max;
                max = min + probabilities[i];

                if (min <= value && value <= max) {
                    return items[i];
                }
            }

            return items[items.Count - 1];
        }
    }
}