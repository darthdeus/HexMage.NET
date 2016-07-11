using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HexMage.GUI {
    public class ImGui {
        class TextRectangle {
            public string Text { get; set; }
            public Rectangle AABB { get; set; }

            public TextRectangle(string text, SpriteFont font, Point position) {
                Text = text;
                AABB = new Rectangle(position, font.MeasureString(text).ToPoint());
            }

            public bool Contains(Point point) {
                return AABB.Contains(point);
            }
        }

        // TODO - don't reallocate everything on a new frame
        private readonly List<TextRectangle> _labels = new List<TextRectangle>();
        private readonly List<TextRectangle> _buttons = new List<TextRectangle>();

        private readonly InputManager _inputManager;
        private readonly SpriteFont _font;

        public ImGui(InputManager inputManager, SpriteFont font) {
            _inputManager = inputManager;
            _font = font;
        }

        public void Label(string text, Point pos) {
            _labels.Add(new TextRectangle(text, _font, pos));
        }

        public bool Button(string text, Point pos) {
            var button = new TextRectangle(text, _font, pos);
            _buttons.Add(button);

            return button.Contains(_inputManager.MousePosition) && _inputManager.JustLeftClickReleased();
        }

        public void Draw(Texture2D bgTex, SpriteBatch spriteBatch) {
            spriteBatch.Begin();

            foreach (var label in _labels) {
                spriteBatch.DrawString(
                    _font,
                    label.Text,
                    label.AABB.Location.ToVector2(),
                    Color.Black);
            }

            foreach (var button in _buttons) {
                Point bgSize = button.AABB.Size;
                bgSize += new Point(4, 4);
                Rectangle bgRect = new Rectangle(button.AABB.Location, bgSize);
                Rectangle shadowRect = bgRect;
                shadowRect.Offset(2, 2);

                Point textPos = button.AABB.Location;
                textPos += new Point(2, 2);

                Color bgColor = Color.White;

                if (bgRect.Contains(_inputManager.MousePosition)) {
                    bgColor = new Color(new Vector3(0.9f, 0.9f, 0.9f));

                    if (Mouse.GetState().LeftButton == ButtonState.Pressed) {
                        bgRect.Offset(1, 1);
                        textPos += new Point(1, 1);
                    }
                }

                spriteBatch.Draw(bgTex, shadowRect, Color.Gray);
                spriteBatch.Draw(bgTex, bgRect, bgColor);

                spriteBatch.DrawString(_font, button.Text, textPos.ToVector2(), Color.Black);
            }


            spriteBatch.End();
        }
    }
}