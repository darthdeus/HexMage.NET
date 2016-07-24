using System;
using System.Collections.Generic;

namespace HexMage.Simulator {
    public static class Generator {
        public static Mob RandomMob(Team team, int size, Predicate<AxialCoord> isCoordAvailable) {
            var abilities = new List<Ability>();

            var elements = new AbilityElement[] {
                AbilityElement.Earth, AbilityElement.Fire, AbilityElement.Air, AbilityElement.Water
            };

            var random = new Random();
            for (int i = 0; i < Mob.AbilityCount; i++) {
                abilities.Add(new Ability(random.Next(1, 10),
                                          random.Next(3, 7),
                                          5,
                                          random.Next(0, 3),
                                          elements[random.Next(0, 4)]));
            }

            var mob = new Mob(team, 10, 10, abilities);
            team.Mobs.Add(mob);

            while (true) {
                var x = random.Next(-size, size);
                var y = random.Next(-size, size);
                var z = -x - y;
                var cube = new CubeCoord(x, y, z);
                var zero = new CubeCoord(0, 0, 0);

                if (isCoordAvailable(cube) && cube.Distance(zero) < size) {
                    mob.Coord = cube.ToAxial();
                    break;
                }
            }

            return mob;
        }
    }
}