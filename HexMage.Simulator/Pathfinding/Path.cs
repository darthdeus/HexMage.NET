namespace HexMage.Simulator
{
    public class Path
    {
        public AxialCoord? Source { get; set; }
        public VertexState State { get; set; }
        public int Distance { get; set; }
        public bool Reachable { get; set; }

        public override string ToString() {
            return $"{Source} - {State} - {Distance}";
        }
    }
}