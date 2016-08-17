using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    public class ParticleSystemRenderer : IRenderer {
        private readonly ParticleSystem _particleSystem;

        public ParticleSystemRenderer(ParticleSystem particleSystem) {
            this._particleSystem = particleSystem;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            foreach (var particle in _particleSystem.Particles.OrderBy(p => p.Age)) {
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null,
                    null, entity.RenderTransform);

                var ageColor = Color.White*(1 - particle.Age)*(1 - particle.Age)*(1 - particle.Age);
                var rotation = (float) Math.PI * particle.Age;
                batch.Draw(_particleSystem.ParticleSprite,
                    particle.Position,
                    null, ageColor, rotation, Vector2.Zero, 1 - particle.Age*particle.Age, SpriteEffects.None, 0);

                batch.End();
            }
        }
    }
}