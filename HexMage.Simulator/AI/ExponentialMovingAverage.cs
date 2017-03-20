namespace HexMage.Simulator {
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

        public static ExponentialMovingAverage Instance = new ExponentialMovingAverage(0.9);
    }
}