using System.Linq;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.UI {
    public class Panel : Entity
    {
        protected override void Layout() {
            var width = Children.Max(x => x.CachedSize.X + x.Position.X);
            var height = Children.Max(x => x.CachedSize.Y + x.Position.Y);

            CachedSize = new Vector2(width, height);
        }
    }
}