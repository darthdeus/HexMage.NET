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

        public int RedAlive = 0;
        public int BlueAlive = 0;

        public bool IsFinished => RedAlive <= 0 || BlueAlive <= 0;

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
            MobManager.MobPositions = new HexMap<int?>(Size);
            MobManager.Reset();
            Map.PrecomputeCubeLinedraw();
            Pathfinder.PathfindDistanceAll();
            TurnManager.PresortTurnOrder();
            TurnManager.StartNextTurn(Pathfinder);
        }

        public void SlowUpdateIsFinished() {
            RedAlive = 0;
            BlueAlive = 0;
            foreach (var mobId in MobManager.Mobs) {
                var mobInfo = MobManager.MobInfoForId(mobId);
                var mobInstance = MobManager.MobInstanceForId(mobId);

                if (mobInstance.Hp > 0 && mobInfo.Team == TeamColor.Red) {
                    RedAlive++;
                }
                if (mobInstance.Hp > 0 && mobInfo.Team == TeamColor.Blue) {
                    BlueAlive++;
                }
            }
        }

        public bool IsAbilityUsable(int mobId, int abilityId) {
            var ability = MobManager.AbilityForId(abilityId);
            var mob = MobManager.MobInstanceForId(mobId);
            return mob.Ap >= ability.Cost && MobManager.CooldownFor(abilityId) == 0;
        }

        public bool IsAbilityUsable(int mobId, int targetId, int abilityId) {
            var mob = MobManager.MobInstanceForId(mobId);
            var target = MobManager.MobInstanceForId(targetId);

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
            var target = MobManager.MobInstanceForId(targetId);
            var targetInfo = MobManager.MobInfoForId(targetId);
            Debug.Assert(MobManager.CooldownFor(abilityId) == 0, "Trying to use an ability with non-zero cooldown.");
            Debug.Assert(target.Hp > 0, "Target is dead.");

            DefenseDesire result;

            var ability = MobManager.AbilityForId(abilityId);

            MobManager.SetCooldownFor(abilityId, ability.Cooldown);
            if (target.Ap >= targetInfo.DefenseCost) {
                var controller = MobManager.Teams[targetInfo.Team];
                var res = controller.FastRequestDesireToDefend(targetId, abilityId);

                if (res == DefenseDesire.Block) {
                    MobManager.ChangeMobAp(mobId, -MobManager.AbilityForId(abilityId).Cost);
                    MobManager.ChangeMobAp(targetId, -MobManager.MobInfoForId(targetId).DefenseCost);
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
            var target = MobManager.MobInstanceForId(targetId);
            var targetInfo = MobManager.MobInfoForId(targetId);
            Debug.Assert(MobManager.CooldownFor(abilityId) == 0, "Trying to use an ability with non-zero cooldown.");
            Debug.Assert(target.Hp > 0, "Target is dead.");

            DefenseDesire result;

            var ability = MobManager.AbilityForId(abilityId);

            MobManager.SetCooldownFor(abilityId, ability.Cooldown);
            if (target.Ap >= targetInfo.DefenseCost) {
                var controller = MobManager.Teams[targetInfo.Team];
                var res = await controller.SlowRequestDesireToDefend(targetId, abilityId);

                if (res == DefenseDesire.Block) {
                    MobManager.ChangeMobAp(mobId, -MobManager.AbilityForId(abilityId).Cost);
                    MobManager.ChangeMobAp(targetId, -MobManager.MobInfoForId(targetId).DefenseCost);
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

        private void TargetHit(int abilityId, int mobId, int targetId) {
            var ability = MobManager.AbilityForId(abilityId);

            MobManager.ChangeMobHp(this, targetId, -ability.Dmg);

            var targetInstance = MobManager.MobInstanceForId(targetId);
            var targetInfo = MobManager.MobInfoForId(targetId);

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
            MobManager.ChangeMobAp(mobId, -ability.Cost);
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


        public GameInstance DeepCopy() {
#warning TODO - tohle prepsat poradne
            var mapCopy = Map.DeepCopy();
            var mobManagerCopy = MobManager.DeepCopy();
            var game = new GameInstance(mapCopy, mobManagerCopy);

            return game;
        }

#warning TODO - funguje tohle jeste?
        public void Reset() {
            Map.Reset();
            MobManager.Reset();
            TurnManager.Reset();
            Pathfinder.Reset();

            RedAlive = 0;
            BlueAlive = 0;

            foreach (var mob in MobManager.MobInfos) {
                if (mob.Team == TeamColor.Red) {
                    RedAlive++;
                }

                if (mob.Team == TeamColor.Blue) {
                    BlueAlive++;
                }
            }
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

        public void FastUseWithDefenseDesire(int mobId, int targetId, int ability,
                                             DefenseDesire defenseDesire) {
            throw new NotImplementedException();
        }

        public static GameInstance FromJSON(string jsonStr) {
            var mapRepresentation = JsonConvert.DeserializeObject<MapRepresentation>(jsonStr);

            var map = new Map(5);

            var result = new GameInstance(map);
            return result;
        }
    }


    public class MapItem {
        public AxialCoord Coord { get; set; }
        public HexType HexType { get; set; }

        public MapItem() {}

        public MapItem(AxialCoord coord, HexType hexType) {
            Coord = coord;
            HexType = hexType;
        }
    }

    public class MapRepresentation {
        public int Size { get; set; }
        public MapItem[] Hexes { get; set; }

        public MapRepresentation() {}

        public MapRepresentation(Map map) {
            Hexes = new MapItem[map.AllCoords.Count];
            Size = map.Size;

            for (int i = 0; i < map.AllCoords.Count; i++) {
                var coord = map.AllCoords[i];
                Hexes[i] = new MapItem(coord, map[coord]);
            }
        }

        public void UpdateMap(Map map) {
            if (map.Size != Size) {
                throw new NotImplementedException("Map needs to be resized, not implemented yet");
            }
            foreach (var hex in Hexes) {
                map[hex.Coord] = hex.HexType;
            }
        }
    }
}