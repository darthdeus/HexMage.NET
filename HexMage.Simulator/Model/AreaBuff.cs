namespace HexMage.Simulator.Model {
    public struct AreaBuff {
        public AxialCoord Coord;
        public int Radius;
        public Buff Effect;

        public AreaBuff(AxialCoord coord, int radius, Buff effect) {
            Coord = coord;
            Radius = radius;
            Effect = effect;
        }

        public void DecreaseLifetime() {
            var copy = Effect;
            copy.Lifetime--;
            Effect = copy;
        }

        public override string ToString() {
            return $"{nameof(Radius)}: {Radius}, {nameof(Effect)}: {Effect}";
        }
    }
}