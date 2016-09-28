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
            throw new NotImplementedException();
            //Utils.Log(LogSeverity.Debug, nameof(Pathfinder), $"Finding path from {mob.Coord} to {target.Coord}");
            //var path = PathToMob(target);

            //if (path != null) {
            //    return FurthestPointOnPath(mob, path);
            //} else {
            //    return null;
            //}
        }


        private bool IsWalkable(AxialCoord coord) {
            return IsValidCoord(coord) && (_map[coord] == HexType.Empty) && (_mobManager.AtCoord(coord) == null);
        }

        public int Distance(AxialCoord c) {
            return _current[c];
        }

        public void PathfindFromCurrentMob(TurnManager turnManager) {
            if (turnManager.CurrentMob != null) PathfindFrom(turnManager.CurrentMob.Coord);
        }

        readonly HexMap<HexMap<int>> _allPaths;
        private HexMap<int> _current;

        public void PathfindDistanceAll() {
            foreach (var source in _allPaths.AllCoords) {
                _allPaths[source] = new HexMap<int>(_map.Size);
                PathfindDistanceOnlyFrom(_allPaths[source], source);
            }
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
                if (states[start] == VertexState.Closed) continue;
                states[start] = VertexState.Closed;

                foreach (var diff in _diffs) {
                    var neighbour = current + diff;

                    if (IsValidCoord(neighbour)) {
                        // We can immediately skip the starting position
                        if (neighbour == start) continue;

                        if (states[neighbour] != VertexState.Closed) {
                            distanceMap[neighbour] = distanceMap[current] + 1;
                        }

                        states[neighbour] = VertexState.Open;
                        queue.Enqueue(neighbour);
                    }
                }
            }
        }

        public void PathfindFrom(AxialCoord start) {
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
            //int distance = (Math.Abs(c.X)
            //                + Math.Abs(c.X + c.Y)
            //                + Math.Abs(c.Y)) / 2;
            //return distance <= _map.Size;

            //return _map.AxialDistance(c, new AxialCoord(0, 0)) <= _map.Size;
            return _map.CubeDistance(new CubeCoord(0, 0, 0), c) <= _map.Size;
        }

    }
}