using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class Pathfinder {
        private readonly Map _map;
        private readonly MobManager _mobManager;
        public HexMap<Path> Paths { get; set; }

        public Pathfinder(Map map, MobManager mobManager) {
            _map = map;
            _mobManager = mobManager;
            Paths = new HexMap<Path>(map.Size);
            Paths.Initialize(() => new Path());
        }

        private int Size => _map.Size;

        public IList<AxialCoord> PathTo(AxialCoord target) {
            var result = new List<AxialCoord>();

            AxialCoord current = target;
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

        public AxialCoord FurthestPointToTarget(Mob mob, Mob target) {
            var path = PathTo(target.Coord);
            Debug.Assert(path.Count > 1, "Calculating a path while standing next to an enemy.");

            path.RemoveAt(path.Count - 1);
            return FurthestPointOnPath(mob, path);
        }

        public AxialCoord FurthestPointOnPath(Mob mob, IList<AxialCoord> path) {
            int currentAp = mob.Ap;
            for (int i = path.Count - 1; i >= 0; i--) {
                if (currentAp == 0) {
                    return path[i];
                }

                currentAp--;
            }

            throw new InvalidOperationException("Trying to move on an empty path, which is invalid.");
        }

        public void MoveAsFarAsPossible(Mob mob, IList<AxialCoord> path) {
            int i = path.Count - 1;

            while (mob.Ap > 0 && i > 0) {
                _mobManager.MoveMob(mob, path[i]);
                i--;
            }
        }

        public int Distance(AxialCoord c) {
            return Paths[c].Distance;
        }

        public void PathfindFrom(AxialCoord start) {
            var queue = new Queue<AxialCoord>();

            var diffs = new List<AxialCoord> {
                new AxialCoord(-1, 0),
                new AxialCoord(1, 0),
                new AxialCoord(0, -1),
                new AxialCoord(0, 1),
                new AxialCoord(1, -1),
                new AxialCoord(-1, 1),
            };

            foreach (var coord in _map.AllCoords) {
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
                    Utils.Log(LogSeverity.Error, nameof(Pathfinder), "Pathfinder stuck when calculating a path.");
                }

                // TODO - opet, nema byt Y a X?
                var p = Paths[current];

                if (p.State == VertexState.Closed) continue;

                p.Reachable = true;
                p.State = VertexState.Closed;

                foreach (var diff in diffs) {
                    AxialCoord neighbour = current + diff;

                    if (IsValidCoord(neighbour)) {
                        Path n = Paths[neighbour];

                        // TODO - this is not right
                        if (n == null) continue;

                        bool notClosed = n.State != VertexState.Closed;
                        bool noWall = _map[neighbour] != HexType.Wall;
#warning TODO - fix pathfinder so that it doesn't walk into mobs but still allows abilities to target them
                        bool noMob = true || _mobManager.AtCoord(neighbour) == null || neighbour == start;

                        //if (notClosed && noWall && noMob) {
                        if (notClosed && noWall && noMob) {
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

        public bool IsValidCoord(AxialCoord c) {
            //return _map.AllCoords.Contains(c);
            return c.ToCube().Sum() == 0 && _map.CubeDistance(new CubeCoord(0, 0, 0), c) <= _map.Size;
            //return c.Abs().Max() < Size && c.Min() >= 0;
        }
    }
}