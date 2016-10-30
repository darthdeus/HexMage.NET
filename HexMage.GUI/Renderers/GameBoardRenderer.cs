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
        GlobalHeatmap
    }

    public class GameBoardRenderer : IRenderer {
        private readonly GameInstance _gameInstance;
        private readonly GameBoardController _gameBoardController;
        private readonly Camera2D _camera;
        private SpriteBatch _spriteBatch;
        private AssetManager _assetManager;
        public BoardRenderMode Mode { get; set; }

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
                var currentMob = _gameInstance.TurnManager.CurrentMob;
                if (currentMob.HasValue) {
                    if (Mode == BoardRenderMode.HoverHeatmap) {
                        DrawHoverHeatmap(currentMob.Value);
                    } else if (Mode == BoardRenderMode.GlobalHeatmap) {
                        DrawGlobalHeatmap(currentMob.Value);
                    }
                }


                DrawHoverPath();
            } else {
                var hexTooFar = _assetManager[AssetManager.HexPathSprite];
                var mouseHex = Camera2D.Instance.MouseHex;
                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: Camera2D.SamplerState);
                    DrawAt(hexTooFar, mouseHex);
                    _spriteBatch.End();
                }
            }
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
                var mobManager = _gameInstance.MobManager;
                var mouseMob = mobManager.AtCoord(_camera.MouseHex);

                if (mouseMob.HasValue) {
                    var heatmap = new HexMap<int>(_gameInstance.Size);
                    var mobInfo = mobManager.MobInfos[mouseMob.Value];
                    var mobInstance = mobManager.MobInstances[mouseMob.Value];

                    int maxDmg = 0;

                    foreach (var coord in heatmap.AllCoords) {
                        int maxAbilityDmg = 0;

                        foreach (var abilityId in mobInfo.Abilities) {
                            var abilityInfo = mobManager.AbilityForId(abilityId);

                            if (coord.Distance(mobInstance.Coord) <= abilityInfo.Range &&
                                abilityInfo.Cost <= mobInstance.Ap) {
                                if (abilityInfo.Dmg > maxAbilityDmg) {
                                    maxAbilityDmg = abilityInfo.Dmg;
                                }
                            }
                        }

                        heatmap[coord] += maxAbilityDmg;

                        if (heatmap[coord] > maxDmg) maxDmg = heatmap[coord];
                    }

                    _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: Camera2D.SamplerState);
                    foreach (var coord in heatmap.AllCoords) {
                        float percent = heatmap[coord]/(float) maxDmg;
                        DrawAt(_assetManager[AssetManager.HexHoverSprite], coord, Color.Red*percent*percent);
                    }
                    _spriteBatch.End();
                }
            }
        }


        private void DrawGlobalHeatmap(int currentMob) {
            var mobManager = _gameInstance.MobManager;
            var heatmap = new HexMap<int>(_gameInstance.Size);
            int maxDmg = 0;

            foreach (var mobId in mobManager.Mobs) {
                var playerTeam = mobManager.MobInfos[currentMob].Team;
                var mobInfo = mobManager.MobInfos[mobId];
                var mobInstance = mobManager.MobInstances[mobId];

                if (playerTeam != mobInfo.Team) {
                    foreach (var coord in heatmap.AllCoords) {
                        int maxAbilityDmg = 0;
                        foreach (var abilityId in mobInfo.Abilities) {
                            var abilityInfo = mobManager.AbilityForId(abilityId);

#warning TODO - resit cooldowny
                            if (coord.Distance(mobInstance.Coord) <= abilityInfo.Range &&
                                abilityInfo.Cost <= mobInstance.Ap) {
                                if (abilityInfo.Dmg > maxAbilityDmg) {
                                    maxAbilityDmg = abilityInfo.Dmg;
                                }
                            }
                        }

                        heatmap[coord] += maxAbilityDmg;
                        if (heatmap[coord] > maxDmg) maxDmg = heatmap[coord];
                    }
                }
            }

            _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: Camera2D.SamplerState);
            foreach (var coord in heatmap.AllCoords) {
                float percent = heatmap[coord]/(float) maxDmg;
                DrawAt(_assetManager[AssetManager.HexHoverSprite], coord, Color.Red*percent*percent);
            }
            _spriteBatch.End();
        }


        private void DrawHoverPath() {
            _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: Camera2D.SamplerState);

            var hexUsable = _assetManager[AssetManager.HexWithinDistance];
            var hexTooFar = _assetManager[AssetManager.HexPathSprite];

            if (_gameInstance.TurnManager.CurrentMob.HasValue && _gameInstance.Pathfinder.IsValidCoord(_camera.MouseHex) &&
                _gameInstance.Pathfinder.Distance(_camera.MouseHex) != int.MaxValue) {
                IList<AxialCoord> path;

                var mouseMob = _gameInstance.MobManager.AtCoord(_camera.MouseHex);
                if (mouseMob != null) {
                    path =
                        _gameInstance.Pathfinder.PathToMob(_gameInstance.MobManager.MobInstances[mouseMob.Value].Coord);
                } else {
                    path = _gameInstance.Pathfinder.PathTo(_camera.MouseHex);
                }

                var currentMob = _gameInstance.TurnManager.CurrentMob;
                var abilityIndex = _gameBoardController.SelectedAbilityIndex;

                if (abilityIndex.HasValue) {
                    var mobInfo = _gameInstance.MobManager.MobInfos[currentMob.Value];
                    var mobInstance = _gameInstance.MobManager.MobInstances[currentMob.Value];

                    var cubepath = _gameInstance.Map.AxialLinedraw(mobInstance.Coord, _camera.MouseHex);

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
                    if (_gameInstance.Map[_camera.MouseHex] != HexType.Wall) {
                        if (!mouseMob.HasValue || mouseMob.Value != currentMob.Value) {
                            var mobInstance = _gameInstance.MobManager.MobInstances[currentMob.Value];
                            foreach (var coord in path) {
                                if (_gameInstance.Pathfinder.Distance(coord) <= mobInstance.Ap) {
                                    DrawAt(hexUsable, coord);
                                } else {
                                    DrawAt(hexTooFar, coord);
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