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
        private AbilityInfo _abilityInfo;

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

                var mobInfo = mobManager.MobInfos[mobId.Value];

                // Update and rendering are skipped if the ability isn't present
                if (_abilityIndex < mobInfo.Abilities.Count) {
                    Entity.Hidden = false;
                } else {
                    Entity.Hidden = true;
                    return;
                }

                var abilityId = mobInfo.Abilities[_abilityIndex];
                _abilityInfo = mobManager.AbilityForId(abilityId);

                var inputManager = InputManager.Instance;
                var aabb = new Rectangle(Entity.RenderPosition.ToPoint(),
                                         Entity.LayoutSize.ToPoint());

                if (inputManager.JustLeftClickReleased()) {
                    if (aabb.Contains(inputManager.MousePosition)) {
                        EnqueueClickEvent(() => OnClick?.Invoke(_abilityIndex));
                    }
                }

                _dmgLabel.Text = $"DMG {_abilityInfo.Dmg}, Cost {_abilityInfo.Cost}";
                _rangeLabel.Text = $"Range {_abilityInfo.Range}";
                _elementLabel.Text = _abilityInfo.Element.ToString();

                var buffTextBuilder = new StringBuilder();

                if (!_abilityInfo.Buff.IsZero) {
                    var buff = _abilityInfo.Buff;
                    buffTextBuilder.AppendLine($"Buff {buff.HpChange}/{buff.ApChange}\nover {buff.Lifetime} turns");
                }

                if (!_abilityInfo.AreaBuff.IsZero) {
                    var areaBuff = _abilityInfo.AreaBuff;
                    buffTextBuilder.AppendLine($"Area buff {areaBuff.Effect.HpChange}/{areaBuff.Effect.ApChange}\nover {areaBuff.Effect.Lifetime}, radius {areaBuff.Radius}");
                }

                _buffsLabel.Text = buffTextBuilder.ToString();
            }
        }
    }
}