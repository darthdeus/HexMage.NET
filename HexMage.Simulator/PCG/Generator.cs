using System;
using System.Collections.Generic;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.PCG {
    public struct MapSeed {
        private readonly Guid _guid;

        public MapSeed(Guid guid) {
            _guid = guid;
        }

        // TODO - figure out a better way to seed the random generator
        public Random Random => new Random(_guid.ToByteArray().Sum(x => (int) x));

        public static MapSeed CreateRandom() {
            return new MapSeed(Guid.NewGuid());
        }

        public override string ToString() {
            return _guid.ToString();
        }
    }

    public class MapGenerator {
        public Map Generate(int mapSize, MapSeed seed) {
            var random = seed.Random;

            var map = new Map(mapSize);

            for (int i = 0, total = 0; i < 10 && total < 1000; i++, total++) {
                var coord = map.AllCoords[random.Next(map.AllCoords.Count)];
                if (map[coord] == HexType.Empty) {
                    map[coord] = HexType.Wall;
                } else {
                    i++;
                }
            }

            return map;
        }
    }

    public static class Generator {
        private static readonly Random _random = new Random();

        public static GameInstance RandomGame(int size, MapSeed seed, int teamSize,
                                              Func<GameInstance, IMobController> controllerFunc) {
            var map = new MapGenerator().Generate(size, seed);
            var game = new GameInstance(map);

            const TeamColor t1 = TeamColor.Red;
            const TeamColor t2 = TeamColor.Blue;

            game.MobManager.Teams[t1] = controllerFunc(game);
            game.MobManager.Teams[t2] = controllerFunc(game);

            for (int i = 0; i < teamSize; i++) {
                game.MobManager.AddMob(RandomMob(t1, size, c => game.MobManager.AtCoord(c) == null));
                game.MobManager.AddMob(RandomMob(t2, size, c => game.MobManager.AtCoord(c) == null));
            }

            return game;
        }

        public static Mob RandomMob(TeamColor team, int size) {
            return RandomMob(team, size, _ => true);
        }

        public static Mob RandomMob(TeamColor team, int size, Predicate<AxialCoord> isCoordAvailable) {
            var abilities = new List<Ability>();

            var elements = new[] {
                AbilityElement.Earth, AbilityElement.Fire, AbilityElement.Air, AbilityElement.Water
            };

            for (int i = 0; i < Mob.AbilityCount; i++) {
                var element = elements[_random.Next(0, 4)];
                var buffs = RandomBuffs(element);

                var areaBuffs = RandomAreaBuffs(element);

                abilities.Add(new Ability(_random.Next(1, 10),
                                          _random.Next(3, 7),
                                          _random.Next(3, 10),
                                          _random.Next(0, 3),
                                          element,
                                          buffs,
                                          areaBuffs));
            }

            int iniciative = _random.Next(10);

            var mob = new Mob(team, 10, 10, 3, iniciative, abilities);

            while (true) {
                var x = _random.Next(-size, size);
                var y = _random.Next(-size, size);
                //var z = -x - y;
                //var cube = new CubeCoord(x, y, z);
                //var zero = new CubeCoord(0, 0, 0);

                var zero = new AxialCoord(0, 0);
                var coord = new AxialCoord(x, y);

                if (isCoordAvailable(coord) && coord.Distance(zero) < size) {
                    mob.Coord = coord;
                    mob.OrigCoord = coord;
                    break;
                }
            }

            return mob;
        }

        public static List<Buff> RandomBuffs(AbilityElement element) {
            var result = new List<Buff>();

            int count = _random.Next(2);
            for (int i = 0; i < count; i++) {
                result.Add(RandomBuff(element));
            }

            return result;
        }

        public static Buff RandomBuff(AbilityElement element) {
            var hpChange = _random.Next(-2, 1);
            var apChange = _random.Next(-1, 1);
            var lifetime = _random.Next(1, 3);

            while (hpChange == 0 && apChange == 0) {
                hpChange = _random.Next(-2, 1);
                apChange = _random.Next(-1, 1);
            }
            return new Buff(element, hpChange, apChange, lifetime);
        }

        public static List<AreaBuff> RandomAreaBuffs(AbilityElement element) {
            var result = new List<AreaBuff>();

            int count = _random.Next(2);
            for (int i = 0; i < count; i++) {
                result.Add(new AreaBuff(_random.Next(4), RandomBuff(element)));
            }

            return result;
        }
    }
}