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
            var size = 7;

            var s = Stopwatch.StartNew();
            CoordRadiusCache.Instance.PrecomputeUpto(50);
            Console.WriteLine($"Cache precomputed in {s.Elapsed.TotalMilliseconds}ms");

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

            for (int i = 0; i < 5; i++) {
                pathfinder.PathfindDistanceAll();
                Console.WriteLine();
            }

            var totalStopwatch = Stopwatch.StartNew();
            var stopwatch = new Stopwatch();
            var iterations = 0;
            int roundsPerThousand = 0;

            while (iterations < 1000000) {
                iterations++;

                turnManager.StartNextTurn(pathfinder);

                stopwatch.Start();
                var rounds = hub.MainLoop(TimeSpan.Zero);
                rounds.Wait();
                stopwatch.Stop();

                roundsPerThousand += rounds.Result;

                if (iterations%1000 == 0) {
                    Console.WriteLine($"Starting a new game {iterations}, {roundsPerThousand/1000} average rounds, {stopwatch.Elapsed.TotalMilliseconds}ms");
                    roundsPerThousand = 0;
                    stopwatch.Reset();
                }

                gameInstance.Reset();

                //replayRecorder.DumpReplay(Console.Out);
                replayRecorder.Clear();
            }

            Console.WriteLine("Took {0}ms", totalStopwatch.ElapsedMilliseconds);
        }
    }

    internal class Program {
        private static void Main(string[] args) {
            new Tester().Run();
        }
    }
}