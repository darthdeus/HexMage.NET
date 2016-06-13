using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI
{
    class GameManager
    {
        public Camera2D Camera { get; set; }
        public InputManager InputManager { get; set; }
        public AssetManager AssetManager { get; set; }
        public SpriteBatch SpriteBatch { get; set; }

        public GameManager(Camera2D camera, InputManager inputManager, AssetManager assetManager, SpriteBatch spriteBatch) {
            Camera = camera;
            InputManager = inputManager;
            AssetManager = assetManager;
            SpriteBatch = spriteBatch;
        }
    }
}