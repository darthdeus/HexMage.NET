using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class MobManager : IDeepCopyable<MobManager>, IResettable {
        public List<Ability> Abilities = new List<Ability>();
        public List<MobId> Mobs = new List<MobId>();
        public List<MobInfo> MobInfos = new List<MobInfo>();
        public List<MobInstance> MobInstances = new List<MobInstance>();
        public List<int> Cooldowns = new List<int>();

        public readonly Dictionary<TeamColor, IMobController> Teams = new Dictionary<TeamColor, IMobController>();

        //public bool MoveOneHex(Mob mob, AxialCoord to) {
        //    if (mob.Coord == to) {
        //        Utils.Log(LogSeverity.Debug, nameof(MobManager), "MoveMob failed trying to move zero distance.");
        //        return false;
        //    }
        //    Debug.Assert(mob.Coord != to, "Trying to move zero distance.");
        //    Debug.Assert(mob.Coord.Distance(to) == 1, "Trying to walk more than 1 hex");

        //    if (mob.Ap > 0) {
        //        mob.Coord = to;
        //        mob.Ap--;

        //        return true;
        //    } else {
        //        return false;
        //    }
        //}

        public Ability AbilityForId(AbilityId id) {
            return Abilities[id.Id];
        }

        public int CooldownFor(AbilityId id) {
            return Cooldowns[id.Id];
        }

        public void SetCooldownFor(AbilityId id, int cooldown) {
            Cooldowns[id.Id] = cooldown;
        }

        public MobId? AtCoord(AxialCoord c) {
            foreach (var mobId in Mobs) {
                var mobInstance = MobInstanceForId(mobId);
                if (mobInstance.Coord.Equals(c)) {
                    return mobId;
                }
            }

            return null;
        }

        public MobId AddMobWithInfo(MobInfo mobInfo) {
            var id = new MobId(Mobs.Count);
            Mobs.Add(id);

            MobInfos.Add(mobInfo);
            MobInstances.Add(new MobInstance(id));

            return id;
        }


        public void ChangeMobHp(MobId mobId, int hpChange) {
            
        }

        public void ChangeMobAp(MobId mobId, int hpChange)
        {

        }

        public void ApplyDots(Map map, GameInstance gameInstance) {
            foreach (var mobId in Mobs) {
                var mobInfo = MobInfoForId(mobId);
                
                var buffs = mobInfo.Buffs;
                for (int i = 0; i < buffs.Count; i++) {
                    var buff = buffs[i];

                    ChangeMobHp(mobId, buff.HpChange);
                    ChangeMobAp(mobId, buff.ApChange);
                    gameInstance.MobHpChanged(MobInstanceForId(mobId), MobInfoForId(mobId).Team);               
                    buff.Lifetime--;
                    buffs[i] = buff;
                }

                mob.Buffs.RemoveAll(x => x.Lifetime == 0);
            }

            var newBuffs = new List<AreaBuff>();

            for (int i = 0; i < map.AreaBuffs.Count; i++) {
                var areaBuff = map.AreaBuffs[i];
                foreach (var mobId in Mobs) {
                    if (map.AxialDistance(MobInstanceForId(mobId).Coord, areaBuff.Coord) <= areaBuff.Radius) {
                        ChangeMobHp(mobId, areaBuff.Effect.HpChange);
                        ChangeMobAp(mobId, areaBuff.Effect.ApChange);
                        gameInstance.MobHpChanged(MobInstanceForId(mobId), MobInfoForId(mobId).Team);
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

        public void FastMoveMob(Map map, Pathfinder pathfinder, MobId mobId, AxialCoord pos) {
            var mobInfo = MobInfoForId(mobId);
            var mobInstance = MobInstanceForId(mobId);

            int distance = mobInstance.Coord.Distance(pos);

            Debug.Assert(distance <= mobInstance.Ap, "Trying to move a mob that doesn't have enough AP.");
            Debug.Assert(map[pos] == HexType.Empty, "Trying to move a mob into a wall.");
            Debug.Assert(AtCoord(pos) == null, "Trying to move into a mob.");

            mobInstance.Ap -= distance;
            mobInstance.Coord = pos;
            pathfinder.PathfindFrom(pos);
        }

        public void LowerCooldowns() {
            for (int i = 0; i < Cooldowns.Count; i++) {
                if (Cooldowns[i] > 0) {
                    Cooldowns[i]--;
                }
            }
        }

        public MobManager DeepCopy() {
            var mobManagerCopy = new MobManager();
            for (int i = 0; i < Cooldowns.Count; i++)
            {
                mobManagerCopy.Cooldowns.Add(0);
            }

            mobManagerCopy.Abilities = Abilities;

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

        public MobInfo MobInfoForId(MobId mobId) {
            throw new NotImplementedException();
        }

        public MobInstance MobInstanceForId(MobId mobId) {
            throw new NotImplementedException();
        }

        public void ResetAp(MobId mobId) {
            throw new NotImplementedException();
        }

        public void SetMobPosition(MobId mobId, AxialCoord coord) {
            var instance = MobInstanceForId(mobId);
            instance.Coord = coord;
            instance.OrigCoord = coord;
            MobInstances[mobId.Id] = instance;
        }
    }
}