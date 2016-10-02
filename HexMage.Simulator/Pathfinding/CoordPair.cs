namespace HexMage.Simulator {
    public struct CoordPair
    {
        private AxialCoord _a;
        private AxialCoord _b;
        public CoordPair(AxialCoord a, AxialCoord b)
        {
            _a = a;
            _b = b;
        }

        public bool Equals(CoordPair other)
        {
            return _a.Equals(other._a) && _b.Equals(other._b);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CoordPair && Equals((CoordPair)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_a.GetHashCode() * 397) ^ _b.GetHashCode();
            }
        }

        public static bool operator ==(CoordPair left, CoordPair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CoordPair left, CoordPair right)
        {
            return !left.Equals(right);
        }
    }
}