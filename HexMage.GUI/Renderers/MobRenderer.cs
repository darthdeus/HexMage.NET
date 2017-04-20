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
        private readonly GameInstance _game;
        private readonly int _mobId;
        private readonly MobAnimationController _animationController;

        private const int _healthbarWidth = (int) (1.0 / 6 * AssetManager.TileSize);
        private const int _healthbarHeight = (int) (5.0 / 8 * AssetManager.TileSize);

        private readonly Point _healthbarOffset = new Point((int) (7.5 / 10 * AssetManager.TileSize),
                                                            (int) (1.0 / 7 * AssetManager.TileSize));

        public MobRenderer(GameInstance game, int mobId, MobAnimationController animationController) {
            _game = game;
            _mobId = mobId;
            _animationController = animationController;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var pos = entity.RenderPosition;

            if (_game.CurrentMob == _mobId) {
                batch.Draw(assetManager[AssetManager.HexHoverSprite], pos, Color.White);
            }

            var mobInfo = _game.MobManager.MobInfos[_mobId];
            var mobInstance = _game.State.MobInstances[_mobId];

            var color = mobInfo.Team == TeamColor.Red ? Color.OrangeRed : Color.Blue;
            if (mobInstance.Hp > 0) {
                var mobOffset = new Vector2(-5, -3);

                _animationController.CurrentAnimation.RenderFrame(null, pos + mobOffset, color, batch, assetManager);

                var hbPos = pos.ToPoint() + _healthbarOffset;

                DrawHealthbar((double) mobInstance.Hp / mobInfo.MaxHp,
                              batch, assetManager, hbPos, Color.DarkGreen, Color.LightGreen);

                var apPos = hbPos + new Point(_healthbarWidth, 0);
                DrawHealthbar((double) mobInstance.Ap / mobInfo.MaxAp,
                              batch, assetManager, apPos, Color.DarkBlue, Color.LightBlue);

                const int textOffset = 20;

                float hpPercentage = mobInstance.Hp / (float) mobInfo.MaxHp;
                float apPercentage = mobInstance.Ap / (float) mobInfo.MaxAp;

                batch.DrawString(assetManager.AbilityFontSmall, $"{mobInstance.Hp}",
                                 hbPos.ToVector2() + new Vector2(-2, 24),
                                 hpPercentage < 0.2 ? Color.White : Color.Black);
                batch.DrawString(assetManager.AbilityFontSmall, $"{mobInstance.Ap}",
                                 hbPos.ToVector2() + new Vector2(11, 19),
                                 apPercentage < 0.38 ? Color.White : Color.Black);
            } else {
                batch.Draw(assetManager[AssetManager.DarkMageDeath], pos, color);
            }
        }

        private void DrawHealthbar(double percentage, SpriteBatch batch, AssetManager assetManager, Point pos,
                                   Color emptyColor, Color fullColor) {
            var gray = assetManager[AssetManager.SolidGrayColor];

            batch.Draw(gray, new Rectangle(pos, new Point(_healthbarWidth, _healthbarHeight)), emptyColor);

            var percentageHeight = (int) (_healthbarHeight * percentage);
            batch.Draw(gray,
                       new Rectangle(pos + new Point(0, _healthbarHeight - percentageHeight),
                                     new Point(_healthbarWidth, percentageHeight)),
                       fullColor);
        }
    }
}