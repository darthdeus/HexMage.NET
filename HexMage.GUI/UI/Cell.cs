using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HexMage.GUI.UI {
    public interface ILayoutInfo {}

    public enum AlignmentOptions {
        Left,
        Right,
        Center
    }

    public enum ElementMouseState
    {
        Nothing,
        Hover,
        Pressed,
        Clicked
    }

    public class Element : Entity {
        public AlignmentOptions Alignment;

        public bool FillParent;
        public Vector4 Padding;
        public ElementMouseState MouseState;


        //public virtual void Render(Vector2 fromParent, AssetManager assetManager, SpriteBatch batch) {
        //    foreach (var element in ChildElements) {
        //        element.Render(fromParent + element.CachedRelativePosition, assetManager, batch);
        //    }
        //}

        public void Add(Element element) {
            element.Parent = this;
            Children.Add(element);
        }
    }


    public class TextButton : Element, IRenderer {
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

                if (inputManager.JustLeftClickReleased())
                {
                    MouseState = ElementMouseState.Clicked;
                    OnClick?.Invoke(inputManager.MousePosition);
                }
            }
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var tex = assetManager[AssetManager.GrayTexture];

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

    public class Label : Element, IRenderer {
        public string Text { get; set; }
        public SpriteFont Font { get; set; }

        public Label(string text, SpriteFont font) {
            Text = text;
            Font = font;
            Renderer = this;
        }

        protected override void Layout() {
            CachedSize = Font.MeasureString(Text);
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            batch.DrawString(Font, Text, RenderPosition, Color.Black);
        }
    }

    public class VerticalLayout : Element {
        protected override void Layout() {
            float offset = 0;
            float maxWidth = 0;

            foreach (var element in Children) {
                element.Position = new Vector2(0, offset);
                offset += element.CachedSize.Y;
                maxWidth = Math.Max(maxWidth, element.CachedSize.X);
            }

            CachedSize = new Vector2(maxWidth, Children.Sum(x => x.CachedSize.Y));
        }
    }

    public class HorizontalLayout : Element {
        protected override void Layout() {
            float offset = 0;
            float maxHeight = 0;

            foreach (var element in Children) {
                element.Position = new Vector2(offset, 0);
                offset += element.CachedSize.X;
                maxHeight = Math.Max(maxHeight, element.CachedSize.Y);
            }

            CachedSize = new Vector2(Children.Sum(x => x.CachedSize.X), maxHeight);
        }
    }

    public class Panel : Element {
        protected override void Layout() {
            var width = Children.Max(x => x.CachedSize.X + x.Position.X);
            var height = Children.Max(x => x.CachedSize.Y + x.Position.Y);

            CachedSize = new Vector2(width, height);
        }
    }

    public class SpriteElement : Element {
        public SpriteElement(Texture2D tex) {
            Renderer = new SpriteRenderer(tex);
        }

        protected override void Layout() {
            CachedSize = ((SpriteRenderer) Renderer).Tex.Bounds.Size.ToVector2();
        }
    }

    public class ColorRenderer : IRenderer {
        public Color Color { get; set; }

        public ColorRenderer(Color color) {
            Color = color;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            batch.Draw(assetManager[AssetManager.GrayTexture], entity.AABB, Color);
        }
    }
    
    //public interface ILayout {
    //    void Layout();
    //    void Invalidate();
    //    void InvalidateHierarchy();
    //    void Validate();
    //    void Pack();

    //    Vector2 MinSize { get; }
    //    Vector2 PreferredSize { get; }
    //    Vector2 MaxSize { get; }
    //}

    //public class Entity {

    //}

    //public class Group {
    //    private readonly List<Element> _elements = new List<Element>();
    //}

    //public class Element {
    //    internal Rectangle BoundingBox;
    //}
}