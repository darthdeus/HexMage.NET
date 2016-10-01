using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HexMage.Simulator {
    public class HexMap<T> : IDeepCopyable<HexMap<T>> {
        private readonly T[,] _data;

        private readonly int _size;

        public HexMap(int size) {
            Debug.Assert(size > 0);
            _size = size;
            _data = new T[_size*2 + 1, _size*2 + 1];
        }

        public T this[AxialCoord c] {
            get { return _data[c.X + _size, c.Y + _size]; }
            set { _data[c.X + _size, c.Y + _size] = value; }
        }

        public T this[int x, int y] {
            get { return this[new AxialCoord(x, y)]; }
            set { this[new AxialCoord(x, y)] = value; }
        }        

        private static Dictionary<int, List<AxialCoord>> _allCoordDictionary = new Dictionary<int, List<AxialCoord>>();
        private List<AxialCoord> _allCoords;

        private List<AxialCoord> CalculateAllCoords(int size) {
            var result = new List<AxialCoord>();

            var from = -size;
            var to = size;

            for (var i = from; i <= to; i++) {
                for (var j = from; j <= to; j++) {
                    for (var k = from; k <= to; k++) {
                        if (i + j + k == 0) {
                            result.Add(new AxialCoord(j, i));
                        }
                    }
                }
            }
            return result;
        }

        public List<AxialCoord> AllCoords {
            get {
                if (!_allCoordDictionary.ContainsKey(_size)) {
                    _allCoordDictionary[_size] = CalculateAllCoords(_size);
                }
                return _allCoordDictionary[_size];
            }
        }

        public void Initialize(Func<T> builder) {
            foreach (var coord in AllCoords) {
                this[coord] = builder();
            }
        }

        public void RecalculateCoords() {
            AllCoords.Clear();

            // TODO - go from -Size
            for (var i = -_size; i < _size; i++) {
                for (var j = -_size; j < _size; j++) {
                    for (var k = -_size; k < _size; k++) {
                        if (i + j + k == 0) {
                            AllCoords.Add(new AxialCoord(j, i));
                        }
                    }
                }
            }
        }

        public HexMap<T> DeepCopy() {
            var copy = new HexMap<T>(_size);

            foreach (var coord in AllCoords) {
                copy[coord] = this[coord];
            }

            return copy;
        }
    }
}