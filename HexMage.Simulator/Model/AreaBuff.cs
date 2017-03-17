﻿namespace HexMage.Simulator.Model {
    public struct AreaBuff {
        public AxialCoord Coord;
        public int Radius;
        public Buff Effect;

        public static AreaBuff ZeroBuff() {
            return new AreaBuff(AxialCoord.Zero, 0, Buff.ZeroBuff());
        }

        public AreaBuff(AxialCoord coord, int radius, Buff effect) {
            Coord = coord;
            Radius = radius;
            Effect = effect;
        }

        public bool IsZero => Radius == 0;

        public void DecreaseLifetime() {
            var copy = Effect;
            copy.Lifetime--;
            Effect = copy;
        }

        public override string ToString() {
            return $"{nameof(Radius)}: {Radius}, {nameof(Effect)}: {Effect}";
        }

        public bool Equals(AreaBuff other) {
            return Coord.Equals(other.Coord) && Radius == other.Radius && Effect.Equals(other.Effect);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is AreaBuff && Equals((AreaBuff) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = Coord.GetHashCode();
                hashCode = (hashCode * 397) ^ Radius;
                hashCode = (hashCode * 397) ^ Effect.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(AreaBuff left, AreaBuff right) {
            return left.Equals(right);
        }

        public static bool operator !=(AreaBuff left, AreaBuff right) {
            return !left.Equals(right);
        }
    }
}