using System;
using System.Collections.Generic;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Renderers {
    public class GameBoardRenderer : IRenderer {
        private readonly GameInstance _gameInstance;
        private readonly GameBoardController _gameBoardController;
        private readonly Camera2D _camera;
        private SpriteBatch _spriteBatch;
        private AssetManager _assetManager;

        public GameBoardRenderer(GameInstance gameInstance, GameBoardController gameBoardController, Camera2D camera) {
            _gameInstance = gameInstance;
            _gameBoardController = gameBoardController;
            _camera = camera;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            _spriteBatch = batch;
            _assetManager = assetManager;

            DrawBackground();
            if (_gameInstance.TurnManager.CurrentController is PlayerController) {
                DrawHoverPath();
            } else {
                var hexTooFar = _assetManager[AssetManager.HexPathSprite];
                var mouseHex = Camera2D.Instance.MouseHex;
                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    _spriteBatch.Begin(transformMatrix: _camera.Transform);
                    DrawAt(hexTooFar, mouseHex);
                    _spriteBatch.End();
                }
            }
        }

        private void DrawBackground() {
            _spriteBatch.Begin(transformMatrix: _camera.Transform);

            var hexGreen = _assetManager[AssetManager.HexEmptySprite];
            var hexWall = _assetManager[AssetManager.HexWallSprite];

            var map = _gameInstance.Map;

            foreach (var coord in map.AllCoords) {
                if (_gameInstance.Map[coord] == HexType.Empty) {
                    DrawAt(hexGreen, coord);
                } else {
                    DrawAt(hexWall, coord);
                }

                var hexBuffs = map.BuffsAt(coord);

                if (hexBuffs.Count > 0) {
                    foreach (var buff in hexBuffs) {
                        DrawAt(_assetManager[AssetManager.HexHoverSprite], coord, ElementColor(buff.Element));
                    }
                }
            }

            _spriteBatch.End();
        }

        private Color ElementColor(AbilityElement element) {
            switch (element) {
                case AbilityElement.Earth:
                    return Color.Brown;
                case AbilityElement.Fire:
                    return Color.Red;
                case AbilityElement.Air:
                    return Color.LightGray;
                case AbilityElement.Water:
                    return Color.LightBlue;
                default:
                    throw new InvalidOperationException("Invalid element type.");
            }
        }

        private void DrawHoverPath() {
            _spriteBatch.Begin(transformMatrix: _camera.Transform);

            var hexUsable = _assetManager[AssetManager.HexWithinDistance];
            var hexTooFar = _assetManager[AssetManager.HexPathSprite];

            if (_gameInstance.Pathfinder.IsValidCoord(_camera.MouseHex)) {
                IList<AxialCoord> path;
                var mouseMob = _gameInstance.MobManager.AtCoord(_camera.MouseHex);
                if (mouseMob != null) {
                    path = _gameInstance.Pathfinder.PathToMob(mouseMob);
                } else {
                    path = _gameInstance.Pathfinder.PathTo(_camera.MouseHex);
                }


                var currentMob = _gameInstance.TurnManager.CurrentMob;
                var abilityIndex = _gameBoardController.SelectedAbilityIndex;

                if (abilityIndex.HasValue) {
                    var cubepath = _gameInstance.Map.CubeLinedraw(currentMob.Coord, _camera.MouseHex);

                    int distance = 1;
                    bool walled = false;
                    foreach (var cubeCoord in cubepath) {
                        if (!_gameInstance.Pathfinder.IsValidCoord(cubeCoord)) {
                            Utils.Log(LogSeverity.Warning, nameof(GameBoardRenderer),
                                      $"Computed invalid cube visibility path of {cubeCoord}.");
                            continue;
                        }
                        if (_gameInstance.Map[cubeCoord] == HexType.Wall) {
                            walled = true;
                        }

                        if (distance <= currentMob.Abilities[abilityIndex.Value].Range && !walled) {
                            DrawAt(hexUsable, cubeCoord);
                        } else {
                            DrawAt(hexTooFar, cubeCoord);
                        }

                        distance++;
                    }
                } else {
                    foreach (var coord in path) {
                        if (currentMob.Coord.Distance(coord) <= currentMob.Ap) {
                            DrawAt(hexUsable, coord);
                        } else {
                            DrawAt(hexTooFar, coord);
                        }
                    }
                }
            }
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