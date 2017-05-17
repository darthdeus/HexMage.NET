using System;
using HexMage.GUI.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Core {
    /// <summary>
    /// An animated variant of <code>SpriteRenderer</code>
    /// </summary>
    public class AnimatedSpriteRenderer : IRenderer {
        public readonly Texture2D Tex;
        private readonly Func<Rectangle> _spriteSelector;

        public AnimatedSpriteRenderer(Texture2D tex, Func<Rectangle> spriteSelector) {
            Tex = tex;
            _spriteSelector = spriteSelector;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            batch.Draw(Tex, entity.RenderPosition, _spriteSelector(), Color.White);
        }
    }
}