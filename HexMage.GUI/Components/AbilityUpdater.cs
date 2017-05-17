using System;
using System.Diagnostics;
using System.Text;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    /// <summary>
    /// Updates the ability view on the side of the arena scene.
    /// </summary>
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
                var game = _gameFunc();
                var mobManager = game.MobManager;

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

                var buff = _abilityInfo.Buff.IsZero ? Buff.ZeroBuff() : _abilityInfo.Buff;
                _buffLabel.Text =
                    $"{buff.HpChange}/{buff.ApChange} " +
                    $"({buff.Lifetime} turns)";

                var areaBuff = _abilityInfo.AreaBuff.IsZero ? AreaBuff.ZeroBuff() : _abilityInfo.AreaBuff;
                _areaBuffLabel.Text =
                    $"{areaBuff.Effect.HpChange}/{areaBuff.Effect.ApChange} " +
                    $"({areaBuff.Effect.Lifetime} turns, {areaBuff.Radius}r)";

                _cooldownLabel.Text = _abilityInfo.Cooldown.ToString();                
            }
        }
    }
}