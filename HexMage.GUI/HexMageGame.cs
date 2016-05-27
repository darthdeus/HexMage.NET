using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
            _game = new Simulator.Game(30);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

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
            for (int i = 0; i < _game.Map.Size; i++) {
                for (int j = 0; j < _game.Map.Size; j++) {
                    _spriteBatch.Draw(_hexTexture, new Vector2(i*GridSize, j*GridSize));
                }
            }

            _spriteBatch.Draw(_hexTexture, new Vector2(50, 50));
            _spriteBatch.End();

            _frameCounter.DrawFPS(_spriteBatch, _arialFont);

            base.Draw(gameTime);
        }
    }
}