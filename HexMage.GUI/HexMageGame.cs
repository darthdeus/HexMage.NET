using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenTK;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace HexMage.GUI
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class HexMageGame : Game
    {
        public readonly int GridSize = 32;

        private readonly FrameCounter _frameCounter = new FrameCounter();
        private readonly Camera2D _camera = new Camera2D();

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Simulator.Game _game;
        private Texture2D _hexTexture;
        private SpriteFont _arialFont;
        private Texture2D _hexGreen;
        private Texture2D _mobTexture;


        private Vector2 _lastMousePos = new Vector2(0);
        private bool _mouseChanged = false;

        public HexMageGame() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            _game = new Simulator.Game(10);
            IsMouseVisible = true;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            //_hexGreen = new Texture2D(GraphicsDevice, GridSize, GridSize);
            _hexGreen = Content.Load<Texture2D>("green_hex");
            _mobTexture = Content.Load<Texture2D>("mob");

            _hexTexture = new Texture2D(GraphicsDevice, GridSize, GridSize);

            var hexColors = new Color[GridSize, GridSize];
            for (int i = 0; i < GridSize; i++) {
                for (int j = 0; j < GridSize; j++) {
                    if (Math.Sqrt(i*i + j*j) < GridSize) {
                        hexColors[i, j] = Color.MediumPurple;
                    } else {
                        hexColors[i, j] = Color.Black;
                    }
                }
            }

            _hexTexture.SetData<Color>(hexColors.Cast<Color>().ToArray());

            _arialFont = Content.Load<SpriteFont>("Arial");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var mouse = Mouse.GetState();

            var mousePos = new Vector2(mouse.X, mouse.Y);

            _mouseChanged = _lastMousePos != mousePos;
            _lastMousePos = mousePos;

            _camera.Update(gameTime);

            _frameCounter.Update(gameTime.ElapsedGameTime.TotalSeconds);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(transformMatrix: _camera.Projection());
            for (int row = 0; row < _game.Map.Size; row++) {
                for (int col = 0; col < _game.Map.Size; col++) {
                    DrawAt(_hexGreen, row, col);
                }
            }

            DrawAt(_mobTexture, 1, 1);
            DrawAt(_mobTexture, 4, 7);
            DrawAt(_mobTexture, 8, 3);

            {
                var bounds = GraphicsDevice.PresentationParameters.Bounds;
                var mouseTextPos = new Vector2(0, 450);
                Console.WriteLine(mouseTextPos);
                _spriteBatch.DrawString(_arialFont, $"{_lastMousePos} - {bounds}", mouseTextPos, Color.Black);
            }

            var mouseHex = PixelToHex(_lastMousePos);
            DrawAt(_mobTexture, (int) mouseHex.Y, (int) mouseHex.X);

            _spriteBatch.End();

            _frameCounter.DrawFPS(_spriteBatch, _arialFont);

            base.Draw(gameTime);
        }

        public Vector2 HexToPixel(int row, int col) {
            int size = GridSize/2;

            float x = (float) (size * Math.Sqrt(3) * (col + row / 2.0));
            float y = (float) (size * 3.0 / 2.0 * row);
            return new Vector2(x, y);
        }

        public Vector2 PixelToHex(Vector2 pos) {
            int size = GridSize/2;
            int x = (int) (Math.Sqrt(3)/3.0f*pos.X - 1.0f/3.0*pos.Y)/size;
            int y = (int) (2.0f/3.0f*pos.Y/size);

            return new Vector2(x, y);
        }

        private void DrawAt(Texture2D texture, int row, int col) {
            int size = GridSize/2;
            //int x = (int) (size*Math.Sqrt(3)*(col + row/2.0));
            //int y = (int) (size*3.0/2.0*row);

            //var mat = size * new Matrix2d( Math.Sqrt(3),  (Math.Sqrt(3) / 2.0), 0, 3.0 / 2.0);
            //var coord = new OpenTK.Vector2d(col, row);

            int x = col * GridSize + row * (GridSize / 2);
            double heightOffset = GridSize / 4 + Math.Sin(30 * Math.PI / 180) * GridSize;
            int y = (int)(row * heightOffset);
            _spriteBatch.Draw(texture, new Vector2(x, y));

            //_spriteBatch.Draw(texture, HexToPixel(row, col));
        }
    }
}