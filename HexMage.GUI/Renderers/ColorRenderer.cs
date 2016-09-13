using HexMage.GUI.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Renderers {
    public class ColorRenderer : IRenderer {
        public Color Color { get; set; }

        public ColorRenderer(Color color) {
            Color = color;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            batch.Draw(assetManager[AssetManager.SolidGrayColor], entity.AABB, Color);
        }
    }
}