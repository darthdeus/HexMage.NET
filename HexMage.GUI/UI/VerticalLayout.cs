using System;
using System.Linq;
using HexMage.GUI.Core;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.UI {
    /// <summary>
    /// A simple layout which lays out entities vertically next to each other.
    /// Can have optional spacing.
    /// </summary>
    public class VerticalLayout : Entity {
        public int Spacing = 0;

        protected override void Layout() {
            float offset = 0;
            float maxWidth = 0;

            foreach (var element in Children) {
                var off = new Vector2(PaddingOffset.X,
                    PaddingOffset.Y);

                element.Position = new Vector2(0, offset) + off;
                offset += element.LayoutSize.Y + Spacing;
                maxWidth = Math.Max(maxWidth, element.LayoutSize.X);
            }

            LayoutSize = new Vector2(maxWidth, Children.Sum(x => x.LayoutSize.Y))
                         + PaddingSizeIncrease;
        }
    }
}