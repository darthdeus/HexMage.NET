using System.Collections.Generic;

namespace HexMage.Simulator.Model {
    public struct MobId {
        public static MobId Invalid = new MobId(-1);
        public int Id;

        public MobId(int id) {
            Id = id;
        }

        public bool Equals(MobId other) {
            return Id == other.Id;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MobId && Equals((MobId) obj);
        }

        public override int GetHashCode() {
            return Id;
        }

        public static bool operator ==(MobId left, MobId right) {
            return left.Equals(right);
        }

        public static bool operator !=(MobId left, MobId right) {
            return !left.Equals(right);
        }
    }

    public struct MobInfo {
        public static readonly int NumberOfAbilities = 6;

        private static int _lastId = 0;
        public int Id { get; private set; }


        public int MaxHp { get; set; }
        public int MaxAp { get; set; }
        public int DefenseCost { get; set; }
        public int Iniciative { get; set; }

        public List<AbilityId> Abilities { get; set; }
        public TeamColor Team { get; set; }

        public static int AbilityCount => 6;
        // TODO - should this maybe just be internal?
        public List<Buff> Buffs { get; set; }


        public MobInfo(TeamColor team, int maxHp, int maxAp, int defenseCost, int iniciative, List<AbilityId> abilities) {
            Team = team;
            MaxHp = maxHp;
            MaxAp = maxAp;
            DefenseCost = defenseCost;
            Iniciative = iniciative;
            Abilities = abilities;
            Id = _lastId++;
            Buffs = new List<Buff>();
        }
    }

    public struct MobInstance {
        public MobId Id { get; set; }
        public AxialCoord Coord { get; set; }
        public AxialCoord OrigCoord { get; set; }
        public int Hp { get; set; }
        public int Ap { get; set; }

        public MobInstance(MobId id) : this() {
            Id = id;
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