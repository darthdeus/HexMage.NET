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
        private readonly Mob _mob;
        private readonly MobAnimationController _animationController;

        private readonly int _healthbarWidth = (int) (1.0/6*AssetManager.TileSize);
        private readonly int _healthbarHeight = (int) (5.0/8*AssetManager.TileSize);

        private readonly Point _healthbarOffset = new Point((int) (9.0/10*AssetManager.TileSize),
                                                            (int) (1.0/7*AssetManager.TileSize));

        public MobRenderer(GameInstance gameInstance, Mob mob, MobAnimationController animationController) {
            _gameInstance = gameInstance;
            _mob = mob;
            _animationController = animationController;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var mobEntity = (MobEntity) _mob.Metadata;

#warning TODO - why isn't this entity.RenderPosition?
            var pos = entity.Position;

            if (_gameInstance.TurnManager.CurrentMob == _mob) {
                batch.Draw(assetManager[AssetManager.HexHoverSprite], pos, Color.White);
            }

            var color = _mob.Team == TeamColor.Red ? Color.OrangeRed : Color.Blue;
            if (_mob.Hp > 0) {
                _animationController.CurrentAnimation.RenderFrame(mobEntity, pos, color, batch, assetManager);

                var hbPos = pos.ToPoint() + _healthbarOffset;
                DrawHealthbar((double) _mob.Hp/_mob.MaxHp,
                              batch, assetManager, hbPos, Color.DarkGreen, Color.LightGreen);

                var apPos = hbPos + new Point(_healthbarWidth, 0);
                DrawHealthbar((double) _mob.Ap/_mob.MaxAp,
                              batch, assetManager, apPos, Color.DarkBlue, Color.LightBlue);
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