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
        public class GenerationMember {
            public DNA dna;
            public EvaluationResult fitness;

            public override string ToString() {
                return fitness.ToFitnessString(dna);
            }
        }

        private GameInstance game;
        private DNA initialDna;
        const int evolutionMapSize = 5;

        public Evolution() {
            string content = File.ReadAllText("team-1.json");
            var team = JsonLoader.LoadTeam(content);
            team.mobs.RemoveAt(0);

            initialDna = GenomeLoader.FromTeam(team);
            Console.WriteLine($"Initial: {initialDna.ToDNAString()}\n\n");

            game = new GameInstance(evolutionMapSize);

            new Setup(team.mobs, team.mobs).UnpackIntoGame(game);

            GameInstanceEvaluator.PreparePositions(game, game.MobManager.Mobs, 0, evolutionMapSize);
        }

        public void Run() {
            var generation = new List<GenerationMember>();

            const int numGenerations = 100000;
            const int teamsPerGeneration = 1;
            for (int i = 0; i < teamsPerGeneration; i++) {
                var copy = initialDna.Copy();
                for (int j = 0; j < initialDna.Data.Count; j++) {
                    copy.Data[j] = (float) Generator.Random.NextDouble();
                }

                var member = new GenerationMember();
                member.dna = copy;
                member.fitness = CalculateFitness(copy);

                generation.Add(member);
            }

            double T = 1;

            for (int i = 0; i < numGenerations; i++) {
                T -= 1.0 / numGenerations;

                var genWatch = new Stopwatch();
                genWatch.Start();

                for (int j = 0; j < generation.Count; j++) {
                    var teamWatch = new Stopwatch();
                    teamWatch.Start();

                    var member = generation[j];

                    var newDna = Mutate(member.dna);
                    var newFitness = CalculateFitness(newDna);

                    if (newFitness.HpFitness <= 0.015) {
                        Console.WriteLine("Found extra good");
                    }

                    // We don't want to move into a timeouted state to save time
                    // TODO - check if disabling this helps
                    if (newFitness.Timeouted) continue;

                    float e = member.fitness.HpFitness;
                    float ep = newFitness.HpFitness;

                    if (e <= ep) {
                        if (Probability.Uniform(1 - T)) {
                            member.fitness = newFitness;
                            member.dna = newDna;
                        }
                    } else {
                        float probability = (float) Math.Exp(-(ep - e)/T);

                        if (probability > Generator.Random.NextDouble()) {
                            member.fitness = newFitness;
                            member.dna = newDna;
                            //Console.WriteLine($"Zih {probability}");
                        } else {
                            //Console.WriteLine($"Nezih {probability}");
                        }
                    }

                    //var fitness = CalculateFitness(dna);

                    Console.WriteLine(member);

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
                //Console.WriteLine($"Generation {i}. done in {genWatch.ElapsedMilliseconds}ms");
                //Console.WriteLine("****************************************************");
            }
        }

        public EvaluationResult CalculateFitness(DNA dna) {
            PrepareGame(dna);

            var result = new GameInstanceEvaluator(game, Console.Out).Evaluate();

            return result;
        }

        public static DNA Mutate(DNA dna) {
            var copy = dna.Copy();

            do {
                var i = Generator.Random.Next(0, dna.Data.Count);

                //for (int i = 0; i < dna.Data.Count; i++) {
                float delta = (float) Generator.Random.NextDouble() / dna.Data.Count;

                float change = Generator.Random.Next(0, 2) == 0 ? 1 + delta : 1 - delta;

                copy.Data[i] = Mathf.Clamp(0.01f, dna.Data[i] * change, 1);

                // TODO - zkusit ruzny pravdepodobnosti - ovlivnovat to teplotou?
            } while (Probability.Uniform(0.25f));

            return copy;
        }

        public void PrepareGame(DNA dna) {
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


        // TODO - remove this
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

        // TODO - remove this

        public static Team RandomTeam(int mobs, int spellsPerMob) {
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
    }
}