using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class GameState {
        public MobInstance[] MobInstances = new MobInstance[0];
        public List<int> Cooldowns = new List<int>();
        public int? CurrentMobIndex;
        public int TurnNumber;

        [JsonIgnore] public HexMap<Path> CurrentPaths;

        [JsonIgnore] public HexMap<int?> MobPositions;
        public int RedAlive = 0;
        public int BlueAlive = 0;

        public bool IsFinished => RedAlive <= 0 || BlueAlive <= 0;

        public bool IsAlive(int mobId) {
            return MobInstances[mobId].Hp > 0;
        }

        public void FastMoveMob(Map map, Pathfinder pathfinder, int mobId, AxialCoord pos) {
            var mobInstance = MobInstances[mobId];

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
            var instance = MobInstances[mobId];
            MobPositions[instance.Coord] = null;
            MobPositions[coord] = mobId;

            MobInstances[mobId].Coord = coord;
            MobInstances[mobId].OrigCoord = coord;
        }


        public int? AtCoord(AxialCoord c) {
            return MobPositions[c];
        }


        public void ApplyDots(Map map, GameInstance gameInstance) {
            foreach (var mobId in gameInstance.MobManager.Mobs) {
                var mobInstance = MobInstances[mobId];
                if (mobInstance.Hp <= 0) continue;

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
            var gameStateCopy = new GameState();
            for (int i = 0; i < Cooldowns.Count; i++) {
                gameStateCopy.Cooldowns.Add(0);
            }

            gameStateCopy.MobInstances = new MobInstance[MobInstances.Length];

            gameStateCopy.MobPositions = new HexMap<int?>(MobPositions.Size);
            foreach (var coord in MobPositions.AllCoords) {
                gameStateCopy.MobPositions[coord] = MobPositions[coord];
            }

            for (int i = 0; i < MobInstances.Length; i++) {
                gameStateCopy.MobInstances[i] = MobInstances[i].DeepCopy();
            }

            return gameStateCopy;
        }

        public void Clear() {
            MobInstances = new MobInstance[0];
            Cooldowns.Clear();
            foreach (var coord in MobPositions.AllCoords) {
                MobPositions[coord] = null;
            }
        }

        public void Reset() {
            throw new NotImplementedException();
            //foreach (var mobId in Mobs) {
            //    var mobInfo = MobInfoForId(mobId);
            //    MobInstances[mobId].Hp = mobInfo.MaxHp;
            //    MobInstances[mobId].Ap = mobInfo.MaxAp;
            //    SetMobPosition(mobId, MobInstances[mobId].OrigCoord);
            //}
        }
    }
}