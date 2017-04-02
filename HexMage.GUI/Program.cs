using System;
using System.Threading;
using HexMage.Simulator;
using HexMage.Simulator.PCG;

namespace HexMage.GUI {
    public static class Program {
        public static CancellationToken CancellationToken;

        [STAThread]
        static void Main() {
            Constants.MctsLogging = true;
            var cts = new CancellationTokenSource();
            CoordRadiusCache.Instance.PrecomputeUpto(50);

            using (var game = new HexMageGame())
                game.Run();

            cts.Cancel();
        }
    }
}