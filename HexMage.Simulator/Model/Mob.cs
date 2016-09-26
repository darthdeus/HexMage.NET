using System;
using System.Collections.Generic;
using System.Linq;
using HexMage.Simulator;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.Model {
    public class Mob : IResettable {
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
        public TeamColor Team { get; set; }
        public AxialCoord Coord { get; set; }
        public AxialCoord OrigCoord { get; set; }
        public static int AbilityCount => 6;
        public object Metadata { get; set; }
        public List<Buff> Buffs { get; set; } = new List<Buff>();

        public Mob(TeamColor team, int maxHp, int maxAp, int defenseCost, int iniciative, List<Ability> abilities) {
            Team = team;
            MaxHp = maxHp;
            MaxAp = maxAp;
            DefenseCost = defenseCost;
            Iniciative = iniciative;
            Abilities = abilities;
            Hp = maxHp;
            Ap = maxAp;
            Coord = new AxialCoord(0, 0);
            OrigCoord = Coord;
            Id = _lastId++;
        }

        public override string ToString() {
            return $"{Hp}/{MaxHp} {Ap}/{MaxAp}";
        }

        public void Reset() {
            Buffs.Clear();
            Coord = OrigCoord;
            Hp = MaxHp;
            Ap = MaxAp;
            Metadata = null;
        }

        public float SpeedModifier => Buffs.Select(b => b.MoveSpeedModifier)
                                           .Aggregate(1.0f, (a, m) => a*(1/m));

        public int ModifiedDistance(int distance) {
            return (int) Math.Round(distance*SpeedModifier);
        }

        public Mob DeepCopy() {
            var abilitiesCopy = new List<Ability>();
            foreach (var ability in Abilities) {
                abilitiesCopy.Add(ability.DeepCopy());
            }

            var copy = new Mob(Team, MaxHp, MaxAp, DefenseCost, Iniciative, abilitiesCopy);
            copy.Coord = Coord;
            copy.Metadata = null;

            foreach (var buff in Buffs) {
                copy.Buffs.Add(buff.DeepCopy());
            }

            return copy;
        }
    }
}