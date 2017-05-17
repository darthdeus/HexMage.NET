using System;
using HexMage.GUI.Core;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Renderers {
    /// <summary>
    /// Renderer for the map editor.
    /// </summary>
    public class MapEditorRenderer : IRenderer {
        private readonly Func<Map> _mapFunc;

        public MapEditorRenderer(Func<Map> mapFunc) {
            _mapFunc = mapFunc;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var hexEmpty = assetManager[AssetManager.HexEmptySprite];
            var hexWall = assetManager[AssetManager.HexWallSprite];

            var map = _mapFunc();
            var camera = Camera2D.Instance;
            batch.Begin(transformMatrix: camera.Transform, samplerState: Camera2D.SamplerState);

            foreach (var coord in map.AllCoords) {
                var pixelCoord = camera.HexToPixel(coord) + entity.RenderPosition;

                var drawTexture = map[coord] == HexType.Empty ? hexEmpty : hexWall;
                batch.Draw(drawTexture, pixelCoord);
            }

            foreach (var redStartingPoint in map.RedStartingPoints)
                DrawAt(batch,
                       assetManager[AssetManager.HexHoverSprite],
                       redStartingPoint,
                       Color.Red * 0.5f);

            for (var i = 0; i < map.RedStartingPoints.Count; i++) {
                var point = map.RedStartingPoints[i];
                DrawAt(batch,
                       assetManager[AssetManager.HexHoverSprite],
                       point,
                       Color.Red * 0.5f
                );
                batch.DrawString(assetManager.Font, $"{i}", camera.HexToPixel(point), Color.White);
            }

            for (var i = 0; i < map.BlueStartingPoints.Count; i++) {
                var point = map.BlueStartingPoints[i];
                DrawAt(batch,
                       assetManager[AssetManager.HexHoverSprite],
                       point,
                       Color.Blue * 0.5f
                );

                batch.DrawString(assetManager.Font, $"{i}", camera.HexToPixel(point), Color.White);
            }

            var mouseHex = camera.MouseHex;

            if (map.IsValidCoord(mouseHex)) {
                batch.Draw(assetManager[AssetManager.HexHoverSprite], camera.HexToPixel(mouseHex));
            }
            batch.End();
        }

        private void DrawAt(SpriteBatch batch, Texture2D texture, AxialCoord coord) {
            batch.Draw(texture, Camera2D.Instance.HexToPixel(coord));
        }

        private void DrawAt(SpriteBatch batch, Texture2D texture, AxialCoord coord, Color color) {
            batch.Draw(texture, Camera2D.Instance.HexToPixel(coord), color);
        }
    }
}