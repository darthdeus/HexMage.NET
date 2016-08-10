using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    public class AssetManager {
        public static readonly string WallTexture = "wall_hex";
        public static readonly string PathTexture = "path_hex";
        public static readonly string MobTexture = "mage";
        public static readonly string EmptyHexTexture = "photoshopTile";
        public static readonly string GrayTexture = "gray";

        public static string DarkMage {
            get { throw new NotImplementedException(); }
        }

        public static readonly string DarkMageIdle = "dark-mage-idle";
        public static readonly string DarkMageClicked = "dark-mage-clicked";

        private static readonly string FontName = "Arial";

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
            get {
                Debug.Assert(_font != null);
                return _font;
            }
        }


        public void Preload() {
            _font = _contentManager.Load<SpriteFont>(FontName);
            foreach (var texture in new[] {WallTexture, PathTexture, MobTexture, EmptyHexTexture}) {
                _textures[texture] = _contentManager.Load<Texture2D>(texture);
            }
        }

        public void RegisterTexture(string name, Texture2D texture2D) {
            _textures[name] = texture2D;
        }
    }
}