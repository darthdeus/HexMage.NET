using System;

namespace HexMage.GUI
{
    class Config
    {
        public static readonly int GridSize = 32;
        public static readonly double HeightOffset = GridSize / 4 + Math.Sin(30 * Math.PI / 180) * GridSize;
    }
}