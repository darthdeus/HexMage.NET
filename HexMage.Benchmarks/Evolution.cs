using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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

            DNA initialDna = GenomeLoader.FromTeam(team);
            Console.WriteLine($"Initial: {initialDna.ToDNAString()}\n\n");

            const int evolutionMapSize = 5;
            var game = new GameInstance(evolutionMapSize);

            new Setup(team.mobs, team.mobs).UnpackIntoGame(game);

            GameInstanceEvaluator.PreparePositions(game, game.MobManager.Mobs, 0, evolutionMapSize);

            var generation = new List<DNA>();

            const int numGenerations = 10000;
            const int teamsPerGeneration = 1;
            for (int i = 0; i < teamsPerGeneration; i++) {
                var copy = initialDna.Copy();
                for (int j = 0; j < initialDna.Data.Count; j++) {
                    copy.Data[j] = (float) Generator.Random.NextDouble();
                }

                generation.Add(copy);
            }

            var queue = new ConcurrentQueue<GenomeTask>();

            const bool sequential = true;

            if (!sequential) {
                // TODO - pocet threadu podle HW cpu
                //StartThreadpool(12, team, queue);
            }

            for (int i = 0; i < numGenerations; i++) {
                var genWatch = new Stopwatch();
                genWatch.Start();

                foreach (var dna in generation) {
                    var teamWatch = new Stopwatch();
                    teamWatch.Start();

                    var fitness = CalculateFitness(game, initialDna, dna);

                    string fstr = fitness.HpFitness.ToString("0.00");
                    string wstr = fitness.WinPercentage.ToString("0.0");

                    Console.WriteLine($"F:{fstr}\tW:{wstr}\t{dna.ToDNAString()}");

                    Mutate(dna);

                    //Mutate(dna, result.WinPercentage - 0.5);

                    //double mpi = fitness.MillisecondsPerIteration;
                    //double mpt = fitness.MillisecondsPerTurn;

                    //writer.WriteLine(
                    //    $"Total:\t\t{teamWatch.ElapsedMilliseconds}ms\nPer turn:\t{mpt}ms\nPer iteration:\t{mpi}ms");
                    //writer.WriteLine();
                    //writer.WriteLine();

                    //Console.WriteLine("Win stats:");
                    //foreach (var pair in GameInstanceEvaluator.GlobalControllerStatistics) {
                    //    Console.WriteLine($"{pair.Key}: {pair.Value}");
                    //}
                    //Console.WriteLine(
                    //    $"Expand: {UctAlgorithm.ExpandCount}, BestChild: {UctAlgorithm.BestChildCount}, Ratio: {(float) UctAlgorithm.ExpandCount / (float) UctAlgorithm.BestChildCount}");
                    //Console.WriteLine("\n\n");
                }

                //Console.WriteLine("****************************************************");
                //Console.WriteLine("****************************************************");
                //Console.WriteLine($"Generation {i}. done in {genWatch.ElapsedMilliseconds}ms");
                //Console.WriteLine("****************************************************");
                //Console.WriteLine("****************************************************");

                queue.Enqueue(new GenomeTask() {IsPoison = true});
            }
        }

        public static EvaluationResult CalculateFitness(GameInstance game, DNA initialDna, DNA dna) {
            PrepareGame(game, initialDna, dna);

            var result = new GameInstanceEvaluator(game, Console.Out).Evaluate();

            return result;
        }

        public static void Mutate(DNA dna) {
            for (int i = 0; i < dna.Data.Count; i++) {
                float delta = (float) Generator.Random.NextDouble() / dna.Data.Count;
                float change;
                if (Generator.Random.Next(0, 2) == 0) {
                    change = 1 + delta;
                } else {
                    change = 1 - delta;
                }

                dna.Data[i] = Mathf.Clamp(0.01f, dna.Data[i] * change, 1);
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


        public static void PrepareGame(GameInstance game, DNA initialDna, DNA dna) {
            var genTeam = GenomeLoader.FromDna(dna);

            for (int i = initialDna.MobCount, dnaIndex = 0; i < game.MobManager.Mobs.Count; i++, dnaIndex++) {
                var mobId = game.MobManager.Mobs[i];

                var genMob = genTeam.mobs[dnaIndex];

                var mobInfo = game.MobManager.MobInfos[mobId];
                mobInfo.MaxHp = genMob.hp;
                mobInfo.MaxAp = genMob.ap;

                for (int abilityIndex = 0; abilityIndex < mobInfo.Abilities.Count; abilityIndex++) {
                    int abilityId = mobInfo.Abilities[abilityIndex];
                    var genAbility = genMob.abilities[abilityIndex];

                    var abilityInfo = game.MobManager.Abilities[abilityId];
                    abilityInfo.Dmg = genAbility.dmg;
                    abilityInfo.Cost = genAbility.ap;
                    abilityInfo.Range = genAbility.range;

                    game.MobManager.Abilities[abilityId] = abilityInfo;
                }

                game.MobManager.MobInfos[mobId] = mobInfo;
            }
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

        // TODO - aktualizovat z singlethreaded verze
        //private void StartThreadpool(int poolSize, Team team, ConcurrentQueue<GenomeTask> queue) {
        //    var threads = new List<Thread>();

        //    // TODO - moznost threadpool jednoduse zabit
        //    for (int j = 0; j < poolSize; j++) {
        //        var thread = new Thread(() => {
        //            //while (Interlocked.Read(ref done) < generation.Count)
        //            while (true) {
        //                GenomeTask task;
        //                if (queue.TryDequeue(out task)) {
        //                    if (task.IsPoison) {
        //                        queue.Enqueue(task);
        //                        break;
        //                    }

        //                    var writer = new StringWriter();

        //                    PopulationMember(team, task.GenTeam, writer);

        //                    writer.WriteLine();
        //                    writer.WriteLine("Win stats:\n" +
        //                                     $"Rule: {GameInstanceEvaluator.RuleBasedAiWins}\n" +
        //                                     $"MCTS: {GameInstanceEvaluator.MctsWins}\n" +
        //                                     $"RAND: {GameInstanceEvaluator.RandomAiWins}\n");

        //                    Interlocked.Increment(ref done);

        //                    Console.Write(writer.ToString());
        //                }
        //            }
        //        });
        //        threads.Add(thread);
        //        thread.Start();
        //    }
        //}
    }
}