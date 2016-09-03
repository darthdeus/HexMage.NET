using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HexMage.Simulator
{
    public class MobManager
    {
        // TODO - combine this into the property
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

        public Team AddTeam(TeamColor color, IMobController controller) {
            if (Teams.Any(t => t.Color == color)) {
                throw new ArgumentException("Team color is already in use", nameof(color));
            }
            var team = new Team(color, controller);
            Teams.Add(team);
            return team;
        }

        public void AddMob(Mob mob) {
            _mobs.Add(mob);
        }

        public void ApplyDots(Map map) {
            foreach (var mob in Mobs) {
                foreach (var buff in mob.Buffs) {
                    ApplyBuff(mob, buff);
                }

                mob.Buffs.RemoveAll(x => x.Lifetime == 0);

                var buffs = map.BuffsAt(mob.Coord);

                foreach (var buff in buffs) {
                    ApplyBuff(mob, buff);
                }
            }
        }

        public void ApplyBuff(Mob mob, Buff buff) {
            mob.AP += buff.ApChange;
            mob.HP += buff.HpChange;
            buff.Lifetime--;
        }
    }
}