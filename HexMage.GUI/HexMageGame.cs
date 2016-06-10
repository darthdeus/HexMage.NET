using System;
using System.Collections.Generic;
using System.Linq;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        // TODO - don't reallocate everything on a new frame
        public class GuiWindow
        {
            List<Tuple<string, Rectangle>> Labels = new List<Tuple<string, Rectangle>>();
            List<Tuple<string, Rectangle>> Buttons = new List<Tuple<string, Rectangle>>();

            private readonly InputManager _inputManager;
            private readonly SpriteBatch _spriteBatch;

            public GuiWindow(InputManager inputManager, SpriteBatch spriteBatch) {
                _inputManager = inputManager;
                _spriteBatch = spriteBatch;
            }

            public void Label(string text, Rectangle rect) {
                Labels.Add(Tuple.Create(text, rect));
            }

            public bool Button(string text, Rectangle rect) {
                Buttons.Add(Tuple.Create(text, rect));

                return rect.Contains(_inputManager.MousePosition.ToPoint());
            }

            public void Draw(SpriteFont font) {
                foreach (var label in Labels) {
                    _spriteBatch.DrawString(
                        font,
                        label.Item1,
                        label.Item2.Location.ToVector2(),
                        Color.Black);
                }

                foreach (var button in Buttons) {
                    _spriteBatch.DrawString(
                        font,
                        button.Item1,
                        button.Item2.Location.ToVector2(),
                        Color.Red);
                }
            }
        }

        public static readonly int GridSize = 32;
        private static readonly double HeightOffset = GridSize/4 + Math.Sin(30*Math.PI/180)*GridSize;
        private readonly Camera2D _camera = new Camera2D(GridSize, HeightOffset);

        private readonly FrameCounter _frameCounter = new FrameCounter();
        private SpriteFont _arialFont;

        private GameInstance _gameInstance;

        private GraphicsDeviceManager _graphics;
        private Texture2D _hexGreen;
        private Texture2D _hexPath;
        private Texture2D _hexWall;


        private Vector2 _lastMousePos = new Vector2(0);
        private MouseState _lastMouseState;
        private KeyboardState _lastKeyboardState;
        private Texture2D _mobTexture;
        private bool _mouseChanged;
        private SpriteBatch _spriteBatch;

        public HexMageGame() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        ///     Allows the game to perform any initialization it needs to before starting to run.
        ///     This is where it can query for any required services and load any non-graphic
        ///     related content.  Calling base.Initialize will enumerate through any components
        ///     and initialize them as well.
        /// </summary>
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

            IsMouseVisible = true;

            base.Initialize();
        }

        /// <summary>
        ///     LoadContent will be called once per game and is the place to load
        ///     all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            //_hexGreen = new Texture2D(GraphicsDevice, GridSize, GridSize);
            _hexGreen = Content.Load<Texture2D>("green_hex");
            _hexWall = Content.Load<Texture2D>("wall_hex");
            _hexPath = Content.Load<Texture2D>("path_hex");
            _mobTexture = Content.Load<Texture2D>("mob");

            _arialFont = Content.Load<SpriteFont>("Arial");
        }

        /// <summary>
        ///     UnloadContent will be called once per game and is the place to unload
        ///     game-specific content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        ///     Allows the game to run logic such as updating the world,
        ///     checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var mouseState = Mouse.GetState();

            var mousePos = new Vector2(mouseState.X, mouseState.Y);

            if (_lastMouseState.RightButton == ButtonState.Pressed
                && mouseState.RightButton == ButtonState.Released) {
                var mouseHex = _camera.PixelToHex(mousePos);
                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    if (_gameInstance.Map[mouseHex] == HexType.Empty) {
                        _gameInstance.Map[mouseHex] = HexType.Wall;
                    } else {
                        _gameInstance.Map[mouseHex] = HexType.Empty;
                    }

                    // TODO - pathfindovani ze zdi najde cesty
                    _gameInstance.Pathfinder.PathfindFrom(new AxialCoord(0, 0));
                }
            }

            _mouseChanged = _lastMousePos != mousePos;
            _lastMousePos = mousePos;
            _lastMouseState = mouseState;

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

        /// <summary>
        ///     This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            DrawBackground();
            DrawHoverPath();
            DrawAllMobs();
            DrawMousePosition();

            _frameCounter.DrawFPS(_spriteBatch, _arialFont);

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

            _spriteBatch.DrawString(_arialFont, $"{minX},{minY},{minZ}   {maxX},{maxY},{maxZ}", new Vector2(0, 50),
                Color.Red);
            _spriteBatch.End();
        }

        private void DrawHoverPath() {
            _spriteBatch.Begin(transformMatrix: _camera.Projection());
            var mouseHex = _camera.PixelToHex(_lastMousePos);

            if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                var path = _gameInstance.Pathfinder.PathTo(mouseHex);

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

                string str = $"{_lastMousePos} - {bounds} - {_camera.PixelToHex(_lastMousePos)}";
                _spriteBatch.DrawString(_arialFont, str, mouseTextPos, Color.Black);
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