namespace HexMage.Simulator.Model {
    public struct AreaBuff {
        public AxialCoord Coord { get; set; }
        public int Radius { get; set; }
        public Buff Effect { get; set; }

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