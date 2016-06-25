using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI
{
    public enum SceneUpdateResult
    {
        Terminate,
        Continue
    }

    public abstract class GameScene
    {
        protected readonly GameManager _gameManager;

        protected GameScene(GameManager gameManager) {
            _gameManager = gameManager;
        }

        protected Camera2D _camera => _gameManager.Camera;
        protected InputManager _inputManager => _gameManager.InputManager;
        protected AssetManager _assetManager => _gameManager.AssetManager;
        protected SpriteBatch _spriteBatch => _gameManager.SpriteBatch;

        public abstract void Initialize();
        public abstract void Cleanup();
        public abstract Either<GameScene, SceneUpdateResult> Update(GameTime gameTime);
        public abstract void Draw(GameTime gameTime);
    }

    
}