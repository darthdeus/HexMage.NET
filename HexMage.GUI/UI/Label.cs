using System;
using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using HexMage.Simulator;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.UI {
    /// <summary>
    /// A simple text label with a customizable font and text color.
    /// </summary>
    public class Label : Entity, IRenderer {
        public string Text { get; set; } = "";
        public SpriteFont Font { get; set; }
        public Color TextColor { get; set; } = Color.Black;

        public Label(SpriteFont font) {
            Font = font;
            Renderer = this;
        }

        public Label(SpriteFont font, Color textColor) {
            Font = font;
            Renderer = this;
            TextColor = textColor;
        }

        public Label(string text, SpriteFont font) {
            Text = text;
            Font = font;
            Renderer = this;
        }

        public Label(string text, SpriteFont font, Color textColor) {
            Text = text;
            Font = font;
            TextColor = textColor;
            Renderer = this;
        }

        public Label(Func<string> textFunc, SpriteFont font) {
            Font = font;
            Renderer = this;
            AddComponent(() => Text = textFunc());
        }

        public Label(Func<string> textFunc, SpriteFont font, Color textColor) {
            Font = font;
            TextColor = textColor;
            Renderer = this;
            AddComponent(() => Text = textFunc());
        }

        protected override void Layout() {
            LayoutSize = Font.MeasureString(Text);
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            if (DebugMode) {
                Utils.Log(LogSeverity.Debug, nameof(Label) + "RENDER",
                          $"{this} at {RenderPosition} with size {LayoutSize}");
            }
            batch.DrawString(Font, Text, RenderPosition, TextColor);
        }
    }
}