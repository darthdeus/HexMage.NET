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

        public override string ToString() {
            return
                $"{nameof(Element)}: {Element}, {nameof(HpChange)}: {HpChange}, {nameof(ApChange)}: {ApChange}, {nameof(Lifetime)}: {Lifetime}";
        }
    }
}