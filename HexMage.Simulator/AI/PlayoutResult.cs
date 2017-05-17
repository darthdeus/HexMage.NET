using System;
using MathNet.Numerics.Distributions;

namespace HexMage.Simulator.AI {
    /// <summary>
    /// Represents a result of a playout, used in ES.
    /// </summary>
    public struct PlayoutResult {
        public int TotalTurns;
        public float HpPercentage;
        public bool AllPlayed;
        public readonly bool Timeout;
        public int RedWins;
        public int BlueWins;

        public PlayoutResult(int totalTurns, float hpPercentage, bool allPlayed, bool timeout, int redWins,
                             int blueWins) {
            HpPercentage = hpPercentage;
            TotalTurns = totalTurns;
            AllPlayed = allPlayed;
            Timeout = timeout;
            RedWins = redWins;
            BlueWins = blueWins;
        }

        public string ToFitnessString(DNA dna, float fitness) {
            string fstr = fitness.ToString("0.000000");

            return $"F:{fstr}\t{dna.ToDnaString()}";
        }

        public float SimpleFitness() {
            float fitA = 1 - HpPercentage;
            float fitB = (float) LengthSample(TotalTurns);

            float fitness = (fitA + fitB) / 2;

            if (!AllPlayed) {
                fitness = 0.0001f;
            }

            return fitness;
        }


        public static double LengthSample(double x) {
            const double ev = 10;

            if (x < 2 * ev) {
                return new Normal(ev, 3).CumulativeDistribution(x);
            } else {
                return new Normal(ev, 2).CumulativeDistribution(4 * ev - x);
            }
        }
    }
}