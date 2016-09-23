using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HexMage.Simulator.Model {
    public class UsableAbility {
        public readonly int Index;
        public readonly Ability Ability;
        private readonly Mob _mob;
        private readonly Mob _target;

        public UsableAbility(Mob mob, Mob target, Ability ability, int index) {
            _mob = mob;
            _target = target;
            Ability = ability;
            Index = index;
        }

        public async Task<DefenseDesire> Use(Map map) {
            Debug.Assert(Ability.CurrentCooldown == 0, "Trying to use an ability with non-zero cooldown.");

            DefenseDesire result;

            Ability.CurrentCooldown = Ability.Cooldown;
            if (_target.Ap >= _target.DefenseCost) {
                var res = await _target.Team.Controller.RequestDesireToDefend(_target, Ability);

                if (res == DefenseDesire.Block) {
                    _target.Ap -= _target.DefenseCost;
                    result = DefenseDesire.Block;
                } else {
                    TargetHit(map);

                    result = DefenseDesire.Pass;
                }
            } else {
                TargetHit(map);
                result = DefenseDesire.Pass;
            }

            return result;
        }

        public void UseWithDefenseResult(Map map, DefenseDesire defenseDesire) {
            switch (defenseDesire) {
                case DefenseDesire.Block:
                    _target.Ap -= _target.DefenseCost;
                    break;
                case DefenseDesire.Pass:
                    TargetHit(map);
                    break;
                default:
                    throw new ArgumentException($"Invalid DefenseDesire value {defenseDesire}", nameof(defenseDesire));
            }
        }

        private void TargetHit(Map map) {
            var elements = _target.Buffs.Select(b => b.Element).Distinct();

            AbilityElement opposite = OppositeElement(Ability.Element);

            foreach (var buff in _target.Buffs.Where(b => b.Element == opposite)) {
                buff.Lifetime = 0;
            }

            bool bonusDmg = elements.Contains(BonusElement(Ability.Element));
            int modifier = bonusDmg ? 2 : 1;

            _target.Hp = Math.Max(0, _target.Hp - Ability.Dmg*modifier);

            _target.Buffs.Add(Ability.ElementalEffect);
            foreach (var abilityBuff in Ability.Buffs) {
                // TODO - handle lifetimes
                _target.Buffs.Add(abilityBuff.DeepCopy());
            }

            foreach (var areaBuff in Ability.AreaBuffs) {
                var affectedArea = map.AllCoords.Where(x => map.CubeDistance(x, _target.Coord) <= areaBuff.Radius);
                Utils.Log(LogSeverity.Debug, nameof(UsableAbility), $"Applying buffs at {map.Guid}");
                foreach (var coord in affectedArea) {
                    map.BuffsAt(coord).Add(areaBuff.Effect.DeepCopy());
                }
            }

            // TODO - handle negative AP
            _mob.Ap -= Ability.Cost;
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
    }
}