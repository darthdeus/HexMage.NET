using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    /// <summary>
    /// A generic hex map shaped array-like data structure with
    /// precomputed coords up to a maximum radius.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HexMap<T> : IDeepCopyable<HexMap<T>> {
        [JsonProperty] private readonly T[,] _data;

        [JsonProperty] public readonly int Size;

        [JsonConstructor]
        public HexMap() { }

        public HexMap(int size) {
            Debug.Assert(size > 0);
            Size = size;
            _data = new T[Size * 2 + 1, Size * 2 + 1];
        }

        public T this[AxialCoord c] {
            get { return _data[c.X + Size, c.Y + Size]; }
            set { _data[c.X + Size, c.Y + Size] = value; }
        }

        public T this[int x, int y] {
            get { return this[new AxialCoord(x, y)]; }
            set { this[new AxialCoord(x, y)] = value; }
        }

        private static readonly Dictionary<int, List<AxialCoord>> _allCoordDictionary =
            new Dictionary<int, List<AxialCoord>>();

        [JsonIgnore]
        public List<AxialCoord> AllCoords {
            get {
                lock (_allCoordDictionary) {
                    if (!_allCoordDictionary.ContainsKey(Size)) {
                        _allCoordDictionary[Size] = CalculateAllCoords(Size);
                    }
                }

                return _allCoordDictionary[Size];
            }
        }

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

        public void Initialize(Func<T> builder) {
            foreach (var coord in AllCoords) {
                this[coord] = builder();
            }
        }

        public void RecalculateCoords() {
            AllCoords.Clear();

            for (var i = -Size; i < Size; i++) {
                for (var j = -Size; j < Size; j++) {
                    for (var k = -Size; k < Size; k++) {
                        if (i + j + k == 0) {
                            AllCoords.Add(new AxialCoord(j, i));
                        }
                    }
                }
            }
        }

        public HexMap<T> DeepCopy() {
            var copy = new HexMap<T>(Size);

            foreach (var coord in AllCoords) {
                copy[coord] = this[coord];
            }

            return copy;
        }
    }
}