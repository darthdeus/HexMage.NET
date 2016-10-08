using System;
using HexMage.GUI.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Components {
    public class Animation {
        private readonly string _spritesheet;
        public readonly TimeSpan FrameTime;
        private readonly int _frameSize;
        private readonly int _totalFrames;
        private int _currentFrame = 0;
        public Vector2 Origin = Vector2.Zero;
        public event Action AnimationDone;

        public Animation(string spritesheet, TimeSpan frameTime, int frameSize, int totalFrames) {
            _spritesheet = spritesheet;
            FrameTime = frameTime;
            _frameSize = frameSize;
            _totalFrames = totalFrames;
        }

        public void NextFrame() {
            _currentFrame++;
            if (_currentFrame == _totalFrames) {
                _currentFrame = 0;
                AnimationDone?.Invoke();
            }
        }

        public void Reset() {
            _currentFrame = 0;
        }

        public void RenderFrame(Entity entity, Vector2 position, Color color, SpriteBatch batch, AssetManager manager) {
            var sourceRectangle = new Rectangle(_frameSize*_currentFrame, 0, _frameSize, _frameSize);

            batch.Draw(
                manager[_spritesheet],
                position,
                sourceRectangle,
                color,
                entity?.Rotation ?? 0,
                Origin,
                Vector2.One,
                SpriteEffects.None,
                0);
        }


        public void RenderFrame(Entity entity, Vector2 position, SpriteBatch batch, AssetManager assetManager) {
            RenderFrame(entity, position, Color.White, batch, assetManager);
        }
    }
}