using System;
using System.Collections.Generic;
using System.Linq;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator.Pathfinding {
    public class Map : IDeepCopyable<Map>, IResettable {
        [JsonProperty] private readonly HexMap<HexType> _hexes;

        [JsonIgnore] private Dictionary<int, bool> _visibility = new Dictionary<int, bool>();

        [JsonIgnore] private Dictionary<int, List<AxialCoord>> _visibilityLines =
            new Dictionary<int, List<AxialCoord>>();

        public List<AreaBuff> AreaBuffs = new List<AreaBuff>();

        public List<AxialCoord> BlueStartingPoints = new List<AxialCoord>();
        public List<AxialCoord> RedStartingPoints = new List<AxialCoord>();

        public List<AxialCoord> EmptyCoords = new List<AxialCoord>();

        // TODO - remove Guid, it's no longer needed
        public Guid Guid = Guid.NewGuid();

        [JsonConstructor]
        public Map() { }

        public Map(int size, HexMap<HexType> hexes, List<AreaBuff> buffs) {
            Size = size;
            _hexes = hexes;
            AreaBuffs = buffs;
        }

        public Map(int size) {
            Size = size;
            _hexes = new HexMap<HexType>(size);
        }

        public int Size { get; set; }

        [JsonIgnore]
        public List<AxialCoord> AllCoords => _hexes.AllCoords;

        public HexType this[AxialCoord c] {
            get { return _hexes[c]; }
            set { _hexes[c] = value; }
        }

        public Map DeepCopy() {
            //var hexesCopy = new HexMap<HexType>(Size);
            var buffsCopy = new List<AreaBuff>();

            //foreach (var coord in AllCoords) {
            //    hexesCopy[coord] = _hexes[coord];
            //}

            foreach (var buff in AreaBuffs) buffsCopy.Add(buff);

            var map = new Map(Size, _hexes, buffsCopy) {
                Guid = Guid
            };

            map.RedStartingPoints = RedStartingPoints;
            map.BlueStartingPoints = BlueStartingPoints;
            map._visibility = _visibility;
            map._visibilityLines = _visibilityLines;
            return map;
        }

        public void Reset() {
            AreaBuffs.Clear();
        }

        public void Toggle(AxialCoord coord) {
            if (this[coord] == HexType.Empty) {
                this[coord] = HexType.Wall;
            } else {
                this[coord] = HexType.Empty;
            }
        }

        public List<Buff> BuffsAt(AxialCoord coord) {
            return AreaBuffs.Where(b => AxialDistance(b.Coord, coord) <= b.Radius)
                            .Select(b => b.Effect)
                            .ToList();
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
    }
}