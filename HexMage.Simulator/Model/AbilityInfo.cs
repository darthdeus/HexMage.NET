using System;
using System.Collections.Generic;

namespace HexMage.Simulator.Model {
    // TODO - rename
    public enum AbilityElement {
        Earth,
        Fire,
        Air,
        Water
    }

    public class AbilityInstance : IDeepCopyable<AbilityInstance> {
        public int CurrentCooldown { get; set; }
        private readonly AbilityInfo _abilityInfo;
        public AbilityInfo AbilityInfo => _abilityInfo;

        public int Dmg => _abilityInfo.Dmg;
        public int Cost => _abilityInfo.Cost;
        public int Range => _abilityInfo.Range;
        public int Cooldown => _abilityInfo.Cooldown;
        public AbilityElement Element => _abilityInfo.Element;
        public List<Buff> Buffs => _abilityInfo.Buffs;
        public List<AreaBuff> AreaBuffs => _abilityInfo.AreaBuffs;
        public Buff ElementalEffect => _abilityInfo.ElementalEffect;

        public AbilityInstance(AbilityInfo abilityInfo) {
            _abilityInfo = abilityInfo;
            CurrentCooldown = abilityInfo.Cooldown;
        }

        public AbilityInstance DeepCopy() {
            return (AbilityInstance) MemberwiseClone();
        }
    }

    public class AbilityInfo {
        public int Dmg { get; set; }
        public int Cost { get; set; }
        public int Range { get; set; }
        public int Cooldown { get; set; }
        public AbilityElement Element { get; set; }
        public List<Buff> Buffs { get; set; }
        public List<AreaBuff> AreaBuffs { get; set; }

        public AbilityInfo(int dmg, int cost, int range, int cooldown, AbilityElement element)
            : this(dmg, cost, range, cooldown, element, new List<Buff>(), new List<AreaBuff>()) {}

        public AbilityInfo(int dmg, int cost, int range, int cooldown, AbilityElement element, List<Buff> buffs,
                           List<AreaBuff> areaBuffs) {
            Dmg = dmg;
            Cost = cost;
            Range = range;
            Cooldown = cooldown;
            Element = element;
            Buffs = buffs;
            AreaBuffs = areaBuffs;
        }

        public AbilityInstance CreateInstance() {
            return new AbilityInstance(this);
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
                        return new Buff(AbilityElement.Water, 0, 0, 1, new List<AbilityElement>() {
                                            AbilityElement.Air
                                        });
                    default:
                        throw new InvalidOperationException("Invalid element type");
                }
            }
        }
    }
}