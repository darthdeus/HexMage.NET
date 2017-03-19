using System;
using System.Diagnostics;

namespace HexMage.Simulator {
    public static class CoordPair {
        public static int Build(AxialCoord a, AxialCoord b) {
            Debug.Assert(a.X < 100);
            Debug.Assert(a.Y < 100);
            Debug.Assert(b.X < 100);
            Debug.Assert(b.Y < 100);            

            return a.X * 1000000 + a.Y * 10000 + b.X * 100 + b.Y;
        }
    }
}