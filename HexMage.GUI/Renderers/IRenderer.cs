using HexMage.GUI.Core;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Renderers {
    /// <summary>
    /// Each entity can have an assigned IRenderer, which implements the raw
    /// drawing of graphics on the canvas.
    /// </summary>
    public interface IRenderer {
        void Render(Entity entity, SpriteBatch batch, AssetManager assetManager);
    }
}