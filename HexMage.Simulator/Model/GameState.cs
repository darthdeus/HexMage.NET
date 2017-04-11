using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class GameState {
        public List<AreaBuff> AreaBuffs = new List<AreaBuff>();

        public MobInstance[] MobInstances = new MobInstance[0];
        public readonly List<int> Cooldowns = new List<int>();
        public int? CurrentMobIndex;
        public TeamColor? LastTeamColor;
        public List<int> TurnOrder = new List<int>();

        // This is optimized specifically for the smaller data sets on which
        // the evolution is being performed.
        // TODO - pole lepsi?
        //[JsonIgnore] public Dictionary<int, AxialCoord> MobPositions = new Dictionary<int, AxialCoord>();
        public int RedAlive = 0;

        public int BlueAlive = 0;

        [JsonIgnore]
        public bool IsFinished => RedAlive <= 0 || BlueAlive <= 0;

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
            MobInstances[mobId].Hp = Math.Max(0, MobInstances[mobId].Hp + hpChange);
            MobHpChanged(MobInstances[mobId].Hp, gameInstance.MobManager.MobInfos[mobId].Team);
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
                LastTeamColor = LastTeamColor,
                TurnOrder = TurnOrder.ToList()
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
            TurnOrder.Clear();
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

            CopyTurnOrderFromPresort(game);
            SlowUpdateIsFinished(game.MobManager);
        }
    }
}