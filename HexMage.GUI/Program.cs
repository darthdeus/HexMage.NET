using System;
using HexMage.Simulator;
using HexMage.Simulator.PCG;

namespace HexMage.GUI {
    public static class Program {
        [STAThread]
        static void Main() {
            CoordRadiusCache.Instance.PrecomputeUpto(50);

            using (var game = new HexMageGame())
                game.Run();
        }
    }
}