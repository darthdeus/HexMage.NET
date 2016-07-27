using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.UI {
    public class VerticalLayout : Entity {
        protected override void Layout() {
            float offset = 0;
            float maxWidth = 0;

            foreach (var element in Children) {
                var off = new Vector2(PaddingOffset.X,
                    PaddingOffset.Y);

                element.Position = new Vector2(0, offset) + off;
                offset += element.CachedSize.Y;
                maxWidth = Math.Max(maxWidth, element.CachedSize.X);
            }

            CachedSize = new Vector2(maxWidth, Children.Sum(x => x.CachedSize.Y))
                         + PaddingSizeIncrease;
        }
    }
}