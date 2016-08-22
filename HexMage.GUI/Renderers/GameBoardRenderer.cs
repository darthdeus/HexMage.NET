using System;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Renderers {
    public class GameBoardRenderer : IRenderer {
        private readonly GameInstance _gameInstance;
        private readonly Camera2D _camera;
        private SpriteBatch _spriteBatch;
        private AssetManager _assetManager;

        public GameBoardRenderer(GameInstance gameInstance, Camera2D camera) {
            _gameInstance = gameInstance;
            _camera = camera;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            _spriteBatch = batch;
            _assetManager = assetManager;

            DrawBackground();
            DrawHoverPath();
            DrawMousePosition();
        }

        private void DrawBackground() {
            _spriteBatch.Begin(transformMatrix: _camera.Transform);

            int maxX = Int32.MinValue;
            int maxY = Int32.MinValue;
            int maxZ = Int32.MinValue;

            int minX = Int32.MaxValue;
            int minY = Int32.MaxValue;
            int minZ = Int32.MaxValue;

            var hexGreen = _assetManager[AssetManager.HexEmptySprite];
            var hexWall = _assetManager[AssetManager.HexWallSprite];

            foreach (var coord in _gameInstance.Map.AllCoords) {
                maxX = Math.Max(maxX, coord.ToCube().X);
                maxY = Math.Max(maxY, coord.ToCube().Y);
                maxZ = Math.Max(maxZ, coord.ToCube().Z);

                minX = Math.Min(minX, coord.ToCube().X);
                minY = Math.Min(minY, coord.ToCube().Y);
                minZ = Math.Min(minZ, coord.ToCube().Z);

                if (_gameInstance.Map[coord] == HexType.Empty) {
                    DrawAt(hexGreen, coord);
                } else {
                    DrawAt(hexWall, coord);
                }
            }

            //_spriteBatch.DrawString(_assetManager.Font, $"{minX},{minY},{minZ}   {maxX},{maxY},{maxZ}",
            //                        new Vector2(0, 50),
            //                        Color.Red);
            _spriteBatch.End();
        }

        private void DrawHoverPath() {
            _spriteBatch.Begin(transformMatrix: _camera.Transform);

            var hexUsable = _assetManager[AssetManager.HexWithinDistance];
            var hexTooFar = _assetManager[AssetManager.HexPathSprite];

            if (_gameInstance.Pathfinder.IsValidCoord(_camera.MouseHex)) {
                var path = _gameInstance.Pathfinder.PathTo(_camera.MouseHex);

                var currentMob = _gameInstance.TurnManager.CurrentMob;
                var abilityIndex = _gameInstance.TurnManager.SelectedAbilityIndex;

                if (abilityIndex.HasValue) {
                    var cubepath = _gameInstance.Map.CubeLinedraw(currentMob.Coord, _camera.MouseHex);

                    int distance = 1;
                    foreach (var cubeCoord in cubepath) {
                        if (distance <= currentMob.Abilities[abilityIndex.Value].Range) {
                            DrawAt(hexUsable, cubeCoord);
                        } else {
                            DrawAt(hexTooFar, cubeCoord);
                        }

                        distance++;
                    }
                } else {
                    foreach (var coord in path) {
                        if (currentMob.Coord.Distance(coord) <= currentMob.AP) {
                            DrawAt(hexUsable, coord);
                        } else {
                            DrawAt(hexTooFar, coord);
                        }
                    }
                }
            }
            _spriteBatch.End();
        }

        private void DrawMousePosition() {
            _spriteBatch.Begin();
            var mouseTextPos = new Vector2(0, 850);

            var mousePos = _camera.MouseWorldPixelPos;

            string str = $"{mousePos} - {_camera.MouseHex}";
            _spriteBatch.DrawString(_assetManager.Font, str, mouseTextPos, Color.Black);
            _spriteBatch.End();
        }

        private void DrawAt(Texture2D texture, AxialCoord coord) {
            _spriteBatch.Draw(texture, _camera.HexToPixel(coord));
        }

        private void DrawAt(Texture2D texture, AxialCoord coord, Color color) {
            _spriteBatch.Draw(texture, _camera.HexToPixel(coord), color);
        }
    }

    public static class ColorToXNAConverter {
        public static Microsoft.Xna.Framework.Color ToXnaColor(this HexMage.Simulator.Color color) {
            return new Microsoft.Xna.Framework.Color(new Vector3((float) color.X, (float) color.Y, (float) color.Z));
        }
    }
}