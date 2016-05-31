using System;
using System.Linq;
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
        public static readonly int GridSize = 32;
        private static readonly double HeightOffset = GridSize/4 + Math.Sin(30*Math.PI/180)*GridSize;
        private readonly Camera2D _camera = new Camera2D();

        private readonly FrameCounter _frameCounter = new FrameCounter();
        private SpriteFont _arialFont;

        private GameInstance _gameInstance;

        private GraphicsDeviceManager _graphics;
        private Texture2D _hexGreen;
        private Texture2D _hexPath;
        private Texture2D _hexTexture;
        private Texture2D _hexWall;


        private Vector2 _lastMousePos = new Vector2(0);
        private MouseState _lastMouseState;
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

            _hexTexture = new Texture2D(GraphicsDevice, GridSize, GridSize);

            var hexColors = new Color[GridSize, GridSize];
            for (var i = 0; i < GridSize; i++) {
                for (var j = 0; j < GridSize; j++) {
                    if (Math.Sqrt(i*i + j*j) < GridSize) {
                        hexColors[i, j] = Color.MediumPurple;
                    } else {
                        hexColors[i, j] = Color.Black;
                    }
                }
            }

            _hexTexture.SetData(hexColors.Cast<Color>().ToArray());

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
                var mouseHex = PixelToHex(mousePos);
                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    if (_gameInstance.Map[mouseHex] == HexType.Empty) {
                        _gameInstance.Map[mouseHex] = HexType.Wall;
                    } else {
                        _gameInstance.Map[mouseHex] = HexType.Empty;
                    }

                    // TODO - pathfindovani ze zdi najde cesty
                    _gameInstance.Pathfinder.PathfindFrom(new Coord(0, 0), _gameInstance.Map, _gameInstance.MobManager);
                }
            }

            _mouseChanged = _lastMousePos != mousePos;
            _lastMousePos = mousePos;
            _lastMouseState = mouseState;

            _camera.Update(gameTime);

            _frameCounter.Update(gameTime.ElapsedGameTime.TotalSeconds);

            base.Update(gameTime);
        }

        /// <summary>
        ///     This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(transformMatrix: _camera.Projection());

            foreach (var coord in _gameInstance.Map.AllCoords) {
                if (_gameInstance.Map[coord] == HexType.Empty) {
                    DrawAt(_hexGreen, coord);
                } else {
                    DrawAt(_hexWall, coord);
                }
            }

            foreach (var mob in _gameInstance.MobManager.Mobs) {
                DrawAt(_mobTexture, mob.Coord);
            }

            var mouseHex = PixelToHex(_lastMousePos);
            DrawAt(_mobTexture, mouseHex);

            _spriteBatch.End();

            if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                _spriteBatch.Begin(transformMatrix: _camera.Projection());
                var path = _gameInstance.Pathfinder.PathTo(mouseHex);

                foreach (var coord in path) {
                    DrawAt(_hexPath, coord);
                }

                _spriteBatch.End();
            }

            DrawMousePosition();

            _frameCounter.DrawFPS(_spriteBatch, _arialFont);

            base.Draw(gameTime);
        }

        private void DrawMousePosition() {
            _spriteBatch.Begin();
            {
                var bounds = GraphicsDevice.PresentationParameters.Bounds;
                var mouseTextPos = new Vector2(0, 450);
                _spriteBatch.DrawString(_arialFont, $"{_lastMousePos} - {bounds}", mouseTextPos, Color.Black);
            }
            _spriteBatch.End();
        }

        private void DrawAt(Texture2D mobTexture, Coord coord) {
            DrawAt(mobTexture, coord.Y, coord.X);
        }

        public Vector2 HexToPixel(int row, int col) {
            var x = (int) (GridSize*(col + row/2.0));
            var y = (int) (row*HeightOffset);

            return new Vector2(x, y);
        }

        public Coord PixelToHex(Vector2 pos) {
            var row = (int) (pos.Y/HeightOffset);
            var col = (int) (pos.X/GridSize - row/2.0);

            return new Coord(col, row);
        }

        private void DrawAt(Texture2D texture, int row, int col) {
            _spriteBatch.Draw(texture, HexToPixel(row, col));
        }
    }
}