using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HexMage.Simulator.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HexMage.Simulator {
    public class GameInstance : IDeepCopyable<GameInstance>, IResettable {
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
        }

        public GameInstance(int size) : this(new Map(size)) {}
        public GameInstance(Map map) : this(map, new MobManager()) {}

        private GameInstance(int size, Map map, MobManager mobManager, Pathfinder pathfinder) {
            Size = size;
            MobManager = mobManager;
            Map = map;
            Pathfinder = pathfinder;
            TurnManager = new TurnManager(this);
        }

        public void PrepareEverything() {
            State.MobPositions = new HexMap<int?>(Size);
            State.Reset();
            Map.PrecomputeCubeLinedraw();
            Pathfinder.PathfindDistanceAll();
            TurnManager.PresortTurnOrder();
            TurnManager.StartNextTurn(Pathfinder, State);
        }

     
        public bool IsAbilityUsable(int mobId, int abilityId) {
            var ability = MobManager.AbilityForId(abilityId);
            var mob = State.MobInstances[mobId];
            return mob.Ap >= ability.Cost && State.Cooldowns[abilityId] == 0;
        }

        public bool IsAbilityUsable(int mobId, int targetId, int abilityId) {
            var mob = State.MobInstances[mobId];
            var target = State.MobInstances[targetId];

            var line = Map.AxialLinedraw(mob.Coord, target.Coord);
            int distance = line.Count - 1;

            foreach (var coord in line) {
                if (Map[coord] == HexType.Wall) {
                    Utils.Log(LogSeverity.Debug, nameof(GameInstance), "Path obstructed, no usable abilities.");
                    return false;
                }
            }

            var ability = MobManager.AbilityForId(abilityId);

            return ability.Range >= distance && IsAbilityUsable(mobId, abilityId);
        }


        public DefenseDesire FastUse(int abilityId, int mobId, int targetId) {
            var target = State.MobInstances[targetId];
            var targetInfo = MobManager.MobInfos[targetId];
            Debug.Assert(State.Cooldowns[abilityId] == 0, "Trying to use an ability with non-zero cooldown.");
            Debug.Assert(target.Hp > 0, "Target is dead.");

            DefenseDesire result;

            var ability = MobManager.AbilityForId(abilityId);

            State.Cooldowns[abilityId] = ability.Cooldown;

            if (target.Ap >= targetInfo.DefenseCost) {
                var controller = MobManager.Teams[targetInfo.Team];
                var res = controller.FastRequestDesireToDefend(targetId, abilityId);

                if (res == DefenseDesire.Block) {
                    State.ChangeMobAp(mobId, -MobManager.AbilityForId(abilityId).Cost);
                    State.ChangeMobAp(targetId, -MobManager.MobInfos[targetId].DefenseCost);
                    return DefenseDesire.Block;
                } else {
                    TargetHit(abilityId, mobId, targetId);

                    result = DefenseDesire.Pass;
                }
            } else {
                TargetHit(abilityId, mobId, targetId);
                result = DefenseDesire.Pass;
            }

            return result;
        }

        public async Task<DefenseDesire> SlowUse(int abilityId, int mobId, int targetId) {
            var target = State.MobInstances[targetId];
            var targetInfo = MobManager.MobInfos[targetId];
            Debug.Assert(State.Cooldowns[abilityId] == 0, "Trying to use an ability with non-zero cooldown.");
            Debug.Assert(target.Hp > 0, "Target is dead.");

            DefenseDesire result;

            var ability = MobManager.AbilityForId(abilityId);

            State.Cooldowns[abilityId] = ability.Cooldown;

            if (target.Ap >= targetInfo.DefenseCost) {
                var controller = MobManager.Teams[targetInfo.Team];
                var res = await controller.SlowRequestDesireToDefend(targetId, abilityId);

                if (res == DefenseDesire.Block) {
                    State.ChangeMobAp(mobId, -MobManager.AbilityForId(abilityId).Cost);
                    State.ChangeMobAp(targetId, -MobManager.MobInfos[targetId].DefenseCost);
                    return DefenseDesire.Block;
                } else {
                    TargetHit(abilityId, mobId, targetId);

                    result = DefenseDesire.Pass;
                }
            } else {
                TargetHit(abilityId, mobId, targetId);
                result = DefenseDesire.Pass;
            }

            return result;
        }

        public void FastUseWithDefenseDesire(int mobId, int targetId, int abilityId, DefenseDesire defenseDesire) {
            var target = State.MobInstances[targetId];
            var targetInfo = MobManager.MobInfos[targetId];
            Debug.Assert(State.Cooldowns[abilityId] == 0, "Trying to use an ability with non-zero cooldown.");
            Debug.Assert(target.Hp > 0, "Target is dead.");

            var ability = MobManager.AbilityForId(abilityId);

            State.Cooldowns[abilityId] = ability.Cooldown;

            if (target.Ap >= targetInfo.DefenseCost) {
                if (defenseDesire == DefenseDesire.Block) {
                    State.ChangeMobAp(mobId, -MobManager.AbilityForId(abilityId).Cost);
                    State.ChangeMobAp(targetId, -MobManager.MobInfos[targetId].DefenseCost);
                } else {
                    TargetHit(abilityId, mobId, targetId);
                }
            } else {
                TargetHit(abilityId, mobId, targetId);
            }
        }

        private void TargetHit(int abilityId, int mobId, int targetId) {
            var ability = MobManager.AbilityForId(abilityId);

            State.ChangeMobHp(this, targetId, -ability.Dmg);

            var targetInstance = State.MobInstances[targetId];
            var targetInfo = MobManager.MobInfos[targetId];

            targetInstance.Buffs.Add(ability.ElementalEffect);
            foreach (var abilityBuff in ability.Buffs) {
                // TODO - handle lifetimes
                targetInstance.Buffs.Add(abilityBuff);
            }

            foreach (var areaBuff in ability.AreaBuffs) {
                var copy = areaBuff;
                copy.Coord = targetInstance.Coord;
                Map.AreaBuffs.Add(copy);
            }

            // TODO - handle negative AP
            State.ChangeMobAp(mobId, -ability.Cost);
        }

        public GameInstance DeepCopy() {
#warning TODO - tohle prepsat poradne!
            var mapCopy = Map.DeepCopy();
            var mobManagerCopy = MobManager.DeepCopy();

            var game = new GameInstance(mapCopy, mobManagerCopy);
            game.TurnManager = TurnManager.DeepCopy(game);
            game.Pathfinder = Pathfinder.DeepCopy(game);

            return game;
        }

#warning TODO - funguje tohle jeste?
        public void Reset() {
            Map.Reset();
            MobManager.Reset();
            TurnManager.Reset();
            Pathfinder.Reset();

            State.SlowUpdateIsFinished(MobManager);
            //RedAlive = 0;
            //BlueAlive = 0;

            //foreach (var mob in MobManager.MobInfos) {
            //    if (mob.Team == TeamColor.Red) {
            //        RedAlive++;
            //    }

            //    if (mob.Team == TeamColor.Blue) {
            //        BlueAlive++;
            //    }
            //}
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


        /// TODO - fix stuff below
        public int AddMobWithInfo(MobInfo mobInfo) {
            var id = MobManager.Mobs.Count;
            MobManager.Mobs.Add(id);

            MobManager.MobInfos.Add(mobInfo);

            Array.Resize(ref State.MobInstances, State.MobInstances.Length + 1);
            State.MobInstances[State.MobInstances.Length - 1] = new MobInstance(id);

            return id;
        }
    }
}