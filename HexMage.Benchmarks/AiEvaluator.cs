using System;
using System.IO;
using HexMage.Simulator;

namespace HexMage.Benchmarks {
    public class AiEvaluator {
        public void Run() {
            for (int mobs = 2; mobs < 5; mobs++) {
                for (int spellsPerMob = 1; spellsPerMob < 3; spellsPerMob++) {
                    for (int i = 0; i < 100; i++) {
                        Console.WriteLine("*********************************");
                        Console.WriteLine($"M:{mobs}, S:{spellsPerMob}, {i}:\n");

                        string content = File.ReadAllText("team-1.json");
                        var team1 = JsonLoader.LoadTeam(content);

                        var team = Evolution.RandomTeam(mobs, spellsPerMob);

                        // TODO - na co tu je rating?
                        Evolution.PopulationMember(team1, new GenerationTeam(team1, 0.0), Console.Out);
                    }
                }
            }
        }
    }
}