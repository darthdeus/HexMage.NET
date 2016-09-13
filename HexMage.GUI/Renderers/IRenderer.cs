using HexMage.GUI.Core;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Renderers {
    public interface IRenderer {
        void Render(Entity entity, SpriteBatch batch, AssetManager assetManager);
    }
}