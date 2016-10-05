using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            foreach (var mob in MobManager.Mobs) {
                if (mob.Hp > 0 && mob.Team == TeamColor.Red) { RedAlive++; }
                if (mob.Hp > 0 && mob.Team == TeamColor.Blue) { BlueAlive++; }
            }
        }

        [Obsolete]
        public bool SlowIsFinished() {
            return MobManager.Mobs.Where(m => m.Hp > 0)
                             .Select(m => m.Team)
                             .Distinct()
                             .Count() <= 1;
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

        public bool IsAbilityUsable(Mob mob, AbilityId abilityId) {
            var ability = MobManager.AbilityForId(abilityId);
            return mob.Ap >= ability.Cost && MobManager.CooldownFor(abilityId) == 0;
        }

        public bool IsAbilityUsable(Mob mob, Mob target, AbilityId abilityId) {
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
            return ability.Range >= distance && IsAbilityUsable(mob, abilityId);
        }

        public DefenseDesire FastUse(AbilityId abilityId, Mob mob, Mob target) {
            Debug.Assert(MobManager.CooldownFor(abilityId) == 0, "Trying to use an ability with non-zero cooldown.");
            Debug.Assert(target.Hp > 0, "Target is dead.");

            DefenseDesire result;

            var ability = MobManager.AbilityForId(abilityId);

            MobManager.SetCooldownFor(abilityId, ability.Cooldown);
            if (target.Ap >= target.DefenseCost) {
                var controller = MobManager.Teams[target.Team];
                var res = controller.FastRequestDesireToDefend(target, abilityId);

                if (res == DefenseDesire.Block) {
                    target.Ap -= target.DefenseCost;
                    result = DefenseDesire.Block;
                } else {
                    TargetHit(abilityId, mob, target);

                    result = DefenseDesire.Pass;
                }
            } else {
                TargetHit(abilityId, mob, target);
                result = DefenseDesire.Pass;
            }

            return result;
        }


        private void TargetHit(AbilityId abilityId, Mob mob, Mob target) {
            var ability = MobManager.AbilityForId(abilityId);
            var abilityElement = ability.Element;
            AbilityElement opposite = OppositeElement(abilityElement);

            target.Buffs.RemoveAll(b => b.Element == opposite);

            bool bonusDmg = false;

            var bonusElement = BonusElement(abilityElement);
            foreach (var buff in target.Buffs) {
                if (buff.Element == bonusElement) {
                    bonusDmg = true;
                }
            }

            int modifier = bonusDmg ? 2 : 1;

            target.Hp = Math.Max(0, target.Hp - ability.Dmg*modifier);
            MobHpChanged(target);

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
            mob.Ap -= ability.Cost;
        }

        public void MobHpChanged(Mob mob) {
            if (mob.Hp == 0) {
                switch (mob.Team) {
                    case TeamColor.Red:
                        RedAlive--;
                        break;
                    case TeamColor.Blue:
                        BlueAlive--;
                        break;
                }
            }
        }

        //public IList<Mob> PossibleTargets(Mob mob) {
        //    var result = new List<Mob>();

        //    var ability = mob.UsableMaxRange();

        //    foreach (var target in MobManager.Mobs) {
        //        if (target.Hp > 0 && Pathfinder.Distance(target.Coord) <= ability.Range && target.Team != mob.Team) {
        //            result.Add(target);
        //        }
        //    }

        //    return result;
        //}

        public IList<Mob> Enemies(Mob mob) {
            var result = new List<Mob>();

            foreach (var target in MobManager.Mobs) {
                if (target.Hp > 0 && target.Team != mob.Team) {
                    result.Add(target);
                }
            }

            return result;
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

            RedAlive = MobManager.Mobs.Count(m => m.Team == TeamColor.Red);
            BlueAlive = MobManager.Mobs.Count(m => m.Team == TeamColor.Blue);
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

        public void FastUseWithDefenseDesire(Mob mob, Mob target, AbilityId ability,
                                             DefenseDesire defenseDesire) {
            throw new NotImplementedException();
        }
    }
}