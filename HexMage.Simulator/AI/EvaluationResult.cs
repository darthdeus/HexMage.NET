namespace HexMage.Simulator.AI {
    public struct EvaluationResult {
        public int RedWins;
        public int BlueWins;
        public int Timeouts;
        public int TotalTurns;
        public long TotalElapsedMilliseconds;
        public int TotalIterations;
        public float HpFitness;
        public bool Timeouted;

        public double MillisecondsPerIteration => (double) TotalElapsedMilliseconds / (double) TotalIterations;
        public double MillisecondsPerTurn => (double) TotalElapsedMilliseconds / (double) TotalTurns;
        public double WinPercentage => ((double) RedWins) / (double) TotalTurns;

        public override string ToString() {
            return $"{RedWins}/{BlueWins} (draws: {Timeouts}), total: {TotalTurns}";
        }

        public string ToFitnessString(DNA dna) {
            string fstr = HpFitness.ToString("0.00");
            string wstr = WinPercentage.ToString("0.0");

            return $"F:{fstr}\tW:{wstr}\t{dna.ToDNAString()}";

        }
    }
}