using System;
using System.Diagnostics;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Renderers {
    /// <summary>
    /// Renders a single ability.
    /// </summary>
    public class SpellRenderer : IRenderer {
        private readonly GameInstance _game;
        private readonly GameBoardController _gameBoardController;
        private readonly Func<CachedMob> _mobFunc;
        private readonly int _abilityIndex;

        public SpellRenderer(GameInstance game,
                             GameBoardController gameBoardController,
                             Func<CachedMob> mobFunc,
                             int abilityIndex) {
            _game = game;
            _gameBoardController = gameBoardController;
            _mobFunc = mobFunc;
            _abilityIndex = abilityIndex;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var effect = assetManager.LoadEffect(AssetManager.ShaderAbility);

            batch.Begin(effect: effect, samplerState: Camera2D.SamplerState);

            var mob = _mobFunc();
            if (mob != null) {
                var abilityId = mob.MobInfo.Abilities[_abilityIndex];

                var isActive = _gameBoardController.SelectedAbilityIndex == _abilityIndex;

                if (GameInvariants.IsAbilityUsableNoTarget(_game, mob.MobId, abilityId)) {
                    isActive = true;
                }

                var ability = _game.MobManager.Abilities[abilityId];
                batch.Draw(assetManager[AssetManager.SpellBg], entity.RenderPosition);

                if (entity.AABB.Contains(InputManager.Instance.MousePosition)) {
                    batch.Draw(assetManager[AssetManager.SpellHighlight], entity.RenderPosition);
                }
            } else {
                Debug.WriteLine("ERROR - Rendering abilities, but no mob is currently active.");
                batch.Draw(assetManager[AssetManager.NoTexture], entity.RenderPosition);
            }

            batch.End();
        }
    }
}