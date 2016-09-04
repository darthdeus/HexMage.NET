using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace HexMage.Simulator {
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

        public async Task<DefenseDesire> Use() {
            Debug.Assert(Ability.CurrentCooldown == 0, "Trying to use an ability with non-zero cooldown.");

            Ability.CurrentCooldown = Ability.Cooldown;
            if (_target.AP >= _target.DefenseCost) {
                var res = await _target.Team.Controller.RequestDesireToDefend(_target, Ability);

                if (res == DefenseDesire.Block) {
                    _target.AP -= _target.DefenseCost;
                    return DefenseDesire.Block;
                } else {
                    TargetHit();

                    return DefenseDesire.Pass;
                }
            } else {
                TargetHit();
                return DefenseDesire.Pass;
            }
        }

        public AbilityElement BonusElement(AbilityElement element) {
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

        public AbilityElement OppositeElement(AbilityElement element) {
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

        private void TargetHit() {
            var elements = _target.Buffs.Select(b => b.Element).Distinct();

            AbilityElement opposite = OppositeElement(Ability.Element);

            foreach (var buff in _target.Buffs.Where(b => b.Element == opposite)) {
                buff.Lifetime = 0;
            }

            bool bonusDmg = elements.Contains(BonusElement(Ability.Element));
            int modifier = bonusDmg ? 2 : 1;

            _target.HP = Math.Max(0, _target.HP - Ability.Dmg * modifier);

            _target.Buffs.Add(Ability.ElementalEffect);
            foreach (var abilityBuff in Ability.Buffs) {
                // TODO - handle lifetimes
                _target.Buffs.Add(abilityBuff.Clone());
            }

            // TODO - handle negative AP
            _mob.AP -= Ability.Cost;
        }
    }
}