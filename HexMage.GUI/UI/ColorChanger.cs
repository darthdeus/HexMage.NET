using HexMage.GUI.UI;
using Microsoft.Xna.Framework;

namespace HexMage.GUI {
    public class ColorChanger : Component {
        private readonly Color _hoverColor;
        private ColorRenderer _renderer;
        private Color _origColor;

        public ColorChanger(Color hoverColor) {
            _hoverColor = hoverColor;
        }

        public override void Initialize() {
            _renderer = (ColorRenderer)Entity.Renderer;
            _origColor = _renderer.Color;
        }

        public override void Update(GameTime time) {

            if (Entity.AABB.Contains(InputManager.Instance.MousePosition)) {
                _renderer.Color = _hoverColor;
            } else {
                _renderer.Color = _origColor;
            }
        }
    }
}