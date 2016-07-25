using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.UI {
    public class VerticalLayout : Entity
    {
        protected override void Layout() {
            float offset = 0;
            float maxWidth = 0;

            bool first = true;

            foreach (var element in Children) {
                var off = new Vector2(PaddingOffset.X,
                                      first ? PaddingOffset.Y : 0);

                element.Position = new Vector2(0, offset) + off;
                offset += element.CachedSize.Y;
                maxWidth = Math.Max(maxWidth, element.CachedSize.X);
                first = false;
            }

            CachedSize = new Vector2(maxWidth, Children.Sum(x => x.CachedSize.Y)) + PaddingSizeIncrease;
        }
    }
}