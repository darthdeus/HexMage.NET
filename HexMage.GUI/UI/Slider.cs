using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.UI {
    public class Slider : Entity, IRenderer {
        private readonly int _min;
        private readonly int _max;
        private readonly Point _size;
        private float _value;
        private readonly Point _sliderSize;
        private readonly Point _sliderOffset;
        private readonly Point _sliderHoverSize;
        private readonly Point _sliderHoverOffset;

        private bool _dragging = false;

        private Point _valueOffset => new Point((int) (_size.X*_value), 0);

        private Rectangle _sliderRectangle => new Rectangle(
            RenderPosition.ToPoint() + _valueOffset + _sliderOffset, _sliderSize);

        private bool _hovering = false;

        public event Action<int> OnChange;
        public int Value => (int) ((_max - _min)*_value);

        public Slider(int min, int max, Point size) {
            _min = min;
            _max = max;
            _size = size;
            _sliderSize = new Point(_size.Y + 2, _size.Y + 2);
            _sliderOffset = new Point(-_sliderSize.X/2, -1);
            _sliderHoverSize = new Point(2, 2);
            _sliderHoverOffset = new Point(-1, -1);
            _value = 0;
            Renderer = this;
        }

        protected override void Update(GameTime time) {
            base.Update(time);

            var inputManager = InputManager.Instance;
            _hovering = _sliderRectangle.Contains(inputManager.MousePosition);

            if (inputManager.JustLeftClicked() && _hovering) {
                _dragging = true;
            }
            if (inputManager.JustLeftClickReleased()) {
                _dragging = false;
            }
        }

        protected override void Layout() {
            LayoutSize = _size.ToVector2();
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var tex = assetManager[AssetManager.SolidGrayColor];
            var rect = new Rectangle(RenderPosition.ToPoint(), _size);
            batch.Draw(tex, rect, Color.White);

            UpdateValue();

            var renderRectangle = _sliderRectangle;

            if (_hovering) {
                renderRectangle.Offset(_sliderHoverOffset);
                renderRectangle.Inflate(_sliderHoverSize.X, _sliderHoverSize.Y);
            }

            batch.Draw(tex, renderRectangle, Color.DarkGray);
        }

        private void UpdateValue() {
            if (_dragging) {
                float leftEdge = RenderPosition.X;
                float rightEdge = RenderPosition.X + _size.X;

                var mousePosition = InputManager.Instance.MousePosition;

                var horizontalMousePosition = MathHelper.Clamp(mousePosition.X, leftEdge, rightEdge);

                float percent = (horizontalMousePosition - leftEdge)/(rightEdge - leftEdge);

                // Even though Render shouldn't really update the value (and Update should),
                // we don't have access to the current RenderPosition, which forces us to
                // update the value while rendering.
                _value = percent;

                OnChange?.Invoke(Value);
            }
        }
    }
}