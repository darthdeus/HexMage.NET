using System;

namespace HexMage.Simulator
{
    public class UsableAbility
    {
        private readonly Ability _ability;
        private readonly Mob _mob;
        private readonly Mob _target;

        public UsableAbility(Mob mob, Mob target, Ability ability) {
            this._mob = mob;
            this._target = target;
            this._ability = ability;
        }

        public void Use() {
            _target.HP = Math.Max(0, _target.HP - _ability.Dmg);

            // TODO - handle negative AP
            _mob.AP -= _ability.Cost;
        }
    }
}