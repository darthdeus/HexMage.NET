using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace HexMage.GUI
{
    public class Camera2D
    {
        private const float ScrollAmount = 0.03f;
        private const float TranslateAmount = 10;
        private readonly int _gridSize;
        private readonly double _heightOffset;
        private int _lastWheel;
        private Vector3 _translate = Vector3.Zero;
        private float _zoomLevel = 1.0f;

        public Camera2D(int gridSize, double heightOffset, InputManager inputManager) {
            _gridSize = gridSize;
            _heightOffset = heightOffset;
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

        public Matrix Projection() {
            return Matrix.CreateScale(_zoomLevel)*Matrix.CreateTranslation(_translate);
        }

        public Vector2 HexToPixel(AxialCoord coord) {
            int row = coord.Y;
            int col = coord.X;

            var x = (int) (_gridSize*(col + row/2.0));
            var y = (int) (row*_heightOffset);

            return new Vector2(x, y);
        }

        public AxialCoord PixelToHex(Vector2 pos) {
            pos = Vector2.Transform(pos, Matrix.Invert(Projection()));

            var row = (int) (pos.Y/_heightOffset);
            var col = (int) (pos.X/_gridSize - row/2.0);

            return new AxialCoord(col, row);
        }

        public AxialCoord MouseHex { get {  return PixelToHex(new Vector2(Mouse.GetState().X, Mouse.GetState().Y));} }
    }
}