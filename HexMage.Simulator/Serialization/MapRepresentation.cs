using System;
using System.Collections.Generic;
using System.IO;
using HexMage.Simulator.Pathfinding;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class MapRepresentation {
        public int Size { get; set; }
        public MapItem[] Hexes { get; set; }

        public MapRepresentation() {}

        public MapRepresentation(Map map) {
            Hexes = new MapItem[map.AllCoords.Count];
            Size = map.Size;

            for (int i = 0; i < map.AllCoords.Count; i++) {
                var coord = map.AllCoords[i];
                Hexes[i] = new MapItem(coord, map[coord]);
            }
        }

        public void SaveToStream(TextWriter writer) {
            writer.Write(JsonConvert.SerializeObject(this));
        }

        public static MapRepresentation FromFile(string filename) {
            using (var reader = new StreamReader(filename)) {
                var contents = reader.ReadToEnd();

                return FromString(contents);
            }
        }

        public static MapRepresentation FromString(string contents) {
            return JsonConvert.DeserializeObject<MapRepresentation>(contents);
        }

        public void UpdateMap(Map map) {
            if (map.Size != Size) {
                throw new NotImplementedException("Map needs to be resized, not implemented yet");
            }
            foreach (var hex in Hexes) {
                map[hex.Coord] = hex.HexType;
            }
        }
    }
}