using System;

namespace HexMage.GUI.Core {
    // TODO: move into constants?
    static class GuiConfig {
        public const bool SanityChecks = false;
        public static readonly int GridSize = 64;
        public static readonly double HeightOffset = GridSize / 4 + Math.Sin(30 * Math.PI / 180) * GridSize;
    }
}