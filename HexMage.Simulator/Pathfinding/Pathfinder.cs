using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator.Pathfinding {
    public class Pathfinder {
        private static readonly List<AxialCoord> NeighbourDiffs = new List<AxialCoord> {
            new AxialCoord(-1, 0),
            new AxialCoord(1, 0),
            new AxialCoord(0, -1),
            new AxialCoord(0, 1),
            new AxialCoord(1, -1),
            new AxialCoord(-1, 1)
        };

        public HexMap<HexMap<Path>> AllPaths;

        private Dictionary<int, List<AxialCoord>> _precomputedPaths =
            new Dictionary<int, List<AxialCoord>>();


        [JsonIgnore] public GameInstance Game;

        [JsonIgnore]
        private int Size => Game.Size;

        public Pathfinder DeepCopy(GameInstance gameCopy) {
            var copy = new Pathfinder(gameCopy);

            copy.PathfindDistanceAll();

            return copy;
        }


        [JsonConstructor]
        public Pathfinder() { }

        public Pathfinder(GameInstance game) : this() {
            Game = game;
            AllPaths = new HexMap<HexMap<Path>>(Game.Size);
        }

        private Pathfinder(GameInstance game, HexMap<HexMap<Path>> allPaths) {
            Game = game;
            AllPaths = allPaths;
        }

        public AxialCoord? FurthestPointToTarget(CachedMob mob, CachedMob target) {
            List<AxialCoord> path = PrecomputedPathTo(mob.MobInstance.Coord, target.MobInstance.Coord);

            if (path.Count == 0 && mob.MobInstance.Coord.Distance(target.MobInstance.Coord) == 1) {
                return null;
            }

            AxialCoord? furthestPoint = null;
            foreach (var coord in path) {
                int distance = Distance(mob.MobInstance.Coord, coord);
                var mobAtCoord = Game.State.AtCoord(coord, true);

                if (distance <= mob.MobInstance.Ap) {
                    if (mobAtCoord == null) {
                        furthestPoint = coord;
                    }
                } else {
                    break;
                }
            }

            return furthestPoint;
        }

        public List<AxialCoord> PathTo(AxialCoord from, AxialCoord target) {
            var result = new List<AxialCoord>();

            // Return an empty path if the coord is invalid
            if (!IsValidCoord(target)) return result;

            var current = target;
            result.Add(current);

            var paths = AllPaths[from];
            Debug.Assert(paths != null);
            var path = paths[current];

            if (!path.Reachable) return result;

            var iterations = 1000;
            while ((path.Distance > 0) && (--iterations > 0)) {
                if (path.Source != null) {
                    result.Add(path.Source.Value);
                    path = paths[path.Source.Value];
                } else {
                    result.Clear();
                    break;
                }
            }

            Debug.Assert(iterations > 0);
            return result;
        }

        /// <summary>
        /// Finds a path to a hex closest to the target coord.
        /// </summary>
        public List<AxialCoord> PathToMob(AxialCoord from, AxialCoord to) {
            var target = NearestEmpty(from, to);
            if (target.HasValue) {
                return PathTo(from, target.Value);
            } else {
                return null;
            }
        }


        private bool IsWalkable(AxialCoord coord) {
            return IsValidCoord(coord) && (Game.Map[coord] == HexType.Empty) &&
                   (Game.State.AtCoord(coord, true) == null);
        }

        public void PathfindDistanceAll() {
            int done = 0;
            int loops = 0;
            long total = 0;
            var sw = Stopwatch.StartNew();

            foreach (var source in AllPaths.AllCoords) {
                AllPaths[source] = new HexMap<Path>(Game.Size);
                PathfindDistanceOnlyFrom(AllPaths[source], source);
                done++;
                if (done == 100) {
                    loops++;
                    done = 0;
                    long elapsed = sw.ElapsedMilliseconds;
                    total += elapsed;
                    sw.Restart();
                }
            }

            _precomputedPaths.Clear();
            foreach (var source in AllPaths.AllCoords) {
                foreach (var destination in AllPaths.AllCoords) {
                    var key = CoordPair.Build(source, destination);

                    if (_precomputedPaths.ContainsKey(key)) continue;

                    // TODO - path returns a reversed path including the starting point
                    var path = PathTo(source, destination);
                    if (path.Count > 0) {
                        path.RemoveAt(0);
                    }
                    path.Reverse();
                    _precomputedPaths.Add(key, path);
                }
            }
        }

        public List<AxialCoord> PrecomputedPathTo(AxialCoord from, AxialCoord to) {
            return _precomputedPaths[CoordPair.Build(from, to)];
        }

        public void PathfindDistanceOnlyFrom(HexMap<Path> distanceMap, AxialCoord start) {
            var queue = new Queue<AxialCoord>();
            var states = new HexMap<VertexState>(Game.Size);

            foreach (var coord in distanceMap.AllCoords) {
                distanceMap[coord] = new Path() {Reachable = false, Distance = int.MaxValue};
                states[coord] = VertexState.Unvisited;
            }

            states[start] = VertexState.Open;
            var path = distanceMap[start];
            path.Reachable = true;
            path.Distance = 0;
            distanceMap[start] = path;

            queue.Enqueue(start);

            int iterations = 0;

            while (queue.Count > 0) {
                iterations++;
                if ((iterations > Size * Size * 10) || (queue.Count > 1000)) {
                    Utils.Log(LogSeverity.Error, nameof(Pathfinder), "Pathfinder stuck when calculating a path.");
                }

                var current = queue.Dequeue();
                if (states[current] == VertexState.Closed) continue;
                states[current] = VertexState.Closed;

                foreach (var diff in NeighbourDiffs) {
                    var neighbour = current + diff;

                    if (IsValidCoord(neighbour) && Game.Map[neighbour] != HexType.Wall) {
                        // We can immediately skip the starting position
                        if (neighbour == start) continue;

                        if (states[neighbour] != VertexState.Closed) {
                            if (states[neighbour] == VertexState.Unvisited ||
                                distanceMap[neighbour].Distance > distanceMap[current].Distance + 1) {
                                distanceMap[neighbour] = new Path() {
                                    Distance = distanceMap[current].Distance + 1,
                                    Source = current,
                                    Reachable = true
                                };
                            }

                            states[neighbour] = VertexState.Open;
                            queue.Enqueue(neighbour);
                        }
                    }
                }
            }
        }

        public int Distance(AxialCoord from, AxialCoord to) {
            var current = AllPaths[from];
            Debug.Assert(current != null);
            return current[to].Distance;
        }

        private AxialCoord? NearestEmpty(AxialCoord from, AxialCoord to) {
            AxialCoord? result = null;
            foreach (var diff in NeighbourDiffs) {
                var neighbour = to + diff;
                if (IsWalkable(neighbour)) {
                    if (!result.HasValue) result = neighbour;

                    if (Distance(from, neighbour) < Distance(from, result.Value)) {
                        result = neighbour;
                    }
                }
            }

            return result;
        }

        public bool IsValidCoord(AxialCoord c) {
            int a = (c.X + c.Y);
            int distance = ((c.X < 0 ? -c.X : c.X)
                            + (a < 0 ? -a : a)
                            + (c.Y < 0 ? -c.Y : c.Y)) / 2;

            //int distance = (Math.Abs(c.X)
            //                + Math.Abs(c.X + c.Y)
            //                + Math.Abs(c.Y)) / 2;
            return distance <= Game.Size;

            //return _map.AxialDistance(c, new AxialCoord(0, 0)) <= _map.Size;
            //return _map.CubeDistance(new CubeCoord(0, 0, 0), c) <= _map.Size;
        }

        public Pathfinder ShallowCopy(GameInstance gameInstanceCopy) {
            var copy = new Pathfinder(gameInstanceCopy, AllPaths) {
                _precomputedPaths = _precomputedPaths
            };

            return copy;
        }
    }
}