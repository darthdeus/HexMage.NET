using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;

namespace HexMage.Benchmarks {
    public class Evolution {
        public static readonly string SaveFile = @"evo-save.txt";
        public static readonly string SaveDir = "save-files/";

        public class GenerationMember {
            public DNA dna;
            public EvaluationResult result;

            public override string ToString() {
                return result.ToFitnessString(dna);
            }
        }

        private GameInstance game;
        private DNA initialDna;
        const int evolutionMapSize = 5;

        public Evolution() {
            if (!Directory.Exists(SaveDir)) {
                Directory.CreateDirectory(SaveDir);
            }

            string content = File.ReadAllText("team-1.json");
            var team = JsonLoader.LoadTeam(content);
            team.mobs.RemoveAt(0);

            initialDna = GenomeLoader.FromTeam(team);
            initialDna.Randomize();

            Console.WriteLine($"Initial: {initialDna.ToDNAString()}\n\n");

            game = new GameInstance(evolutionMapSize);

            GameInstanceEvaluator.UnpackTeamsIntoGame(game, initialDna, initialDna);
            GameInstanceEvaluator.PreparePositions(game);

            game.PrepareEverything();
        }

        public void Run() {
            var generation = new List<GenerationMember>();

            //double[] xx = new double[5000];
            //double[] yy = new double[5000];

            //for (int i = 0; i < 5000; i++) {
            //    double val = i * 30.0 / 5000.0;
            //    xx[i] = val;
            //    yy[i] = Probability.Norm(val);
            //}

            //GnuPlot.Plot(xx, yy);

            //Console.ReadKey();

            const int numGenerations = 100000;
            const int teamsPerGeneration = 1;
            for (int i = 0; i < teamsPerGeneration; i++) {
                var copy = initialDna.Copy();
                copy.Randomize();

                var member = new GenerationMember();
                member.dna = copy;
                member.result = CalculateFitness(copy);

                generation.Add(member);
            }

            const double initialT = 1;
            double Tpercentage = 1;
            double T = initialT;

            List<double> plotT = new List<double>();
            List<double> plotFit = new List<double>();
            List<double> plotProb = new List<double>();

            int extraIterations = 10000;
            int maxTotalHp = 0;

            int goodCount = 0;

            for (int i = 0; i < numGenerations; i++) {
                Tpercentage = Math.Max(0, Tpercentage - 1.0 / numGenerations);

                T = initialT * Tpercentage;

                if (Tpercentage < 0.01) {
                    i -= extraIterations;
                    extraIterations = 0;
                    T = 0.01;
                }

                var genWatch = new Stopwatch();
                genWatch.Start();

                for (int j = 0; j < generation.Count; j++) {
                    var teamWatch = new Stopwatch();
                    teamWatch.Start();

                    var member = generation[j];

                    var newDna = Mutate(member.dna, (float) T);
                    GameInstanceEvaluator.PreparePositions(game, game.MobManager.Mobs, 0, evolutionMapSize);
                    var newFitness = CalculateFitness(newDna);

                    double probability;

                    const bool saveGoodOnes = true;

                    if (saveGoodOnes && newFitness.Fitness > 0.995) {
                        goodCount++;
                        Console.WriteLine($"Found extra good {newFitness.Fitness}");

                        using (var writer = new StreamWriter(SaveDir + goodCount.ToString() + SaveFile)) {
                            writer.WriteLine(initialDna.ToSerializableString());
                            writer.WriteLine(member.dna.ToSerializableString());
                        }
                    }

                    // We don't want to move into a timeouted state to save time
                    // TODO - check if disabling this helps
                    //if (newFitness.Timeouted) continue;

                    float ep = member.result.Fitness;
                    float e = newFitness.Fitness;

                    probability = Math.Exp(-(ep - e) / T);

                    const bool alwaysJumpToBetter = false;

                    if (((e - ep) > 0 && alwaysJumpToBetter) || Probability.Uniform(probability)) {
                        member.result = newFitness;
                        member.dna = newDna;
                    }

                    plotT.Add(T);
                    plotFit.Add(member.result.Fitness);
                    plotProb.Add(probability);

                    if (i % 1000 == 0) {
                        Console.WriteLine($"P: {probability.ToString("0.000")}\tT:{T.ToString("0.0000")}\t{member}");
                    }

                    //if (e <= ep) {
                    //    if (Probability.Uniform(1 - T)) {
                    //        member.fitness = newFitness;
                    //        member.dna = newDna;
                    //    }
                    //} else {
                    //    float probability = (float) Math.Exp(-(ep - e)/T);

                    //    if (probability > Generator.Random.NextDouble()) {
                    //        member.fitness = newFitness;
                    //        member.dna = newDna;
                    //        //Console.WriteLine($"Zih {probability}");
                    //    } else {
                    //        //Console.WriteLine($"Nezih {probability}");
                    //    }
                    //}

                    //var fitness = CalculateFitness(dna);


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

            var gnuplotConfigString = $"title '{numGenerations} generations," +
                                      $"T_s = {initialT}'";

            GnuPlot.HoldOn();
            GnuPlot.Set($"xrange [{initialT}:0] reverse");
            GnuPlot.Set($"yrange [0:1] ");
            GnuPlot.Plot(plotT.ToArray(), plotFit.ToArray(), gnuplotConfigString);
            //GnuPlot.Plot(plotT.ToArray(), plotProb.ToArray(), gnuplotConfigString);
            Console.ReadKey();
        }

        public EvaluationResult CalculateFitness(DNA dna) {
            PrepareGame(dna);

            var result = new GameInstanceEvaluator(game, Console.Out).Evaluate();

            return result;
        }

        public static DNA Mutate(DNA dna, float T) {
            var copy = dna.Copy();

            do {
                var i = Generator.Random.Next(0, dna.Data.Count);

                //for (int i = 0; i < dna.Data.Count; i++) {
                double delta = Generator.Random.NextDouble() / dna.Data.Count;

                bool changeUp = Probability.Uniform(0.5);
                double change = changeUp ? 1 + delta : 1 - delta;

                copy.Data[i] = (float) Mathf.Clamp(0.01f, dna.Data[i] * change, 1);

                // TODO - zkusit ruzny pravdepodobnosti - ovlivnovat to teplotou?
            } while (Probability.Uniform(0.85f));

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