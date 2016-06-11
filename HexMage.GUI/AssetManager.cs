using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI
{
    class AssetManager
    {
        private readonly ContentManager _contentManager;
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private SpriteFont _font;

        public AssetManager(ContentManager contentManager) {
            _contentManager = contentManager;
        }

        public Texture2D this[string name] {
            get {
                if (_textures.ContainsKey(name)) {
                    return _textures[name];
                } else {
                    _textures[name] = _contentManager.Load<Texture2D>(name);
                    return _textures[name];
                }
            }
        }

        public SpriteFont Font {
            get { return _font ?? (_font = _contentManager.Load<SpriteFont>("Arial")); }
        }

        public void Preload() {
            var _ = Font;
        }
    }
}