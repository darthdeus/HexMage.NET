using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class MobManager : IDeepCopyable<MobManager> {
        // TODO - combine this into the property
        private readonly List<Mob> _mobs = new List<Mob>();
        public IEnumerable<Mob> Mobs => _mobs;
        public IEnumerable<Mob> AliveMobs => Mobs.Where(m => m.Hp > 0);

        public readonly Dictionary<TeamColor, IMobController> Teams = new Dictionary<TeamColor, IMobController>();

        public bool MoveMob(Mob mob, AxialCoord to) {
            if (mob.Coord == to) {
                Utils.Log(LogSeverity.Debug, nameof(MobManager), "MoveMob failed trying to move zero distance.");
                return false;
            }
            Debug.Assert(mob.Coord != to, "Trying to move zero distance.");
            Debug.Assert(mob.Coord.Distance(to) == 1, "Trying to walk more than 1 hex");

            if (mob.Ap > 0) {
                mob.Coord = to;
                mob.Ap--;

                return true;
            } else {
                return false;
            }
        }

        public IEnumerable<Mob> MobsInTeam(TeamColor color) {
            return Mobs.Where(mob => mob.Team == color);
        }

        public Mob AtCoord(AxialCoord c) {
            return Mobs.FirstOrDefault(mob => Equals(mob.Coord, c));
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

                foreach (var buff in buffs) {
                    ApplyBuff(mob, buff, LifetimeChange.KeepLifetime);
                }
            }

            // TODO - store these in a list instead so that the whole map doesn't have to be iterated each turn
            foreach (var coord in map.AllCoords) {
                var buffs = map.BuffsAt(coord);
                foreach (var buff in buffs) {
                    buff.Lifetime--;
                    Debug.Assert(buff.Lifetime >= 0,
                                 "Buff lifetime should never be negative, as they're removed when they reach zero.");
                }

                buffs.RemoveAll(x => x.Lifetime == 0);
            }
        }

        public void ApplyBuff(Mob mob, Buff buff, LifetimeChange lifetimeChange) {
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

        public MobManager DeepCopy() {
            var mobManagerCopy = new MobManager();

            foreach (var mob in Mobs) {
                mobManagerCopy.AddMob(mob.DeepCopy());
            }

            return mobManagerCopy;
        }

        public void Clear() {
            _mobs.Clear();
            Teams.Clear();           
        }
    }
}