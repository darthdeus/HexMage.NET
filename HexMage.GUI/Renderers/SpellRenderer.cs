using System;
using System.Diagnostics;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Renderers {
    public class SpellRenderer : IRenderer {
        private readonly GameInstance _gameInstance;
        private readonly GameBoardController _gameBoardController;
        private readonly Func<Mob> _mobFunc;
        private readonly int _abilityIndex;

        public SpellRenderer(GameInstance gameInstance, GameBoardController gameBoardController, Func<Mob> mobFunc , int abilityIndex) {
            _gameInstance = gameInstance;
            _gameBoardController = gameBoardController;
            _mobFunc = mobFunc;
            _abilityIndex = abilityIndex;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var effect = assetManager.LoadEffect(AssetManager.ShaderAbility);

            var time = ((float) DateTime.Now.Millisecond)/1000*2 - 1;

            //effect.Parameters["Time"].SetValue(time);
            batch.Begin(effect: effect);

            var mob = _mobFunc();
            if (mob != null) {
                var ability = mob.Abilities[_abilityIndex];

                var isActive = _gameBoardController.SelectedAbilityIndex == _abilityIndex;

                if (_gameInstance.IsAbilityUsable(mob, ability)) {
                    isActive = true;
                }

                batch.Draw(assetManager[ElementBg(ability, isActive)], entity.RenderPosition);

                if (entity.AABB.Contains(InputManager.Instance.MousePosition)) {
                    batch.Draw(assetManager[AssetManager.SpellHighlight], entity.RenderPosition);
                }
            } else {
                Debug.WriteLine("ERROR - Rendering abilities, but no mob is currently active.");
                batch.Draw(assetManager[AssetManager.NoTexture], entity.RenderPosition);
            }

            batch.End();
        }

        private string ElementBg(Ability ability, bool active = false) {
            switch (ability.Element) {
                case AbilityElement.Earth:
                    return active ? AssetManager.SpellEarthActiveBg : AssetManager.SpellEarthBg;
                case AbilityElement.Fire:
                    return active ? AssetManager.SpellFireActiveBg : AssetManager.SpellFireBg;
                case AbilityElement.Air:
                    return active ? AssetManager.SpellAirActiveBg : AssetManager.SpellAirBg;
                case AbilityElement.Water:
                    return active ? AssetManager.SpellWaterActiveBg : AssetManager.SpellWaterBg;
                default:
                    throw new ArgumentException("Invalid ability element", nameof(ability));
            }
        }
    }
}