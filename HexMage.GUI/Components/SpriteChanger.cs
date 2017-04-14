using System;
using HexMage.GUI.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Components {
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