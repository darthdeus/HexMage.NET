using System.Collections.Generic;

namespace HexMage.Simulator
{
    public class Mob
    {
        private static int _lastId = 0;
        public int ID { get; set; }

        public int HP { get; set; }
        public int AP { get; set; }
        public int MaxHP { get; set; }
        public int MaxAP { get; set; }

        public List<Ability> Abilities { get; set; }
        public Team Team { get; set; }
        public AxialCoord Coord { get; set; }
        public static int AbilityCount => 6;

        public Mob(Team team, int maxHp, int maxAp, List<Ability> abilities) {
            Team = team;
            MaxHP = maxHp;
            MaxAP = maxAp;
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