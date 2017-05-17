using System;

namespace HexMage.Simulator {
    /// <summary>
    /// Represents a pixel coordinate of the hex, see http://www.redblobgames.com/grids/hexagons/
    /// </summary>
    public struct PixelCoord : IEquatable<PixelCoord> {
        public int X { get; set; }
        public int Y { get; set; }

        public PixelCoord(int x, int y) {
            X = x;
            Y = y;
        }

        public PixelCoord Abs() {
            return new PixelCoord(Math.Abs(X), Math.Abs(Y));
        }

        public int Max() {
            return Math.Max(X, Y);
        }

        public int Min() {
            return Math.Min(X, Y);
        }

        public bool Equals(PixelCoord other) {
            return X == other.X && Y == other.Y;
        }

        public override string ToString() {
            return $"[{X},{Y}]";
        }

        public static PixelCoord operator +(PixelCoord lhs, PixelCoord rhs) {
            return new PixelCoord(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }

        public static PixelCoord operator -(PixelCoord lhs, PixelCoord rhs) {
            return new PixelCoord(lhs.X - rhs.X, lhs.Y - rhs.Y);
        }
    }
}