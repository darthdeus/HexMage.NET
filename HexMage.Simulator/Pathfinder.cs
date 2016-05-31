using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HexMage.Simulator
{
    public class Pathfinder
    {
        public Matrix<Path> Paths { get; set; }
        public int Size { get; set; }

        public Pathfinder(int size) {
            Size = size;
            Paths = new Matrix<Path>(size, size);
            Paths.Initialize(() => new Path());
        }

        public IList<Coord> PathTo(Coord target) {
            var result = new List<Coord>();

            Coord current = target;
            result.Add(current);

            Path path = Paths[current];

            // TODO - this is ugly, the hexagonal-map abstraction leaks
            if (path == null) {
                return result;
            }

            int iterations = 1000;
            while (path.Distance > 0 && --iterations > 0) {
                if (path.Source != null) {
                    result.Add(path.Source.Value);
                    path = Paths[path.Source.Value];
                } else {
                    result.Clear();
                    break;
                }
            }

            Debug.Assert(iterations > 0);
            return result;
        }

        public void MoveAsFarAsPossible(MobManager mobManager, Mob mob, IList<Coord> path) {
            int i = path.Count - 1;

            while (mob.AP > 0 && i > 0) {
                mobManager.MoveMob(mob, path[i]);
                i--;
            }
        }

        public int Distance(Coord c) {
            return Paths[c].Distance;
        }

        public void PathfindFrom(Coord start, Map map, MobManager mobManager) {
            var queue = new Queue<Coord>();

            var diffs = new List<Coord> {
                new Coord(-1, 0),
                new Coord(1, 0),
                new Coord(0, -1),
                new Coord(0, 1),
                new Coord(1, -1),
                new Coord(-1, 1),
            };

            foreach (var coord in map.AllCoords) {
                var path = Paths[coord];

                path.Source = null;
                path.Distance = Int32.MaxValue;
                path.Reachable = false;
                path.State = VertexState.Unvisited;
            }

            // TODO - nema byt Y - X?
            var startPath = Paths[start];

            startPath.Distance = 0;
            startPath.State = VertexState.Open;
            startPath.Reachable = true;

            queue.Enqueue(start);

            int iterations = 0;

            while (queue.Count > 0) {
                var current = queue.Dequeue();

                iterations++;

                if (iterations > Size*Size*10 || queue.Count > 1000) {
                    Console.WriteLine("CHYBA, PATHFINDING SE ZASEKL");
                }

                // TODO - opet, nema byt Y a X?
                var p = Paths[current];

                if (p.State == VertexState.Closed) continue;

                p.Reachable = true;
                p.State = VertexState.Closed;

                foreach (var diff in diffs) {
                    Coord neighbour = current + diff;

                    if (IsValidCoord(neighbour)) {
                        Path n = Paths[neighbour];

                        bool notClosed = n.State != VertexState.Closed;
                        bool noWall = map[neighbour] != HexType.Wall;
                        bool noMob = mobManager.AtCoord(neighbour) == null;

                        //if (notClosed && noWall && noMob) {
                        if (notClosed && noWall) {
                            if (n.State == VertexState.Unvisited || n.Distance > p.Distance + 1) {
                                n.Distance = p.Distance + 1;
                                n.Source = current;
                                n.Reachable = true;
                            }

                            n.State = VertexState.Open;
                            queue.Enqueue(neighbour);
                        }
                    }
                }
            }
        }

        public bool IsValidCoord(Coord c) {
            return c.Abs().Max() < Size && c.Min() >= 0;
        }
    }
}