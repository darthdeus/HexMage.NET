using System;
using System.Diagnostics;
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

        private void TargetHit() {
            _target.HP = Math.Max(0, _target.HP - Ability.Dmg);

            // TODO - handle negative AP
            _mob.AP -= Ability.Cost;
        }
    }
}