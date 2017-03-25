using System;
using System.Collections.Generic;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.PCG {
    // TODO - remove map seeds altogether
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

            // TODO - neni jich uz moc?
            int wallCount = mapSize > 6 ? 50 : 30;
            for (int i = 0, total = 0; i < wallCount && total < 1000; i++, total++) {
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
        public static Random Random = new Random(12345);

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

        public static void RandomPlaceMob(MobManager mobManager, int mob, Map map, GameState state) {
            int size = map.Size;

            Predicate<AxialCoord> isCoordAvailable = c => {
                bool isWall = map[c] == HexType.Wall;
                var atCoord = state.AtCoord(c);

                return !isWall && (!atCoord.HasValue || atCoord.Value == mob);
            };

            var mobInstance = state.MobInstances[mob];
            mobInstance.Coord = AxialCoord.Zero;
            state.MobInstances[mob] = mobInstance;

            int iterations = 10000;

            while (--iterations > 0) {
                var x = Random.Next(-size, size);
                var y = Random.Next(-size, size);

                var zero = new AxialCoord(0, 0);
                var coord = new AxialCoord(x, y);

                if (isCoordAvailable(coord) && coord.Distance(zero) < size) {
                    if (state.AtCoord(coord) == null || state.AtCoord(coord).Value == mob) {
                        var infoCopy = mobManager.MobInfos[mob];
                        infoCopy.OrigCoord = coord;
                        mobManager.MobInfos[mob] = infoCopy;
                        break;
                    } else {
                        throw new InvalidOperationException($"There already is a mob at {coord}");
                    }
                }
            }

            if (iterations == 0) {
                throw new InvalidOperationException($"Failed to place a mob");
            }
        }

        public static MobInfo RandomMob(MobManager mobManager, TeamColor team, GameState state) {
            var elements = new[] {
                AbilityElement.Earth, AbilityElement.Fire, AbilityElement.Air, AbilityElement.Water
            };

            var abilities = new List<int>();
            var maxHp = Random.Next(30, 82);
            // TODO - fix me later, only used for debugging
            //maxHp = 1;
            var maxAp = Random.Next(17, 30);

            for (int i = 0; i < 1; i++) {
                var element = elements[Random.Next(0, 4)];
                var buff = RandomBuff(element);

                var areaBuff = RandomAreaBuff(element);

                var dmg = Random.Next(1, 10);
                var cost = Random.Next(3, 7);
                var range = Random.Next(3, 10);

                // TODO - re-enable cooldowns?
                //var cooldown = Random.Next(0, 3);
                var cooldown = 0;

                int score = 0;
                score += dmg;
                score += (dmg - cost) * 2;


                var ability = new Ability(dmg,
                                          cost,
                                          range,
                                          cooldown,
                                          element,
                                          buff,
                                          areaBuff);

                // TODO - use GameInstance.AddAbilityWithInfo instead
                int id = mobManager.Abilities.Count;
                mobManager.Abilities.Add(ability);
                state.Cooldowns.Add(0);

                abilities.Add(id);
            }

            int iniciative = Random.Next(10);

#warning TODO - generated mobs do not have their coords assigned
            return new MobInfo(team, maxHp, maxAp, iniciative, abilities);
        }

        public static Buff RandomBuff(AbilityElement element) {
            var hpChange = Random.Next(-5, 1);
            var apChange = Random.Next(-2, 1);
            var lifetime = Random.Next(1, 3);

            while (hpChange == 0 && apChange == 0) {
                hpChange = Random.Next(-5, 1);
                apChange = Random.Next(-2, 1);
            }
            return new Buff(element, hpChange, apChange, lifetime);
        }

        public static AreaBuff RandomAreaBuff(AbilityElement element) {
            return new AreaBuff(AxialCoord.Zero, Random.Next(4), RandomBuff(element));
        }
    }
}