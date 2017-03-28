using System;
using System.Collections.Generic;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Renderers {
    public enum BoardRenderMode {
        Default,
        HoverHeatmap,
        GlobalHeatmap,
        VisibilityMap
    }

    public class GameBoardRenderer : IRenderer {
        private readonly GameInstance _gameInstance;
        private readonly GameBoardController _gameBoardController;
        private readonly GameEventHub _eventHub;
        private readonly Camera2D _camera;
        private SpriteBatch _spriteBatch;
        private AssetManager _assetManager;
        public BoardRenderMode Mode { get; set; }

        public GameBoardRenderer(GameInstance gameInstance, GameBoardController gameBoardController,
            GameEventHub eventHub, Camera2D camera) {
            _gameInstance = gameInstance;
            _gameBoardController = gameBoardController;
            _eventHub = eventHub;
            _camera = camera;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            _spriteBatch = batch;
            _assetManager = assetManager;

            DrawBackground();
            if (_gameInstance.TurnManager.CurrentController is PlayerController) {
                var currentMob = _gameInstance.TurnManager.CurrentMob;
                if (currentMob.HasValue) {
                    if (Mode == BoardRenderMode.HoverHeatmap) {
                        DrawHoverHeatmap(currentMob.Value);
                    } else if (Mode == BoardRenderMode.GlobalHeatmap) {
                        DrawGlobalHeatmap();
                    } else if (Mode == BoardRenderMode.VisibilityMap) {
                        DrawVisibilityMap(currentMob.Value);
                    }
                }

                if (Mode != BoardRenderMode.VisibilityMap) {
                    DrawHoverPath();
                }
            } else {
                var hexTooFar = _assetManager[AssetManager.HexPathSprite];
                var mouseHex = Camera2D.Instance.MouseHex;
                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: Camera2D.SamplerState);
                    DrawAt(hexTooFar, mouseHex);
                    _spriteBatch.End();
                }
            }

            batch.Begin();
            batch.DrawString(assetManager.Font, $"State: {_eventHub.State}", new Vector2(800, 30), Color.Black);
            batch.End();
        }

        private void DrawVisibilityMap(int currentMob) {
            var from = _gameInstance.State.MobInstances[currentMob].Coord;

            var mouseHex = Camera2D.Instance.MouseHex;
            if (_gameInstance.State.AtCoord(mouseHex).HasValue) {
                from = mouseHex;
            }

            _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: Camera2D.SamplerState);
            foreach (var to in _gameInstance.Map.AllCoords) {
                if (_gameInstance.Map.IsVisible(from, to)) {
                    // TODO - extrahovat hover sprite
                    DrawAt(_assetManager[AssetManager.HexHoverSprite], to, Color.Red * 0.5f);
                }
            }

            _spriteBatch.End();
        }

        private void DrawBackground() {
            _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: Camera2D.SamplerState);

            var hexGreen = _assetManager[AssetManager.HexEmptySprite];
            var hexWall = _assetManager[AssetManager.HexWallSprite];

            var map = _gameInstance.Map;

            foreach (var coord in map.AllCoords) {
                if (_gameInstance.Map[coord] == HexType.Empty) {
                    DrawAt(hexGreen, coord);
                } else {
                    DrawAt(hexWall, coord);
                }

                var pos = _camera.HexToPixel(coord);
                //_spriteBatch.DrawString(_assetManager.Font, _gameInstance.Pathfinder.Distance(coord).ToString(), pos + new Vector2(15, 10),
                //Color.Black);

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

        private void DrawHoverHeatmap(int currentMob) {
            if (_gameInstance.Pathfinder.IsValidCoord(_camera.MouseHex)) {
                var state = _gameInstance.State;
                var mouseMob = state.AtCoord(_camera.MouseHex);

                if (mouseMob.HasValue) {
                    var heatmap = _gameInstance.BuildHeatmap(mouseMob);

                    DrawHeatmap(heatmap);
                }
            }
        }

        private void DrawGlobalHeatmap() {
            var state = _gameInstance.State;

            var mouseMob = state.AtCoord(_camera.MouseHex);
            var heatmap = _gameInstance.BuildHeatmap(mouseMob);

            DrawHeatmap(heatmap);
        }

        private void DrawHeatmap(Heatmap heatmap) {
            _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: Camera2D.SamplerState);
            foreach (var coord in heatmap.Map.AllCoords) {
                if (heatmap.Map[coord] == 0) continue;

                float percent = heatmap.Map[coord] / (float) heatmap.MaxValue / 2 + 0.5f;
                DrawAt(_assetManager[AssetManager.HexHoverSprite], coord, Color.Red * percent * percent);
            }
            _spriteBatch.End();
        }

        private void DrawHoverPath() {
            _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: Camera2D.SamplerState);

            var hexUsable = _assetManager[AssetManager.HexWithinDistance];
            var hexTooFar = _assetManager[AssetManager.HexPathSprite];

            var mouseHex = _camera.MouseHex;

            var currentMob = _gameInstance.TurnManager.CurrentMob;

            if (currentMob.HasValue) {
                var mobInfo = _gameInstance.MobManager.MobInfos[currentMob.Value];
                var mobInstance = _gameInstance.State.MobInstances[currentMob.Value];

                // TODO - tohle smazat, az se prestanou placovat mobove mimo mapu
                if (mouseHex.Distance(AxialCoord.Zero) >= _gameInstance.Size) {
                    // TODO - pouzit util log
                    //Console.WriteLine("Hovering outside of the map, not drawing path");
                    _spriteBatch.End();
                    return;
                }

                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex) &&
                    _gameInstance.Pathfinder.Distance(mobInstance.Coord, mouseHex) != int.MaxValue) {
                    IList<AxialCoord> path;

                    var mouseMob = _gameInstance.State.AtCoord(mouseHex);
                    if (mouseMob != null) {
                        path = _gameInstance.Pathfinder.PathToMob(mobInstance.Coord,
                            _gameInstance.State.MobInstances[mouseMob.Value].Coord);
                    } else {
                        path = _gameInstance.Pathfinder.PathTo(mobInstance.Coord, mouseHex);
                    }

                    var abilityIndex = _gameBoardController.SelectedAbilityIndex;

                    if (abilityIndex.HasValue) {
                        var cubepath = _gameInstance.Map.AxialLinedraw(mobInstance.Coord, mouseHex);

                        int distance = 0;
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

                            var abilityId = mobInfo.Abilities[abilityIndex.Value];
                            var ability = _gameInstance.MobManager.AbilityForId(abilityId);
                            if (distance <= ability.Range && !walled) {
                                DrawAt(hexUsable, cubeCoord);
                            } else {
                                DrawAt(hexTooFar, cubeCoord);
                            }

                            distance++;
                        }
                    } else {
                        if (_gameInstance.Pathfinder.IsValidCoord(mouseHex) &&
                            _gameInstance.Map[mouseHex] != HexType.Wall) {
                            if (!mouseMob.HasValue || mouseMob.Value != currentMob.Value) {
                                foreach (var coord in path) {
                                    if (_gameInstance.Pathfinder.Distance(mobInstance.Coord, coord) <= mobInstance.Ap) {
                                        DrawAt(hexUsable, coord);
                                    } else {
                                        DrawAt(hexTooFar, coord);
                                    }
                                }
                            }
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