using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class Map : IDeepCopyable<Map>, IResettable {
        private readonly HexMap<HexType> _hexes;
        public int Size { get; set; }
        // TODO - remove Guid, it's no longer needed
        public Guid Guid = Guid.NewGuid();

        public List<AreaBuff> AreaBuffs = new List<AreaBuff>();

        public List<AxialCoord> AllCoords => _hexes.AllCoords;

        private Dictionary<CoordPair, List<AxialCoord>> _visibilityLines = new Dictionary<CoordPair, List<AxialCoord>>();
        private Dictionary<CoordPair, bool> _visibility = new Dictionary<CoordPair, bool>();

        public Map(int size, HexMap<HexType> hexes, List<AreaBuff> buffs) {
            Size = size;
            _hexes = hexes;
            AreaBuffs = buffs;
        }

        public Map(int size) {
            Size = size;
            _hexes = new HexMap<HexType>(size);
        }

        public HexType this[AxialCoord c] {
            get { return _hexes[c]; }
            set { _hexes[c] = value; }
        }

        public void Toogle(AxialCoord coord) {
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
            return _visibility[new CoordPair(from, to)];
        }

        public void PrecomputeCubeLinedraw() {
            foreach (var a in AllCoords) {
                foreach (var b in AllCoords) {
                    var result = ComputeCubeLinedraw(a, b);
                    var line = result.Select(x => x.ToAxial()).ToList();

                    var key = new CoordPair(a, b);

                    _visibilityLines[key] = line;

                    bool targetVisible = true;
                    foreach (var coord in line) {
                        if (this[coord] != HexType.Empty) {
                            targetVisible = false;
                            break;
                        }
                    }

                    _visibility[key] = targetVisible;
                }
            }
        }

        public List<AxialCoord> AxialLinedraw(AxialCoord a, AxialCoord b) {
            return _visibilityLines[new CoordPair(a, b)];
        }

        private List<CubeCoord> ComputeCubeLinedraw(CubeCoord a, CubeCoord b) {
            var result = new List<CubeCoord>();

            if (a == b) {
                return result;
            }

            var N = CubeDistance(a, b);
            for (int i = 0; i < N + 1; i++) {
                result.Add(CubeLerp(a, b, ((float) i) / N));
            }

            return result;
        }

        public Map DeepCopy() {
            //var hexesCopy = new HexMap<HexType>(Size);
            var buffsCopy = new List<AreaBuff>();

            //foreach (var coord in AllCoords) {
            //    hexesCopy[coord] = _hexes[coord];
            //}

            foreach (var buff in AreaBuffs) {
                buffsCopy.Add(buff);
            }

            var map = new Map(Size, _hexes, buffsCopy) {
                Guid = Guid
            };

            map._visibility = _visibility;
            map._visibilityLines = _visibilityLines;
            return map;
        }

        public void Reset() {
            AreaBuffs.Clear();
        }
    }
}