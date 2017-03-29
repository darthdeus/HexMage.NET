using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class MapItem {
        public AxialCoord Coord { get; set; }
        public HexType HexType { get; set; }

        public MapItem() {}

        public MapItem(AxialCoord coord, HexType hexType) {
            Coord = coord;
            HexType = hexType;
        }
    }
}