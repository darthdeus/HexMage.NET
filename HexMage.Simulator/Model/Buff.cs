using System;
using System.Collections.Generic;

namespace HexMage.Simulator.Model {
    public class Buff : IDeepCopyable<Buff> {
        public AbilityElement Element { get; set; }
        public int HpChange { get; set; }
        public int ApChange { get; set; }
        public int Lifetime { get; set; }
        public float MoveSpeedModifier { get; set; }
        public List<AbilityElement> DisabledElements { get; set; }

        public Buff(AbilityElement element, int hpChange, int apChange, int lifetime) :
            this(element, hpChange, apChange, lifetime, 1, new List<AbilityElement>()) {}

        public Buff(AbilityElement element, int hpChange, int apChange, int lifetime, float moveSpeedModifier) :
            this(element, hpChange, apChange, lifetime, moveSpeedModifier, new List<AbilityElement>()) {}

        public Buff(AbilityElement element, int hpChange, int apChange, int lifetime, float moveSpeedModifier,
                    List<AbilityElement> disabledElements) {
            Element = element;
            HpChange = hpChange;
            ApChange = apChange;
            Lifetime = lifetime;
            MoveSpeedModifier = moveSpeedModifier;
            DisabledElements = disabledElements;
        }

        // TODO - maybe replace with struct instead?
        [Obsolete]
        public Buff Clone() {
            return (Buff) MemberwiseClone();
        }

        public Buff DeepCopy() {
            var disabledElementsCopy = new List<AbilityElement>();

            foreach (var element in DisabledElements) {
                disabledElementsCopy.Add(element);
            }
            return new Buff(Element, HpChange, ApChange, Lifetime, MoveSpeedModifier, disabledElementsCopy);
        }

        public override string ToString() {
            return
                $"{nameof(Element)}: {Element}, {nameof(HpChange)}: {HpChange}, {nameof(ApChange)}: {ApChange}, {nameof(Lifetime)}: {Lifetime}, {nameof(MoveSpeedModifier)}: {MoveSpeedModifier}";
        }
    }
}