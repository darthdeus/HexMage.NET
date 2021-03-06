﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;
using MathNet.Numerics.Random;

namespace HexMage.Simulator.PCG {
    /// <summary>
    /// Wraps a number of simple helpers for generating the initial encounter content.
    /// </summary>
    public static class Generator {
        public static ThreadLocalGenerator Random = new ThreadLocalGenerator();
        //public static Random Random = new SystemRandomSource();

        public class ThreadLocalGenerator {
            private static Random _srng = new Random();
            private ThreadLocal<Random> _rng = new ThreadLocal<Random>(() => new Random(_srng.Next(int.MaxValue)));
            private const bool useLocks = false;

            static ThreadLocalGenerator() {
                if (Constants.UseGlobalSeed) {
                    _srng = new Random(Constants.RandomSeed);
                } else {
                    _srng = new Random();
                }
            }

            public int Next(int min, int max) {
                if (useLocks) {
                    lock (this) {
                        return _srng.Next(min, max);
                    }
                } else {
                    return _rng.Value.Next(min, max);
                }
            }

            public int Next(int max) {
                if (useLocks) {
                    lock (this) {
                        return _srng.Next(max);
                    }
                } else {
                    return _rng.Value.Next(max);
                }
            }

            public double NextDouble() {
                if (useLocks) {
                    lock (this) {
                        return _srng.NextDouble();
                    }
                } else {
                    return _rng.Value.NextDouble();
                }
            }
        }

        public static void RandomPlaceMob(GameInstance game, int mobId) {
            var map = game.Map;
            var state = game.State;
            var mobManager = game.MobManager;
            int size = map.Size;

            Predicate<AxialCoord> isCoordAvailable = c => {
                bool isWall = map[c] == HexType.Wall;
                var atCoord = state.AtCoord(c, true);

                return !isWall && (!atCoord.HasValue || atCoord.Value == mobId);
            };

            state.MobInstances[mobId].Coord = AxialCoord.Zero;

            int iterations = 10000;

            while (--iterations > 0) {
                var coord = map.RandomCoord();

                if (map.RedStartingPoints.Contains(coord) ||
                    map.BlueStartingPoints.Contains(coord)) continue;

                bool isWall = map[coord] == HexType.Wall;
                bool isTaken = mobManager.MobInfos.Any(info => info.OrigCoord == coord);

                if (!isWall && !isTaken) {
                    game.PlaceMob(mobId, coord);
                    break;
                }
            }

            if (iterations == 0) {
                throw new InvalidOperationException($"Failed to place a mob");
            }
        }

        public static MobInfo RandomMob(MobManager mobManager, TeamColor team, GameState state) {
            var abilities = new List<int>();
            int maxAp = RandomAp();
            int maxHp = RandomHp();

            for (int i = 0; i < 2; i++) {
                var buff = RandomBuff();
                var areaBuff = RandomAreaBuff();

                var cooldown = Random.Next(0, 2);

                var ability = new AbilityInfo(RandomDmg(),
                                              RandomCost(maxAp),
                                              RandomRange(),
                                              cooldown,
                                              buff,
                                              areaBuff);

                int id = mobManager.Abilities.Count;
                mobManager.Abilities.Add(ability);
                state.Cooldowns.Add(0);

                abilities.Add(id);
            }

            int iniciative = Random.Next(10);

            return new MobInfo(team, maxHp, maxAp, iniciative, abilities);
        }

        public static int RandomHp() {
            return Random.Next(40, Constants.HpMax);
        }

        public static int RandomAp() {
            return Random.Next(13, Constants.ApMax);
        }

        public static int RandomDmg() {
            return Random.Next(5, Constants.DmgMax);
        }

        public static int RandomCost(int? maxAp) {
            maxAp = maxAp ?? Constants.ApMax;
            return Random.Next(3, Math.Min(Constants.CostMax, maxAp.Value));
        }

        public static int RandomRange() {
            return Random.Next(3, Constants.RangeMax);
        }

        public static Buff RandomBuff() {
            var hpChange = Random.Next(-5, 0);
            var apChange = Random.Next(-2, 0);
            var lifetime = Random.Next(1, 3);

            while (hpChange == 0 && apChange == 0) {
                hpChange = Random.Next(-5, 0);
                apChange = Random.Next(-2, 0);
            }
            return new Buff(hpChange, apChange, lifetime);
        }

        public static AreaBuff RandomAreaBuff() {
            var buff = new AreaBuff(AxialCoord.Zero, Random.Next(4), RandomBuff());
            if (buff.Radius < 2) {
                return AreaBuff.ZeroBuff();
            } else {
                return buff;
            }
        }
    }
}