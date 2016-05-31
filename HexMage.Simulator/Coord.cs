using System;

namespace HexMage.Simulator
{
    public struct Coord : IEquatable<Coord>
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Coord(int x, int y) {
            X = x;
            Y = y;
        }

        public Coord Abs() {
            return new Coord(Math.Abs(X), Math.Abs(Y));
        }

        public int Max() {
            return Math.Max(X, Y);
        }

        public int Min() {
            return Math.Min(X, Y);
        }

        public bool Equals(Coord other) {
            return X == other.X && Y == other.Y;
        }

        public override string ToString() {
            return $"[{X},{Y}]";
        }

        public static Coord operator +(Coord lhs, Coord rhs) {
            return new Coord(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }

        public static Coord operator -(Coord lhs, Coord rhs) {
            return new Coord(lhs.X - rhs.X, lhs.Y - rhs.Y);
        }
    }
}