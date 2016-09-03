using System.Threading;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    public class GameManager {
        public Camera2D Camera { get; set; }
        public InputManager InputManager { get; set; }
        public AssetManager AssetManager { get; set; }
        public SpriteBatch SpriteBatch { get; set; }
        private readonly SceneSynchronizationContext _synchronizationContext;

        public GameManager(Camera2D camera, InputManager inputManager, AssetManager assetManager,
                           SpriteBatch spriteBatch) {
            Camera = camera;
            InputManager = inputManager;
            AssetManager = assetManager;
            SpriteBatch = spriteBatch;
            _synchronizationContext = new SceneSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
        }

        public void ProcessSynchronizationContextQueue() {
            _synchronizationContext.ProcessQueueOnCurrentThread();
        }
    }
}