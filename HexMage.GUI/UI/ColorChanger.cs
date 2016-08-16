using System;
using HexMage.GUI.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
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

    public class SpriteChanger : Component {
        private readonly Texture2D _hoverSprite;
        private SpriteRenderer _renderer;
        private Texture2D _origSprite;
        public Func<Texture2D> RegularSpriteFunc;

        public event Func<Texture2D> OnHover; 

        public SpriteChanger(Texture2D hoverSprite) {
            _hoverSprite = hoverSprite;
        }

        public override void Initialize(AssetManager assetManager) {
            base.Initialize(assetManager);

            _renderer = (SpriteRenderer) Entity.Renderer;
            _origSprite = _renderer.Tex;
        }

        public override void Update(GameTime time) {
            base.Update(time);

            if (Entity.AABB.Contains(InputManager.Instance.MousePosition)) {
                _renderer.Tex = OnHover?.Invoke() ?? _hoverSprite;
            } else {
                _renderer.Tex = RegularSpriteFunc?.Invoke() ?? _origSprite;
            }
        }
    }
}