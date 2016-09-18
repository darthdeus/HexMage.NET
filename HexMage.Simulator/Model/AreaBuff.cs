using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class AreaBuff : IDeepCopyable<AreaBuff> {
        public int Radius { get; set; }

        public Buff Effect { get; set; }

        public AreaBuff(int radius, Buff effect) {
            Radius = radius;
            Effect = effect;
        }

        public AreaBuff DeepCopy() {
            return new AreaBuff(Radius, Effect.DeepCopy());
        }

        public override string ToString() {
            return $"{nameof(Radius)}: {Radius}, {nameof(Effect)}: {Effect}";
        }
    }
}