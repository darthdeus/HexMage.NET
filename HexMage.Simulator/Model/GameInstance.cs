using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public GameState State { get; set; }

        public int Size { get; set; }

        [JsonIgnore]
        [Obsolete]
        public bool AllDead => State.RedTotalHp == 0 && State.BlueTotalHp == 0;

        [JsonIgnore]
        public bool IsFinished => State.IsFinished;

        [JsonIgnore]
        public IMobController CurrentController
            => CurrentMob != null ? MobManager.Teams[MobManager.MobInfos[CurrentMob.Value].Team] : null;

        [JsonIgnore]
        public int? CurrentMob => State.CurrentMob;

        [JsonConstructor]
        public GameInstance() { }

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

        private GameInstance(Map map, MobManager mobManager, Pathfinder pathfinder) {
            Size = map.Size;
            MobManager = mobManager;
            Map = map;
            Pathfinder = pathfinder;
            TurnManager = new TurnManager(this);
            State = new GameState();
        }

        public void PrepareEverything() {
            MobManager.InitializeState(State);
            Map.PrecomputeCubeLinedraw();
            Pathfinder.PathfindDistanceAll();
            TurnManager.PresortTurnOrder();
            State.Reset(this);
            State.LastTeamColor = CurrentTeam;
        }

        public void PrepareTurnOrder() {
            // TODO - je tohle potreba?
            TurnManager.PresortTurnOrder();
            State.Reset(this);
        }

        [JsonIgnore]
        public TeamColor? CurrentTeam => State.CurrentTeamColor;

        [JsonIgnore]
        public TeamColor? VictoryTeam {
            get {
                if (State.RedTotalHp > 0 && State.BlueTotalHp <= 0) {
                    return TeamColor.Red;
                } else if (State.RedTotalHp <= 0 && State.BlueTotalHp > 0) {
                    return TeamColor.Blue;
                } else if (State.RedTotalHp <= 0 && State.BlueTotalHp <= 0) {
                    return null;
                } else {
                    Debug.Assert(!IsFinished);
                    throw new InvalidOperationException(
                        "Trying to access the victory team before the game is finished.");
                }
            }
        }

        [JsonIgnore]
        public IMobController VictoryController {
            get {
                if (VictoryTeam.HasValue) {
                    return MobManager.Teams[VictoryTeam.Value];
                } else {
                    return null;
                }
            }
        }

        [JsonIgnore]
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

        public GameInstance CopyStateOnly() {
            var game = new GameInstance(Map, MobManager, null);
            game.Pathfinder = Pathfinder.ShallowCopy(game);
            game.TurnManager = TurnManager.ShallowCopy(game);
            game.State = State.DeepCopy();

            return game;
        }

        public GameInstance DeepCopy() {
            var mapCopy = Map.DeepCopy();
            var mobManagerCopy = MobManager.DeepCopy();

            var game = new GameInstance(mapCopy, mobManagerCopy);
            game.Pathfinder = Pathfinder.DeepCopy(game);

            game.TurnManager = TurnManager.DeepCopy(game);
            game.State = State.DeepCopy();

            return game;
        }

        public float PercentageHp(TeamColor team) {
            float totalMaxHp = 0;
            float totalCurrentHp = 0;

            foreach (var mobId in MobManager.Mobs) {
                var mobInfo = MobManager.MobInfos[mobId];
                if (mobInfo.Team != team) continue;

                var mobInstance = State.MobInstances[mobId];

                totalMaxHp += mobInfo.MaxHp;
                totalCurrentHp += mobInstance.Hp;
            }

            return totalCurrentHp / totalMaxHp;
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

        public CachedMob CachedMob(int mobId) {
            return Model.CachedMob.Create(this, mobId);
        }

        public void PlaceMob(int mobId, AxialCoord coord) {
            var atCoord = State.AtCoord(coord, true);
            Debug.Assert(atCoord == null || atCoord == mobId);

            State.SetMobPosition(mobId, coord);
            var info = MobManager.MobInfos[mobId];
            info.OrigCoord = coord;
            MobManager.MobInfos[mobId] = info;
        }

        public void Reset() {
            State.Reset(this);
        }

        public void AssignAiControllers(IMobController c1, IMobController c2) {
            MobManager.Teams[TeamColor.Red] = c1;
            MobManager.Teams[TeamColor.Blue] = c2;
        }
    }
}