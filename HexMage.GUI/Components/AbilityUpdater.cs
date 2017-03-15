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
        private readonly Func<int?> _mobFunc;
        private readonly int _abilityIndex;
        private readonly Label _dmgLabel;
        private readonly Label _rangeLabel;
        private readonly Label _elementLabel;
        private readonly Label _cooldownLabel;
        private readonly Label _buffsLabel;
        private Ability _ability;

        public event Action<int> OnClick;

        public AbilityUpdater(Func<GameInstance> gameFunc, Func<int?> mobFunc, int abilityIndex, Label dmgLabel,
                              Label rangeLabel,
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
            var mobId = _mobFunc();
            if (mobId != null) {
                var gameInstance = _gameFunc();
                var mobManager = gameInstance.MobManager;

                var mobInstance = gameInstance.State.MobInstances[mobId.Value];
                var mobInfo = mobManager.MobInfos[mobId.Value];

                // Update and rendering ale skipped if the ability isn't present
                if (_abilityIndex < mobInfo.Abilities.Count) {
                    Entity.Hidden = false;
                } else {
                    Entity.Hidden = true;
                    return;
                }

                var abilityId = mobInfo.Abilities[_abilityIndex];
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

                var cooldown = gameInstance.State.Cooldowns[abilityId];
                if (cooldown == 0) {
                    _cooldownLabel.Text = $"Cooldown: {_ability.Cooldown} turns";
                } else {
                    _cooldownLabel.Text = $"Again in {cooldown} turns";
                }

                var buffTextBuilder = new StringBuilder();

                if (!_ability.Buff.IsZero) {
                    var buff = _ability.Buff;
                    buffTextBuilder.AppendLine($"Buff {buff.HpChange}/{buff.ApChange}\nover {buff.Lifetime} turns");
                }

                if (!_ability.AreaBuff.IsZero) {
                    var areaBuff = _ability.AreaBuff;
                    buffTextBuilder.AppendLine($"Area buff {areaBuff.Effect.HpChange}/{areaBuff.Effect.ApChange}\nover {areaBuff.Effect.Lifetime}, radius {areaBuff.Radius}");
                }

                _buffsLabel.Text = buffTextBuilder.ToString();
            }
        }
    }
}