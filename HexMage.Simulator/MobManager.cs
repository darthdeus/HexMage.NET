using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class MobManager : IDeepCopyable<MobManager>, IResettable {
        public readonly List<Ability> Abilities = new List<Ability>();
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

        public Ability AbilityForId(AbilityId id) {
            return Abilities[id.Id];
        }

        public int CooldownFor(AbilityId id) {
            throw new NotImplementedException();
        }

        public void SetCooldownFor(AbilityId id, int cooldown) {
            throw new NotImplementedException();
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

        public void ApplyDots(Map map) {
            foreach (var mob in Mobs) {
                var buffs = mob.Buffs;
                for (int i = 0; i < buffs.Count; i++) {
                    var buff = buffs[i];

                    mob.Ap += buff.ApChange;
                    mob.Hp += buff.HpChange;
                    buff.Lifetime--;
                    buffs[i] = buff;
                }

                mob.Buffs.RemoveAll(x => x.Lifetime == 0);
            }

            var newBuffs = new List<AreaBuff>();

            for (int i = 0; i < map.AreaBuffs.Count; i++) {
                var areaBuff = map.AreaBuffs[i];
                foreach (var mob in Mobs) {
                    if (map.AxialDistance(mob.Coord, areaBuff.Coord) <= areaBuff.Radius) {
                        mob.Ap += areaBuff.Effect.ApChange;
                        mob.Hp += areaBuff.Effect.HpChange;
                    }
                }

                areaBuff.DecreaseLifetime();
                map.AreaBuffs[i] = areaBuff;
            }

            for (int i = 0; i < map.AreaBuffs.Count; i++) {
                var buff = map.AreaBuffs[i];
                buff.DecreaseLifetime();
                map.AreaBuffs[i] = buff;
            }

            foreach (var buff in map.AreaBuffs) {
                if (buff.Effect.Lifetime > 0) {
                    newBuffs.Add(buff);
                }
            }

            map.AreaBuffs = newBuffs;
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