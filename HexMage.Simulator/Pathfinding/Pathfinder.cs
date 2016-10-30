using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public enum VertexState {
        Unvisited,
        Open,
        Closed
    }

    public struct Path {
        public int Distance;
        public AxialCoord? Source;
        public bool Reachable;
    }

    public class Pathfinder : IResettable {
        private readonly GameInstance _gameInstance;
        private readonly List<AxialCoord> _diffs;
        readonly HexMap<HexMap<Path>> _allPaths;
        private HexMap<Path> _current;

        public Pathfinder(GameInstance gameInstance) {
            _gameInstance = gameInstance;
            _allPaths = new HexMap<HexMap<Path>>(_gameInstance.Size);

            _diffs = new List<AxialCoord> {
                new AxialCoord(-1, 0),
                new AxialCoord(1, 0),
                new AxialCoord(0, -1),
                new AxialCoord(0, 1),
                new AxialCoord(1, -1),
                new AxialCoord(-1, 1)
            };
        }


        private int Size => _gameInstance.Size;

        public void Reset() {
            // Right now we're not caching anything, so there's nothing to reset
        }

        public AxialCoord? FurthestPointToTarget(MobInstance mob, MobInstance target) {
            int iterations = 0;
            while (true) {
                if (iterations++ > 1000)
                    throw new InvalidOperationException("Pathfinding got stuck searching for a shorter path");
                var closer = NearestEmpty(target.Coord);
                if (closer == null) return null;

                if (Distance(closer.Value) <= mob.Ap) {
                    return closer;
                }
            }
        }

        public IList<AxialCoord> PathTo(AxialCoord target) {
            var result = new List<AxialCoord>();

            // Return an empty path if the coord is invalid
            if (!IsValidCoord(target)) return result;

            var current = target;
            result.Add(current);

            var path = _current[current];

            if (!path.Reachable) return result;

            var iterations = 1000;
            while ((path.Distance > 0) && (--iterations > 0)) {
                if (path.Source != null) {
                    result.Add(path.Source.Value);
                    path = _current[path.Source.Value];
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
        public IList<AxialCoord> PathToMob(AxialCoord coord) {
            var target = NearestEmpty(coord);
            if (target.HasValue) {
                return PathTo(target.Value);
            } else {
                return null;
            }
        }


        private bool IsWalkable(AxialCoord coord) {
            return IsValidCoord(coord) && (_gameInstance.Map[coord] == HexType.Empty) && (_gameInstance.MobManager.AtCoord(coord) == null);
        }

        public int Distance(AxialCoord c) {
            Debug.Assert(_current != null);
            return _current[c].Distance;
        }

        public void PathfindFromCurrentMob(TurnManager turnManager) {
            if (turnManager.CurrentMob != null) {
                PathfindFrom(_gameInstance.MobManager.MobInstanceForId(turnManager.CurrentMob.Value).Coord);
            } else {
                Utils.Log(LogSeverity.Warning, nameof(Pathfinder), "CurrentMob is NULL, pathfind current failed");
            }
        }


        public void PathfindDistanceAll() {
            int done = 0;
            int loops = 0;
            long total = 0;
            var sw = Stopwatch.StartNew();

            foreach (var source in _allPaths.AllCoords) {
                _allPaths[source] = new HexMap<Path>(_gameInstance.Size);
                PathfindDistanceOnlyFrom(_allPaths[source], source);
                done++;
                if (done == 100) {
                    loops++;
                    done = 0;
                    long elapsed = sw.ElapsedMilliseconds;
                    total += elapsed;
                    sw.Restart();
                }
            }
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
                if ((iterations > Size*Size*10) || (queue.Count > 1000)) {
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

            //Console.WriteLine("Pathfinder done");
        }

        public void PathfindFrom(AxialCoord start) {
            Debug.Assert(_allPaths[start] != null, "Trying to pathfind from an uninitialized location");
            _current = _allPaths[start];
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
            int a = (c.X + c.Y);
            int distance = ((c.X < 0 ? -c.X : c.X)
                            + (a < 0 ? -a : a)
                            + (c.Y < 0 ? -c.Y : c.Y))/2;

            //int distance = (Math.Abs(c.X)
            //                + Math.Abs(c.X + c.Y)
            //                + Math.Abs(c.Y)) / 2;
            return distance <= _gameInstance.Size;

            //return _map.AxialDistance(c, new AxialCoord(0, 0)) <= _map.Size;
            //return _map.CubeDistance(new CubeCoord(0, 0, 0), c) <= _map.Size;
        }
    }
}