using System;
using HexMage.GUI.Core;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Renderers {
    public class MapPreviewRenderer : IRenderer {
        private Func<Map> _mapFunc;
        private readonly float _scale;
        private Camera2D _camera;

        public MapPreviewRenderer(Func<Map> mapFunc, float scale) {
            _mapFunc = mapFunc;
            _scale = scale;
            _camera = Camera2D.Instance;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var hexEmpty = assetManager[AssetManager.HexEmptySprite];
            var hexWall = assetManager[AssetManager.HexWallSprite];

            var map = _mapFunc();

            foreach (var coord in map.AllCoords) {
                var pixelCoord = _camera.HexToPixel(coord, _scale) + entity.RenderPosition;

                if (map[coord] == HexType.Empty) {
                    batch.Draw(hexEmpty, pixelCoord, scale: new Vector2(_scale));
                } else {
                    batch.Draw(hexWall, pixelCoord, scale: new Vector2(_scale));
                }
            }
        }
    }
}