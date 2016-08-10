using System;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

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

        public void RenderFrame(Vector2 position, SpriteBatch batch, AssetManager manager) {
            var sourceRectangle = new Rectangle(_frameSize*_currentFrame, 0, _frameSize, _frameSize);
            batch.Draw(manager[_spritesheet], position, sourceRectangle,
                Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
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

    public class MobAnimationController : Component {
        private MobEntity _mobEntity;
        private Mob _mob;
        private MobRenderer _mobRenderer;

        private double _time;
        private Animation _animationClicked;
        private Animation _animationIdle;

        public Animation CurrentAnimation { get; set; }

        public override void Initialize(AssetManager assetManager) {
            base.Initialize(assetManager);

            _mobEntity = (MobEntity) Entity;
            _mob = _mobEntity.Mob;
            _mobRenderer = (MobRenderer) _mobEntity.Renderer;

            _animationIdle = new Animation(AssetManager.DarkMageIdle, 0.5f, 32, 2);

            _animationClicked = new Animation(AssetManager.DarkMageClicked, 0.07f, 32, 14);
            _animationClicked.AnimationDone += () => CurrentAnimation = _animationIdle;

            CurrentAnimation = _animationIdle;
        }

        public override void Update(GameTime time) {
            base.Update(time);

            var mouseHex = Camera2D.Instance.MouseHex;
            if (_mob.Coord.Equals(mouseHex)) {
                SwitchAnimation(_animationClicked);
            }

            _time += time.ElapsedGameTime.TotalSeconds;

            if (_time > CurrentAnimation.FrameTime) {
                CurrentAnimation.NextFrame();
                _time = 0;
            }
        }

        private void SwitchAnimation(Animation newAnimation) {
            if (CurrentAnimation != newAnimation) {
                _time = 0;
                CurrentAnimation.Reset();
                newAnimation.Reset();
                CurrentAnimation = newAnimation;
            }
        }
    }
}