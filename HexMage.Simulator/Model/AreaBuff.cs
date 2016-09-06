namespace HexMage.Simulator {
    public class AreaBuff {
        public int Radius { get; set; }

        public Buff Effect { get; set; }

        public AreaBuff(int radius, Buff effect) {
            Radius = radius;
            Effect = effect;
        }

        public override string ToString() {
            return $"{nameof(Radius)}: {Radius}, {nameof(Effect)}: {Effect}";
        }
    }
}