using System;
using System.CodeDom;
using System.Collections.Generic;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    // TODO - rename

#warning TODO - this should be a struct
    public class Ability {
        public int Dmg { get; set; }
        public int Cost { get; set; }
        public int Range { get; set; }
        public int Cooldown { get; set; }
        public AbilityElement Element { get; set; }
        public Buff Buff { get; set; }
        public AreaBuff AreaBuff { get; set; }

        public Ability() {}

        public Ability(int dmg, int cost, int range, int cooldown, AbilityElement element)
            : this(dmg, cost, range, cooldown, element, Buff.ZeroBuff(), AreaBuff.ZeroBuff()) {}

        public Ability(int dmg, int cost, int range, int cooldown, AbilityElement element, Buff buff,
                       AreaBuff areaBuff) {
            Dmg = dmg;
            Cost = cost;
            Range = range;
            Cooldown = cooldown;
            Element = element;
            Buff = buff;
            AreaBuff = areaBuff;
        }

#warning TODO - ulozit je do nejaky tabulky a jenom referencovat
        [JsonIgnore]
        public Buff ElementalEffect {
            get {
                switch (Element) {
                    case AbilityElement.Earth:
                        return new Buff(AbilityElement.Earth, 0, 0, 1);
                    case AbilityElement.Fire:
                        return new Buff(AbilityElement.Fire, -1, 0, 2);
                    case AbilityElement.Air:
                        return new Buff(AbilityElement.Air, 0, 0, 1);
                    case AbilityElement.Water:
                        return new Buff(AbilityElement.Water, 0, 0, 1);
                    default:
                        throw new InvalidOperationException("Invalid element type");
                }
            }
        }

        protected bool Equals(Ability other) {
            return Dmg == other.Dmg && Cost == other.Cost && Range == other.Range && Cooldown == other.Cooldown &&
                   Element == other.Element && Buff.Equals(other.Buff) && AreaBuff.Equals(other.AreaBuff);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Ability) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = Dmg;
                hashCode = (hashCode * 397) ^ Cost;
                hashCode = (hashCode * 397) ^ Range;
                hashCode = (hashCode * 397) ^ Cooldown;
                hashCode = (hashCode * 397) ^ (int) Element;
                hashCode = (hashCode * 397) ^ Buff.GetHashCode();
                hashCode = (hashCode * 397) ^ AreaBuff.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Ability left, Ability right) {
            return Equals(left, right);
        }

        public static bool operator !=(Ability left, Ability right) {
            return !Equals(left, right);
        }
    }
}