using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class GameState {
        public struct MobPosition {
            public int MobId;
            public AxialCoord Coord;
        }

        public MobInstance[] MobInstances = new MobInstance[0];
        public readonly List<int> Cooldowns = new List<int>();
        public int? CurrentMobIndex;

        // This is optimized specifically for the smaller data sets on which
        // the evolution is being performed.
        // TODO - pole lepsi?
        //[JsonIgnore] public Dictionary<int, AxialCoord> MobPositions = new Dictionary<int, AxialCoord>();
        public int RedAlive = 0;
        public int BlueAlive = 0;

        public bool IsFinished => RedAlive <= 0 || BlueAlive <= 0;

        public bool IsAlive(int mobId) {
            return MobInstances[mobId].Hp > 0;
        }

        public void FastMoveMob(int mobId, AxialCoord pos) {
            var mobInstance = MobInstances[mobId];

            int distance = mobInstance.Coord.Distance(pos);

            Debug.Assert(distance <= mobInstance.Ap, "Trying to move a mob that doesn't have enough AP.");
            //Debug.Assert(Map[pos] == HexType.Empty, "Trying to move a mob into a wall.");
            Debug.Assert(AtCoord(pos) == null, "Trying to move into a mob.");

            // TODO - odebrat dvojte kopirovani tady
            mobInstance.Ap -= distance;
            MobInstances[mobId] = mobInstance;
            SetMobPosition(mobId, pos);
        }

        public void LowerCooldowns() {
            for (int i = 0; i < Cooldowns.Count; i++) {
                if (Cooldowns[i] > 0) {
                    Cooldowns[i]--;
                }
            }
        }

        public void MobHpChanged(int hp, TeamColor team) {
            if (hp <= 0) {
                switch (team) {
                    case TeamColor.Red:
                        RedAlive--;
                        break;
                    case TeamColor.Blue:
                        BlueAlive--;
                        break;
                }
            }
        }

        public void ChangeMobHp(GameInstance gameInstance, int mobId, int hpChange) {
            MobInstances[mobId].Hp += hpChange;
            MobHpChanged(MobInstances[mobId].Hp, gameInstance.MobManager.MobInfos[mobId].Team);
        }

        public void ChangeMobAp(int mobId, int apChange) {
            MobInstances[mobId].Ap += apChange;
        }

        public void SetMobPosition(int mobId, AxialCoord coord) {
            MobInstances[mobId].Coord = coord;
        }

        public int? AtCoord(AxialCoord c) {
            for (int i = 0; i < MobInstances.Length; i++) {
                if (MobInstances[i].Coord == c) {
                    return i;
                }
            }

            return null;
        }

        public void ApplyDots(Map map, GameInstance gameInstance) {
            foreach (var mobId in gameInstance.MobManager.Mobs) {
                var mobInstance = MobInstances[mobId];
                if (mobInstance.Hp <= 0) continue;

                if (!mobInstance.Buff.IsZero) {
                    mobInstance.Buff.Lifetime--;
                    MobInstances[mobId] = mobInstance;

                    ChangeMobHp(gameInstance, mobId, mobInstance.Buff.HpChange);
                    ChangeMobAp(mobId, mobInstance.Buff.ApChange);
                    
                    Debug.Assert(mobInstance.Buff.Lifetime >= 0, "mobInstance.Buff.Lifetime >= 0");
                }
            }

            for (int i = 0; i < map.AreaBuffs.Count; i++) {
                var areaBuff = map.AreaBuffs[i];
                foreach (var mobId in gameInstance.MobManager.Mobs) {
                    if (!IsAlive(mobId)) continue;

                    if (map.AxialDistance(MobInstances[mobId].Coord, areaBuff.Coord) <= areaBuff.Radius) {
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

            var newBuffs = new List<AreaBuff>();

            foreach (var buff in map.AreaBuffs) {
                if (buff.Effect.Lifetime > 0) {
                    newBuffs.Add(buff);
                }
            }

            map.AreaBuffs = newBuffs;
        }

        public void SlowUpdateIsFinished(MobManager mobManager) {
            RedAlive = 0;
            BlueAlive = 0;
            foreach (var mobId in mobManager.Mobs) {
                var mobInfo = mobManager.MobInfos[mobId];
                var mobInstance = MobInstances[mobId];

                if (mobInstance.Hp > 0 && mobInfo.Team == TeamColor.Red) {
                    RedAlive++;
                }
                if (mobInstance.Hp > 0 && mobInfo.Team == TeamColor.Blue) {
                    BlueAlive++;
                }
            }
        }

        public GameState DeepCopy() {
            var gameStateCopy = new GameState {
                RedAlive = RedAlive,
                BlueAlive = BlueAlive,
                CurrentMobIndex = CurrentMobIndex,
            };

            for (int i = 0; i < Cooldowns.Count; i++) {
                gameStateCopy.Cooldowns.Add(Cooldowns[i]);
            }

            gameStateCopy.MobInstances = new MobInstance[MobInstances.Length];
            for (int i = 0; i < MobInstances.Length; i++) {
                gameStateCopy.MobInstances[i] = MobInstances[i].DeepCopy();
            }

            return gameStateCopy;
        }

        public void Clear() {
            MobInstances = new MobInstance[0];
            Cooldowns.Clear();
            CurrentMobIndex = null;
            RedAlive = 0;
            BlueAlive = 0;
        }

        public void Reset(MobManager mobManager) {
            foreach (var mobId in mobManager.Mobs) {
                var mobInfo = mobManager.MobInfos[mobId];
                MobInstances[mobId].Hp = mobInfo.MaxHp;
                MobInstances[mobId].Ap = mobInfo.MaxAp;
                SetMobPosition(mobId, mobInfo.OrigCoord);
            }
            
            SlowUpdateIsFinished(mobManager);            
        }
    }
}