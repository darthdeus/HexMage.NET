using System.Collections.Generic;
using System.Threading.Tasks;

namespace HexMage.Simulator
{
    public class Buff {
        public int HpChange { get; set; }
        public int ApChange { get; set; }
        public int Lifetime { get; set; }

        public Buff(int hpChange, int apChange, int lifetime) {
            HpChange = hpChange;
            ApChange = apChange;
            Lifetime = lifetime;
        }
    }

    public class Mob
    {
        public static readonly int NumberOfAbilities = 6;

        private static int _lastId = 0;
        public int ID { get; set; }

        public int HP { get; set; }
        public int AP { get; set; }
        public int MaxHP { get; set; }
        public int MaxAP { get; set; }
        public int DefenseCost { get; set; }

        public List<Ability> Abilities { get; set; }
        public Team Team { get; set; }
        public AxialCoord Coord { get; set; }
        public static int AbilityCount => 6;
        public object Metadata { get; set; }
        // TODO - should this maybe just be internal?
        public List<Buff> Buffs { get; set; } = new List<Buff>();

        public Mob(Team team, int maxHp, int maxAp, int defenseCost, List<Ability> abilities) {
            Team = team;
            MaxHP = maxHp;
            MaxAP = maxAp;
            DefenseCost = defenseCost;
            Abilities = abilities;
            HP = maxHp;
            AP = maxAp;
            Coord = new AxialCoord(0, 0);
            ID = _lastId++;

            team.Mobs.Add(this);
        }

        public override string ToString() {
            return $"{HP}/{MaxHP} {AP}/{MaxAP}";
        }
    }
}