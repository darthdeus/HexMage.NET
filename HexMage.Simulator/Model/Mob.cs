using System;
using System.Collections.Generic;
using System.Linq;

namespace HexMage.Simulator.Model {
    public class Mob {
        public static readonly int NumberOfAbilities = 6;

        private static int _lastId = 0;
        public int Id { get; private set; }

        public int Hp { get; set; }
        public int Ap { get; set; }
        public int MaxHp { get; set; }
        public int MaxAp { get; set; }
        public int DefenseCost { get; set; }
        public int Iniciative { get; set; }

        public List<Ability> Abilities { get; set; }
        public Team Team { get; set; }
        public AxialCoord Coord { get; set; }
        public static int AbilityCount => 6;
        public object Metadata { get; set; }
        // TODO - should this maybe just be internal?
        public List<Buff> Buffs { get; set; } = new List<Buff>();

        public Mob(Team team, int maxHp, int maxAp, int defenseCost, int iniciative, List<Ability> abilities) {
            Team = team;
            MaxHp = maxHp;
            MaxAp = maxAp;
            DefenseCost = defenseCost;
            Iniciative = iniciative;
            Abilities = abilities;
            Hp = maxHp;
            Ap = maxAp;
            Coord = new AxialCoord(0, 0);
            Id = _lastId++;
        }

        public override string ToString() {
            return $"{Hp}/{MaxHp} {Ap}/{MaxAp}";
        }

        public float SpeedModifier => Buffs.Select(b => b.MoveSpeedModifier)
                                           .Aggregate(1.0f, (a, m) => a*(1/m));

        public int ModifiedDistance(int distance) {
            return (int) Math.Round(distance*SpeedModifier);
        }

        public Mob DeepCopy(Team teamCopy) {
            var abilitiesCopy = new List<Ability>();
            foreach (var ability in Abilities) {
                abilitiesCopy.Add(ability.DeepCopy());
            }

            var copy = new Mob(teamCopy, MaxHp, MaxAp, DefenseCost, Iniciative, abilitiesCopy);
            copy.Coord = Coord;
#warning TODO - check if metadata needs to be copied over (probably yes)
            copy.Metadata = Metadata;

            foreach (var buff in Buffs) {
                copy.Buffs.Add(buff.DeepCopy());
            }

            return copy;
        }
    }
}