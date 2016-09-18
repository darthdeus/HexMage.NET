using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class Map : IDeepCopyable<Map> {
        private readonly HexMap<HexType> _hexes;
        private readonly HexMap<List<Buff>> _buffs;
        public int Size { get; set; }
        public Guid Guid = Guid.NewGuid();

        public List<AxialCoord> AllCoords => _hexes.AllCoords;

        public Map(int size, HexMap<HexType> hexes, HexMap<List<Buff>> buffs) {
            Size = size;
            _hexes = hexes;
            _buffs = buffs;
        }

        public Map(int size) {
            Size = size;
            _hexes = new HexMap<HexType>(size);
            _buffs = new HexMap<List<Buff>>(size);

            foreach (var coord in _buffs.AllCoords) {
                _buffs[coord] = new List<Buff>();
            }
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
            return _buffs[coord];
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

        public List<CubeCoord> CubeLinedraw(CubeCoord a, CubeCoord b) {
            var result = new List<CubeCoord>();

            var N = CubeDistance(a, b);
            for (int i = 0; i < N + 1; i++) {
                result.Add(CubeLerp(a, b, 1.0f/N*i));
            }

            return result;
        }

        public Map DeepCopy() {
            var hexesCopy = new HexMap<HexType>(Size);
            var buffsCopy = new HexMap<List<Buff>>(Size);

            foreach (var coord in AllCoords) {
                hexesCopy[coord] = _hexes[coord];
                Debug.Assert(buffsCopy[coord] == null, "Duplicate coords in AllCoords.");
                buffsCopy[coord] = new List<Buff>();

                foreach (var buff in _buffs[coord]) {
                    buffsCopy[coord].Add(buff.DeepCopy());
                }
            }

            return new Map(Size, hexesCopy, buffsCopy) {
                Guid = Guid
            };
        }
    }
}