﻿using HexMage.Simulator;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
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