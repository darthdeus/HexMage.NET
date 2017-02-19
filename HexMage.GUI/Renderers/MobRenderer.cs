using System;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Renderers {
    public class MobRenderer : IRenderer {
        private readonly GameInstance _gameInstance;
        private readonly int _mobId;
        private readonly MobAnimationController _animationController;

        private readonly int _healthbarWidth = (int) (1.0/6*AssetManager.TileSize);
        private readonly int _healthbarHeight = (int) (5.0/8*AssetManager.TileSize);

        private readonly Point _healthbarOffset = new Point((int) (9.0/10*AssetManager.TileSize),
                                                            (int) (1.0/7*AssetManager.TileSize));

        public MobRenderer(GameInstance gameInstance, int mobId, MobAnimationController animationController) {
            _gameInstance = gameInstance;
            _mobId = mobId;
            _animationController = animationController;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var pos = entity.RenderPosition;

            if (_gameInstance.TurnManager.CurrentMob == _mobId) {
                batch.Draw(assetManager[AssetManager.HexHoverSprite], pos, Color.White);
            }

            var mobInfo = _gameInstance.MobManager.MobInfos[_mobId];
            var mobInstance = _gameInstance.State.MobInstances[_mobId];

            var color = mobInfo.Team == TeamColor.Red ? Color.OrangeRed : Color.Blue;
            if (mobInstance.Hp > 0) {
                _animationController.CurrentAnimation.RenderFrame(null, pos, color, batch, assetManager);

                var hbPos = pos.ToPoint() + _healthbarOffset;
                var gray = assetManager[AssetManager.SolidGrayColor];
                batch.Draw(gray, new Rectangle(hbPos.X, hbPos.Y, 55, 40), new Color(0.7f, 0.6f, 0.5f));

                DrawHealthbar((double) mobInstance.Hp/mobInfo.MaxHp,
                              batch, assetManager, hbPos, Color.DarkGreen, Color.LightGreen);

                var apPos = hbPos + new Point(_healthbarWidth, 0);
                DrawHealthbar((double) mobInstance.Ap/mobInfo.MaxAp,
                              batch, assetManager, apPos, Color.DarkBlue, Color.LightBlue);

                const int textOffset = 20;

                batch.DrawString(assetManager.Font, $"{mobInstance.Hp}/{mobInfo.MaxHp}",
                                 hbPos.ToVector2() + new Vector2(textOffset, 0),
                                 Color.Black);
                batch.DrawString(assetManager.Font, $"{mobInstance.Ap}/{mobInfo.MaxAp}",
                                 hbPos.ToVector2() + new Vector2(textOffset, 14),
                                 Color.Black);
            } else {
                batch.Draw(assetManager[AssetManager.DarkMageDeath], pos, color);
            }
        }

        private void DrawHealthbar(double percentage, SpriteBatch batch, AssetManager assetManager, Point pos,
                                   Color emptyColor, Color fullColor) {
            var gray = assetManager[AssetManager.SolidGrayColor];

            batch.Draw(gray, new Rectangle(pos, new Point(_healthbarWidth, _healthbarHeight)), emptyColor);

            var percentageHeight = (int) (_healthbarHeight*percentage);
            batch.Draw(gray,
                       new Rectangle(pos + new Point(0, _healthbarHeight - percentageHeight),
                                     new Point(_healthbarWidth, percentageHeight)),
                       fullColor);
        }
    }
}