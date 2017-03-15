namespace HexMage.Simulator.Model {
    public struct AreaBuff {
        public AxialCoord Coord;
        public int Radius;
        public Buff Effect;

        public static AreaBuff ZeroBuff() {
            return new AreaBuff(AxialCoord.Zero, 0, Buff.ZeroBuff());
        }

        public AreaBuff(AxialCoord coord, int radius, Buff effect) {
            Coord = coord;
            Radius = radius;
            Effect = effect;
        }

        public bool IsZero => Radius == 0;

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