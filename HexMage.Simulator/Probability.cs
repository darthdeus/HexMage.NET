using System;
using HexMage.Simulator.PCG;

namespace HexMage.Simulator {
    public static class Probability {
        // TODO - pravdepodpobnostni abstrakcio - hozeni kostkou, norm, apod, expo/poiss?
        public static bool Uniform(double percentage) {
            return Generator.Random.NextDouble() < percentage;
        }

        public static double Norm(double x) {
            const double mu = 10;
            const double sigma = 3;
            return Math.Exp(-(x - mu) * (x - mu) / (2 * sigma * sigma)) / Math.Sqrt(2 * sigma * sigma);
        }
    }
}