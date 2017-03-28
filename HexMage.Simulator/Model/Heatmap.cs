namespace HexMage.Simulator {
    public class Heatmap {
        // TODO - make these properties
        public HexMap<int> Map;
        public int Size;
        public int MaxValue;
        public int MinValue;

        public Heatmap(int size) {
            Size = size;
            Map = new HexMap<int>(size);
            MaxValue = 0;
            MinValue = int.MaxValue;
        }
    }
}