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

        public GameInstance(Map map, MobManager mobManager) {
            Map = map;
            MobManager = mobManager;

            Size = map.Size;
            Pathfinder = new Pathfinder(Map, MobManager);
            TurnManager = new TurnManager(MobManager, Map);
        }

        public GameInstance(int size) : this(new Map(size)) {
        }

        public GameInstance(Map map) : this(map, new MobManager()) {
        }

        private GameInstance(int size, Map map, MobManager mobManager, Pathfinder pathfinder, TurnManager turnManager) {
            Size = size;
            MobManager = mobManager;
            Map = map;
            Pathfinder = pathfinder;
            TurnManager = turnManager;
        }

        public bool IsFinished() {
            bool redAlive = false;
            bool blueAlive = false;

            foreach (var mob in MobManager.Mobs) {
                if (mob.Hp > 0) {
                    switch (mob.Team) {
                        case TeamColor.Red:
                            redAlive = true;
                            break;
                        case TeamColor.Blue:
                            blueAlive = true;
                            break;
                    }
                }
            }

            return !redAlive || !blueAlive;
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

            target.Hp = Math.Max(0, target.Hp - desc.Dmg*modifier);

            target.Buffs.Add(desc.ElementalEffect);
            foreach (var abilityBuff in desc.Buffs) {
                // TODO - handle lifetimes
                target.Buffs.Add(abilityBuff);
            }

            foreach (var areaBuff in desc.AreaBuffs) {
                var copy = areaBuff;
                copy.Coord = target.Coord;
                Map.AreaBuffs.Add(copy);
            }

            // TODO - handle negative AP
            mob.Ap -= desc.Cost;
        }

        public IList<Mob> PossibleTargets(Mob mob) {
            var result = new List<Mob>();

            var ability = mob.UsableMaxRange();

            foreach (var target in MobManager.Mobs) {
                if (target.Hp > 0 && Pathfinder.Distance(target.Coord) <= ability.Range && target.Team != mob.Team) {
                    result.Add(target);
                }
            }

            return result;
        }

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
            return new GameInstance(Size, mapCopy, mobManagerCopy, new Pathfinder(mapCopy, mobManagerCopy),
                new TurnManager(mobManagerCopy, mapCopy));
        }

        public void Reset() {
            Map.Reset();
            MobManager.Reset();
            TurnManager.Reset();
            Pathfinder.Reset();
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

        public void FastUseWithDefenseDesire(Mob mob, Mob target, ref AbilityInstance ability,
            DefenseDesire defenseDesire) {
            throw new NotImplementedException();
        }
    }
}