using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class GameInstance : IDeepCopyable<GameInstance>, IResettable {
        public Map Map { get; set; }
        public MobManager MobManager { get; set; }
        public Pathfinder Pathfinder { get; set; }
        public TurnManager TurnManager { get; set; }
        public int Size { get; set; }

        public int RedAlive = 0;
        public int BlueAlive = 0;

        public bool IsFinished => RedAlive == 0 || BlueAlive == 0;

        public void SlowUpdateIsFinished() {
            RedAlive = 0;
            foreach (var mobId in MobManager.Mobs) {
                var mobInfo = MobManager.MobInfoForId(mobId);
                var mobInstance = MobManager.MobInstanceForId(mobId);

                if (mobInstance.Hp > 0 && mobInfo.Team == TeamColor.Red) { RedAlive++; }
                if (mobInstance.Hp > 0 && mobInfo.Team == TeamColor.Blue) { BlueAlive++; }
            }
        }

        public GameInstance(Map map, MobManager mobManager) {
            Map = map;
            MobManager = mobManager;

            Size = map.Size;
            Pathfinder = new Pathfinder(Map, MobManager);
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

        public bool IsAbilityUsable(MobId mobId, AbilityId abilityId) {
            var ability = MobManager.AbilityForId(abilityId);
            var mob = MobManager.MobInstanceForId(mobId);
            return mob.Ap >= ability.Cost && MobManager.CooldownFor(abilityId) == 0;
        }

        public bool IsAbilityUsable(MobId mobId, MobId targetId, AbilityId abilityId) {
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

            // TODO - neni tohle extra lookupovani AbilityForId zbytecny?
            return ability.Range >= distance && IsAbilityUsable(mobId, abilityId);
        }


        public DefenseDesire FastUse(AbilityId abilityId, MobId mobId, MobId targetId) {
            new List<int>();
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
                    throw new NotImplementedException();
#warning TODO - tohle je spatne, AP se neaktualizuje
                    //target.Ap -= target.DefenseCost;
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


        private void TargetHit(AbilityId abilityId, MobId mobId, MobId targetId) {
            var ability = MobManager.AbilityForId(abilityId);

            var targetInstance = MobManager.MobInstanceForId(targetId);
            var targetInfo = MobManager.MobInfoForId(targetId);

            MobManager.ChangeMobHp(targetId, -ability.Dmg);
            MobHpChanged(targetInstance, targetInfo.Team);

            target.Buffs.Add(ability.ElementalEffect);
            foreach (var abilityBuff in ability.Buffs) {
                // TODO - handle lifetimes
                target.Buffs.Add(abilityBuff);
            }

            foreach (var areaBuff in ability.AreaBuffs) {
                var copy = areaBuff;
                copy.Coord = target.Coord;
                Map.AreaBuffs.Add(copy);
            }

            // TODO - handle negative AP
            MobManager.ChangeMobAp(mobId, -ability.Cost);
        }

        public void MobHpChanged(MobInstance mob, TeamColor team) {
            if (mob.Hp == 0) {                
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
            var game = new GameInstance(Size, mapCopy, mobManagerCopy, new Pathfinder(mapCopy, mobManagerCopy));

            return game;
        }

        public void Reset() {
            Map.Reset();
            MobManager.Reset();
            TurnManager.Reset();
            Pathfinder.Reset();

            RedAlive = MobManager.Mobs.Count(m => MobManager.MobInfoForId(m).Team == TeamColor.Red);
            BlueAlive = MobManager.Mobs.Count(m => MobManager.MobInfoForId(m).Team == TeamColor.Blue);
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

        public void FastUseWithDefenseDesire(MobId mob, MobId target, AbilityId ability,
                                             DefenseDesire defenseDesire) {
            throw new NotImplementedException();
        }
    }
}