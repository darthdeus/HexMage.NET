using System;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenTK.Platform.Windows;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI
{
    public static class XnaCoordHelpers
    {
        public static Vector3 ToVector3(this PixelCoord coord) {
            return new Vector3(coord.X, coord.Y, 0);
        }

        public static Point ToPoint(this PixelCoord coord) {
            return new Point(coord.X, coord.Y);
        }
    }

    /// <summary>
    ///     This is the main type for your game.
    /// </summary>
    public class HexMageGame : Game
    {
        public static readonly int GridSize = 32;
        private static readonly double HeightOffset = GridSize/4 + Math.Sin(30*Math.PI/180)*GridSize;
        private readonly Camera2D _camera;

        private readonly FrameCounter _frameCounter = new FrameCounter();

        private GameInstance _gameInstance;

        private GraphicsDeviceManager _graphics;
        private Texture2D _hexGreen;
        private Texture2D _hexPath;
        private Texture2D _hexWall;


        private KeyboardState _lastKeyboardState;
        private Texture2D _mobTexture;
        private SpriteBatch _spriteBatch;
        private readonly InputManager _inputManager = new InputManager();
        private Texture2D _texGray;
        private readonly AssetManager _assetManager;

        public HexMageGame() {
            _graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 1024
            };

            _assetManager = new AssetManager(Content);

            _camera = new Camera2D(GridSize, HeightOffset, _inputManager);

            Content.RootDirectory = "Content";
        }

        protected override void Initialize() {
            _gameInstance = new GameInstance(20);

            var t1 = _gameInstance.MobManager.AddTeam();
            var t2 = _gameInstance.MobManager.AddTeam();

            for (int team = 0; team < 2; team++) {
                for (int mobI = 0; mobI < 5; mobI++) {
                    var mob = Generator.RandomMob(team%2 == 0 ? t1 : t2, _gameInstance.Size,
                        c => _gameInstance.MobManager.AtCoord(c) == null);

                    _gameInstance.MobManager.AddMob(mob);
                }
            }

            _gameInstance.TurnManager.StartNextTurn();

            // MonoGame doesn't show mouse by default
            IsMouseVisible = true;

            base.Initialize();
        }

        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _texGray = TextureGenerator.SolidColor(GraphicsDevice, 32, 32, Color.LightGray);

            //_hexGreen = new Texture2D(GraphicsDevice, GridSize, GridSize);
            _hexGreen = Content.Load<Texture2D>("photoshopTile");
            _hexWall = Content.Load<Texture2D>("wall_hex");
            _hexPath = Content.Load<Texture2D>("path_hex");
            _mobTexture = Content.Load<Texture2D>("mob");

            _assetManager.Preload();
        }

        protected override void Update(GameTime gameTime) {
            _inputManager.Refresh();

            if (_inputManager.IsKeyJustPressed(Keys.Escape)) {
                Exit();
            }


            if (_inputManager.JustRightClicked()) {
                var mouseHex = _camera.MouseHex;
                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    _gameInstance.Map.Toogle(mouseHex);

                    // TODO - pathfindovani ze zdi najde cesty
                    _gameInstance.Pathfinder.PathfindFrom(new AxialCoord(0, 0));
                }
            }

            bool keyboardChanged = _lastKeyboardState != Keyboard.GetState();
            HandleUserInput(keyboardChanged, Keyboard.GetState());
            _lastKeyboardState = Keyboard.GetState();

            _camera.Update(gameTime);

            _frameCounter.Update(gameTime.ElapsedGameTime.TotalSeconds);

            base.Update(gameTime);
        }

        private void HandleUserInput(bool keyboardStatechanged, KeyboardState currentState) {
            if (keyboardStatechanged) {
                if (currentState.IsKeyDown(Keys.Space)) {
                    _gameInstance.TurnManager.MoveNext();
                    _gameInstance.Pathfinder.PathfindFrom(_gameInstance.TurnManager.CurrentMob().Coord);
                }
            }
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            DrawBackground();
            DrawHoverPath();
            DrawAllMobs();
            DrawMousePosition();

            _frameCounter.DrawFPS(_spriteBatch, _assetManager.Font);

            var gui = new ImGui(_inputManager, _assetManager.Font);

            //gui.Button("Foo bar\nbaz", new Point(50, 50));
            //if (gui.Button("Hello", new Point(100, 100))) {
            //    Console.WriteLine("prd");
            //}

            gui.Draw(_texGray, _spriteBatch);

            base.Draw(gameTime);
        }

        private void DrawBackground() {
            _spriteBatch.Begin(transformMatrix: _camera.Projection());

            int maxX = Int32.MinValue;
            int maxY = Int32.MinValue;
            int maxZ = Int32.MinValue;

            int minX = Int32.MaxValue;
            int minY = Int32.MaxValue;
            int minZ = Int32.MaxValue;

            foreach (var coord in _gameInstance.Map.AllCoords) {
                maxX = Math.Max(maxX, coord.ToCube().X);
                maxY = Math.Max(maxY, coord.ToCube().Y);
                maxZ = Math.Max(maxZ, coord.ToCube().Z);

                minX = Math.Min(minX, coord.ToCube().X);
                minY = Math.Min(minY, coord.ToCube().Y);
                minZ = Math.Min(minZ, coord.ToCube().Z);

                if (_gameInstance.Map[coord] == HexType.Empty) {
                    DrawAt(_hexGreen, coord);
                } else {
                    DrawAt(_hexWall, coord);
                }
            }

            _spriteBatch.DrawString(_assetManager.Font, $"{minX},{minY},{minZ}   {maxX},{maxY},{maxZ}", new Vector2(0, 50),
                Color.Red);
            _spriteBatch.End();
        }

        private void DrawHoverPath() {
            _spriteBatch.Begin(transformMatrix: _camera.Projection());

            if (_gameInstance.Pathfinder.IsValidCoord(_camera.MouseHex)) {
                var path = _gameInstance.Pathfinder.PathTo(_camera.MouseHex);

                foreach (var coord in path) {
                    DrawAt(_hexPath, coord);
                }
            }
            _spriteBatch.End();
        }

        private void DrawAllMobs() {
            _spriteBatch.Begin(transformMatrix: _camera.Projection());
            foreach (var mob in _gameInstance.MobManager.Mobs) {
                DrawAt(_mobTexture, mob.Coord);
            }
            _spriteBatch.End();
        }

        private void DrawMousePosition() {
            _spriteBatch.Begin();
            {
                var bounds = GraphicsDevice.PresentationParameters.Bounds;
                var mouseTextPos = new Vector2(0, 450);

                string str = $"{bounds} - {_camera.MouseHex}";
                _spriteBatch.DrawString(_assetManager.Font, str, mouseTextPos, Color.Black);
            }
            _spriteBatch.End();
        }

        private void DrawAt(Texture2D texture, AxialCoord coord) {
            _spriteBatch.Draw(texture, _camera.HexToPixel(coord));
        }

        private void DrawAt(Texture2D texture, AxialCoord coord, Color color) {
            _spriteBatch.Draw(texture, _camera.HexToPixel(coord), color);
        }
    }
}