using System.Collections.Generic;

namespace HexMage.Simulator {
    public enum AbilityElement {
        Earth,
        Fire,
        Air,
        Water
    }

    public class AreaBuff {
        public int Radius { get; set; }
        public Buff Effect { get; set; }

        public AreaBuff(int radius, Buff effect) {
            Radius = radius;
            Effect = effect;
        }
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
    }
}