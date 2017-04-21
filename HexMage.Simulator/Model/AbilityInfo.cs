using System;
using Newtonsoft.Json;

namespace HexMage.Simulator.Model {
    // TODO - rename

#warning TODO - this should be a struct

    public class AbilityInfo : IDeepCopyable<AbilityInfo> {
        public int Dmg { get; set; }
        public int Cost { get; set; }
        public int Range { get; set; }
        public int Cooldown { get; set; }
        public Buff Buff { get; set; }
        public AreaBuff AreaBuff { get; set; }

        public float DmgCostRatio => (float) Dmg / (float) Cost;

        [JsonConstructor]
        [Obsolete]
#warning TODO: is this still needed?
        public AbilityInfo() { }

        public AbilityInfo(int dmg, int cost, int range, int cooldown)
            : this(dmg, cost, range, cooldown, Buff.ZeroBuff(), AreaBuff.ZeroBuff()) { }

        public AbilityInfo(int dmg, int cost, int range, int cooldown, Buff buff,
                           AreaBuff areaBuff) {
            Dmg = dmg;
            Cost = cost;
            Range = range;
            Cooldown = cooldown;
            Buff = buff;
            AreaBuff = areaBuff;
        }

        public AbilityInfo DeepCopy() {
            // Buff and AreaBuff are strucs, so they're copied automatically.
            var copy = new AbilityInfo(Dmg, Cost, Range, Cooldown, Buff, AreaBuff);
            return copy;
        }

#warning TODO - ulozit je do nejaky tabulky a jenom referencovat

        protected bool Equals(AbilityInfo other) {
            return Dmg == other.Dmg && Cost == other.Cost && Range == other.Range && Cooldown == other.Cooldown &&
                   Buff.Equals(other.Buff) && AreaBuff.Equals(other.AreaBuff);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AbilityInfo) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = Dmg;
                hashCode = (hashCode * 397) ^ Cost;
                hashCode = (hashCode * 397) ^ Range;
                hashCode = (hashCode * 397) ^ Cooldown;
                hashCode = (hashCode * 397) ^ Buff.GetHashCode();
                hashCode = (hashCode * 397) ^ AreaBuff.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(AbilityInfo left, AbilityInfo right) {
            return Equals(left, right);
        }

        public static bool operator !=(AbilityInfo left, AbilityInfo right) {
            return !Equals(left, right);
        }
    }
}