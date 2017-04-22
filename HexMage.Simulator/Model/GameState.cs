using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Pathfinding;
using Newtonsoft.Json;

namespace HexMage.Simulator.Model {
    public class GameState {
        public List<AreaBuff> AreaBuffs = new List<AreaBuff>();

        public MobInstance[] MobInstances = new MobInstance[0];
        public readonly List<int> Cooldowns = new List<int>();
        public int? CurrentMobIndex { get; private set; }
        public TeamColor? LastTeamColor;
        public TeamColor? CurrentTeamColor;
        public List<int> TurnOrder = new List<int>();
        public bool AllPlayed = false;
        public List<int> PlayersPlayed = new List<int>();

        // This is optimized specifically for the smaller data sets on which
        // the evolution is being performed.
        // TODO - pole lepsi?
        //[JsonIgnore] public Dictionary<int, AxialCoord> MobPositions = new Dictionary<int, AxialCoord>();
        public int RedTotalHp = 0;

        public int BlueTotalHp = 0;

        [JsonIgnore]
        public bool IsFinished {
            get {
                if (RedTotalHp < 0) {
                    throw new InvariantViolationException($"RedTotalHp is {RedTotalHp} but should never be below 0.");
                }
                if (BlueTotalHp < 0) {
                    throw new InvariantViolationException($"RedTotalHp is {BlueTotalHp} but should never be below 0.");
                }
                return RedTotalHp == 0 || BlueTotalHp == 0;
            }
        }

        [JsonIgnore]
        public int? CurrentMob {
            get {
                if (!CurrentMobIndex.HasValue) {
                    return null;
                } else if (CurrentMobIndex.Value < TurnOrder.Count) {
                    return TurnOrder[CurrentMobIndex.Value];
                } else {
                    return null;
                }
            }
        }

        public void UpdateCurrentTeam(GameInstance game) {
            var cm = CurrentMob;
            if (cm.HasValue) {
                CurrentTeamColor = game.MobManager.MobInfos[cm.Value].Team;
            } else {
                CurrentTeamColor = null;
            }
        }

        public void SetCurrentMobIndex(GameInstance game, int? index) {
            CurrentMobIndex = index;
            UpdateCurrentTeam(game);
        }

        public bool IsAlive(int mobId) {
            return MobInstances[mobId].Hp > 0;
        }

        public List<Buff> BuffsAt(AxialCoord coord) {
            // TODO - pomale?
            return AreaBuffs.Where(b => b.Coord.Distance(coord) <= b.Radius)
                            .Select(b => b.Effect)
                            .ToList();
        }

        public void LowerCooldowns() {
            for (int i = 0; i < Cooldowns.Count; i++) {
                if (Cooldowns[i] > 0) {
                    Cooldowns[i]--;
                }
            }
        }

        public void ChangeMobHp(GameInstance game, int mobId, int hpChange) {
            var mobInfo = game.MobManager.MobInfos[mobId];
            int before = MobInstances[mobId].Hp;

            MobInstances[mobId].Hp = Math.Max(0, before + hpChange);

            int change = MobInstances[mobId].Hp - before;

            if (mobInfo.Team == TeamColor.Red) {
                RedTotalHp += change;
            } else if (mobInfo.Team == TeamColor.Blue) {
                BlueTotalHp += change;
            } else {
                throw new InvalidOperationException($"Invalid mob team ${mobInfo.Team}");
            }
        }

        public void ChangeMobAp(int mobId, int apChange) {
            MobInstances[mobId].Ap = Math.Max(0, MobInstances[mobId].Ap + apChange);
        }

        public void SetMobPosition(int mobId, AxialCoord coord) {
            MobInstances[mobId].Coord = coord;
        }

        public int? AtCoord(AxialCoord c, bool aliveOnly) {
            for (int i = 0; i < MobInstances.Length; i++) {
                var instance = MobInstances[i];
                bool aliveCheck = !aliveOnly || instance.Hp > 0;
                if (instance.Coord == c && aliveCheck) {
                    return i;
                }
            }

            return null;
        }

        public void ApplyDots(Map map, GameInstance gameInstance) {
            // TODO: vycistit, fuj

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

            Debug.Assert(this == gameInstance.State, "this == gameInstance.State");
            
            for (int i = 0; i < AreaBuffs.Count; i++) {
                var areaBuff = AreaBuffs[i];
                foreach (var mobId in gameInstance.MobManager.Mobs) {
                    if (!IsAlive(mobId)) continue;

                    if (map.AxialDistance(MobInstances[mobId].Coord, areaBuff.Coord) <= areaBuff.Radius) {
                        ChangeMobHp(gameInstance, mobId, areaBuff.Effect.HpChange);
                        ChangeMobAp(mobId, areaBuff.Effect.ApChange);
                    }
                }

                areaBuff.DecreaseLifetime();
                AreaBuffs[i] = areaBuff;
            }

            var newBuffs = new List<AreaBuff>();

            foreach (var buff in AreaBuffs) {
                if (buff.Effect.Lifetime > 0) {
                    newBuffs.Add(buff);
                }
            }

            AreaBuffs = newBuffs;
        }

        public void SlowUpdateIsFinished(MobManager mobManager) {
            RedTotalHp = 0;
            BlueTotalHp = 0;
            foreach (var mobId in mobManager.Mobs) {
                var mobInfo = mobManager.MobInfos[mobId];

                int currentHp = MobInstances[mobId].Hp;

                if (mobInfo.Team == TeamColor.Red) {
                    RedTotalHp += currentHp;
                } else {
                    BlueTotalHp += currentHp;
                }
            }
        }

        public GameState DeepCopy() {
            var gameStateCopy = new GameState {
                RedTotalHp = RedTotalHp,
                BlueTotalHp = BlueTotalHp,
                CurrentMobIndex = CurrentMobIndex,
                LastTeamColor = LastTeamColor,
                CurrentTeamColor = CurrentTeamColor,
                TurnOrder = TurnOrder.ToList(),
                AllPlayed = AllPlayed,
                PlayersPlayed = PlayersPlayed?.ToList()
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
            CurrentTeamColor = null;
            LastTeamColor = null;
            RedTotalHp = 0;
            BlueTotalHp = 0;
            TurnOrder.Clear();
            PlayersPlayed.Clear();
        }

        private void CopyTurnOrderFromPresort(GameInstance game) {
            TurnOrder = new List<int>(game.TurnManager.PresortedOrder.Count);

            foreach (var id in game.TurnManager.PresortedOrder) {
                TurnOrder.Add(id);
            }
        }

        public void Reset(GameInstance game) {
            foreach (var mobId in game.MobManager.Mobs) {
                var mobInfo = game.MobManager.MobInfos[mobId];
                MobInstances[mobId].Hp = mobInfo.MaxHp;
                MobInstances[mobId].Ap = mobInfo.MaxAp;
                SetMobPosition(mobId, mobInfo.OrigCoord);
            }

            for (int i = 0; i < Cooldowns.Count; i++) {
                Cooldowns[i] = 0;
            }

            AreaBuffs.Clear();
            PlayersPlayed.Clear();
            CopyTurnOrderFromPresort(game);
            SetCurrentMobIndex(game, 0);
            SlowUpdateIsFinished(game.MobManager);
        }
    }
}