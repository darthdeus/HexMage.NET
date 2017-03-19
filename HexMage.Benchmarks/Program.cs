//#define COPY_BENCH

#define FAST
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;
using Newtonsoft.Json;

namespace HexMage.Benchmarks {
    public class Tester {
        public void Run() {
            var size = 7;

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

            Generator.Random = new Random(1234);
            //Generator.Random = new Random();            

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
            //mobManager.Teams[t2] = new AiRandomController(gameInstance);

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

                    turnManager.StartNextTurn(pathfinder, gameInstance.State);

                    Console.WriteLine($"Starting, actions: {UctAlgorithm.actions}");
                    stopwatch.Start();
#if FAST
                    var rounds = hub.FastMainLoop(TimeSpan.Zero);
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

    internal class Program {
        //private static Random rand = new Random();
        private static Random rand = new Random(22);

        private static void Main(string[] args) {
            Console.WriteLine("Choose:");
            Console.WriteLine();
            Console.WriteLine("\t1) Benchmark");
            Console.WriteLine("\t2) Team evaluation");
            Console.WriteLine("\t3) Generate new team");

            var key = Console.ReadKey();

            Console.WriteLine();

            if (key.Key == ConsoleKey.D2) {
                RunEvaluator();
            } else if (key.Key == ConsoleKey.D3) {
                RunGenerator();
            } else {
                RunBenchmarks();
            }
        }

        internal class GenerationTeam {
            public Team Team;
            public double Rating;

            public GenerationTeam(Team team, double rating) {
                Team = team;
                Rating = rating;
            }
        }

        private static void RunGenerator() {
            string content = File.ReadAllText("team-1.json");
            var team = JsonLoader.LoadTeam(content);

            var opponent = RandomTeam(2, 2);

            var map = new Map(5);
            var stopwatch = new Stopwatch();

            var generation = new List<GenerationTeam>();

            const int teamsPerGeneration = 100;
            for (int i = 0; i < teamsPerGeneration; i++) {
                generation.Add(new GenerationTeam(RandomTeam(2, 2), 0.0));
            }

            const int numGenerations = 100;
            for (int i = 0; i < numGenerations; i++) {
                var genWatch = new Stopwatch();
                genWatch.Start();

                const bool sequential = true;
                if (sequential) {
                    foreach (var genTeam in generation) {
                        PopulationMember(team, genTeam, map, Console.Out);
                    }
                } else {
                    var tasks = generation.Select(genTeam => {
                        return Task.Factory.StartNew(() => {
                            var writer = new StringWriter();

                            PopulationMember(team, genTeam, map, writer);

                            Console.Write(writer.ToString());
                        });
                    });

                    Task.WaitAll(tasks.ToArray());
                }

                Console.WriteLine("****************************************************");
                Console.WriteLine("****************************************************");
                Console.WriteLine($"Generation done in {genWatch.ElapsedMilliseconds}ms");
                Console.WriteLine("****************************************************");
                Console.WriteLine("****************************************************");
            }


            //for (int i = 0; i < 100; i++) {
            //    stopwatch.Start();
            //    var setup = new Setup() {red = team.mobs, blue = opponent.mobs};

            //    var result = EvaluateSetup(setup, map);

            //    Console.WriteLine($"Win: {result.WinPercentage * 100}%");

            //    double delta = 0.075;
            //    if (result.WinPercentage < 0.5 - delta) {
            //        Console.WriteLine("WEAKENING");
            //        Mutate(opponent, -1);
            //    } else if (result.WinPercentage > 0.5 + delta) {
            //        Console.WriteLine("STRENGHTENING");
            //        Mutate(opponent, +1);
            //        //Strenghten(opponent);
            //    } else {
            //        break;
            //    }

            //    stopwatch.Stop();

            //    Console.WriteLine($"\ttook: {stopwatch.Elapsed.Milliseconds}ms");
            //    Console.WriteLine();
            //    Console.WriteLine(JsonConvert.SerializeObject(opponent));
            //    Console.WriteLine();
            //    Console.WriteLine();
            //}
        }

        public static void PopulationMember(Team team, GenerationTeam genTeam, Map map, TextWriter writer) {
            var teamWatch = new Stopwatch();
            teamWatch.Start();
            var setup = new Setup() {red = team.mobs, blue = genTeam.Team.mobs};
            EvaluationResult result = EvaluateSetup(setup, map, writer);

            genTeam.Rating = result.WinPercentage;
            writer.WriteLine(genTeam.Rating);

            Mutate(genTeam.Team, result.WinPercentage - 0.5);

            writer.WriteLine(
                $"Total:\t\t{teamWatch.ElapsedMilliseconds}ms\nPer turn:\t{result.MillisecondsPerTurn}ms\nPer iteration:\t{result.MillisecondsPerIteration}ms");
            writer.WriteLine();
            writer.WriteLine();
        }

        public static void Mutate(Team team, double scale) {
            var mobIndex = rand.Next(team.mobs.Count);
            var mob = team.mobs[mobIndex];
            var abilities = mob.abilities;
            var abilityIndex = rand.Next(abilities.Count);
            var ability = abilities[abilityIndex];

            switch (rand.Next(6)) {
                case 0:
                    ability.dmg = (int) Math.Max(1, ability.dmg + 2 * scale);
                    break;
                case 1:
                    ability.ap = (int) Math.Max(1, ability.ap + 3 * scale);
                    break;
                case 2:
                    ability.range = (int) Math.Max(1, ability.range - 3 * scale);
                    break;
                case 3:
                    ability.cooldown = (int) Math.Max(0, ability.cooldown + 1 * scale);
                    break;
                case 4:
                    mob.hp = (int) Math.Max(mob.hp + 5 * scale, 1);
                    break;
                case 5:
                    mob.ap = (int) Math.Max(mob.ap + 5 * scale, 1);
                    break;
            }
        }

        public static void Strenghten(Team team) {
            var mobIndex = rand.Next(team.mobs.Count);
            var mob = team.mobs[mobIndex];
            var abilities = mob.abilities;
            var abilityIndex = rand.Next(abilities.Count);
            var ability = abilities[abilityIndex];

            switch (rand.Next(6)) {
                case 0:
                    ability.dmg++;
                    break;
                case 1:
                    ability.ap = Math.Max(ability.ap - 1, 1);
                    break;
                case 2:
                    ability.range++;
                    break;
                case 3:
                    ability.cooldown = Math.Max(ability.cooldown - 1, 0);
                    break;
                case 4:
                    mob.hp++;
                    break;
                case 5:
                    mob.ap++;
                    break;
            }
        }

        private static Team RandomTeam(int mobs, int spellsPerMob) {
            var team = new Team();
            team.mobs = new List<JsonMob>();

            for (int i = 0; i < mobs; i++) {
                var abilities = new List<JsonAbility>();

                for (int j = 0; j < spellsPerMob; j++) {
                    abilities.Add(new JsonAbility(rand.Next(3, 8), rand.Next(2, 6), rand.Next(3, 5), rand.Next(2)));
                }

                team.mobs.Add(new JsonMob {
                    abilities = abilities,
                    hp = rand.Next(30, 100),
                    ap = rand.Next(10, 20)
                });
            }

            return team;
        }

        private static void RunEvaluator() {
            string content = File.ReadAllText(@"simple.json");
            var setup = JsonLoader.Load(content);

            var result = EvaluateSetup(setup, new Map(5), Console.Out);

            Console.WriteLine(result);
        }

        private static EvaluationResult EvaluateSetup(Setup setup, Map map, TextWriter writer) {
            var game = new GameInstance(new Map(5));
            var mobIds = new List<int>();

            foreach (var mob in setup.red) {
                var ids = mob.abilities.Select(ab => game.AddAbilityWithInfo(ab.ToAbility()));
                mobIds.Add(game.AddMobWithInfo(mob.ToMobInfo(TeamColor.Red, ids)));
            }

            foreach (var mob in setup.blue) {
                var ids = mob.abilities.Select(ab => game.AddAbilityWithInfo(ab.ToAbility()));
                mobIds.Add(game.AddMobWithInfo(mob.ToMobInfo(TeamColor.Blue, ids)));
            }

            game.State.SetMobPosition(mobIds[0], new AxialCoord(2, 3));
            game.State.SetMobPosition(mobIds[1], new AxialCoord(3, 2));

            game.State.SetMobPosition(mobIds[2], new AxialCoord(-2, -3));
            game.State.SetMobPosition(mobIds[3], new AxialCoord(-3, -2));

            game.PrepareEverything();

            var result = new GameInstanceEvaluator(game, writer).Evaluate();

            return result;
        }

        private static void RunBenchmarks() {
            for (int i = 0; i < 10; i++) {
                new Tester().Run();
            }
        }
    }
}