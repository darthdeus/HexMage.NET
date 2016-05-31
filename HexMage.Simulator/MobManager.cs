using System.Collections.Generic;
using System.Linq;

namespace HexMage.Simulator
{
    public class MobManager
    {
        public List<Mob> Mobs { get; set; } = new List<Mob>();
        public List<Team> Teams { get; set; } = new List<Team>();

        public bool MoveMob(Mob mob, Coord to) {
            // TODO - check that the move is only to a neighbour block
            if (mob.AP > 0) {
                mob.Coord = to;
                mob.AP--;

                return true;
            } else {
                return false;
            }
        }

        public Mob AtCoord(Coord c) {
            return Mobs.FirstOrDefault(mob => Equals(mob.Coord, c));
        }

        public Team AddTeam() {
            var team = new Team();
            Teams.Add(team);
            return team;
        }
    }
}