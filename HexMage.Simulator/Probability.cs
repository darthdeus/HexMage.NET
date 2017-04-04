using System;
using HexMage.Simulator.PCG;

namespace HexMage.Simulator {
    public static class Probability {
        // TODO - pravdepodpobnostni abstrakcio - hozeni kostkou, norm, apod, expo/poiss?
        public static bool Uniform(double percentage) {
            return Generator.Random.NextDouble() < percentage;
        }

        public static double Norm(double x) {
            return Math.Exp(-(x - Constants.Mu) * (x - Constants.Mu)
                            / (2 * Constants.Sigma * Constants.Sigma))
                   / Math.Sqrt(2 * Constants.Sigma * Constants.Sigma);
        }

        public static double Exponential(double lambda, double u) {
            // TODO - otestovat, co to vlastne ma delat :)
            return Math.Log(1 - u) / -lambda;
        }

        public static double Exponential(double lambda) {
            double u = Generator.Random.NextDouble();
            return Exponential(lambda, u);
        }
    }
}