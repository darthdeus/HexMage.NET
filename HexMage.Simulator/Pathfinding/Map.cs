using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class Map : IDeepCopyable<Map>, IResettable {
        private readonly HexMap<HexType> _hexes;
        public int Size { get; set; }
        public Guid Guid = Guid.NewGuid();

        public List<AreaBuff> AreaBuffs = new List<AreaBuff>();

        public List<AxialCoord> AllCoords => _hexes.AllCoords;

        private readonly Dictionary<CoordPair, List<AxialCoord>> _visibilityLines =
            new Dictionary<CoordPair, List<AxialCoord>>();

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
                    + Math.Abs(a.Y - b.Y))/2;
        }

        public int CubeDistance(CubeCoord a, CubeCoord b) {
            return (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z))/2;
        }

        public float Lerp(float a, float b, float t) {
            return a + (b - a)*t;
        }

        public int LerpRound(float a, float b, float t, float offset = 0) {
            return (int) Math.Round(Lerp(a, b, t) + offset);
        }

        public CubeCoord CubeLerp(CubeCoord a, CubeCoord b, float t) {
            return new CubeCoord(LerpRound(a.X, b.X, t, 0.000001f),
                                 LerpRound(a.Y, b.Y, t, 0.000001f),
                                 LerpRound(a.Z, b.Z, t, -0.000002f));
        }


        public void PrecomputeCubeLinedraw() {
            foreach (var a in AllCoords) {
                foreach (var b in AllCoords) {
                    var result = ComputeCubeLinedraw(a, b);
                    _visibilityLines[new CoordPair(a, b)] = result.Select(x => x.ToAxial()).ToList();
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
                result.Add(CubeLerp(a, b, ((float) i)/N));
            }

            return result;
        }

        public Map DeepCopy() {
            var hexesCopy = new HexMap<HexType>(Size);
            var buffsCopy = new List<AreaBuff>();

            foreach (var coord in AllCoords) {
                hexesCopy[coord] = _hexes[coord];
            }

            foreach (var buff in AreaBuffs) {
                buffsCopy.Add(buff);
            }

            return new Map(Size, hexesCopy, buffsCopy) {
                Guid = Guid
            };
        }

        public void Reset() {
            AreaBuffs.Clear();
        }
    }
}