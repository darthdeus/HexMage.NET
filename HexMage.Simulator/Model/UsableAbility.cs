using System;

namespace HexMage.Simulator
{
    public class UsableAbility {
        public readonly Ability Ability;
        private readonly Mob _mob;
        private readonly Mob _target;

        public UsableAbility(Mob mob, Mob target, Ability ability) {
            this._mob = mob;
            this._target = target;
            this.Ability = ability;
        }

        public void Use() {
            _target.HP = Math.Max(0, _target.HP - Ability.Dmg);

            // TODO - handle negative AP
            _mob.AP -= Ability.Cost;
        }
    }
}