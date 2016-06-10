using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HexMage.GUI
{
    public class ImGui
    {
        // TODO - don't reallocate everything on a new frame
        List<Tuple<string, Rectangle>> Labels = new List<Tuple<string, Rectangle>>();
        List<Tuple<string, Rectangle>> Buttons = new List<Tuple<string, Rectangle>>();

        private readonly InputManager _inputManager;

        public ImGui(InputManager inputManager) {
            _inputManager = inputManager;
        }

        public void Label(string text, Rectangle rect) {
            Labels.Add(Tuple.Create(text, rect));
        }

        public bool Button(string text, Rectangle rect) {
            Buttons.Add(Tuple.Create(text, rect));

            return rect.Contains(_inputManager.MousePosition.ToPoint()) && _inputManager.JustLeftClicked();
        }

        public void Draw(SpriteFont font, Texture2D bgTex, SpriteBatch spriteBatch) {
            spriteBatch.Begin();

            foreach (var label in Labels) {
                spriteBatch.DrawString(
                    font,
                    label.Item1,
                    label.Item2.Location.ToVector2(),
                    Color.Black);
            }

            foreach (var button in Buttons) {
                var loc = button.Item2.Location;

                var bg = loc - new Point(2);
                var dim = button.Item2.Size + new Point(4);

                var rect = new Rectangle(bg, dim);
                var rectShadow = rect;
                rectShadow.Offset(3, 3);

                Color bgColor = Color.White;

                if (rect.Contains(_inputManager.MousePosition.ToPoint())) {
                    bgColor = new Color(new Vector3(0.9f, 0.9f, 0.9f));

                    if (Mouse.GetState().LeftButton == ButtonState.Pressed) {
                        rect.Offset(1, 1);
                        loc += new Point(1, 1);
                    }
                }

                spriteBatch.Draw(bgTex, rectShadow, Color.Gray);
                spriteBatch.Draw(bgTex, rect, bgColor);

                spriteBatch.DrawString(font, button.Item1, loc.ToVector2(), Color.Black);
            }

            spriteBatch.End();
        }
    }
}