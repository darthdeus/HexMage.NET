using System;
using System.CodeDom;
using System.Collections.Generic;

namespace HexMage.Simulator {
    // TODO - rename
    public enum AbilityElement {
        Earth,
        Fire,
        Air,
        Water
    }

    public class Ability {
        public int Dmg { get; set; }
        public int Cost { get; set; }
        public int Range { get; set; }
        public int Cooldown { get; set; }
        public int CurrentCooldown { get; set; }
        public AbilityElement Element { get; set; }
        public List<Buff> Buffs { get; set; }
        public List<AreaBuff> AreaBuffs { get; set; }

        public Ability(int dmg, int cost, int range, int cooldown, AbilityElement element)
            : this(dmg, cost, range, cooldown, element, new List<Buff>(), new List<AreaBuff>()) {}

        public Ability(int dmg, int cost, int range, int cooldown, AbilityElement element, List<Buff> buffs,
                       List<AreaBuff> areaBuffs) {
            Dmg = dmg;
            Cost = cost;
            Range = range;
            Cooldown = cooldown;
            Element = element;
            Buffs = buffs;
            AreaBuffs = areaBuffs;
            CurrentCooldown = 0;
        }

        public Buff ElementalEffect {
            get {
                switch (Element) {
                    case AbilityElement.Earth:
                        return new Buff(AbilityElement.Earth, 0, 0, 1, 0.5f);
                    case AbilityElement.Fire:
                        return new Buff(AbilityElement.Fire, -1, 0, 2);
                    case AbilityElement.Air:
                        return new Buff(AbilityElement.Air, 0, 0, 1, 2f);
                    case AbilityElement.Water:
                        return new Buff(AbilityElement.Water, 0, 0, 1, 1, new List<AbilityElement>() {
                                            AbilityElement.Air
                                        });
                    default:
                        throw new InvalidOperationException("Invalid element type");
                }
            }
        }
    }
}