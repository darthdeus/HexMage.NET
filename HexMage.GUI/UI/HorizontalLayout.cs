using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.UI {
    public class HorizontalLayout : Entity
    {
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
}