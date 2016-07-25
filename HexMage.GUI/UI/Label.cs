using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.UI {
    public class Label : Entity, IRenderer {
        public string Text { get; set; }
        public SpriteFont Font { get; set; }
        public Color TextColor { get; set; } = Color.Black;

        public Label(string text, SpriteFont font) {
            Text = text;
            Font = font;
            Renderer = this;
        }

        protected override void Layout() {
            CachedSize = Font.MeasureString(Text);
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            batch.DrawString(Font, Text, RenderPosition, TextColor);
        }
    }
}