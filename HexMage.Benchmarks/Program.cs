using System;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;

namespace HexMage.Benchmarks {
    internal class Program {
        private static void Main(string[] args) {
            var size = 30;
            var g = new GameInstance(size);

            var pc1 = new AiRandomController(g);

            var t1 = TeamColor.Red;
            var t2 = TeamColor.Blue;

            var m1 = Generator.RandomMob(t1, size, _ => true);
            var m2 = Generator.RandomMob(t2, size, c => !c.Equals(m1.Coord));

            var turnManager = g.TurnManager;
            var mobManager = g.MobManager;
            var pathfinder = g.Pathfinder;

            mobManager.AddMob(m1);
            mobManager.AddMob(m2);

            m1.Coord = new AxialCoord(0, 0);
            m2.Coord = new AxialCoord(0, 1);

            var map = g.Map;

            var s = new Stopwatch();
            var iterations = 0;
            s.Start();
            while (iterations < 10000000) {
                foreach (var mob in mobManager.Mobs) {
                    mob.Hp = mob.MaxHp;
                }

                while (!g.IsFinished()) {
                    iterations++;
                    //Console.WriteLine("Taking a turn");

                    if (iterations%100000 == 0) {
                        Console.WriteLine("Done {0} iterations", iterations);
                    }

                    var mob = turnManager.CurrentMob;
                    var targets = g.PossibleTargets(mob);

                    if (targets.Count > 0) {
                        var target = targets.First();
                        var abilities = g.UsableAbilities(mob, target);

                        if (abilities.Count > 0) {
#warning Change this to await the defense query
                            abilities.First().Use(map, mobManager);
                        } else {
                            var path = pathfinder.PathTo(target.Coord);

                            pathfinder.MoveAsFarAsPossible(mob, path);
                        }
                    } else {
                        Console.WriteLine("No enemies in range, moving closer");
                        var enemies = g.Enemies(mob);

                        Debug.Assert(enemies.Count > 0);

                        var enemy = enemies.First();
                        var path = pathfinder.PathTo(enemy.Coord);
                        pathfinder.MoveAsFarAsPossible(mob, path);
                    }

                    turnManager.NextMobOrNewTurn(pathfinder);
                }

                //Console.WriteLine("Starting a new game");
            }

            Console.WriteLine("Took {0}ms", s.ElapsedMilliseconds);
        }
    }
}