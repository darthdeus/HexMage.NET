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

        public Vector2 CachedPosition { get; protected internal set; }
        public virtual Vector2 CachedSize { get; set; }

        public virtual void Layout() {}
        public virtual void Render(AssetManager assetManager, SpriteBatch batch) {}
    }

    public class TextButton : Element {
        public string Text { get; set; }
        public SpriteFont Font { get; set; }

        public TextButton(string text, SpriteFont font) {
            Text = text;
            Font = font;
        }

        public override void Layout() {
            CachedSize = Font.MeasureString(Text);
        }

        public override void Render(AssetManager assetManager, SpriteBatch batch) {
            batch.DrawString(Font, Text, Vector2.Zero, Color.Black);
            foreach (var element in Children) {
                // TODO - recursively render children using composed transformation matrix
            }
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

        public override void Render(AssetManager assetManager, SpriteBatch batch) {
            batch.Draw(assetManager[AssetManager.GrayTexture], new Rectangle(Point.Zero, CachedSize.ToPoint()),
                       Color.Gray);
            batch.DrawString(Font, Text, Vector2.Zero, Color.Black);
        }
    }

    public class VerticalLayout : Element {
        public override void Layout() {
            float offset = 0;
            float maxWidth = 0;

            foreach (var child in Children) {
                child.Layout();

                child.CachedPosition = new Vector2(0, offset);
                offset += child.CachedSize.Y;
                maxWidth = Math.Max(maxWidth, child.CachedSize.X);
            }

            CachedSize = new Vector2(maxWidth, Children.Sum(x => x.CachedSize.Y));
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