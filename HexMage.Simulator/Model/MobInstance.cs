namespace HexMage.Simulator.Model {
    public struct MobInstance {
        public static readonly int InvalidId = -1;
        // TODO - remove the id?
        public int Id;
        public AxialCoord Coord;
        public int Hp;
        public int Ap;
        public Buff Buff;

        public MobInstance(int id) : this() {
            Id = id;
        }

        public MobInstance DeepCopy() {
            var copy = new MobInstance(Id) {
                Coord = Coord,
                Hp = Hp,
                Ap = Ap,
                Buff = Buff
            };

            return copy;
        }

        public override string ToString() {
            return $"{Hp}HP {Ap}AP {Coord}";
        }

        public bool Equals(MobInstance other) {
            return Id == other.Id && Coord.Equals(other.Coord) && Hp == other.Hp && Ap == other.Ap &&
                   Buff.Equals(other.Buff);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MobInstance && Equals((MobInstance) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ Coord.GetHashCode();
                hashCode = (hashCode * 397) ^ Hp;
                hashCode = (hashCode * 397) ^ Ap;
                hashCode = (hashCode * 397) ^ Buff.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(MobInstance left, MobInstance right) {
            return left.Equals(right);
        }

        public static bool operator !=(MobInstance left, MobInstance right) {
            return !left.Equals(right);
        }
    }
}