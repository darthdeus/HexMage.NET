using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    /// <summary>
    /// A simple constant color texture generator.
    /// </summary>
    public static class TextureGenerator {
        public static Texture2D SolidColor(GraphicsDevice device, int width, int height, Color color) {
            var texture2D = new Texture2D(device, width, height);

            var colors = new Color[width * height];
            for (int i = 0; i < width * height; i++) {
                colors[i] = color;
            }

            texture2D.SetData(colors);

            return texture2D;
        }
    }
}