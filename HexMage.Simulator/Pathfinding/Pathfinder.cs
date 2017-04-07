using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    // TODO - extract this
    public enum VertexState {
        Unvisited,
        Open,
        Closed
    }

    // TODO - extract this
    public struct Path {
        public int Distance;
        public AxialCoord? Source;
        public bool Reachable;
    }

    public class Pathfinder : IResettable {
        private readonly GameInstance _gameInstance;
        private readonly List<AxialCoord> _diffs;
        public readonly HexMap<HexMap<Path>> AllPaths;
        private int Size => _gameInstance.Size;

        private Dictionary<int, List<AxialCoord>> _precomputedPaths =
            new Dictionary<int, List<AxialCoord>>();

        public Pathfinder(GameInstance gameInstance) {
            _gameInstance = gameInstance;
            AllPaths = new HexMap<HexMap<Path>>(_gameInstance.Size);

            _diffs = new List<AxialCoord> {
                new AxialCoord(-1, 0),
                new AxialCoord(1, 0),
                new AxialCoord(0, -1),
                new AxialCoord(0, 1),
                new AxialCoord(1, -1),
                new AxialCoord(-1, 1)
            };
        }

        public Pathfinder(GameInstance gameInstance, List<AxialCoord> diffs, HexMap<HexMap<Path>> allPaths) {
            _gameInstance = gameInstance;
            _diffs = diffs;
            AllPaths = allPaths;
        }

        public void Reset() {
            // Right now we're not caching anything, so there's nothing to reset
        }

        public AxialCoord? FurthestPointToTarget(MobInstance mob, MobInstance target) {
            List<AxialCoord> path = PrecomputedPathTo(mob.Coord, target.Coord);

            if (path.Count == 0 && mob.Coord.Distance(target.Coord) == 1) {
                return null;
            }            

            AxialCoord? furthestPoint = null;
            foreach (var coord in path) {
                int distance = Distance(mob.Coord, coord);
                var mobAtCoord = _gameInstance.State.AtCoord(coord);

                if (distance <= mob.Ap) {
                    if (mobAtCoord == null) {
                        furthestPoint = coord;
                    }
                } else {
                    break;
                }                
            }
            
            return furthestPoint;

//            int iterations = 0;

//            AxialCoord coord = target.Coord;
//            while (true) {
//                if (iterations++ > 1000) {
//#warning TODO - throw an exception instead
//                    throw new InvalidOperationException("Pathfinding got stuck searching for a shorter path");
//                    return null;
//                }
//                var closer = NearestEmpty(mob.Coord, coord);
//                if (closer == null) return null;


//                if (Distance(mob.Coord, closer.Value) <= mob.Ap) {
//                    return closer;
//                } else {
//                    coord = closer.Value;
//                }
//            }
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
            return IsValidCoord(coord) && (_gameInstance.Map[coord] == HexType.Empty) &&
                   (_gameInstance.State.AtCoord(coord) == null);
        }

        public void PathfindDistanceAll() {
            int done = 0;
            int loops = 0;
            long total = 0;
            var sw = Stopwatch.StartNew();

            foreach (var source in AllPaths.AllCoords) {
                AllPaths[source] = new HexMap<Path>(_gameInstance.Size);
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
            var states = new HexMap<VertexState>(_gameInstance.Size);

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

                foreach (var diff in _diffs) {
                    var neighbour = current + diff;

                    if (IsValidCoord(neighbour) && _gameInstance.Map[neighbour] != HexType.Wall) {
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

        public void PathfindFromCurrentMob(TurnManager turnManager, Pathfinder pathfinder) {
            throw new NotImplementedException();
            //if (turnManager.CurrentMob != null)
            //{
            //    PathfindFrom(pathfinder, MobInstances[turnManager.CurrentMob.Value].Coord);
            //}
            //else
            //{
            //    Utils.Log(LogSeverity.Warning, nameof(Pathfinder), "CurrentMob is NULL, pathfind current failed");
            //}
        }


        private AxialCoord? NearestEmpty(AxialCoord from, AxialCoord to) {
            AxialCoord? result = null;
            foreach (var diff in _diffs) {
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
            return distance <= _gameInstance.Size;

            //return _map.AxialDistance(c, new AxialCoord(0, 0)) <= _map.Size;
            //return _map.CubeDistance(new CubeCoord(0, 0, 0), c) <= _map.Size;
        }

        public Pathfinder ShallowCopy(GameInstance gameInstanceCopy) {
            var copy = new Pathfinder(gameInstanceCopy,
                                      _diffs, AllPaths);

            copy._precomputedPaths = _precomputedPaths;

            return copy;
        }
    }
}