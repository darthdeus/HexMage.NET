using System;
using System.Collections.Generic;

namespace HexMage.Simulator.Model {
    public class Buff : IDeepCopyable<Buff> {
        public AbilityElement Element { get; set; }
        public int HpChange { get; set; }
        public int ApChange { get; set; }
        public int Lifetime { get; set; }

        public Buff(AbilityElement element, int hpChange, int apChange, int lifetime) {
            Element = element;
            HpChange = hpChange;
            ApChange = apChange;
            Lifetime = lifetime;
        }

        public Buff DeepCopy() {
            return new Buff(Element, HpChange, ApChange, Lifetime);
        }

        public override string ToString() {
            return
                $"{nameof(Element)}: {Element}, {nameof(HpChange)}: {HpChange}, {nameof(ApChange)}: {ApChange}, {nameof(Lifetime)}: {Lifetime}";
        }
    }
}