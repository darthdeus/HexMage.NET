using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Components {
    public class MobRenderer : IRenderer {
        // The currently rendered animation frame

        private readonly Mob _mob;
        private readonly MobAnimationController _animationController;

        public MobRenderer(Mob mob, MobAnimationController animationController) {
            _mob = mob;
            _animationController = animationController;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var pos = Camera2D.Instance.HexToPixel(_mob.Coord);

            _animationController.CurrentAnimation.RenderFrame(pos, batch, assetManager);

            // TODO - extract this out
            var gray = assetManager[AssetManager.GrayTexture];

            var hbPos = pos.ToPoint() + new Point(29, 4);

            double hpPercent = (double) _mob.HP/_mob.MaxHP;
            int healthbarHeight = 20;
            batch.Draw(gray, new Rectangle(hbPos, new Point(5, healthbarHeight)), Color.DarkGreen);
            batch.Draw(gray, new Rectangle(hbPos, new Point(5, (int) (healthbarHeight*hpPercent))),
                Color.Yellow);
        }
    }
}