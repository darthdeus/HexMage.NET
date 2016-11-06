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

        //public static GameInstance RandomGame(int size, MapSeed seed, int teamSize, Func<GameInstance, IMobController> controllerFunc) {
        //    var map = new MapGenerator().Generate(size, seed);
        //    var game = new GameInstance(map);

        //    const TeamColor t1 = TeamColor.Red;
        //    const TeamColor t2 = TeamColor.Blue;

        //    game.MobManager.Teams[t1] = controllerFunc(game);
        //    game.MobManager.Teams[t2] = controllerFunc(game);

        //    for (int i = 0; i < teamSize; i++) {
        //        game.MobManager.AddMob(RandomMob(game.MobManager, t1, size, c => game.MobManager.AtCoord(c) == null));
        //        game.MobManager.AddMob(RandomMob(game.MobManager, t2, size, c => game.MobManager.AtCoord(c) == null));
        //    }

        //    game.RedAlive = teamSize;
        //    game.BlueAlive = teamSize;

        //    return game;
        //}

        public static void RandomPlaceMob(MobManager mobManager, int mob, Map map) {
            int size = map.Size;

            Predicate<AxialCoord> isCoordAvailable = c => {
                bool isWall = map[c] == HexType.Wall;
                var atCoord = mobManager.AtCoord(c);

                return !isWall && (!atCoord.HasValue || (atCoord.HasValue && atCoord.Value == mob));
            };

            var copy = mobManager.MobInstances[mob];
            copy.Coord = AxialCoord.Zero;
            mobManager.MobInstances[mob] = copy;

            while (true) {
                var x = Random.Next(-size, size);
                var y = Random.Next(-size, size);

                var zero = new AxialCoord(0, 0);
                var coord = new AxialCoord(x, y);

                if (isCoordAvailable(coord) && coord.Distance(zero) < size) {
                    if (mobManager.AtCoord(coord) == null || mobManager.AtCoord(coord).Value == mob) {
                        mobManager.SetMobPosition(mob, coord);
                        break;
                    } else {
                        throw new InvalidOperationException($"There already is a mob at {coord}");
                    }
                }
            }
        }

        public static MobInfo RandomMob(MobManager mobManager, TeamColor team) {
            var elements = new[] {
                AbilityElement.Earth, AbilityElement.Fire, AbilityElement.Air, AbilityElement.Water
            };

            var abilities = new List<int>();
            var maxHp = Random.Next(7, 12);
            var maxAp = Random.Next(7, 12);

            for (int i = 0; i < MobInfo.AbilityCount; i++) {
                var element = elements[Random.Next(0, 4)];
                var buffs = RandomBuffs(element);


                var areaBuffs = RandomAreaBuffs(element);

                int id = mobManager.Abilities.Count;
                var dmg = Random.Next(1, 10);
                var cost = Random.Next(3, 7);
                var range = Random.Next(3, 10);
                var cooldown = Random.Next(0, 3);

                int score = 0;
                score += dmg;
                score += (dmg - cost)*2;


                var ability = new Ability(id,
                                          dmg,
                                          cost,
                                          range,
                                          cooldown,
                                          element,
                                          buffs,
                                          areaBuffs);

                mobManager.Abilities.Add(ability);
                mobManager.Cooldowns.Add(0);

                abilities.Add(ability.Id);
            }

            int iniciative = Random.Next(10);

#warning TODO - generated mobs do not have their coords assigned
            return new MobInfo(team, maxHp, maxAp, 3, iniciative, abilities);
        }

        public static List<Buff> RandomBuffs(AbilityElement element) {
            var result = new List<Buff>();

            int count = Random.Next(2);
            for (int i = 0; i < count; i++) {
                result.Add(RandomBuff(element));
            }

            return result;
        }

        public static Buff RandomBuff(AbilityElement element) {
            var hpChange = Random.Next(-2, 1);
            var apChange = Random.Next(-1, 1);
            var lifetime = Random.Next(1, 3);

            while (hpChange == 0 && apChange == 0) {
                hpChange = Random.Next(-2, 1);
                apChange = Random.Next(-1, 1);
            }
            return new Buff(element, hpChange, apChange, lifetime);
        }

        public static List<AreaBuff> RandomAreaBuffs(AbilityElement element) {
            var result = new List<AreaBuff>();

            int count = Random.Next(2);
            for (int i = 0; i < count; i++) {
                result.Add(new AreaBuff(AxialCoord.Zero, Random.Next(4), RandomBuff(element)));
            }

            return result;
        }
    }
}