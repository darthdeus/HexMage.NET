using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Components {
    public class Animation {
        private readonly string _spritesheet;
        private readonly float _frameTime;
        private readonly int _totalFrames;

        public Animation(string spritesheet, float frameTime, int totalFrames) {
            _spritesheet = spritesheet;
            _frameTime = frameTime;
            _totalFrames = totalFrames;
        }
    }

    public class MobRenderer : IRenderer {
        // The currently rendered animation frame
        public int AnimationFrame { get; set; } = 0;
        public readonly int TotalFrames = 14;

        private readonly Mob _mob;
        private readonly int _frameSize = 32;

        public MobRenderer(Mob mob) {
            _mob = mob;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var pos = Camera2D.Instance.HexToPixel(_mob.Coord);

            var spriteRect = new Rectangle(_frameSize * AnimationFrame, 0, _frameSize, _frameSize);
            batch.Draw(assetManager[AssetManager.DarkMage], pos, spriteRect, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f );
            //batch.Draw(assetManager[AssetManager.DarkMage], pos);

            // TODO - extract this out
            var gray = assetManager[AssetManager.GrayTexture];

            var hbPos = pos.ToPoint() + new Point(27, 4);

            double hpPercent = (double) _mob.HP/_mob.MaxHP;
            int healthbarHeight = 20;
            batch.Draw(gray, new Rectangle(hbPos, new Point(5, healthbarHeight)), Color.DarkGreen);
            batch.Draw(gray, new Rectangle(hbPos, new Point(5, (int) (healthbarHeight*hpPercent))),
                Color.Yellow);
        }
    }
}