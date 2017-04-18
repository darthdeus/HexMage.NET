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
        private readonly Func<CachedMob> _mobFunc;
        private readonly int _abilityIndex;
        private readonly Label _dmgLabel;
        private readonly Label _abLabel;
        private readonly Label _rangeLabel;
        private readonly Label _buffLabel;
        private readonly Label _cooldownLabel;
        private readonly Label _areaBuffLabel;
        private AbilityInfo _abilityInfo;

        public event Action<int> OnClick;

        public AbilityUpdater(Func<GameInstance> gameFunc,
                              Func<CachedMob> mobFunc,
                              int abilityIndex,
                              Label dmgLabel,
                              Label abLabel,
                              Label rangeLabel,
                              Label buffLabel,
                              Label cooldownLabel,
                              Label areaBuffLabel) {
            _gameFunc = gameFunc;
            _mobFunc = mobFunc;
            _abilityIndex = abilityIndex;
            _dmgLabel = dmgLabel;
            _abLabel = abLabel;
            _rangeLabel = rangeLabel;
            _buffLabel = buffLabel;
            _cooldownLabel = cooldownLabel;
            _areaBuffLabel = areaBuffLabel;
        }

        public override void Update(GameTime time) {
            var mob = _mobFunc();
            if (mob != null) {
                var gameInstance = _gameFunc();
                var mobManager = gameInstance.MobManager;

                // Update and rendering are skipped if the ability isn't present
                if (_abilityIndex < mob.MobInfo.Abilities.Count) {
                    Entity.Hidden = false;
                } else {
                    Entity.Hidden = true;
                    return;
                }

                var abilityId = mob.MobInfo.Abilities[_abilityIndex];
                _abilityInfo = mobManager.AbilityForId(abilityId);

                var inputManager = InputManager.Instance;
                var aabb = new Rectangle(Entity.RenderPosition.ToPoint(),
                                         Entity.LayoutSize.ToPoint());

                if (inputManager.JustLeftClickReleased()) {
                    if (aabb.Contains(inputManager.MousePosition)) {
                        EnqueueClickEvent(() => OnClick?.Invoke(_abilityIndex));
                    }
                }

                _dmgLabel.Text = _abilityInfo.Dmg.ToString();
                _abLabel.Text = _abilityInfo.Cost.ToString();
                _rangeLabel.Text = _abilityInfo.Range.ToString();
                _buffLabel.Text =
                    $"{_abilityInfo.Buff.HpChange}/{_abilityInfo.Buff.ApChange} " +
                    $"({_abilityInfo.Buff.Lifetime} turns)";

                _areaBuffLabel.Text =
                    $"{_abilityInfo.AreaBuff.Effect.HpChange}/{_abilityInfo.AreaBuff.Effect.ApChange} " +
                    $"({_abilityInfo.AreaBuff.Effect.Lifetime} turns, {_abilityInfo.AreaBuff.Radius}r)";

                //_dmgLabel.Text = $"DMG {_abilityInfo.Dmg}, Cost {_abilityInfo.Cost}";
                //_rangeLabel.Text = $"Range {_abilityInfo.Range}";
                //_elementLabel.Text = _abilityInfo.Element.ToString();

                var buffTextBuilder = new StringBuilder();

                //if (!_abilityInfo.Buff.IsZero) {
                //    var buff = _abilityInfo.Buff;
                //    buffTextBuilder.AppendLine($"Buff {buff.HpChange}/{buff.ApChange}\nover {buff.Lifetime} turns");
                //}

                //if (!_abilityInfo.AreaBuff.IsZero) {
                //    var areaBuff = _abilityInfo.AreaBuff;
                //    buffTextBuilder.AppendLine($"Area buff {areaBuff.Effect.HpChange}/{areaBuff.Effect.ApChange}\nover {areaBuff.Effect.Lifetime}, radius {areaBuff.Radius}");
                //}

            }
        }
    }
}