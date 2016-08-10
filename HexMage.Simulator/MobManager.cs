using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HexMage.Simulator
{
    public class MobManager
    {
        private readonly List<Mob> _mobs = new List<Mob>();
        public IEnumerable<Mob> Mobs => _mobs;
        public List<Team> Teams { get; set; } = new List<Team>();

        public bool MoveMob(Mob mob, AxialCoord to) {
            Debug.Assert(mob.Coord.Distance(to) == 1);

            if (mob.AP > 0) {
                mob.Coord = to;
                mob.AP--;

                return true;
            } else {
                return false;
            }
        }

        public Mob AtCoord(AxialCoord c) {
            return Mobs.FirstOrDefault(mob => Equals(mob.Coord, c));
        }

        public Team AddTeam(TeamColor color) {
            if (Teams.Any(t => t.Color == color)) {
                throw new ArgumentException("Team color is already in use", nameof(color));
            }
            var team = new Team(color);
            Teams.Add(team);
            return team;
        }

        public void AddMob(Mob mob) {
            _mobs.Add(mob);
        }
    }
}