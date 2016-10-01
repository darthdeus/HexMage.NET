using System;
using HexMage.GUI.Core;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

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

            var pathfinder = new Pathfinder(map, new MobManager());
            pathfinder.PathfindDistanceAll();
            var distanceMap = new HexMap<int>(map.Size);
            pathfinder.PathfindDistanceOnlyFrom(distanceMap, new AxialCoord(0, 0));
            pathfinder.PathfindFrom(new AxialCoord(0, 0));

            foreach (var coord in map.AllCoords) {
                var pixelCoord = _camera.HexToPixel(coord, _scale) + entity.RenderPosition;

                var scale = new Vector2(_scale);
                if (map[coord] == HexType.Empty) {
                    batch.Draw(hexEmpty, pixelCoord, scale: scale);
                } else {
                    batch.Draw(hexWall, pixelCoord, scale: scale);
                }

                if (coord == new AxialCoord(0, 0)) {
                    batch.Draw(assetManager[AssetManager.HexHoverSprite], pixelCoord, scale: scale);
                }


                batch.DrawString(assetManager.Font, distanceMap[coord].ToString(), pixelCoord, Color.Black);
            }
        }
    }
}