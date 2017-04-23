using System;
using Newtonsoft.Json;

namespace HexMage.Simulator.Model {
    public struct Buff {
        public readonly int HpChange;
        public readonly int ApChange;
        public int Lifetime;
        
        [JsonIgnore]
        public bool IsZero => Lifetime == 0;

        public Buff(int hpChange, int apChange, int lifetime) {
            HpChange = hpChange;
            ApChange = apChange;
            Lifetime = lifetime;
        }

        public static Buff ZeroBuff() {
            return new Buff(0, 0, 0);
        }

        public static Buff Combine(Buff a, Buff b) {
            if (a.IsZero) return b;
            if (b.IsZero) return a;

            return new Buff(a.HpChange + b.HpChange, a.ApChange + b.ApChange,
                            Math.Max(a.Lifetime, b.Lifetime));
        }

        public override string ToString() {
            return
                $"{nameof(HpChange)}: {HpChange}, {nameof(ApChange)}: {ApChange}, {nameof(Lifetime)}: {Lifetime}";
        }

        public bool Equals(Buff other) {
            return HpChange == other.HpChange && ApChange == other.ApChange &&
                   Lifetime == other.Lifetime;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Buff && Equals((Buff) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = HpChange;
                hashCode = (hashCode * 397) ^ ApChange;
                // TODO: nemuze tohle zpusobovat nejaky problemy?
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