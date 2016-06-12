using System;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI
{
    internal class ArenaScene : GameScene
    {
        private readonly GameInstance _gameInstance = new GameInstance(20);
        private SpriteBatch _spriteBatch;
        private Camera2D _camera;
        private AssetManager _assetManager;
        private InputManager _inputManager;
        private GameScene _gameSceneImplementation;

        public ArenaScene() {
            var t1 = _gameInstance.MobManager.AddTeam();
            var t2 = _gameInstance.MobManager.AddTeam();

            for (int team = 0; team < 2; team++) {
                for (int mobI = 0; mobI < 5; mobI++) {
                    var mob = Generator.RandomMob(team%2 == 0 ? t1 : t2, _gameInstance.Size,
                        c => _gameInstance.MobManager.AtCoord(c) == null);

                    _gameInstance.MobManager.AddMob(mob);
                }
            }

            _gameInstance.TurnManager.StartNextTurn();
        }

        private void DrawBackground() {
            _spriteBatch.Begin(transformMatrix: _camera.Projection);

            int maxX = Int32.MinValue;
            int maxY = Int32.MinValue;
            int maxZ = Int32.MinValue;

            int minX = Int32.MaxValue;
            int minY = Int32.MaxValue;
            int minZ = Int32.MaxValue;

            var hexGreen = _assetManager[AssetManager.EmptyHexTexture];
            var hexWall = _assetManager[AssetManager.WallTexture];

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

            _spriteBatch.DrawString(_assetManager.Font, $"{minX},{minY},{minZ}   {maxX},{maxY},{maxZ}",
                new Vector2(0, 50),
                Color.Red);
            _spriteBatch.End();
        }

        private void DrawHoverPath() {
            _spriteBatch.Begin(transformMatrix: _camera.Projection);

            var hexPath = _assetManager[AssetManager.PathTexture];

            if (_gameInstance.Pathfinder.IsValidCoord(_camera.MouseHex)) {
                var path = _gameInstance.Pathfinder.PathTo(_camera.MouseHex);

                foreach (var coord in path) {
                    DrawAt(hexPath, coord);
                }
            }
            _spriteBatch.End();
        }

        private void DrawAllMobs() {
            _spriteBatch.Begin(transformMatrix: _camera.Projection);
            foreach (var mob in _gameInstance.MobManager.Mobs) {
                DrawAt(_assetManager[AssetManager.MobTexture], mob.Coord);
            }
            _spriteBatch.End();
        }

        private void DrawMousePosition() {
            _spriteBatch.Begin();
                var mouseTextPos = new Vector2(0, 850);

                var mousePos = Vector2.Transform(_inputManager.MousePosition.ToVector2(),
                    Matrix.Invert(_camera.Projection));

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

        public override void Draw(GameTime gameTime) {
            var gui = new ImGui(_inputManager, _assetManager.Font);

            gui.Draw(_assetManager[AssetManager.GrayTexture], _spriteBatch);
        }
    }
}