using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.UI {
    public class Label : Entity, IRenderer {
        public string Text { get; set; } = "";
        public SpriteFont Font { get; set; }
        public Color TextColor { get; set; } = Color.Black;

        public Label(SpriteFont font) {
            Font = font;
            Renderer = this;
        }

        public Label(string text, SpriteFont font) {
            Text = text;
            Font = font;
        }

        public Label(Func<string> textFunc, SpriteFont font) {
            Font = font;            
            Renderer = this;
            AddComponent(new TextSetter(this, textFunc));
        }

        private class TextSetter : Component {
            private readonly Label _label;
            private readonly Func<string> _textFunc;

            public TextSetter(Label label, Func<string> textFunc) {
                _label = label;
                _textFunc = textFunc;
            }

            public override void Update(GameTime time) {
                _label.Text = _textFunc();
            }
        }

        protected override void Layout() {
            CachedSize = Font.MeasureString(Text);
        }



        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            batch.DrawString(Font, Text, RenderPosition, TextColor);
        }
    }
}