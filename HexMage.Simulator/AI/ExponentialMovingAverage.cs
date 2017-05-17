namespace HexMage.Simulator.AI {
    /// <summary>
    /// A simple exponential moving average with variable parameters.
    /// </summary>
    public class ExponentialMovingAverage {
        private readonly double _alpha;
        public double? CurrentValue;

        public ExponentialMovingAverage(double alpha) {
            this._alpha = alpha;
        }

        public double Average(double value) {
            if (CurrentValue == null) {
                CurrentValue = value;
                return value;
            }
            double newValue = CurrentValue.Value + _alpha * (value - CurrentValue.Value);
            CurrentValue = newValue;
            return newValue;
        }

        public static readonly ExponentialMovingAverage Instance = new ExponentialMovingAverage(0.9);
    }
}