using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HexMage.Simulator {
    public class MobManager {
        // TODO - combine this into the property
        private readonly List<Mob> _mobs = new List<Mob>();
        public IEnumerable<Mob> Mobs => _mobs;
        public List<Team> Teams { get; set; } = new List<Team>();

        public bool MoveMob(Mob mob, AxialCoord to) {
            Debug.Assert(mob.Coord.Distance(to) == 1);

            if (mob.Ap > 0) {
                mob.Coord = to;
                mob.Ap--;

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

        public enum LifetimeChange {
            UpdateLifetime,
            KeepLifetime
        }

        public void ApplyDots(Map map) {
            foreach (var mob in Mobs) {
                foreach (var buff in mob.Buffs) {
                    ApplyBuff(mob, buff, LifetimeChange.UpdateLifetime);
                }

                mob.Buffs.RemoveAll(x => x.Lifetime == 0);

                var buffs = map.BuffsAt(mob.Coord);

                Console.WriteLine("Area");
                foreach (var buff in buffs) {
                    ApplyBuff(mob, buff, LifetimeChange.KeepLifetime);
                }
            }

            Console.WriteLine($"Starting buff update on map {map.Guid}");
            // TODO - store these in a list instead so that the whole map doesn't have to be iterated each turn
            foreach (var coord in map.AllCoords) {
                Console.WriteLine("\tUpdating buffs");
                var buffs = map.BuffsAt(coord);
                if (buffs.Count > 0) {
                    Console.WriteLine($"There are non-zero buffs {buffs.Count}");
                }
                foreach (var buff in buffs) {
                    buff.Lifetime--;
                    Debug.Assert(buff.Lifetime >= 0, "Buff lifetime should never be negative, as they're removed when they reach zero.");
                }

                buffs.RemoveAll(x => x.Lifetime == 0);
            }
        }

        public void ApplyBuff(Mob mob, Buff buff, LifetimeChange lifetimeChange) {
            Console.WriteLine($"Applying {buff}");
            mob.Ap += buff.ApChange;
            mob.Hp += buff.HpChange;
            if (lifetimeChange == LifetimeChange.UpdateLifetime) {
                buff.Lifetime--;
            }
        }

        public void LowerCooldowns() {
            foreach (var mob in Mobs) {
                foreach (var ability in mob.Abilities) {
                    if (ability.CurrentCooldown > 0) ability.CurrentCooldown--;
                }
            }
        }
    }
}