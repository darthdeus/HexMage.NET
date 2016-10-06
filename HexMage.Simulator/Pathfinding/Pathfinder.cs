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

    public class Pathfinder : IResettable {
        private readonly List<AxialCoord> _diffs;
        private readonly Map _map;
        private readonly MobManager _mobManager;

        public Pathfinder(Map map, MobManager mobManager) {
            _map = map;
            _mobManager = mobManager;
            _allPaths = new HexMap<HexMap<int>>(_map.Size);

            _diffs = new List<AxialCoord> {
                new AxialCoord(-1, 0),
                new AxialCoord(1, 0),
                new AxialCoord(0, -1),
                new AxialCoord(0, 1),
                new AxialCoord(1, -1),
                new AxialCoord(-1, 1)
            };
        }


        private int Size => _map.Size;

        public void Reset() {
            // Right now we're not caching anything, so there's nothing to reset
        }

        public AxialCoord? FurthestPointToTarget(Mob mob, Mob target) {
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


        private bool IsWalkable(AxialCoord coord) {
            return IsValidCoord(coord) && (_map[coord] == HexType.Empty) && (_mobManager.AtCoord(coord) == null);
        }

        public int Distance(AxialCoord c) {
            Debug.Assert(_current != null);
            return _current[c];
        }

        public void PathfindFromCurrentMob(TurnManager turnManager) {
            if (turnManager.CurrentMob != null) PathfindFrom(turnManager.CurrentMob.Coord);
        }

        readonly HexMap<HexMap<int>> _allPaths;
        private HexMap<int> _current;

        public void PathfindDistanceAll() {
            Console.WriteLine($"Initializing pathfinder, {_allPaths.AllCoords.Count} locations");

            int done = 0;
            int loops = 0;
            long total = 0;
            var sw = Stopwatch.StartNew();

            foreach (var source in _allPaths.AllCoords) {
                _allPaths[source] = new HexMap<int>(_map.Size);
                PathfindDistanceOnlyFrom(_allPaths[source], source);
                done++;
                if (done == 100) {
                    loops++;
                    done = 0;
                    long elapsed = sw.ElapsedMilliseconds;
                    total += elapsed;
                    //Console.WriteLine($"Done 100 in {sw.ElapsedTicks * 10}us");
                    Console.WriteLine($"Done 100 in {elapsed}ms");
                    sw.Restart();
                }
            }

            Console.WriteLine($"Pathfinder initialized, took {total}, avg: {total/loops}");
        }

        public void PathfindDistanceOnlyFrom(HexMap<int> distanceMap, AxialCoord start) {
            var queue = new Queue<AxialCoord>();
            var states = new HexMap<VertexState>(_map.Size);

            foreach (var coord in distanceMap.AllCoords) {
                distanceMap[coord] = int.MaxValue;
                states[coord] = VertexState.Unvisited;
            }

            states[start] = VertexState.Open;
            distanceMap[start] = 0;

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

                    if (IsValidCoord(neighbour)) {
                        // We can immediately skip the starting position
                        if (neighbour == start) continue;

                        if (states[neighbour] != VertexState.Closed) {
                            if (states[neighbour] == VertexState.Unvisited ||
                                distanceMap[neighbour] > distanceMap[current] + 1) {
                                distanceMap[neighbour] = distanceMap[current] + 1;
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
                            + (c.Y < 0 ? -c.Y : c.Y)) / 2;

            //int distance = (Math.Abs(c.X)
            //                + Math.Abs(c.X + c.Y)
            //                + Math.Abs(c.Y)) / 2;
            return distance <= _map.Size;

            //return _map.AxialDistance(c, new AxialCoord(0, 0)) <= _map.Size;
            //return _map.CubeDistance(new CubeCoord(0, 0, 0), c) <= _map.Size;
        }
    }
}