using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HexMage.Simulator.Model {
    public struct MobInfo {
        public static readonly int NumberOfAbilities = 6;

        private static int _lastId = 0;

        public int MaxHp { get; set; }
        public int MaxAp { get; set; }
        public int DefenseCost { get; set; }
        public int Iniciative { get; set; }

        public List<int> Abilities { get; set; }
        public TeamColor Team { get; set; }
        public AxialCoord OrigCoord;

        public static int AbilityCount => 6;

        public MobInfo(TeamColor team, int maxHp, int maxAp, int defenseCost, int iniciative, List<int> abilities) {
            Team = team;
            MaxHp = maxHp;
            MaxAp = maxAp;
            DefenseCost = defenseCost;
            Iniciative = iniciative;
            Abilities = abilities;
#warning TODO - shouldn't this be initialized from the constructor?
            OrigCoord = AxialCoord.Zero;
        }
    }

    public struct MobInstance {
        public static readonly int InvalidId = -1;
        public int Id;
        public AxialCoord Coord;
        public int Hp;
        public int Ap;
        public List<Buff> Buffs { get; set; }

        public MobInstance(int id) : this() {
            Id = id;
            Buffs = new List<Buff>();
        }

        public MobInstance DeepCopy() {
            var copy = new MobInstance(Id) {
                Coord = Coord,
                Hp = Hp,
                Ap = Ap
            };

            var buffs = new List<Buff>();

            foreach (var buff in Buffs) {
                buffs.Add(buff);
            }

            copy.Buffs = buffs;

            return copy;
        }

        public override string ToString() {
            return $"{Hp}HP {Ap}AP";
        }
    }

    //public class Mobb : IResettable {
    //    public void Reset() {
    //        Buffs.Clear();
    //        Coord = OrigCoord;
    //        Hp = MaxHp;
    //        Ap = MaxAp;
    //        Metadata = null;
    //    }

    //    public Mob DeepCopy() {
    //        var abilitiesCopy = new List<AbilityId>();
    //        foreach (var ability in Abilities) {
    //            abilitiesCopy.Add(ability);
    //        }

    //        var copy = new Mob(Team, MaxHp, MaxAp, DefenseCost, Iniciative, abilitiesCopy);
    //        copy.Coord = Coord;
    //        copy.Metadata = null;

    //        foreach (var buff in Buffs) {
    //            copy.Buffs.Add(buff);
    //        }

    //        return copy;
    //    }
    //}
}