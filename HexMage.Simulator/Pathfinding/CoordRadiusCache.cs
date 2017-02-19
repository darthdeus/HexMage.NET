using System.Collections.Generic;

namespace HexMage.Simulator {
    public class CoordRadiusCache {
        public static CoordRadiusCache Instance = new CoordRadiusCache();

        public readonly Dictionary<int, List<AxialCoord>> Coords = new Dictionary<int, List<AxialCoord>>();

        public void PrecomputeUpto(int maxSize) {            
            for (int size = 0; size < maxSize; size++) {
                var validCoords = new List<AxialCoord>();

                var from = -size;
                var to = size;

                for (var i = from; i <= to; i++) {
                    for (var j = from; j <= to; j++) {
                        for (var k = from; k <= to; k++) {
                            if (i + j + k == 0) {
                                validCoords.Add(new AxialCoord(j, i));
                            }
                        }
                    }
                }

                Coords[size] = validCoords;
            }
        }
    }
}