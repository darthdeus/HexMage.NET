using System.Linq;
using HexMage.GUI.Core;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.UI {
    public class Panel : Entity {
        protected override void Layout() {
            var width = Children.Max(x => x.LayoutSize.X + x.Position.X);
            var height = Children.Max(x => x.LayoutSize.Y + x.Position.Y);

            LayoutSize = new Vector2(width, height);
        }
    }
}