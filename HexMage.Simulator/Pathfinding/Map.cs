using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;
using Newtonsoft.Json;

namespace HexMage.Simulator.Pathfinding {
    public class Map : IDeepCopyable<Map> {
        [JsonProperty] private readonly HexMap<HexType> _hexes;

        [JsonIgnore] private Dictionary<int, bool> _visibility = new Dictionary<int, bool>();

        [JsonIgnore] private Dictionary<int, List<AxialCoord>> _visibilityLines =
            new Dictionary<int, List<AxialCoord>>();

        public List<AxialCoord> BlueStartingPoints = new List<AxialCoord>();
        public List<AxialCoord> RedStartingPoints = new List<AxialCoord>();

        public List<AxialCoord> EmptyCoords = new List<AxialCoord>();

        public int Size { get; set; }

        [JsonIgnore]
        public List<AxialCoord> AllCoords => _hexes.AllCoords;

        // TODO - remove Guid, it's no longer needed
        public Guid Guid = Guid.NewGuid();

        [JsonConstructor]
        public Map() { }

        public Map(int size, HexMap<HexType> hexes) {
            Size = size;
            _hexes = hexes;
        }

        public Map(int size) {
            Size = size;
            _hexes = new HexMap<HexType>(size);
        }

        public HexType this[AxialCoord c] {
            get { return _hexes[c]; }
            set { _hexes[c] = value; }
        }

        public Map DeepCopy() {
            var map = new Map(Size, _hexes.DeepCopy()) {
                Guid = Guid
            };

            map.EmptyCoords = EmptyCoords.ToList();
            map.RedStartingPoints = RedStartingPoints.ToList();
            map.BlueStartingPoints = BlueStartingPoints.ToList();

            map._visibility = new Dictionary<int, bool>();
            foreach (var v in _visibility) {
                map._visibility[v.Key] = v.Value;
            }

            map._visibilityLines = new Dictionary<int, List<AxialCoord>>();
            foreach (var v in _visibilityLines) {
                map._visibilityLines[v.Key] = v.Value.ToList();
            }
            return map;
        }

        public AxialCoord RandomCoord() {
            for (int i = 0; i < 1000; i++) {
                // +1 since .Next is inclusive of the lower bound
                var x = Generator.Random.Next(-Size + 1, Size);
                var y = Generator.Random.Next(-Size + 1, Size);

                if (Math.Abs(x) + Math.Abs(y) >= Size) {
                    continue;
                }

                Debug.Assert(Math.Abs(x) < Size);
                Debug.Assert(Math.Abs(y) < Size);

                var coord = new AxialCoord(x, y);

                return coord;
            }

            throw new InvalidOperationException(
                "Something went wrong with the random number generator, unable to generate a valid coord under 1000 iterations.");
        }

        public void Toggle(AxialCoord coord) {
            if (this[coord] == HexType.Empty) {
                this[coord] = HexType.Wall;
            } else {
                this[coord] = HexType.Empty;
            }
        }

        public int AxialDistance(AxialCoord a, AxialCoord b) {
            return (Math.Abs(a.X - b.X)
                    + Math.Abs(a.X + a.Y - b.X - b.Y)
                    + Math.Abs(a.Y - b.Y)) / 2;
        }

        public int CubeDistance(CubeCoord a, CubeCoord b) {
            return (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z)) / 2;
        }

        public float Lerp(float a, float b, float t) {
            return a + (b - a) * t;
        }

        public int LerpRound(float a, float b, float t, float offset = 0) {
            return (int) Math.Round(Lerp(a, b, t) + offset);
        }

        public CubeCoord CubeLerp(CubeCoord a, CubeCoord b, float t) {
            return new CubeCoord(LerpRound(a.X, b.X, t, 0.000001f),
                                 LerpRound(a.Y, b.Y, t, 0.000001f),
                                 LerpRound(a.Z, b.Z, t, -0.000002f));
        }

        public bool IsVisible(AxialCoord from, AxialCoord to) {
            return _visibility[CoordPair.Build(from, to)];
        }

        public void PrecomputeCubeLinedraw() {
            EmptyCoords.Clear();
            _visibility.Clear();
            _visibilityLines.Clear();

            foreach (var a in AllCoords) {
                if (this[a] == HexType.Empty) {
                    EmptyCoords.Add(a);
                }

                foreach (var b in AllCoords) {
                    var result = ComputeCubeLinedraw(a, b);
                    var line = result.Select(x => x.ToAxial()).ToList();

                    var key = CoordPair.Build(a, b);

                    _visibilityLines[key] = line;

                    var targetVisible = true;
                    foreach (var coord in line)
                        if (this[coord] != HexType.Empty) {
                            targetVisible = false;
                            break;
                        }

                    _visibility[key] = targetVisible;
                }
            }
        }

        public List<AxialCoord> AxialLinedraw(AxialCoord a, AxialCoord b) {
            return _visibilityLines[CoordPair.Build(a, b)];
        }

        private List<CubeCoord> ComputeCubeLinedraw(CubeCoord a, CubeCoord b) {
            var result = new List<CubeCoord>();

            if (a == b) {
                return result;
            }

            var N = CubeDistance(a, b);
            for (var i = 0; i < N + 1; i++) result.Add(CubeLerp(a, b, (float) i / N));

            return result;
        }

        public bool IsValidCoord(AxialCoord c) {
            var a = c.X + c.Y;
            var distance = ((c.X < 0 ? -c.X : c.X)
                            + (a < 0 ? -a : a)
                            + (c.Y < 0 ? -c.Y : c.Y)) / 2;

            //int distance = (Math.Abs(c.X)
            //                + Math.Abs(c.X + c.Y)
            //                + Math.Abs(c.Y)) / 2;
            return distance <= Size;

            //return _map.AxialDistance(c, new AxialCoord(0, 0)) <= _map.Size;
            //return _map.CubeDistance(new CubeCoord(0, 0, 0), c) <= _map.Size;
        }

        public static Map Load(string filename) {
            return JsonConvert.DeserializeObject<Map>(File.ReadAllText(filename));
        }
    }
}