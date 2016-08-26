﻿using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.UI {
    public class HorizontalLayout : Entity {
        public int Spacing = 0;

        protected override void Layout() {
            float offset = 0;
            float maxHeight = 0;

            foreach (var element in Children) {
                element.Position = new Vector2(offset, 0);
                offset += element.LayoutSize.X + Spacing;
                maxHeight = Math.Max(maxHeight, element.LayoutSize.Y);
            }

            LayoutSize = new Vector2(Children.Sum(x => x.LayoutSize.X), maxHeight);
        }
    }
}