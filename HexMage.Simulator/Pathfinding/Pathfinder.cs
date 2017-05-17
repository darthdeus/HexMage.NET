using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator.Pathfinding {
    /// <summary>
    /// Wraps all of the pathfinding logic.
    /// </summary>
    public class Pathfinder {
        private static readonly List<AxialCoord> NeighbourDiffs = new List<AxialCoord> {
            new AxialCoord(-1, 0),
            new AxialCoord(1, 0),
            new AxialCoord(0, -1),
            new AxialCoord(0, 1),
            new AxialCoord(1, -1),
            new AxialCoord(-1, 1)
        };

        [JsonIgnore]
        public HexMap<HexMap<Path>> AllPaths;

        private Dictionary<int, IList<AxialCoord>> _precomputedPaths =
            new Dictionary<int, IList<AxialCoord>>();


        [JsonIgnore] public GameInstance Game;

        [JsonIgnore]
        private int Size => Game.Size;

        public Pathfinder DeepCopy(GameInstance gameCopy) {
            var copy = new Pathfinder(gameCopy);

            foreach (var pair in _precomputedPaths) {
                copy._precomputedPaths[pair.Key] = pair.Value.ToList();
            }

            foreach (var coord in AllPaths.AllCoords) {
                copy.AllPaths[coord] = AllPaths[coord].DeepCopy();
            }

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

        /// <summary>
        /// Calculates the furthest walkable point ot the given target mob.
        /// </summary>
        public AxialCoord? FurthestPointToTarget(CachedMob mob, CachedMob target) {
            var path = PrecomputedPathTo(mob.MobInstance.Coord, target.MobInstance.Coord);

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

        /// <summary>
        /// Returns a path (from; to]
        /// </summary>
        public IList<AxialCoord> PathFromSourceToTarget(AxialCoord from, AxialCoord to) {
            if (!IsValidCoord(from) || !IsValidCoord(to)) return new AxialCoord[0];

            int distance = Distance(from, to);
            if (distance == 0 || distance == int.MaxValue) return new AxialCoord[] { to };            
            
            var result = new AxialCoord[distance];
            result[distance - 1] = to;

            AxialCoord current = to;
            var paths = AllPaths[from];
            var path = paths[current];

            while (path.Distance > 0 && distance-- >= 0) {
                if (path.Source != null) {
                    result[distance] = path.Source.Value;
                    path = paths[result[distance]];
                } else {
                    return new AxialCoord[0];
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a walkable path between two points.
        /// </summary>
        public List<AxialCoord> PathTo(AxialCoord from, AxialCoord to) {
            var result = new List<AxialCoord>();

            if (!IsValidCoord(from) || !IsValidCoord(to)) return result;

            var current = to;
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
            foreach (var source in AllPaths.AllCoords) {
                AllPaths[source] = new HexMap<Path>(Game.Size);
                PathfindDistanceOnlyFrom(AllPaths[source], source);
            }

            _precomputedPaths.Clear();
            foreach (var source in AllPaths.AllCoords) {
                foreach (var destination in AllPaths.AllCoords) {
                    var key = CoordPair.Build(source, destination);

                    if (_precomputedPaths.ContainsKey(key)) continue;

                    var path = PathFromSourceToTarget(source, destination);
                    _precomputedPaths.Add(key, path);
                }
            }
        }

        public IList<AxialCoord> PrecomputedPathTo(AxialCoord from, AxialCoord to) {
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

        /// <summary>
        /// Calculates a walk distance between two hexes.
        /// </summary>
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

        /// <summary>
        /// Checks if a given coord is valid on the map.
        /// </summary>
        public bool IsValidCoord(AxialCoord c) {
            int a = (c.X + c.Y);
            int distance = ((c.X < 0 ? -c.X : c.X)
                            + (a < 0 ? -a : a)
                            + (c.Y < 0 ? -c.Y : c.Y)) / 2;

            return distance <= Game.Size;
        }

        public Pathfinder ShallowCopy(GameInstance gameInstanceCopy) {
            var copy = new Pathfinder(gameInstanceCopy, AllPaths) {
                _precomputedPaths = _precomputedPaths
            };

            return copy;
        }
    }
}