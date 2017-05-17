using System;

namespace HexMage.Simulator {
    /// <summary>
    /// Cube coord, see http://www.redblobgames.com/grids/hexagons/
    /// </summary>
    public struct CubeCoord : IEquatable<CubeCoord> {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public CubeCoord(int x, int y, int z) {
            X = x;
            Y = y;
            Z = z;
        }

        public int Distance(CubeCoord to) {
            return (Math.Abs(X - to.X) + Math.Abs(Y - to.Y) + Math.Abs(Z - to.Z)) / 2;
        }

        public static CubeCoord operator +(CubeCoord a, CubeCoord b) {
            return new CubeCoord(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static CubeCoord operator -(CubeCoord a, CubeCoord b) {
            return new CubeCoord(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public AxialCoord ToAxial() {
            return new AxialCoord(X, Z);
        }

        public static implicit operator AxialCoord(CubeCoord cube) {
            return cube.ToAxial();
        }

        public bool Equals(CubeCoord other) {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public static bool operator ==(CubeCoord left, CubeCoord right) {
            return left.Equals(right);
        }

        public static bool operator !=(CubeCoord left, CubeCoord right) {
            return !left.Equals(right);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CubeCoord && Equals((CubeCoord) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = X;
                hashCode = (hashCode * 397) ^ Y;
                hashCode = (hashCode * 397) ^ Z;
                return hashCode;
            }
        }

        public int Sum() {
            return X + Y + Z;
        }

        public override string ToString() {
            return $"[{X},{Y},{Z}]";
        }
    }
}