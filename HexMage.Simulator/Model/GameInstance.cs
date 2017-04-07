using System;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;
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

        public bool AllDead => State.RedAlive == 0 && State.BlueAlive == 0;
        public bool IsFinished => State.IsFinished;

        public GameInstance(Map map, MobManager mobManager) {
            Map = map;
            MobManager = mobManager;

            Size = map.Size;
            Pathfinder = new Pathfinder(this);
            TurnManager = new TurnManager(this);
            State = new GameState();

            Array.Resize(ref State.MobInstances, mobManager.Mobs.Count);

            for (int i = 0; i < MobManager.Abilities.Count; i++) {
                State.Cooldowns.Add(0);
            }
            foreach (var mobId in MobManager.Mobs) {
                State.MobInstances[mobId].Id = mobId;
            }
        }

        public GameInstance(int size) : this(new Map(size)) { }

        public GameInstance(Map map) : this(map, new MobManager()) { }

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
            State.SlowUpdateIsFinished(MobManager);
            Map.PrecomputeCubeLinedraw();
            Pathfinder.PathfindDistanceAll();
            TurnManager.PresortTurnOrder();
            TurnManager.StartNextTurn(Pathfinder, State);
        }

        public void PrepareTurnOrder() {
            // TODO - je tohle potreba?
            State.Reset(MobManager);
            State.SlowUpdateIsFinished(MobManager);

            TurnManager.PresortTurnOrder();
            TurnManager.StartNextTurn(Pathfinder, State);
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
                    throw new InvalidOperationException(
                        "Trying to access the victory team before the game is finished.");
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

        public bool IsAbilityUsableNoTarget(int mobId, int abilityId) {
            var ability = MobManager.AbilityForId(abilityId);
            var mobInstance = State.MobInstances[mobId];

            bool enoughAp = mobInstance.Ap >= ability.Cost;
            bool noCooldown = State.Cooldowns[abilityId] == 0;

            return enoughAp && noCooldown;
        }

        public bool CanMoveTo(CachedMob mob, AxialCoord coord) {
            int remainingAp, distance;
            return CanMoveTo(mob, coord, out remainingAp, out distance);
        }

        public bool CanMoveTo(CachedMob mob, AxialCoord coord, out int remainingAp, out int distance) {
            bool isEmpty = Map[coord] == HexType.Empty && State.AtCoord(coord) == null;

            distance = Pathfinder.Distance(mob.MobInstance.Coord, coord);
            remainingAp = mob.MobInstance.Ap - distance;
            bool enoughAp = remainingAp >= 0;

            return isEmpty && enoughAp;
        }

        public bool IsAbilityUsableAtCoord(CachedMob mob, AxialCoord coord, int abilityId) {
            throw new NotImplementedException();
        }

        public bool IsAbilityUsableFrom(CachedMob mob, AxialCoord from, CachedMob target, int abilityId) {
            var ability = MobManager.Abilities[abilityId];

            // TODO - kontrolovat i ze na to policko dojdu?
            bool withinRange = ability.Range >= from.Distance(target.MobInstance.Coord);
            bool enoughAp = mob.MobInstance.Ap >= ability.Cost;

            return withinRange && enoughAp;
        }

        public bool IsTargetable(CachedMob mob, CachedMob target, bool checkVisibility = true) {
            bool isVisible = !checkVisibility || Map.IsVisible(mob.MobInstance.Coord, target.MobInstance.Coord);

            bool isTargetAlive = Constants.AllowCorpseTargetting || target.MobInstance.Hp > 0;
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

            Debug.Assert(MobManager.AbilityForId(abilityId).Cooldown == 0, "Somehow generated an ability with non-zero cooldown.");
            Debug.Assert(State.Cooldowns[abilityId] == 0, "Trying to use an ability with non-zero cooldown.");
            Debug.Assert(State.MobInstances[mobId].Hp > 0, "Source is dead");
            Debug.Assert(target.Hp > 0, "Target is dead.");

            var ability = MobManager.AbilityForId(abilityId);

            Debug.Assert(ability.Cooldown == 0);
            State.Cooldowns[abilityId] = ability.Cooldown;
            Debug.Assert(State.Cooldowns[abilityId] == 0);
            Debug.Assert(ability.Cooldown == 0);

            TargetHit(abilityId, mobId, targetId);
        }

        private void TargetHit(int abilityId, int mobId, int targetId) {
            var ability = MobManager.AbilityForId(abilityId);

            Debug.Assert(ability.Dmg > 0);
            State.ChangeMobHp(this, targetId, -ability.Dmg);

            var targetInstance = State.MobInstances[targetId];
            var targetInfo = MobManager.MobInfos[targetId];

            Constants.WriteLogLine($"Did {ability.Dmg} damage, HP: {targetInstance.Hp}/{targetInfo.MaxHp}");

            if (ability.Buff.IsZero) {
                targetInstance.Buff = Buff.Combine(targetInstance.Buff, ability.ElementalEffect);
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

            if (State.MobInstances[mobId].Ap < ability.Cost) {
                ReplayRecorder.Instance.SaveAndClear(this, 0);
                throw new InvalidOperationException("Trying to use an ability with not enough AP.");
            }
            Debug.Assert(State.MobInstances[mobId].Ap >= ability.Cost, "State.MobInstances[mobId].Ap >= ability.Cost");

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

        public int AddAbilityWithInfo(AbilityInfo abilityInfo) {
            MobManager.Abilities.Add(abilityInfo);
            State.Cooldowns.Add(0);
            return MobManager.Abilities.Count - 1;
        }

        public void FastMove(int mobId, AxialCoord coord) {
            State.FastMoveMob(mobId, coord);
        }

        public CachedMob CachedMob(int mobId) {
            return Model.CachedMob.Create(this, mobId);
        }

        public void PlaceMob(int mobId, AxialCoord coord) {
            var atCoord = State.AtCoord(coord);
            Debug.Assert(atCoord == null || atCoord == mobId);

            State.SetMobPosition(mobId, coord);
            var info = MobManager.MobInfos[mobId];
            info.OrigCoord = coord;
            MobManager.MobInfos[mobId] = info;
        }
    }
}