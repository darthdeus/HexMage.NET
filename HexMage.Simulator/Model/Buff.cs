namespace HexMage.Simulator.Model {
    public struct Buff {
        public AbilityElement Element;
        public int HpChange;
        public int ApChange;
        public int Lifetime;

        public Buff(AbilityElement element, int hpChange, int apChange, int lifetime) {
            Element = element;
            HpChange = hpChange;
            ApChange = apChange;
            Lifetime = lifetime;
        }

        public static Buff ZeroBuff() {
            return new Buff(AbilityElement.Fire, 0, 0, 0);
        }

        public bool IsZero => Lifetime == 0;

        public override string ToString() {
            return
                $"{nameof(Element)}: {Element}, {nameof(HpChange)}: {HpChange}, {nameof(ApChange)}: {ApChange}, {nameof(Lifetime)}: {Lifetime}";
        }
    }
}