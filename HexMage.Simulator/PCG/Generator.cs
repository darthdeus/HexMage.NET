using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;

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
        public static Random Random;

        static Generator() {
            if (Constants.UseGlobalSeed) {
                Random = new Random(Constants.RandomSeed);
            } else {
                Random = new Random();
            }
        }

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

        public static void RandomPlaceMob(GameInstance game, int mobId) {
            var map = game.Map;
            var state = game.State;
            var mobManager = game.MobManager;
            int size = map.Size;

            Predicate<AxialCoord> isCoordAvailable = c => {
                bool isWall = map[c] == HexType.Wall;
                var atCoord = state.AtCoord(c);

                return !isWall && (!atCoord.HasValue || atCoord.Value == mobId);
            };

            state.MobInstances[mobId].Coord = AxialCoord.Zero;

            // TODO: when run out of iteration, sequential scan
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
            var elements = new[] {
                AbilityElement.Earth, AbilityElement.Fire, AbilityElement.Air, AbilityElement.Water
            };

            var abilities = new List<int>();
            int maxAp = RandomAp();
            int maxHp = RandomHp();

            for (int i = 0; i < 1; i++) {
                var element = elements[Random.Next(0, 4)];
                var buff = RandomBuff(element);

                var areaBuff = RandomAreaBuff(element);

                // TODO - re-enable cooldowns?
                //var cooldown = Random.Next(0, 3);
                var cooldown = 0;

                var ability = new AbilityInfo(RandomDmg(),
                    RandomCost(maxAp),
                    RandomRange(),
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

        public static Buff RandomBuff(AbilityElement element) {
            var hpChange = Random.Next(-5, 0);
            var apChange = Random.Next(-2, 0);
            var lifetime = Random.Next(1, 3);

            while (hpChange == 0 && apChange == 0) {
                hpChange = Random.Next(-5, 0);
                apChange = Random.Next(-2, 0);
            }
            return new Buff(element, hpChange, apChange, lifetime);
        }

        public static AreaBuff RandomAreaBuff(AbilityElement element) {
            return new AreaBuff(AxialCoord.Zero, Random.Next(4), RandomBuff(element));
        }
    }
}