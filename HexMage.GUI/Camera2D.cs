using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace HexMage.GUI
{
    public class Camera2D
    {
        private float _zoomLevel = 1.0f;
        private Vector3 _translate = Vector3.Zero;
        private int _lastWheel = 0;

        private const float ScrollAmount = 0.03f;
        private const float TranslateAmount = 10;

        public void Update(GameTime gameTime) {
            int scrollOff = Mouse.GetState().ScrollWheelValue;
            int diff = _lastWheel - scrollOff;
            _lastWheel = scrollOff;

            if (diff > 0) {
                _zoomLevel -= ScrollAmount;
            } else if (diff < 0) {
                _zoomLevel += ScrollAmount;
            }

            _zoomLevel = MathHelper.Clamp(_zoomLevel, 0.3f, 3f);

            var keyboard = Keyboard.GetState();

            if (keyboard.IsKeyDown(Keys.W)) _translate.Y += TranslateAmount ;
            if (keyboard.IsKeyDown(Keys.S)) _translate.Y -= TranslateAmount ;
            if (keyboard.IsKeyDown(Keys.A)) _translate.X += TranslateAmount ;
            if (keyboard.IsKeyDown(Keys.D)) _translate.X -= TranslateAmount ;
        }

        public Matrix Projection() {
            return Matrix.CreateScale(_zoomLevel)*Matrix.CreateTranslation(_translate);
        }
    }
}