using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;

namespace HexMage.Benchmarks {
    public class Tester {
        public void Run() {
            var size = 30;
            var gameInstance = new GameInstance(size);

            var hub = new GameEventHub(gameInstance);
            var replayRecorder = new ReplayRecorder();
            hub.AddSubscriber(replayRecorder);

            //Utils.RegisterLogger(new StdoutLogger());
            Utils.MainThreadId = Thread.CurrentThread.ManagedThreadId;

            var t1 = TeamColor.Red;
            var t2 = TeamColor.Blue;

            var turnManager = gameInstance.TurnManager;
            var mobManager = gameInstance.MobManager;
            var pathfinder = gameInstance.Pathfinder;

            Mob m1 = null;
            Mob m2 = null;

            Generator.Random = new Random(1234);

            for (int i = 0; i < 5; i++) {
                m1 = Generator.RandomMob(t1, size, c => gameInstance.MobManager.AtCoord(c) == null);
                m2 = Generator.RandomMob(t2, size, c => gameInstance.MobManager.AtCoord(c) == null);

                mobManager.AddMob(m1);
                mobManager.AddMob(m2);
            }

            mobManager.Teams[t1] = new AiRandomController(gameInstance);
            mobManager.Teams[t2] = new AiRandomController(gameInstance);

            m1.Coord = new AxialCoord(0, 0);
            m2.Coord = new AxialCoord(0, 1);

            var stopwatch = new Stopwatch();
            var iterations = 0;
            while (iterations < 10000000) {
                iterations++;

                stopwatch.Start();
                turnManager.StartNextTurn(pathfinder);
                var ticks = stopwatch.ElapsedTicks;
                //Console.WriteLine($"Nextt turn {ticks}");
                stopwatch.Start();
                hub.MainLoop(TimeSpan.Zero).Wait();

                Console.WriteLine($"Starting a new game, took {stopwatch.ElapsedMilliseconds}ms");

                stopwatch.Reset();
                gameInstance.Reset();

                //Console.WriteLine();
                //replayRecorder.DumpReplay(Console.Out);
                replayRecorder.Clear();
                //Console.WriteLine();
            }

            Console.WriteLine("Took {0}ms", stopwatch.ElapsedMilliseconds);
        }
    }

    internal class Program {
        private static void Main(string[] args) {
            new Tester().Run();
        }
    }
}