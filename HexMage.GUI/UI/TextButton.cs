using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HexMage.GUI.UI {
    public class TextButton : Entity, IRenderer {
        public string Text { get; set; }
        public SpriteFont Font { get; set; }

        public event Action<Point> OnClick;

        public TextButton(string text, SpriteFont font) {
            Text = text;
            Font = font;
            Renderer = this;
        }

        protected override void Layout() {
            CachedSize = Font.MeasureString(Text) + new Vector2(4);
        }

        protected override void Update(GameTime time) {
            Debug.Assert(CachedSize != Vector2.Zero);

            MouseState = ElementMouseState.Nothing;

            var inputManager = InputManager.Instance;

            if (AABB.Contains(inputManager.MousePosition)) {
                MouseState = ElementMouseState.Hover;

                if (Mouse.GetState().LeftButton == ButtonState.Pressed) {
                    MouseState = ElementMouseState.Pressed;
                }

                if (inputManager.JustLeftClickReleased()) {
                    MouseState = ElementMouseState.Clicked;
                    OnClick?.Invoke(inputManager.MousePosition);
                }
            }
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var tex = assetManager[AssetManager.GraySprite];

            var rectBg = new Rectangle(RenderPosition.ToPoint(), CachedSize.ToPoint());
            if (MouseState == ElementMouseState.Pressed) {
                rectBg.Offset(1, 1);
            }

            var rectShadow = rectBg;
            rectShadow.Offset(2, 2);

            var textOffset = new Vector2(2);
            Color buttonColor = Color.White;

            if (MouseState == ElementMouseState.Pressed) {
                textOffset += new Vector2(1);
                buttonColor = Color.LightGray;
            } else if (MouseState == ElementMouseState.Hover) {
                buttonColor = Color.LightGray;
            }

            batch.Draw(tex, rectShadow, Color.Gray);
            batch.Draw(tex, rectBg, buttonColor);
            batch.DrawString(Font, Text, RenderPosition + textOffset, Color.Black);
        }
    }
}