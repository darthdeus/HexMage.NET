using System;

namespace HexMage.Simulator.Model {
    public struct Buff {
        public readonly AbilityElement Element;
        public readonly int HpChange;
        public readonly int ApChange;
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

        public static Buff Combine(Buff a, Buff b) {
            if (a.IsZero) return b;
            if (b.IsZero) return a;
            
            if (a.Element == b.Element) {
                return new Buff(a.Element, a.HpChange + b.HpChange, a.ApChange + b.ApChange,
                    Math.Max(a.Lifetime, b.Lifetime));
            } else {
                return b;
            }
        }

        public bool IsZero => Lifetime == 0;

        public override string ToString() {
            return
                $"{nameof(Element)}: {Element}, {nameof(HpChange)}: {HpChange}, {nameof(ApChange)}: {ApChange}, {nameof(Lifetime)}: {Lifetime}";
        }

        public bool Equals(Buff other) {
            return Element == other.Element && HpChange == other.HpChange && ApChange == other.ApChange &&
                   Lifetime == other.Lifetime;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Buff && Equals((Buff) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = (int) Element;
                hashCode = (hashCode * 397) ^ HpChange;
                hashCode = (hashCode * 397) ^ ApChange;
                hashCode = (hashCode * 397) ^ Lifetime;
                return hashCode;
            }
        }

        public static bool operator ==(Buff left, Buff right) {
            return left.Equals(right);
        }

        public static bool operator !=(Buff left, Buff right) {
            return !left.Equals(right);
        }
    }
}