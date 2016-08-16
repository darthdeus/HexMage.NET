using System;
using System.Diagnostics;
using HexMage.Simulator;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    public class SpellRenderer : IRenderer {
        private readonly TurnManager _turnManager;
        private readonly int _abilityIndex;

        public SpellRenderer(TurnManager turnManager, int abilityIndex) {
            _turnManager = turnManager;
            _abilityIndex = abilityIndex;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            //var effect = assetManager.LoadEffect(AssetManager.ShaderAbility);

            //batch.Begin(effect: effect);
            batch.Begin();

            var mob = _turnManager.CurrentMob;
            if (mob != null) {
                var ability = mob.Abilities[_abilityIndex];

                var isActive = _turnManager.SelectedAbilityIndex == _abilityIndex;

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
                    return active ? AssetManager.SpellEarthActiveBG : AssetManager.SpellEarthBG;
                case AbilityElement.Fire:
                    return active ? AssetManager.SpellFireActiveBG : AssetManager.SpellFireBG;
                case AbilityElement.Air:
                    return active ? AssetManager.SpellAirActiveBG : AssetManager.SpellAirBG;
                case AbilityElement.Water:
                    return active ? AssetManager.SpellWaterActiveBG : AssetManager.SpellWaterBG;
                default:
                    throw new ArgumentException("Invalid ability element", nameof(ability));
            }
        }
    }
}