﻿namespace HexMage.Simulator.AI {
    public struct EvaluationResult {
        public int RedWins;
        public int BlueWins;
        public int Timeouts;
        public int TotalGames;
        public int TotalTurns;
        public long TotalElapsedMilliseconds;
        public int TotalIterations;
        public float TotalHpPercentage;
        public int TotalHp;
        public bool Timeouted;

        public bool Tainted;

        public float Fitness {
            get {
                float result = 1 - TotalHpPercentage;

                if (Constants.FitnessGameLength) {
                    result *= (float)Probability.Norm(TotalTurns);
                }

                return result;
            }
        }

        public double MillisecondsPerIteration => (double) TotalElapsedMilliseconds / (double) TotalIterations;
        public double MillisecondsPerTurn => (double) TotalElapsedMilliseconds / (double) TotalGames;
        public double WinPercentage => ((double) RedWins) / (double) TotalGames;

        public override string ToString() {
            return $"{RedWins}/{BlueWins} (draws: {Timeouts}), total: {TotalGames}";
        }

        public string ToFitnessString(DNA dna) {
            string fstr = Fitness.ToString("0.00");
            string wstr = WinPercentage.ToString("0.0");

            return $"F:{fstr}\tW:{wstr}\t{dna.ToDNAString()}";
        }
    }
}
