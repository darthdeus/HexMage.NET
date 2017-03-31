namespace HexMage.Simulator.AI {
    public struct EvaluationResult {
        public int RedWins;
        public int BlueWins;
        public int Timeouts;
        public int TotalTurns;
        public long TotalElapsedMilliseconds;
        public int TotalIterations;
        public float HpFitness;

        public double MillisecondsPerIteration => (double) TotalElapsedMilliseconds / (double) TotalIterations;
        public double MillisecondsPerTurn => (double) TotalElapsedMilliseconds / (double) TotalTurns;
        public double WinPercentage => ((double) RedWins) / (double) TotalTurns;

        public override string ToString() {
            return $"{RedWins}/{BlueWins} (draws: {Timeouts}), total: {TotalTurns}";
        }
    }
}