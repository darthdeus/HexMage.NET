using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Components {
    public class MobRenderer : IRenderer {
        // The currently rendered animation frame

        private readonly GameInstance _gameInstance;
        private readonly Mob _mob;
        private readonly MobAnimationController _animationController;

        public MobRenderer(GameInstance gameInstance, Mob mob, MobAnimationController animationController) {
            _gameInstance = gameInstance;
            _mob = mob;            
            _animationController = animationController;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var mobEntity = (MobEntity)_mob.Metadata;

            var camera = Camera2D.Instance;
            var pos = camera.HexToPixel(_mob.Coord);

            if (_gameInstance.TurnManager.CurrentMob == _mob) {
                batch.Draw(assetManager[AssetManager.HoverSprite], pos, Color.White);
            }

            if (_gameInstance.TurnManager.CurrentTarget == _mob) {
                batch.Draw(assetManager[AssetManager.TargetSprite], pos, Color.White);
            }

            var color = _mob.Team.Color == TeamColor.Red ? Color.OrangeRed : Color.Blue;
            _animationController.CurrentAnimation.RenderFrame(mobEntity, pos, color, batch, assetManager);

            // TODO - extract this out
            var gray = assetManager[AssetManager.GraySprite];

            var hbPos = pos.ToPoint() + new Point(29, 4);

            double hpPercent = (double) _mob.HP/_mob.MaxHP;
            int healthbarHeight = 20;
            batch.Draw(gray, new Rectangle(hbPos, new Point(5, healthbarHeight)), Color.DarkGreen);
            batch.Draw(gray, new Rectangle(hbPos, new Point(5, (int) (healthbarHeight*hpPercent))),
                Color.Yellow);
        }

    }
}