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
        public Vector3 Translate = Vector3.Zero;
        public float ZoomLevel = 1.0f;

        private Point _lastMousePosition;
        private bool _dragging = false;

        public Camera2D(InputManager inputManager) {
            _inputManager = inputManager;

            // TODO - change this into a proper singleton later
            Debug.Assert(Instance == null);
            Instance = this;
        }

        public void Reset() {
            Translate = Vector3.Zero;
            ZoomLevel = 1.0f;
        }

        public void Update(GameTime gameTime) {
            var scrollOff = Mouse.GetState().ScrollWheelValue;
            var diff = _lastWheel - scrollOff;
            _lastWheel = scrollOff;

            if (diff > 0) {
                ZoomLevel -= ScrollAmount;
            } else if (diff < 0) {
                ZoomLevel += ScrollAmount;
            }

            ZoomLevel = MathHelper.Clamp(ZoomLevel, 0.3f, 3f);

            var keyboard = Keyboard.GetState();

            if (keyboard.IsKeyDown(Keys.W)) Translate.Y += TranslateAmount;
            if (keyboard.IsKeyDown(Keys.S)) Translate.Y -= TranslateAmount;
            if (keyboard.IsKeyDown(Keys.A)) Translate.X += TranslateAmount;
            if (keyboard.IsKeyDown(Keys.D)) Translate.X -= TranslateAmount;

            var inputManager = InputManager.Instance;
            if (inputManager.JustMiddleClicked()) {
                _dragging = true;
                _lastMousePosition = inputManager.MousePosition;
            }

            if (_dragging) {
                var movement = inputManager.MousePosition - _lastMousePosition;
                _lastMousePosition = inputManager.MousePosition;

                Translate += new Vector3(movement.ToVector2(), 0);
            }

            if (inputManager.JustMiddleClickReleased()) {
                _dragging = false;
            }
        }

        public Matrix Transform => Matrix.CreateScale(ZoomLevel)*Matrix.CreateTranslation(Translate);

        public Vector2 HexToPixel(AxialCoord coord) {
            int row = coord.Y;
            int col = coord.X;

            var x = (int) (Config.GridSize*(col + row/2.0));
            var y = (int) (row*Config.HeightOffset);

            return new Vector2(x, y);
        }

        public Vector2 MousePixelPos => new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
        public Vector2 MouseWorldPixelPos => Vector2.Transform(MousePixelPos, Matrix.Invert(Transform));

        public AxialCoord PixelToHex(Vector2 pos) {
            pos = Vector2.Transform(pos, Matrix.Invert(Transform)) - new Vector2(Config.GridSize/2);

            var row = (int) Math.Round(pos.Y/Config.HeightOffset);
            var col = (int) Math.Round(pos.X/Config.GridSize - row/2.0);

            return new AxialCoord(col, row);
        }

        public AxialCoord MouseHex => PixelToHex(MousePixelPos);
    }
}