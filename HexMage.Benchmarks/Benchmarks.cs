//#define COPY_BENCH
#define FAST
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;
using HexMage.Simulator.PCG;
using Newtonsoft.Json;

namespace HexMage.Benchmarks {
    public class Benchmarks {
        public static void CompareAi() {
            var dna = new DNA(2, 2);
            dna.Randomize();

            const string mapFilename = @"C: \Users\darth\Documents\map.json";
            //var map = JsonConvert.DeserializeObject<Map>(File.ReadAllText(mapFilename));
            var map = new Map(5);

            var game = GameSetup.GenerateFromDna(dna, dna, map);

            game.PrepareEverything();

            IMobController c1, c2;

            switch (Constants.MctsBenchType) {
                case 0:
                    c1 = new MctsController(game, Constants.MctsBenchIterations);
                    c2 = new AiRandomController(game);
                    break;
                case 1:
                    c1 = new MctsController(game, Constants.MctsBenchIterations);
                    c2 = new AiRuleBasedController(game);
                    break;
                case 2:
                    c1 = new AiRandomController(game);
                    c2 = new AiRuleBasedController(game);
                    break;
                case 3:
                    c1 = new FlatMonteCarloController(game);
                    c2 = new AiRandomController(game);
                    break;
                case 4:
                    c1 = new FlatMonteCarloController(game);
                    c2 = new AiRuleBasedController(game);
                    break;
                case 5:
                    c1 = new FlatMonteCarloController(game);
                    c2 = new MctsController(game, Constants.MctsBenchIterations);
                    break;
                default:
                    throw new ArgumentException($"Invalid value of {Constants.MctsBenchType} for --MctsBenchType");
            }

            //c1 = new MctsController(game, 3);
            //c2 = new FlatMonteCarloController(game);
            //c2 = new MctsController(game, 100000);

            var iterationStopwatch = new Stopwatch();

            for (int i = 0; i < 1000; i++) {
                dna.Randomize();

                GameSetup.OverrideGameDna(game, dna, dna);
                iterationStopwatch.Restart();
                GameInstanceEvaluator.PlayoutSingleGame(game, c1, c2);
                iterationStopwatch.Stop();

                GameSetup.OverrideGameDna(game, dna, dna);
                iterationStopwatch.Restart();
                GameInstanceEvaluator.PlayoutSingleGame(game, c2, c1);
                iterationStopwatch.Stop();


                Console.Write($"Iteration: {iterationStopwatch.ElapsedMilliseconds}ms");
                GameInstanceEvaluator.PrintBookkeepingData();
            }
        }

        public void Run() {
            var size = 5;

            var s = Stopwatch.StartNew();
            CoordRadiusCache.Instance.PrecomputeUpto(50);
            Console.WriteLine($"Cache precomputed in {s.Elapsed.TotalMilliseconds}ms");

            var gameInstance = new GameInstance(size);

            var hub = new GameEventHub(gameInstance);
            //var replayRecorder = new ReplayRecorder();
            //hub.AddSubscriber(replayRecorder);

            Utils.RegisterLogger(new StdoutLogger());
            Utils.MainThreadId = Thread.CurrentThread.ManagedThreadId;

            var t1 = TeamColor.Red;
            var t2 = TeamColor.Blue;

            var turnManager = gameInstance.TurnManager;
            var mobManager = gameInstance.MobManager;
            var pathfinder = gameInstance.Pathfinder;

            for (int i = 0; i < 5; i++) {
                MobInfo mi1 = Generator.RandomMob(mobManager, t1, gameInstance.State);
                MobInfo mi2 = Generator.RandomMob(mobManager, t2, gameInstance.State);

                int m1 = gameInstance.AddMobWithInfo(mi1);
                int m2 = gameInstance.AddMobWithInfo(mi2);

                Generator.RandomPlaceMob(mobManager, m1, gameInstance.Map, gameInstance.State);
                Generator.RandomPlaceMob(mobManager, m2, gameInstance.Map, gameInstance.State);
            }

            mobManager.Teams[t1] = new MctsController(gameInstance);
            mobManager.Teams[t2] = new MctsController(gameInstance);

            for (int i = 0; i < 5; i++) {
                pathfinder.PathfindDistanceAll();
                Console.WriteLine();
            }

            mobManager.InitializeState(gameInstance.State);

            //foreach (var coord in gameInstance.Map.AllCoords) {
            //    var count = gameInstance.State.MobInstances.Count(x => x.Coord == coord);
            //    if (count > 1) {
            //        throw new InvalidOperationException($"There are duplicate mobs on the same coord ({coord}), total {count}.");
            //    }
            //}

            gameInstance.State.Reset(gameInstance.MobManager);
            turnManager.PresortTurnOrder();

#if COPY_BENCH
            {
                Console.WriteLine("---- STATE COPY BENCHMARK ----");

                int totalIterations = 1000000;
                int iterations = 0;
                const int dumpIterations = 1000;

                var copyStopwatch = Stopwatch.StartNew();
                var totalCopyStopwatch = Stopwatch.StartNew();

                while (iterations < totalIterations) {
                    iterations++;

                    copyStopwatch.Start();
                    gameInstance.DeepCopy();
                    copyStopwatch.Stop();

                    if (iterations % dumpIterations == 0) {
                        double secondsPerThousand = (double) copyStopwatch.ElapsedTicks / Stopwatch.Frequency;
                        double msPerCopy = secondsPerThousand;
                        Console.WriteLine($"Copy {msPerCopy:0.00}ms, 1M in: {secondsPerThousand * 1000}s");
                        copyStopwatch.Reset();
                    }
                }

                Console.WriteLine($"Total copy time for {totalIterations}: {totalCopyStopwatch.ElapsedMilliseconds}ms");
            }

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();

#endif

            Console.WriteLine("Precomputing cubes");
            gameInstance.Map.PrecomputeCubeLinedraw();
            Console.WriteLine("Cubes precomputed");

            {
                var totalStopwatch = Stopwatch.StartNew();
                var stopwatch = new Stopwatch();
                var iterations = 0;
                int roundsPerThousand = 0;
                int dumpIterations = 1;

                int totalIterations = 500000;
                double ratio = 1000000 / totalIterations;

                while (iterations < totalIterations) {
                    iterations++;

                    // TODO - fuj
                    turnManager.StartNextTurn(pathfinder, gameInstance.State);

                    //Console.WriteLine($"Starting, actions: {UctAlgorithm.Actions}");
                    stopwatch.Start();
#if FAST
                    var rounds = hub.FastMainLoop();
                    stopwatch.Stop();

                    roundsPerThousand += rounds;
#else
                var rounds = hub.MainLoop(TimeSpan.Zero);
                stopwatch.Stop();

                rounds.Wait();
                roundsPerThousand += rounds.Result;
#endif

                    if (iterations % dumpIterations == 0) {
                        double perThousandMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);

                        double estimateSecondsPerMil =
                            Math.Round(totalStopwatch.Elapsed.TotalSeconds / iterations * totalIterations, 2);
                        double perGame = Math.Round(perThousandMs / dumpIterations * 1000, 2);

                        Console.WriteLine(
                            $"Starting a new game {iterations:00000}, {roundsPerThousand / dumpIterations} average rounds, {perThousandMs:00.00}ms\trunning average per 1M: {estimateSecondsPerMil * ratio:00.00}s, per game: {perGame:00.00}us");
                        roundsPerThousand = 0;
                        stopwatch.Reset();
                    }

                    gameInstance.Reset();
                }

                Console.WriteLine("Took {0}ms", totalStopwatch.ElapsedMilliseconds);
            }
        }
    }
}