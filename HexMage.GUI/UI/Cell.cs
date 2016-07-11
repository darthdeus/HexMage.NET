using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.UI {
    public interface ILayoutInfo {}

    public enum AlignmentOptions {
        Left,
        Right,
        Center
    }

    public class Element {
        protected readonly List<Element> Children = new List<Element>();
        //public ILayoutInfo LayoutInfo;
        public bool FillParent;
        public AlignmentOptions Alignment;
        public Vector4 Padding;

        public Vector2 CachedRelativePosition { get; protected internal set; }
        public Vector2 CachedSize { get; set; }

        public virtual void Layout() {
            foreach (var element in Children) {
                element.Layout();
            }
        }

        public virtual void Render(Vector2 fromParent, AssetManager assetManager, SpriteBatch batch) {
            foreach (var element in Children) {
                element.Render(fromParent + element.CachedRelativePosition, assetManager, batch);
            }
        }

        public void Add(Element element) {
            Children.Add(element);
        }
    }

    public class TextButton : Element {
        public string Text { get; set; }
        public SpriteFont Font { get; set; }

        public TextButton(string text, SpriteFont font) {
            Text = text;
            Font = font;
        }

        public override void Layout() {
            CachedSize = Font.MeasureString(Text) + new Vector2(4);
        }

        public override void Render(Vector2 fromParent, AssetManager assetManager, SpriteBatch batch) {
            var tex = assetManager[AssetManager.GrayTexture];

            Rectangle rectBg = new Rectangle(fromParent.ToPoint(), CachedSize.ToPoint());
            Rectangle rectShadow = rectBg;
            rectShadow.Offset(2, 2);
            
            batch.Draw(tex, rectShadow, Color.Gray);
            batch.Draw(tex, rectBg, Color.White);
            batch.DrawString(Font, Text, fromParent + new Vector2(2), Color.Black);
        }
    }

    public class Label : Element {
        public string Text { get; set; }
        public SpriteFont Font { get; set; }

        public Label(string text, SpriteFont font) {
            Text = text;
            Font = font;
        }

        public override void Layout() {
            CachedSize = Font.MeasureString(Text);
        }

        public override void Render(Vector2 fromParent, AssetManager assetManager, SpriteBatch batch) {
            batch.DrawString(Font, Text, fromParent, Color.Black);
        }
    }

    public class VerticalLayout : Element {
        public override void Layout() {
            float offset = 0;
            float maxWidth = 0;

            foreach (var element in Children) {
                element.Layout();

                element.CachedRelativePosition = new Vector2(0, offset);
                offset += element.CachedSize.Y;
                maxWidth = Math.Max(maxWidth, element.CachedSize.X);
            }

            CachedSize = new Vector2(maxWidth, Children.Sum(x => x.CachedSize.Y));
        }
    }

    public class HorizontalLayout : Element {
        public override void Layout() {
            float offset = 0;
            float maxHeight = 0;

            foreach (var element in Children) {
                element.Layout();

                element.CachedRelativePosition = new Vector2(offset, 0);
                offset += element.CachedSize.X;
                maxHeight = Math.Max(maxHeight, element.CachedSize.Y);
            }

            CachedSize = new Vector2(maxHeight, Children.Sum(x => x.CachedSize.X));
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