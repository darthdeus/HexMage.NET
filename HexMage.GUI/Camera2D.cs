using System;
using System.Diagnostics;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace HexMage.GUI
{
    public class Camera2D {
        public static readonly int SortUI = 1000;
        public static readonly int SortMobs = 100;
        public static readonly int SortProjectiles = 200;
        public static readonly int SortBackground = 10;


        public static Camera2D Instance;

        private readonly InputManager _inputManager;
        private const float ScrollAmount = 0.03f;
        private const float TranslateAmount = 10;
        private int _lastWheel;
        private Vector3 _translate = Vector3.Zero;
        private float _zoomLevel = 1.0f;

        public Camera2D(InputManager inputManager) {
            _inputManager = inputManager;

            // TODO - change this into a proper singleton later
            Debug.Assert(Instance == null);
            Instance = this;
        }

        public void Update(GameTime gameTime) {
            var scrollOff = Mouse.GetState().ScrollWheelValue;
            var diff = _lastWheel - scrollOff;
            _lastWheel = scrollOff;

            if (diff > 0) {
                _zoomLevel -= ScrollAmount;
            } else if (diff < 0) {
                _zoomLevel += ScrollAmount;
            }

            _zoomLevel = MathHelper.Clamp(_zoomLevel, 0.3f, 3f);

            var keyboard = Keyboard.GetState();

            if (keyboard.IsKeyDown(Keys.W)) _translate.Y += TranslateAmount;
            if (keyboard.IsKeyDown(Keys.S)) _translate.Y -= TranslateAmount;
            if (keyboard.IsKeyDown(Keys.A)) _translate.X += TranslateAmount;
            if (keyboard.IsKeyDown(Keys.D)) _translate.X -= TranslateAmount;
        }

        public Matrix Projection => Matrix.CreateScale(_zoomLevel)*Matrix.CreateTranslation(_translate);

        public Vector2 HexToPixel(AxialCoord coord) {
            int row = coord.Y;
            int col = coord.X;

            var x = (int) (Config.GridSize*(col + row/2.0));
            var y = (int) (row*Config.HeightOffset);

            return new Vector2(x, y);
        }

        public Vector2 MousePixelPos => new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
        public Vector2 MouseWorldPixelPos => Vector2.Transform(MousePixelPos, Matrix.Invert(Projection));

        public AxialCoord PixelToHex(Vector2 pos) {
            pos = Vector2.Transform(pos, Matrix.Invert(Projection)) - new Vector2(Config.GridSize/2);

            var row = (int) Math.Round(pos.Y/Config.HeightOffset);
            var col = (int) Math.Round(pos.X/Config.GridSize - row/2.0);

            return new AxialCoord(col, row);
        }

        public AxialCoord MouseHex => PixelToHex(MousePixelPos);
    }
}