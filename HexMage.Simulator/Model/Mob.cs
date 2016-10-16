using System.Collections.Generic;

namespace HexMage.Simulator.Model {
    public struct MobId {
        public static MobId Invalid = new MobId(-1);
        public readonly int Id;

        public MobId(int id) {
            Id = id;
        }

        public static implicit operator int(MobId mobId) {
            return mobId.Id;
        }

        public bool Equals(MobId other) {
            return Id == other.Id;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MobId && Equals((MobId) obj);
        }

        public override int GetHashCode() {
            return Id.GetHashCode();
        }

        public static bool operator ==(MobId left, MobId right) {
            return left.Equals(right);
        }

        public static bool operator !=(MobId left, MobId right) {
            return !left.Equals(right);
        }

        public override string ToString() {
            return $"MobId#{Id}";
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

        public MobInfo(TeamColor team, int maxHp, int maxAp, int defenseCost, int iniciative, List<AbilityId> abilities) {
            Team = team;
            MaxHp = maxHp;
            MaxAp = maxAp;
            DefenseCost = defenseCost;
            Iniciative = iniciative;
            Abilities = abilities;
            Id = _lastId++;
        }
    }

    public struct MobInstance {
        public MobId Id;
        public AxialCoord Coord;
        public AxialCoord OrigCoord;
        public int Hp;
        public int Ap;
        public List<Buff> Buffs { get; set; }

        public MobInstance(MobId id) : this() {
            Id = id;
            Buffs = new List<Buff>();
        }

        public MobInstance DeepCopy() {
            var copy = new MobInstance(Id) {
                Coord = Coord,
                OrigCoord = OrigCoord,
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