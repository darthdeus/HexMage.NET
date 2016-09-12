using HexMage.GUI.UI;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI
{
    /// <summary>
    ///     This is the main type for your game.
    /// </summary>
    public class HexMageGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SceneManager _sceneManager;

        private readonly Camera2D _camera;
        private SpriteBatch _spriteBatch;

        private readonly InputManager _inputManager = InputManager.Instance;
        private readonly AssetManager _assetManager;
        private GameManager _gameManager;

        public HexMageGame() {
            _graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 1024
            };

            _assetManager = new AssetManager(Content, _graphics.GraphicsDevice);
            _camera = new Camera2D(_inputManager);

            Content.RootDirectory = "Content";
        }

        protected override void Initialize() {
            // MonoGame doesn't show mouse by default
            IsMouseVisible = true;
            InputManager.Instance.Initialize(this);


            base.Initialize();
        }

        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _assetManager.Preload();
            _assetManager.RegisterTexture(AssetManager.SolidGrayColor,
                TextureGenerator.SolidColor(GraphicsDevice, 32, 32, Color.LightGray));

            LogBox.Initialize(_assetManager.Font, 1000);
            Utils.RegisterLogger(LogBox.Instance);

            _gameManager = new GameManager(_camera, _inputManager, _assetManager, _spriteBatch);
            //_sceneManager = new SceneManager(new ArenaScene(_gameManager));
            _sceneManager = new SceneManager(new MapSelectionScene(_gameManager));
            _sceneManager.Initialize();
        }

        protected override void Update(GameTime gameTime) {
            _inputManager.Refresh();

            if (_inputManager.IsKeyJustPressed(Keys.Escape)) {
                Exit();
            }
            _camera.Update(gameTime);

            _sceneManager.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _sceneManager.Render(gameTime);

            base.Draw(gameTime);
        }

    }
}