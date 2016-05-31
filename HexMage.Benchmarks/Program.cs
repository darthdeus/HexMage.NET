using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.Simulator;

namespace HexMage.Benchmarks
{
    class Program
    {
        static void Main(string[] args) {
            int size = 30;
            var g = new GameInstance(size);

            var t1 = g.MobManager.AddTeam();
            var t2 = g.MobManager.AddTeam();

            var m1 = Generator.RandomMob(t1, size, _ => true);
            var m2 = Generator.RandomMob(t2, size, c => !c.Equals(m1.Coord));

            var turnManager = g.TurnManager;
            var mobManager = g.MobManager;
            var pathfinder = g.Pathfinder;

            mobManager.Mobs.Add(m1);
            mobManager.Mobs.Add(m2);

            m1.Coord = new Coord(0, 0);
            m2.Coord = new Coord(0, 1);

            var map = g.Map;

            var s = new Stopwatch();
            int iterations = 0;
            s.Start();
            while (iterations < 10000000) {
                mobManager.Mobs.ForEach(mob => mob.HP = mob.MaxHP);

                while (!g.IsFinished()) {
                    iterations++;
                    //Console.WriteLine("Taking a turn");

                    if (iterations%100000 == 0) {
                        Console.WriteLine("Done {0} iterations", iterations);
                    }

                    if (turnManager.IsTurnDone()) {
                        //Console.WriteLine("Starting next turn");
                        turnManager.StartNextTurn();
                    } else {
                        var mob = turnManager.CurrentMob();
                        var targets = g.PossibleTargets(mob);

                        if (targets.Count > 0) {
                            var target = targets.First();
                            var abilities = g.UsableAbilities(mob, target);

                            if (abilities.Count > 0) {
                                abilities.First().Use();
                            } else {
                                var path = pathfinder.PathTo(target.Coord);

                                pathfinder.MoveAsFarAsPossible(mobManager, mob, path);
                            }
                        } else {
                            Console.WriteLine("No enemies in range, moving closer");
                            var enemies = g.Enemies(mob);

                            Debug.Assert(enemies.Count > 0);

                            var enemy = enemies.First();
                            var path = pathfinder.PathTo(enemy.Coord);
                            pathfinder.MoveAsFarAsPossible(mobManager, mob, path);
                        }

                        if (turnManager.MoveNext()) {
                            //pathfinder.PathfindFrom(turnManager.CurrentMob().Coord, map, mobManager);
                        }
                    }
                }

                //Console.WriteLine("Starting a new game");
            }

            Console.WriteLine("Took {0}ms", s.ElapsedMilliseconds);
        }
    }
}