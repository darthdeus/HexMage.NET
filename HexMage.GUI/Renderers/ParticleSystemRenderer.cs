using System;
using System.Linq;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Renderers {
    public class ParticleSystemRenderer : IRenderer {
        private readonly ParticleSystem _particleSystem;

        public ParticleSystemRenderer(ParticleSystem particleSystem) {
            this._particleSystem = particleSystem;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            foreach (var particle in _particleSystem.Particles.OrderBy(p => p.Age)) {
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null,
                    null, entity.RenderTransform);

                var tintColor = _particleSystem.ColorFunc?.Invoke() ?? Color.White;
                var ageColor = tintColor*(1 - particle.Age)*(1 - particle.Age)*(1 - particle.Age);
                var rotation = (float) Math.PI*particle.Age;

                batch.Draw(_particleSystem.ParticleSprite,
                    _particleSystem.RenderPosition + particle.Position,
                    null, ageColor, rotation, Vector2.Zero, 1 - particle.Age*particle.Age, SpriteEffects.None, 0);

                batch.End();
            }
        }
    }
}