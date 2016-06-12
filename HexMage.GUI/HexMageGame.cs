using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

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

    /// <summary>
    ///     This is the main type for your game.
    /// </summary>
    public class HexMageGame : Game
    {
        private readonly Camera2D _camera;

        private GameInstance _gameInstance;

        private GraphicsDeviceManager _graphics;

        private SceneManager _sceneManager;

        private SpriteBatch _spriteBatch;

        private readonly InputManager _inputManager = new InputManager();
        private readonly AssetManager _assetManager;

        public HexMageGame() {
            _graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 1024
            };

            _assetManager = new AssetManager(Content);

            _camera = new Camera2D(_inputManager);

            Content.RootDirectory = "Content";
        }

        protected override void Initialize() {
            _sceneManager = new SceneManager(new ArenaScene());

            // MonoGame doesn't show mouse by default
            IsMouseVisible = true;

            base.Initialize();
        }

        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _assetManager.Preload();
            _assetManager.RegisterTexture(AssetManager.GrayTexture,
                TextureGenerator.SolidColor(GraphicsDevice, 32, 32, Color.LightGray));            
        }

        protected override void Update(GameTime gameTime) {
            _inputManager.Refresh();

            if (_inputManager.IsKeyJustPressed(Keys.Escape)) {
                Exit();
            }

            _sceneManager.Update(gameTime);

            if (_inputManager.JustRightClicked()) {
                var mouseHex = _camera.MouseHex;
                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    _gameInstance.Map.Toogle(mouseHex);

                    // TODO - pathfindovani ze zdi najde cesty
                    _gameInstance.Pathfinder.PathfindFrom(new AxialCoord(0, 0));
                }
            }

            HandleUserInput();

            _camera.Update(gameTime);

            base.Update(gameTime);
        }

        private void HandleUserInput() {
            if (_inputManager.IsKeyJustPressed(Keys.Space)) {
                _gameInstance.TurnManager.MoveNext();
                _gameInstance.Pathfinder.PathfindFrom(_gameInstance.TurnManager.CurrentMob().Coord);
            }
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _sceneManager.Draw(gameTime);

            base.Draw(gameTime);
        }

    }
}