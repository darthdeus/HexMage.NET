using HexMage.Simulator.PCG;

namespace HexMage.Simulator
{
    public static class Probability {
        // TODO - pravdepodpobnostni abstrakcio - hozeni kostkou, norm, apod, expo/poiss?
        public static bool Uniform(double percentage) {
            return Generator.Random.NextDouble() < percentage;
        }
    }
}