using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class MobManager : IDeepCopyable<MobManager>, IResettable {
        public List<Ability> Abilities = new List<Ability>();
        public List<int> Mobs = new List<int>();
        public List<MobInfo> MobInfos = new List<MobInfo>();
        public MobInstance[] MobInstances = new MobInstance[0];
        public List<int> Cooldowns = new List<int>();

        [JsonIgnore] public HexMap<int?> MobPositions;

        [JsonIgnore] public readonly Dictionary<TeamColor, IMobController> Teams =
            new Dictionary<TeamColor, IMobController>();

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

        public Ability AbilityForId(int id) {
            return Abilities[id];
        }

        public int CooldownFor(int id) {
            return Cooldowns[id];
        }

        public void SetCooldownFor(int id, int cooldown) {
            Cooldowns[id] = cooldown;
        }

        public int? AtCoord(AxialCoord c) {
            return MobPositions[c];
            //foreach (var mobId in Mobs) {
            //    var mobInstance = MobInstanceForId(mobId);
            //    if (mobInstance.Coord.Equals(c)) {
            //        return mobId;
            //    }
            //}

            //return null;
        }

        public int AddMobWithInfo(MobInfo mobInfo) {
            var id = Mobs.Count;
            Mobs.Add(id);

            MobInfos.Add(mobInfo);
            Array.Resize(ref MobInstances, MobInstances.Length + 1);
            MobInstances[MobInstances.Length - 1] = new MobInstance(id);

            return id;
        }

        public void ChangeMobHp(GameInstance gameInstance, int mobId, int hpChange) {
            MobInstances[mobId].Hp += hpChange;
            gameInstance.MobHpChanged(MobInstances[mobId].Hp, MobInfos[mobId].Team);
        }

        public void ChangeMobAp(int mobId, int apChange) {
            MobInstances[mobId].Ap += apChange;
        }

        public void ApplyDots(Map map, GameInstance gameInstance) {
            foreach (var mobId in Mobs) {
                var mobInstance = MobInstanceForId(mobId);

                var buffs = mobInstance.Buffs;
                for (int i = 0; i < buffs.Count; i++) {
                    var buff = buffs[i];

                    ChangeMobHp(gameInstance, mobId, buff.HpChange);
                    ChangeMobAp(mobId, buff.ApChange);
                    buff.Lifetime--;
                    buffs[i] = buff;
                }

                buffs.RemoveAll(x => x.Lifetime == 0);
            }


            for (int i = 0; i < map.AreaBuffs.Count; i++) {
                var areaBuff = map.AreaBuffs[i];
                foreach (var mobId in Mobs) {
                    if (map.AxialDistance(MobInstanceForId(mobId).Coord, areaBuff.Coord) <= areaBuff.Radius) {
                        ChangeMobHp(gameInstance, mobId, areaBuff.Effect.HpChange);
                        ChangeMobAp(mobId, areaBuff.Effect.ApChange);
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


            //map.AreaBuffs.RemoveAll(b => b.Effect.Lifetime == 0);

            var newBuffs = new List<AreaBuff>();

            foreach (var buff in map.AreaBuffs) {
                if (buff.Effect.Lifetime > 0) {
                    newBuffs.Add(buff);
                }
            }

            map.AreaBuffs = newBuffs;
        }

        public void FastMoveMob(Map map, Pathfinder pathfinder, int mobId, AxialCoord pos) {
            var mobInstance = MobInstanceForId(mobId);

            int distance = mobInstance.Coord.Distance(pos);

            Debug.Assert(distance <= mobInstance.Ap, "Trying to move a mob that doesn't have enough AP.");
            Debug.Assert(map[pos] == HexType.Empty, "Trying to move a mob into a wall.");
            Debug.Assert(AtCoord(pos) == null, "Trying to move into a mob.");

            // TODO - odebrat dvojte kopirovani tady
            mobInstance.Ap -= distance;
            MobInstances[mobId] = mobInstance;
            SetMobPosition(mobId, pos);

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
            for (int i = 0; i < Cooldowns.Count; i++) {
                mobManagerCopy.Cooldowns.Add(0);
            }

            mobManagerCopy.Abilities = Abilities;
            mobManagerCopy.MobInfos = MobInfos;
            mobManagerCopy.Mobs = Mobs;

            mobManagerCopy.MobInstances = new MobInstance[MobInstances.Length];

            for (int i = 0; i < MobInstances.Length; i++) {
                mobManagerCopy.MobInstances[i] = MobInstances[i].DeepCopy();
            }

            return mobManagerCopy;
        }

        public void Clear() {
            Mobs.Clear();
            MobInstances = new MobInstance[0];
            MobInfos.Clear();
            Cooldowns.Clear();
            Abilities.Clear();
            Teams.Clear();
            foreach (var coord in MobPositions.AllCoords) {
                MobPositions[coord] = null;
            }
        }

        public void Reset() {
            foreach (var mobId in Mobs) {
                var mobInfo = MobInfoForId(mobId);
                MobInstances[mobId].Hp = mobInfo.MaxHp;
                MobInstances[mobId].Ap = mobInfo.MaxAp;
                SetMobPosition(mobId, MobInstances[mobId].OrigCoord);
            }
        }

        public MobInfo MobInfoForId(int mobId) {
            return MobInfos[mobId];
        }

        public MobInstance MobInstanceForId(int mobId) {
            return MobInstances[mobId];
        }

        public void ResetAp(int mobId) {
            MobInstances[mobId].Ap = MobInfoForId(mobId).MaxAp;
        }

        public void SetMobPosition(int mobId, AxialCoord coord) {
            var instance = MobInstances[mobId];
            MobPositions[instance.Coord] = null;
            MobPositions[coord] = mobId;

            MobInstances[mobId].Coord = coord;
            MobInstances[mobId].OrigCoord = coord;
        }
    }
}