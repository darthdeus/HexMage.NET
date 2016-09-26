using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class Pathfinder : IResettable {
        private readonly List<AxialCoord> _diffs;
        private readonly Map _map;
        private readonly MobManager _mobManager;

        public Pathfinder(Map map, MobManager mobManager) {
            _map = map;
            _mobManager = mobManager;
            Paths = new HexMap<Path>(map.Size);
            Paths.Initialize(() => new Path());

            _diffs = new List<AxialCoord> {
                new AxialCoord(-1, 0),
                new AxialCoord(1, 0),
                new AxialCoord(0, -1),
                new AxialCoord(0, 1),
                new AxialCoord(1, -1),
                new AxialCoord(-1, 1)
            };
        }

        public HexMap<Path> Paths { get; set; }

        private int Size => _map.Size;

        public void Reset() {
            // Right now we're not caching anything, so there's nothing to reset
        }

        public IList<AxialCoord> PathToMob(Mob mob) {
            var target = NearestEmpty(mob.Coord);
            if (target.HasValue) {
                return PathTo(target.Value);
            } else {
                return null;
            }
        }

        public IList<AxialCoord> PathTo(AxialCoord target) {
            var result = new List<AxialCoord>();

            // Return an empty path if the coord is invalid
            if (!IsValidCoord(target)) return result;

            if (_mobManager.AtCoord(target) != null) {
                //return result;
                throw new InvalidOperationException(
                    $"Searching for a path into a mob is not allowed, use {nameof(PathToMob)} instead");
            }

            var current = target;
            result.Add(current);

            var path = Paths[current];

            if (path == null) return result;

            var iterations = 1000;
            while ((path.Distance > 0) && (--iterations > 0)) {
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

        public AxialCoord? FurthestPointToTarget(Mob mob, Mob target) {
            Utils.Log(LogSeverity.Debug, nameof(Pathfinder), $"Finding path from {mob.Coord} to {target.Coord}");
            var path = PathToMob(target);

            if (path != null) {
                return FurthestPointOnPath(mob, path);
            } else {
                return null;
            }
        }


        private bool IsWalkable(AxialCoord coord) {
            return IsValidCoord(coord) && (_map[coord] == HexType.Empty) && (_mobManager.AtCoord(coord) == null);
        }

        public AxialCoord FurthestPointOnPath(Mob mob, IList<AxialCoord> path) {
            if (path.Count == 0) {
                string errmsg = $"Trying to move on an empty path from {mob.Coord}, which is invalid.";
                Utils.Log(LogSeverity.Error, nameof(Pathfinder), errmsg);
                throw new InvalidOperationException(errmsg);
            }

            var currentAp = mob.Ap;
            for (var i = path.Count - 1; i >= 0; i--) {
                if (currentAp == 0) return path[i];

                currentAp--;
            }

            return path[0];
        }

#warning TODO - move this some place else, it doesn't belong into the pathfinder
        [Obsolete]
        public void MoveAsFarAsPossible(Mob mob, IList<AxialCoord> path) {
            var i = path.Count - 1;

            while ((mob.Ap > 0) && (i > 0)) {
                _mobManager.MoveMob(mob, path[i]);
                i--;
            }
        }

        public int Distance(AxialCoord c) {
            return Paths[c].Distance;
        }

        public void PathfindFromCurrentMob(TurnManager turnManager) {
            if (turnManager.CurrentMob != null) PathfindFrom(turnManager.CurrentMob.Coord);
        }

        public void PathfindFrom(AxialCoord start) {
            var queue = new Queue<AxialCoord>();

            foreach (var coord in _map.AllCoords) {
                var path = Paths[coord];

                path.Source = null;
                path.Distance = int.MaxValue;
                path.Reachable = false;
                path.State = VertexState.Unvisited;
            }

            var startPath = Paths[start];

            startPath.Distance = 0;
            startPath.State = VertexState.Open;
            startPath.Reachable = true;

            queue.Enqueue(start);

            var iterations = 0;

            while (queue.Count > 0) {
                var current = queue.Dequeue();

                iterations++;

                if ((iterations > Size*Size*10) || (queue.Count > 1000))
                    Utils.Log(LogSeverity.Error, nameof(Pathfinder), "Pathfinder stuck when calculating a path.");

                var p = Paths[current];

                if (p.State == VertexState.Closed) continue;

                p.Reachable = true;
                p.State = VertexState.Closed;

                foreach (var diff in _diffs) {
                    var neighbour = current + diff;

                    if (IsValidCoord(neighbour)) {
                        // We can immediately skip the starting position to avoid further complicated checks
                        if (neighbour == start) continue;

                        var n = Paths[neighbour];

                        // TODO - this is not right
                        if (n == null) continue;

                        if ((n.State != VertexState.Closed) && IsWalkable(neighbour)) {
                            if ((n.State == VertexState.Unvisited) || (n.Distance > p.Distance + 1)) {
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

            UpdateMobPaths();
        }

        private AxialCoord? NearestEmpty(AxialCoord coord) {
            AxialCoord? result = null;
            foreach (var diff in _diffs) {
                var neighbour = coord + diff;
                if (IsWalkable(neighbour)) {
                    if (!result.HasValue) result = neighbour;

                    if (Distance(neighbour) < Distance(result.Value)) {
                        result = neighbour;
                    }
                }
            }

            return result;
        }

        public bool IsValidCoord(AxialCoord c) {
            return (c.ToCube().Sum() == 0) && (_map.CubeDistance(new CubeCoord(0, 0, 0), c) <= _map.Size);
        }


        private void UpdateMobPaths() {
            foreach (var mob in _mobManager.Mobs) {
                var path = Paths[mob.Coord];

                foreach (var diff in _diffs) {
                    var neighbour = mob.Coord + diff;

                    if (IsValidCoord(neighbour) && Distance(neighbour) < path.Distance) {
                        path.Distance = 1 + Distance(neighbour);
                        path.Source = neighbour;
                        path.State = VertexState.Closed;
                        path.Reachable = true;
                    }
                }
            }
        }
    }
}