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
        public static Random Random = new Random();

        public static GameInstance RandomGame(int size, MapSeed seed, int teamSize,
                                              Func<GameInstance, IMobController> controllerFunc) {
            var map = new MapGenerator().Generate(size, seed);
            var game = new GameInstance(map);

            const TeamColor t1 = TeamColor.Red;
            const TeamColor t2 = TeamColor.Blue;

            game.MobManager.Teams[t1] = controllerFunc(game);
            game.MobManager.Teams[t2] = controllerFunc(game);

            for (int i = 0; i < teamSize; i++) {
                game.MobManager.AddMob(RandomMob(t1, game));
                game.MobManager.AddMob(RandomMob(t2, game));
            }

            return game;
        }

        public static AxialCoord RandomPosition(GameInstance gameInstance) {
            var map = gameInstance.Map;
            var mobManager = gameInstance.MobManager;

            int size = map.Size;
            int iterations = 0;

            while (iterations++ < 10000) {
                var x = Random.Next(-size, size);
                var y = Random.Next(-size, size);
                
                var zero = new AxialCoord(0, 0);
                var coord = new AxialCoord(x, y);

                if (map.IsValidCoord(coord) && map[coord] != HexType.Wall && mobManager.AtCoord(coord) == null && coord.Distance(zero) < size) {
                    return coord;
                }
            }

            throw new InvalidOperationException("PCG got stuck trying to place a mob, there's no free space available.");
        }

        public static Mob RandomMob(TeamColor team, GameInstance gameInstance) {
            var abilities = new List<AbilityInfo>();

            for (int i = 0; i < Mob.AbilityCount; i++) {
                var ability = RandomAbility();
                
                abilities.Add(ability);
            }

            int iniciative = Random.Next(10);

            var mob = new Mob(team, 10, 10, 3, iniciative, abilities.Select(a => new AbilityInstance(a)).ToList());
            mob.Coord = RandomPosition(gameInstance);
            mob.OrigCoord = mob.Coord;

            return mob;
        }

        private static AbilityInfo RandomAbility() {
            var elements = new[] {
                AbilityElement.Earth, AbilityElement.Fire, AbilityElement.Air, AbilityElement.Water
            };
            var element = elements[Random.Next(0, 4)];
            var buffs = RandomBuffs(element);

            var areaBuffs = RandomAreaBuffs(element);

            return new AbilityInfo(Random.Next(1, 10),
                               Random.Next(3, 7),
                               Random.Next(3, 10),
                               Random.Next(0, 3),
                               element,
                               buffs,
                               areaBuffs);
        }

        private static List<Buff> RandomBuffs(AbilityElement element) {
            var result = new List<Buff>();

            int count = Random.Next(2);
            for (int i = 0; i < count; i++) {
                result.Add(RandomBuff(element));
            }

            return result;
        }

        private static Buff RandomBuff(AbilityElement element) {
            var hpChange = Random.Next(-2, 1);
            var apChange = Random.Next(-1, 1);
            var lifetime = Random.Next(1, 3);

            while (hpChange == 0 && apChange == 0) {
                hpChange = Random.Next(-2, 1);
                apChange = Random.Next(-1, 1);
            }
            return new Buff(element, hpChange, apChange, lifetime);
        }

        private static List<AreaBuff> RandomAreaBuffs(AbilityElement element) {
            var result = new List<AreaBuff>();

            int count = Random.Next(2);
            for (int i = 0; i < count; i++) {
                result.Add(new AreaBuff(Random.Next(4), RandomBuff(element)));
            }

            return result;
        }
    }
}