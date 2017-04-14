using System;
using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    public class ColorChanger : Component {
        private readonly Color _hoverColor;
        private ColorRenderer _renderer;
        private Color _origColor;
        public Func<Color> RegularColorFunc;

        public event Func<Color> OnHover;

        public ColorChanger(Color hoverColor) {
            _hoverColor = hoverColor;
        }

        public override void Initialize(AssetManager assetManager) {
            _renderer = (ColorRenderer)Entity.Renderer;
            _origColor = _renderer.Color;
        }

        public override void Update(GameTime time) {
            if (Entity.AABB.Contains(InputManager.Instance.MousePosition)) {
                _renderer.Color = OnHover?.Invoke() ?? _hoverColor;
            } else {
                _renderer.Color = RegularColorFunc?.Invoke() ?? _origColor;
            }
        }
    }
}