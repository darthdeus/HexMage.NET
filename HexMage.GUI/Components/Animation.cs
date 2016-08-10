using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Components {
    public class Animation {
        private readonly string _spritesheet;
        public readonly float FrameTime;
        private readonly int _frameSize;
        private readonly int _totalFrames;
        private int _currentFrame = 0;
        public event Action AnimationDone;

        public Animation(string spritesheet, float frameTime, int frameSize, int totalFrames) {
            _spritesheet = spritesheet;
            FrameTime = frameTime;
            _frameSize = frameSize;
            _totalFrames = totalFrames;
        }

        public void RenderFrame(Vector2 position, Color color, SpriteBatch batch, AssetManager manager) {
            var sourceRectangle = new Rectangle(_frameSize*_currentFrame, 0, _frameSize, _frameSize);
            batch.Draw(manager[_spritesheet], position, sourceRectangle,
                color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
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
    }
}