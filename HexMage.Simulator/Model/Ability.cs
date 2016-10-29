using System;
using System.CodeDom;
using System.Collections.Generic;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    // TODO - rename
    public enum AbilityElement {
        Earth,
        Fire,
        Air,
        Water
    }

#warning TODO - this should be a struct
    public class Ability {
        public int Id { get; set; }
        public int Dmg { get; set; }
        public int Cost { get; set; }
        public int Range { get; set; }
        public int Cooldown { get; set; }
        public AbilityElement Element { get; set; }
        public List<Buff> Buffs { get; set; }
        public List<AreaBuff> AreaBuffs { get; set; }

        public Ability() {}

        public Ability(int id, int dmg, int cost, int range, int cooldown, AbilityElement element)
            : this(id, dmg, cost, range, cooldown, element, new List<Buff>(), new List<AreaBuff>()) {
        }

        public Ability(int id, int dmg, int cost, int range, int cooldown, AbilityElement element, List<Buff> buffs,
            List<AreaBuff> areaBuffs) {
            Id = id;
            Dmg = dmg;
            Cost = cost;
            Range = range;
            Cooldown = cooldown;
            Element = element;
            Buffs = buffs;
            AreaBuffs = areaBuffs;
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

        //public Ability DeepCopy() {
        //    var buffsCopy = new List<Buff>();
        //    foreach (var buff in Buffs) {
        //        buffsCopy.Add(buff);
        //    }
        //    var areaBuffsCopy = new List<AreaBuff>();
        //    foreach (var areaBuff in AreaBuffs) {
        //        areaBuffsCopy.Add(areaBuff);
        //    }

        //    var copy = new Ability(Id, Dmg, Cost, Range, Cooldown, Element, buffsCopy, areaBuffsCopy);
        //    return copy;
        //}
    }
}