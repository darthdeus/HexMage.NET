using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.UI {
    public class SpriteElement : Entity
    {
        public SpriteElement(Texture2D tex) {
            Renderer = new SpriteRenderer(tex);
        }

        protected override void Layout() {
            CachedSize = ((SpriteRenderer) Renderer).Tex.Bounds.Size.ToVector2();
        }
    }
}