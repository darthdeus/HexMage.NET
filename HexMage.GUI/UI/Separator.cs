using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.UI {
    /// <summary>
    /// A separator of arbitrary size, intended for use in Vertical and Horizontal
    /// layouts.
    /// </summary>
    public class Separator : Entity, IRenderer {
        private readonly int _size;

        public Separator(int size) {
            _size = size;
            Renderer = this;
        }

        protected override void Layout() {
            LayoutSize = new Vector2(_size, _size);
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            // Separators are not rendered by default
        }
    }
}