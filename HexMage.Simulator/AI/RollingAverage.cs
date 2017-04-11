namespace HexMage.Simulator.AI {
    // TODO: make threadsafe
    public class RollingAverage {
        public double Total = 0;
        public int Count = 0;

        public double Average => Total / Count;

        public void Add(double value) {
            Total += value;
            Count++;
        }
    }
}