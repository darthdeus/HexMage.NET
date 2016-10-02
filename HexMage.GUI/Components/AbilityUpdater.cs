using System;
using System.Diagnostics;
using System.Text;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    public class AbilityUpdater : Component {
        private readonly Func<Mob> _mobFunc;
        private readonly int _abilityIndex;
        private readonly Label _dmgLabel;
        private readonly Label _rangeLabel;
        private readonly Label _elementLabel;
        private readonly Label _cooldownLabel;
        private readonly Label _buffsLabel;
        private AbilityInfo _abilityInfo;

        public event Action<int> OnClick;

        public AbilityUpdater(Func<Mob> mobFunc, int abilityIndex, Label dmgLabel, Label rangeLabel,
                              Label elementLabel, Label cooldownLabel, Label buffsLabel) {
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

                _abilityInfo = mob.Abilities[_abilityIndex];

                var inputManager = InputManager.Instance;
                var aabb = new Rectangle(Entity.RenderPosition.ToPoint(),
                                         Entity.LayoutSize.ToPoint());

                if (inputManager.JustLeftClickReleased()) {
                    if (aabb.Contains(inputManager.MousePosition)) {
                        //OnClick?.Invoke(_abilityIndex);
                        EnqueueClickEvent(() => OnClick?.Invoke(_abilityIndex));
                    }
                }

                _dmgLabel.Text = $"DMG {_abilityInfo.Dmg}, Cost {_abilityInfo.Cost}";
                _rangeLabel.Text = $"Range {_abilityInfo.Range}";
                _elementLabel.Text = _abilityInfo.Element.ToString();

                if (_abilityInfo.CurrentCooldown == 0) {
                    _cooldownLabel.Text = $"Cooldown: {_abilityInfo.Cooldown} turns";
                } else {
                    _cooldownLabel.Text = $"Again in {_abilityInfo.CurrentCooldown} turns";
                }

                var buffTextBuilder = new StringBuilder();

                foreach (var buff in _abilityInfo.Buffs) {
                    buffTextBuilder.AppendLine($"Buff {buff.HpChange}/{buff.ApChange}\nover {buff.Lifetime} turns");
                }

                foreach (var areaBuff in _abilityInfo.AreaBuffs) {
                    buffTextBuilder.AppendLine(
                        $"Area buff {areaBuff.Effect.HpChange}/{areaBuff.Effect.ApChange}\nover {areaBuff.Effect.Lifetime}, radius {areaBuff.Radius}");
                }

                _buffsLabel.Text = buffTextBuilder.ToString();
            }
        }
    }
}