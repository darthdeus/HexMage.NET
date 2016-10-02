using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class MobManager : IDeepCopyable<MobManager>, IResettable {
        public readonly List<Mob> Mobs = new List<Mob>();

        public readonly Dictionary<TeamColor, IMobController> Teams = new Dictionary<TeamColor, IMobController>();

        public bool MoveOneHex(Mob mob, AxialCoord to) {
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

        public Mob AtCoord(AxialCoord c) {
            foreach (var mob in Mobs) {
                if (mob.Coord.Equals(c)) {
                    return mob;
                }
            }

            return null;
        }

        public void AddMob(Mob mob) {
            Mobs.Add(mob);
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

        public void FastMoveMob(Map map, Pathfinder pathfinder, Mob mob, AxialCoord pos) {
            int distance = mob.Coord.Distance(pos);

            Debug.Assert(distance <= mob.Ap, "Trying to move a mob that doesn't have enough AP.");
            Debug.Assert(map[pos] == HexType.Empty, "Trying to move a mob into a wall.");
            Debug.Assert(AtCoord(pos) == null, "Trying to move into a mob.");

            mob.Ap -= distance;
            mob.Coord = pos;
            pathfinder.PathfindFrom(pos);
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
            Mobs.Clear();
            Teams.Clear();
        }

        public void Reset() {
            foreach (var mob in Mobs) {
                mob.Reset();
            }
        }
    }
}