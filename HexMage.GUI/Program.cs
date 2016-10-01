﻿using System;
using HexMage.Simulator;
using HexMage.Simulator.PCG;

namespace HexMage.GUI {
    public static class Program {
        class AiTester {
            public void Run(int size, int iterations) {
                for (int i = 0; i < iterations; i++) {
                    var game = Generator.RandomGame(size, MapSeed.CreateRandom(), 5, g => new AiRandomController(g));

                    var eventHub = new GameEventHub(game);
                    Utils.RegisterLogger(new StdoutLogger());

                    eventHub.MainLoop(TimeSpan.Zero).Wait();
                }
            }
        }

        [STAThread]
        static void Main() {
            CoordRadiusCache.Instance.PrecomputeUpto(50);

            using (var game = new HexMageGame())
                game.Run();
        }
    }
}