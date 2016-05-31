using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HexMage.Simulator
{
    public class Matrix<T>
    {
        readonly T[,] _data;
        public int Size { get; set; }
        public List<Coord> AllCoords { get; private set; } = new List<Coord>();

        public Matrix(int m, int n) {
            Debug.Assert(m == n);
            Size = m;
            _data = new T[Size*2 + 5, Size*2 + 5];
            RecalculateCoords();
        }

        private T this[int x, int y] {
            get { return _data[x, y]; }
            set { _data[x, y] = value; }
        }

        public T this[Coord c] {
            get { return _data[c.X + Size, c.Y + Size]; }
            set { _data[c.X + Size, c.Y + Size] = value; }
        }

        public void Initialize(Func<T> builder) {
            foreach (var coord in AllCoords) {
                this[coord] = builder();
            }
        }

        public void RecalculateCoords() {
            AllCoords.Clear();

            // TODO - go from -Size
            for (int i = -Size; i < Size; i++) {
                for (int j = -Size; j < Size; j++) {
                    for (int k = -Size; k < Size; k++) {
                        if (i + j + k == 0) {
                            AllCoords.Add(new Coord(j, i));
                        }
                    }
                }
            }
        }
    }
}