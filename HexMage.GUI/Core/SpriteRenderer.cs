using System;
using HexMage.GUI.Renderers;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Core {
    /// <summary>
    /// A simple sprite renderer which renders a constant sprite.
    /// </summary>
    public class SpriteRenderer : IRenderer {
        private readonly Func<Texture2D> _texFunc;
        public Texture2D Tex;

        public SpriteRenderer(Func<Texture2D> texFunc) {
            _texFunc = texFunc;
        }

        public SpriteRenderer(Texture2D tex) {
            Tex = tex;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            batch.Draw(_texFunc?.Invoke() ?? Tex, entity.RenderPosition);
        }
    }
}