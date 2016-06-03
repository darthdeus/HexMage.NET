using System.Collections.Generic;

namespace HexMage.Simulator
{
    public class Map
    {
        private readonly HexMap<HexType> _hexes;
        public int Size { get; set; }

        public List<AxialCoord> AllCoords {
            get { return _hexes.AllCoords; }
        }

        public Map(int size) {
            Size = size;
            _hexes = new HexMap<HexType>(size);
        }


        public HexType this[AxialCoord c] {
            get { return _hexes[c]; }
            set { _hexes[c] = value; }
        }
    }
}