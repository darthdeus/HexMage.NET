using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;

namespace HexMage.Benchmarks {
    public class Evolution {
        public class GenomeTask {
            public GenerationTeam GenTeam;
            public bool IsPoison;
        }

        private long done = 0;

        public void Run() {
            string content = File.ReadAllText("team-1.json");
            var team = JsonLoader.LoadTeam(content);

            var generation = new List<GenerationTeam>();

            const int numGenerations = 1;
            const int teamsPerGeneration = 100;
            for (int i = 0; i < teamsPerGeneration; i++) {
                generation.Add(new GenerationTeam(RandomTeam(3, 2), 0.0));
            }

            var queue = new ConcurrentQueue<GenomeTask>();

            const bool sequential = true;

            if (!sequential) {
                // TODO - pocet threadu podle HW cpu
                StartThreadpool(12, team, queue);
            }

            for (int i = 0; i < numGenerations; i++) {
                var genWatch = new Stopwatch();
                genWatch.Start();

                if (sequential) {
                    foreach (var genTeam in generation) {
                        PopulationMember(team, genTeam, Console.Out);

                        //Console.WriteLine("Win stats:");
                        //foreach (var pair in GameInstanceEvaluator.GlobalControllerStatistics) {
                        //    Console.WriteLine($"{pair.Key}: {pair.Value}");
                        //}
                        //Console.WriteLine(
                        //    $"Expand: {UctAlgorithm.ExpandCount}, BestChild: {UctAlgorithm.BestChildCount}, Ratio: {(float) UctAlgorithm.ExpandCount / (float) UctAlgorithm.BestChildCount}");
                        //Console.WriteLine("\n\n");
                    }
                } else {
                    done = 0;

                    foreach (var genTeam in generation) {
                        queue.Enqueue(new GenomeTask() {GenTeam = genTeam, IsPoison = false});
                    }

                    while (Interlocked.Read(ref done) < generation.Count) {
                        Thread.Yield();
                    }
                }

                Console.WriteLine("****************************************************");
                Console.WriteLine("****************************************************");
                Console.WriteLine($"Generation {i}. done in {genWatch.ElapsedMilliseconds}ms");
                Console.WriteLine("****************************************************");
                Console.WriteLine("****************************************************");

                queue.Enqueue(new GenomeTask() {IsPoison = true});
            }
        }

        public static void PopulationMember(Team team, GenerationTeam genTeam, TextWriter writer) {
            var teamWatch = new Stopwatch();
            teamWatch.Start();
            var setup = new Setup() {red = team.mobs, blue = genTeam.Team.mobs};
            var results = GameInstanceEvaluator.EvaluateSetup(setup, writer);

            var result = new EvaluationResult();

            float totalHpFitness = 0;

            foreach (var res in results) {
                totalHpFitness += res.HpFitness;
                result.BlueWins += res.BlueWins;
                result.RedWins += res.RedWins;
                result.Timeouts += res.Timeouts;
                result.TotalTurns += res.TotalTurns;
                result.TotalIterations += res.TotalIterations;
                result.TotalElapsedMilliseconds += res.TotalElapsedMilliseconds;
            }

            float fitness = totalHpFitness / results.Count;

            genTeam.Rating = result.WinPercentage;

            string dnaString = string.Join(",", GenomeLoader.FromTeam(genTeam.Team).Data);

            writer.WriteLine($"HP Fitness: {fitness}\tWinrate: {genTeam.Rating}\t{dnaString}");

            Mutate(genTeam.Team, result.WinPercentage - 0.5);

            double mpi = result.MillisecondsPerIteration;
            double mpt = result.MillisecondsPerTurn;

            //writer.WriteLine(
            //    $"Total:\t\t{teamWatch.ElapsedMilliseconds}ms\nPer turn:\t{mpt}ms\nPer iteration:\t{mpi}ms");
            //writer.WriteLine();
            //writer.WriteLine();
        }

        public static void Mutate(Team team, double scale) {
            var rand = Generator.Random;

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
                    // TODO: cooldown mutations are disabled
                    // (int) Math.Max(0, ability.cooldown + 1 * scale);
                    ability.cooldown = 0;
                    break;
                case 4:
                    mob.hp = (int) Math.Max(mob.hp + 5 * scale, 1);
                    break;
                case 5:
                    mob.ap = (int) Math.Max(mob.ap + 5 * scale, 1);
                    break;
            }
        }

        public static Team RandomTeam(int mobs, int spellsPerMob) {
            var rand = Generator.Random;

            var team = new Team();
            team.mobs = new List<JsonMob>();

            for (int i = 0; i < mobs; i++) {
                var abilities = new List<JsonAbility>();

                for (int j = 0; j < spellsPerMob; j++) {
                    abilities.Add(new JsonAbility(Generator.RandomDmg(),
                                                  Generator.RandomCost(),
                                                  Generator.RandomRange(),
                                                  0));
                    // rand.Next(2)));
                }

                team.mobs.Add(new JsonMob {
                    abilities = abilities,
                    hp = Generator.RandomHp(),
                    ap = Generator.RandomAp()
                });
            }

            return team;
        }

        private void StartThreadpool(int poolSize, Team team, ConcurrentQueue<GenomeTask> queue) {
            var threads = new List<Thread>();

            // TODO - moznost threadpool jednoduse zabit
            for (int j = 0; j < poolSize; j++) {
                var thread = new Thread(() => {
                    //while (Interlocked.Read(ref done) < generation.Count)
                    while (true) {
                        GenomeTask task;
                        if (queue.TryDequeue(out task)) {
                            if (task.IsPoison) {
                                queue.Enqueue(task);
                                break;
                            }

                            var writer = new StringWriter();

                            PopulationMember(team, task.GenTeam, writer);

                            writer.WriteLine();
                            writer.WriteLine("Win stats:\n" +
                                             $"Rule: {GameInstanceEvaluator.RuleBasedAiWins}\n" +
                                             $"MCTS: {GameInstanceEvaluator.MctsWins}\n" +
                                             $"RAND: {GameInstanceEvaluator.RandomAiWins}\n");

                            Interlocked.Increment(ref done);

                            Console.Write(writer.ToString());
                        }
                    }
                });
                threads.Add(thread);
                thread.Start();
            }
        }
    }
}