using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.PCG;

namespace HexMage.Benchmarks {
    public class Evolution {
        private readonly GameInstance game;
        private readonly DNA initialDna;

        public Evolution() {
            if (!Directory.Exists(Constants.SaveDir)) {
                Directory.CreateDirectory(Constants.SaveDir);
            }

            string content = File.ReadAllText("team-1.json");
            var team = JsonLoader.LoadTeam(content);
            team.mobs.RemoveAt(0);

            initialDna = GenomeLoader.FromTeam(team);
            //initialDna.Randomize();

            Console.WriteLine($"Initial: {initialDna.ToDNAString()}\n\n");

            game = GameSetup.FromDNAs(initialDna, initialDna);

            //game = new GameInstance(Constants.EvolutionMapSize);

            //GameSetup.UnpackTeamsIntoGame(game, initialDna, initialDna);
            //game.PrepareEverything();

            //GameSetup.ResetPositions(game);
        }

        public void Run() {
            var generation = new List<GenerationMember>();

            const int numGenerations = 100000;
            const int teamsPerGeneration = 1;
            for (int i = 0; i < teamsPerGeneration; i++) {
                var copy = initialDna.Clone();
                copy.Randomize();

                var member = new GenerationMember {
                    dna = copy,
                    result = CalculateFitness(copy)
                };

                generation.Add(member);
            }

            const double initialT = 100;
            double Tpercentage = 1;
            double T = initialT;

            List<double> plotT = new List<double>();
            List<double> plotFit = new List<double>();
            List<double> plotProb = new List<double>();

            int extraIterations = 10000;
            int maxTotalHp = 0;

            int goodCount = 0;
            bool goodEnough = false;

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
                    var newFitness = CalculateFitness(newDna);

                    const bool saveGoodOnes = true;

                    if (saveGoodOnes && !goodEnough && newFitness.Fitness > 0.995) {
                        goodCount++;
                        if (goodCount > 50) goodEnough = true;
                        Console.WriteLine($"Found extra good {newFitness.Fitness}");

                        SaveDna(goodCount, member.dna);
                    }

                    if (newFitness.Tainted) {
                        SaveTainted(newDna);
                        i = numGenerations;
                    }

                    // We don't want to move into a timeouted state to save time
                    // TODO - check if disabling this helps
                    //if (newFitness.Timeouted) continue;

                    float ep = member.result.Fitness;
                    float e = newFitness.Fitness;

                    double probability = Math.Exp(-(ep - e) / T);

                    const bool alwaysJumpToBetter = false;

                    if (((ep - e) > 0 && alwaysJumpToBetter) || Probability.Uniform(probability)) {
                        member.result = newFitness;
                        member.dna = newDna;
                    }

                    plotT.Add(T);
                    plotFit.Add(member.result.Fitness);
                    plotProb.Add(probability);

                    if (i % 1000 == 0) {
                        Console.WriteLine($"P: {probability.ToString("0.000")}\tT:{T.ToString("0.0000")}\t{member}");
                    }
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
            Constants.ResetLogBuffer();
            GameSetup.OverrideGameDNA(game, initialDna, dna);

            var result = new GameInstanceEvaluator(game, Console.Out).Evaluate();

            return result;
        }

        public static DNA Mutate(DNA dna, float T) {
            var copy = dna.Clone();

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

      
        private void SaveTainted(DNA dna) {
            using (var logWriter = new StreamWriter(Constants.SaveDir + "tainted-log.txt")) {
                logWriter.Write(Constants.GetLogBuffer().ToString());
            }

            SaveDna(666, dna);
        }

        private void SaveDna(int savefileIndex, DNA dna) {
            using (var writer = new StreamWriter(Constants.BuildEvoSavePath(savefileIndex))) {
                writer.WriteLine(initialDna.ToSerializableString());
                writer.WriteLine(dna.ToSerializableString());
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

        private static void LogStats() {
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
    }
}