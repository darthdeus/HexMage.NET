using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    public interface IRenderer {
        void Render(Entity entity, SpriteBatch batch, AssetManager assetManager);
    }
}