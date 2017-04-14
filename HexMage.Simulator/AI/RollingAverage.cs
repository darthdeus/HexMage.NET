namespace HexMage.Simulator.AI {
    public class RollingAverage {
        private double _total = 0;
        private int _count = 0;

        public double Average {
            get {
                lock (this) return _total / _count;
            }
        }

        public void Add(double value) {
            lock (this) {
                _total += value;
                _count++;
            }
        }
    }
}