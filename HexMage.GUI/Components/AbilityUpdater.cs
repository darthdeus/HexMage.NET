using System;
using System.Diagnostics;
using System.Text;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    public class AbilityUpdater : Component {
        private readonly Func<GameInstance> _gameFunc;
        private readonly Func<Mob> _mobFunc;
        private readonly int _abilityIndex;
        private readonly Label _dmgLabel;
        private readonly Label _rangeLabel;
        private readonly Label _elementLabel;
        private readonly Label _cooldownLabel;
        private readonly Label _buffsLabel;
        private Ability _ability;

        public event Action<int> OnClick;

        public AbilityUpdater(Func<GameInstance> gameFunc, Func<Mob> mobFunc, int abilityIndex, Label dmgLabel, Label rangeLabel,
                              Label elementLabel, Label cooldownLabel, Label buffsLabel) {
            _gameFunc = gameFunc;
            _mobFunc = mobFunc;
            _abilityIndex = abilityIndex;
            _dmgLabel = dmgLabel;
            _rangeLabel = rangeLabel;
            _elementLabel = elementLabel;
            _cooldownLabel = cooldownLabel;
            _buffsLabel = buffsLabel;
        }

        public override void Update(GameTime time) {
            var mob = _mobFunc();
            if (mob != null) {
                Debug.Assert(mob.Abilities.Count == Mob.AbilityCount);
                Debug.Assert(_abilityIndex < mob.Abilities.Count);

                var mobManager = _gameFunc().MobManager;
                var abilityId = mob.Abilities[_abilityIndex];
                _ability = mobManager.AbilityForId(abilityId);

                var inputManager = InputManager.Instance;
                var aabb = new Rectangle(Entity.RenderPosition.ToPoint(),
                                         Entity.LayoutSize.ToPoint());

                if (inputManager.JustLeftClickReleased()) {
                    if (aabb.Contains(inputManager.MousePosition)) {
                        //OnClick?.Invoke(_abilityIndex);
                        EnqueueClickEvent(() => OnClick?.Invoke(_abilityIndex));
                    }
                }

                _dmgLabel.Text = $"DMG {_ability.Dmg}, Cost {_ability.Cost}";
                _rangeLabel.Text = $"Range {_ability.Range}";
                _elementLabel.Text = _ability.Element.ToString();

                var cooldown = mobManager.CooldownFor(abilityId);
                if (cooldown == 0) {
                    _cooldownLabel.Text = $"Cooldown: {_ability.Cooldown} turns";
                } else {
                    _cooldownLabel.Text = $"Again in {cooldown} turns";
                }

                var buffTextBuilder = new StringBuilder();

                foreach (var buff in _ability.Buffs) {
                    buffTextBuilder.AppendLine($"Buff {buff.HpChange}/{buff.ApChange}\nover {buff.Lifetime} turns");
                }

                foreach (var areaBuff in _ability.AreaBuffs) {
                    buffTextBuilder.AppendLine(
                        $"Area buff {areaBuff.Effect.HpChange}/{areaBuff.Effect.ApChange}\nover {areaBuff.Effect.Lifetime}, radius {areaBuff.Radius}");
                }

                _buffsLabel.Text = buffTextBuilder.ToString();
            }
        }
    }
}