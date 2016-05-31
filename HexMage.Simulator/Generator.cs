using System;
using System.Collections.Generic;

namespace HexMage.Simulator
{
    public static class Generator
    {
        public static Mob RandomMob(Team team, int size, Predicate<Coord> isCoordAvailable) {
            var abilities = new List<Ability>();

            var random = new Random();
            for (int i = 0; i < Mob.AbilityCount; i++) {
                abilities.Add(new Ability(random.Next(1, 10), random.Next(3, 7), 5));
            }

            var mob = new Mob(team, 10, 10, abilities);
            team.Mobs.Add(mob);

            while (true) {
                Coord c = new Coord(random.Next(0, size), random.Next(0, size));
                if (isCoordAvailable(c)) {
                    mob.Coord = c;
                    break;
                }
            }

            return mob;
        }
    }
}