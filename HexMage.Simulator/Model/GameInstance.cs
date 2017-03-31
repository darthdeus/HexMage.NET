using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class GameInstance : IDeepCopyable<GameInstance>, IResettable {
        // TODO - fuj hardcoded cesty
        public static string MapSaveFilename = @"C:\dev\HexMage\HexMage\HexMage.GUI\map.json";
        public static string MobsSaveFilename = @"C:\dev\HexMage\HexMage\HexMage.GUI\mobs.json";

        public Map Map { get; set; }
        public MobManager MobManager { get; set; }
        public Pathfinder Pathfinder { get; set; }
        public TurnManager TurnManager { get; set; }
        public int Size { get; set; }

        public GameState State { get; set; }

        public bool IsFinished => State.IsFinished;

        public GameInstance(Map map, MobManager mobManager) {
            Map = map;
            MobManager = mobManager;

            Size = map.Size;
            Pathfinder = new Pathfinder(this);
            TurnManager = new TurnManager(this);
            State = new GameState();
        }

        public GameInstance(int size) : this(new Map(size)) {}

        public GameInstance(Map map) : this(map, new MobManager()) {}

        private GameInstance(int size, Map map, MobManager mobManager, Pathfinder pathfinder) {
            Size = size;
            MobManager = mobManager;
            Map = map;
            Pathfinder = pathfinder;
            TurnManager = new TurnManager(this);
            State = new GameState();
        }

        public void PrepareEverything() {
            State.Reset(MobManager);
            Map.PrecomputeCubeLinedraw();
            Pathfinder.PathfindDistanceAll();
            TurnManager.PresortTurnOrder();
            TurnManager.StartNextTurn(Pathfinder, State);
            State.SlowUpdateIsFinished(MobManager);
        }

        public TeamColor? CurrentTeam {
            get {
                var currentMob = TurnManager.CurrentMob;
                if (currentMob.HasValue) {
                    var mobInfo = MobManager.MobInfos[currentMob.Value];
                    return mobInfo.Team;
                } else {
                    return null;
                }
            }
        }

        public TeamColor? VictoryTeam {
            get {
#warning TODO - RedAlive/BlueAlive je obcas i zaporne!
                if (State.RedAlive > 0 && State.BlueAlive <= 0) {
                    return TeamColor.Red;
                } else if (State.RedAlive <= 0 && State.BlueAlive > 0) {
                    return TeamColor.Blue;
                } else if (State.RedAlive <= 0 && State.BlueAlive <= 0) {
                    return null;
                } else {
                    Debug.Assert(!IsFinished);
                    throw new InvalidOperationException("Trying to access the victory team before the game is finished.");
                }
            }
        }

        public IMobController VictoryController {
            get {
                if (VictoryTeam.HasValue) {
                    return MobManager.Teams[VictoryTeam.Value];
                } else {
                    return null;
                }
            }
        }

        public IMobController LoserController {
            get {
                if (VictoryTeam.HasValue) {
                    if (VictoryTeam.Value == TeamColor.Red) {
                        return MobManager.Teams[TeamColor.Blue];
                    } else {
                        return MobManager.Teams[TeamColor.Red];
                    }
                } else {
                    return null;
                }
            }
        }

        public Heatmap BuildHeatmap(int? chosenMob = null) {
            var heatmap = new Heatmap(Size);

            int maxDmg = 0;
            int minDmg = int.MaxValue;

            if (!CurrentTeam.HasValue) return heatmap;

            TeamColor playerTeam = CurrentTeam.Value;

            foreach (var coord in heatmap.Map.AllCoords) {
                foreach (var mobId in MobManager.Mobs) {
                    var enemyInfo = MobManager.MobInfos[mobId];
                    var enemyInstance = State.MobInstances[mobId];

                    // TODO - fuj
                    if (chosenMob.HasValue && chosenMob.Value != mobId) continue;

                    bool isFriendly = playerTeam == enemyInfo.Team;
                    bool isVisible = Map.IsVisible(enemyInstance.Coord, coord);

                    if (!isVisible) continue;
                    // We skip friendly mobs only when not focusing on a particular mob
                    if (!chosenMob.HasValue && isFriendly) continue;

                    int maxAbilityDmg = 0;
                    foreach (var abilityId in enemyInfo.Abilities) {
                        var abilityInfo = MobManager.AbilityForId(abilityId);

                        bool withinRange = Map.AxialDistance(enemyInstance.Coord, coord) <= abilityInfo.Range;
                        bool onCooldown = State.Cooldowns[abilityId] > 0;
                        bool hasEnoughAp = abilityInfo.Cost <= enemyInstance.Ap;

                        bool isAbilityUsable = withinRange && !onCooldown && hasEnoughAp;

                        if (isAbilityUsable && abilityInfo.Dmg > maxAbilityDmg) {
                            maxAbilityDmg = abilityInfo.Dmg;
                        }
                    }

                    heatmap.Map[coord] += maxAbilityDmg;

                    int coordValue = heatmap.Map[coord];
                    if (coordValue < minDmg) minDmg = coordValue;
                    if (coordValue > maxDmg) maxDmg = coordValue;
                }
            }

            heatmap.MinValue = minDmg;
            heatmap.MaxValue = maxDmg;

            return heatmap;
        }

        public bool IsAbilityUsableNoTarget(int mobId, int abilityId) {
            var ability = MobManager.AbilityForId(abilityId);
            var mob = State.MobInstances[mobId];
            return mob.Ap >= ability.Cost && State.Cooldowns[abilityId] == 0;
        }

        //public bool IsAbilityUsable(int mobId, int targetId, int abilityId) {
        //    var mob = State.MobInstances[mobId];
        //    var target = State.MobInstances[targetId];

        //    var line = Map.AxialLinedraw(mob.Coord, target.Coord);
        //    int distance = line.Count - 1;

        //    foreach (var coord in line) {
        //        if (Map[coord] == HexType.Wall) {
        //            Utils.Log(LogSeverity.Debug, nameof(GameInstance), "Path obstructed, no usable abilities.");
        //            return false;
        //        }
        //    }

        //    var ability = MobManager.AbilityForId(abilityId);

        //    return ability.Range >= distance && IsAbilityUsable(mobId, abilityId);
        //}

        public bool CanMoveTo(CachedMob mob, AxialCoord coord) {
            bool isEmpty = Map[coord] == HexType.Empty && State.AtCoord(coord) == null;
            bool enoughAp = mob.MobInstance.Ap >= Pathfinder.Distance(mob.MobInstance.Coord, coord);

            return isEmpty && enoughAp;
        }

        public bool IsTargetable(CachedMob mob, CachedMob target) {
            bool isVisible = Map.IsVisible(mob.MobInstance.Coord, target.MobInstance.Coord);
            bool isTargetAlive = target.MobInstance.Hp > 0;
            bool isEnemy = mob.MobInfo.Team != target.MobInfo.Team;

            return isVisible && isTargetAlive && isEnemy;
        }

        public bool IsAbilityUsableApRangeCheck(CachedMob mob, CachedMob target, int abilityId) {
            var abilityInfo = MobManager.Abilities[abilityId];

            bool enoughAp = mob.MobInstance.Ap >= abilityInfo.Cost;
            bool withinRange = mob.MobInstance.Coord.Distance(target.MobInstance.Coord) <= abilityInfo.Range;

            return enoughAp && withinRange;
        }

        public bool IsAbilityUsable(CachedMob mob, CachedMob target, int abilityId) {
            return IsTargetable(mob, target) && IsAbilityUsableApRangeCheck(mob, target, abilityId);
        }

        public void FastUse(int abilityId, int mobId, int targetId) {
            var target = State.MobInstances[targetId];
            var targetInfo = MobManager.MobInfos[targetId];
            Debug.Assert(State.Cooldowns[abilityId] == 0, "Trying to use an ability with non-zero cooldown.");
            Debug.Assert(target.Hp > 0, "Target is dead.");

            var ability = MobManager.AbilityForId(abilityId);

            Debug.Assert(ability.Cooldown == 0);
            State.Cooldowns[abilityId] = ability.Cooldown;

            TargetHit(abilityId, mobId, targetId);
        }

        private void TargetHit(int abilityId, int mobId, int targetId) {
            var ability = MobManager.AbilityForId(abilityId);

            State.ChangeMobHp(this, targetId, -ability.Dmg);

            var targetInstance = State.MobInstances[targetId];
            var targetInfo = MobManager.MobInfos[targetId];

            // TODO - combine with existing buffs
            if (ability.Buff.IsZero) {
                targetInstance.Buff = ability.ElementalEffect;
            } else {
                targetInstance.Buff = ability.Buff;
            }

            State.MobInstances[targetId] = targetInstance;
            //targetInstance.Buffs.Add(ability.ElementalEffect);
            //foreach (var abilityBuff in ability.Buffs) {
            //    // TODO - handle lifetimes
            //    targetInstance.Buffs.Add(abilityBuff);
            //}


            if (!ability.AreaBuff.IsZero) {
                var copy = ability.AreaBuff;
                copy.Coord = targetInstance.Coord;
                Map.AreaBuffs.Add(copy);
            }

            // TODO - handle negative AP
            State.ChangeMobAp(mobId, -ability.Cost);
        }

        public GameInstance CopyStateOnly() {
            var game = new GameInstance(Map.Size, Map, MobManager, null);
            game.TurnManager = TurnManager.DeepCopy(game);
            game.Pathfinder = Pathfinder.ShallowCopy(game);
            game.State = State.DeepCopy();

            return game;
        }


        public GameInstance DeepCopy() {
#warning TODO - tohle prepsat poradne!
            var mapCopy = Map.DeepCopy();

            // TODO - should the MobManager be copied here?                                                           
            var game = new GameInstance(mapCopy, MobManager);
            game.TurnManager = TurnManager.DeepCopy(game);
            game.Pathfinder = Pathfinder.ShallowCopy(game);
            game.State = State.DeepCopy();

            return game;
        }

        public TurnEndResult NextMobOrNewTurn() {
            return TurnManager.NextMobOrNewTurn(Pathfinder, State);
        }

#warning TODO - funguje tohle jeste?
        public void Reset() {
            Map.Reset();
            State.Reset(MobManager);
            TurnManager.Reset();
            Pathfinder.Reset();

            State.SlowUpdateIsFinished(MobManager);
        }

        private AbilityElement BonusElement(AbilityElement element) {
            switch (element) {
                case AbilityElement.Earth:
                    return AbilityElement.Fire;
                case AbilityElement.Fire:
                    return AbilityElement.Air;
                case AbilityElement.Air:
                    return AbilityElement.Water;
                case AbilityElement.Water:
                    return AbilityElement.Earth;
                default:
                    throw new InvalidOperationException("Invalid element type");
            }
        }

        private AbilityElement OppositeElement(AbilityElement element) {
            switch (element) {
                case AbilityElement.Earth:
                    return AbilityElement.Air;
                case AbilityElement.Fire:
                    return AbilityElement.Water;
                case AbilityElement.Air:
                    return AbilityElement.Earth;
                case AbilityElement.Water:
                    return AbilityElement.Fire;
                default:
                    throw new InvalidOperationException("Invalid element type");
            }
        }

        public static GameInstance FromJSON(string jsonStr) {
            var mapRepresentation = JsonConvert.DeserializeObject<MapRepresentation>(jsonStr);

            var map = new Map(5);

            var result = new GameInstance(map);
            return result;
        }

        public float PercentageHp(TeamColor team) {
            int totalMaxHp = 0;
            int totalCurrentHp = 0;

            foreach (var mobId in MobManager.Mobs) {
                var mobInfo = MobManager.MobInfos[mobId];
                var mobInstance = State.MobInstances[mobId];

                totalMaxHp += mobInfo.MaxHp;
                totalCurrentHp += mobInstance.Hp;
            }

            return (float) totalCurrentHp / (float) totalMaxHp;
        }

        /// TODO - fix stuff below
        public int AddMobWithInfo(MobInfo mobInfo) {
            Debug.Assert(State.MobInstances.Length == MobManager.MobInfos.Count,
                         "State.MobInstances.Length == MobManager.MobInfos.Count");
            Debug.Assert(State.MobInstances.Length == MobManager.Mobs.Count,
                         "State.MobInstances.Length == MobManager.Mobs.Count");

            var id = MobManager.Mobs.Count;

            MobManager.Mobs.Add(id);
            MobManager.MobInfos.Add(mobInfo);

            Array.Resize(ref State.MobInstances, State.MobInstances.Length + 1);
            State.MobInstances[State.MobInstances.Length - 1] = new MobInstance(id);

            return id;
        }

        public int AddAbilityWithInfo(Ability ability) {
            MobManager.Abilities.Add(ability);
            State.Cooldowns.Add(0);
            return MobManager.Abilities.Count - 1;
        }

        public void FastMove(int mobId, AxialCoord coord) {
            State.FastMoveMob(Map, Pathfinder, mobId, coord);
        }

        public CachedMob CachedMob(int mobId) {
            return Model.CachedMob.Create(this, mobId);
        }
    }
}