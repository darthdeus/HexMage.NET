﻿using System;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public struct AxialCoord : IEquatable<AxialCoord> {
        public int X { get; set; }
        public int Y { get; set; }

        public AxialCoord(int x, int y) {
            X = x;
            Y = y;
        }

        public bool Equals(AxialCoord other) {
            return X == other.X && Y == other.Y;
        }

        public AxialCoord Abs() {
            return new AxialCoord(Math.Abs(X), Math.Abs(Y));
        }

        public int Min() {
            return Math.Min(X, Y);
        }

        public int Max() {
            return Math.Max(X, Y);
        }

        public int Distance(AxialCoord to) {
            return (Math.Abs(X - to.X)
                    + Math.Abs(X + Y - to.X - to.Y)
                    + Math.Abs(Y - to.Y))/2;
        }

        [Obsolete]
        public int ModifiedDistance(Mob mob, AxialCoord to) {
            return (int)Math.Round(Distance(to)*mob.SpeedModifier);
        }

        public static AxialCoord operator +(AxialCoord a, AxialCoord b) {
            return new AxialCoord(a.X + b.X, a.Y + b.Y);
        }

        public static AxialCoord operator -(AxialCoord a, AxialCoord b) {
            return new AxialCoord(a.X - b.X, a.Y - b.Y);
        }

        public static AxialCoord operator *(AxialCoord a, int x) {
            return new AxialCoord(a.X*x, a.Y*x);
        }

        public CubeCoord ToCube() {
            return new CubeCoord(X, -X - Y, Y);
        }

        public static implicit operator CubeCoord(AxialCoord axial) {
            return axial.ToCube();
        }

        bool IEquatable<AxialCoord>.Equals(AxialCoord other) {
            return X == other.X && Y == other.Y;
        }

        public static bool operator ==(AxialCoord left, AxialCoord right) {
            return left.Equals(right);
        }

        public static bool operator !=(AxialCoord left, AxialCoord right) {
            return !left.Equals(right);
        }

        public override string ToString() {
            return $"[{X},{Y}]";
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is AxialCoord && Equals((AxialCoord) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (X*397) ^ Y;
            }
        }
    }
}