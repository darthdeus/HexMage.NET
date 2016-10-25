using System;
using HexMage.GUI.Renderers;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    public class MobAnimationController : Component {
        private MobEntity _mobEntity;
        private int _mobId;
        private GameInstance _gameInstance;

        private TimeSpan _time;
        private Animation _animationClicked;
        private Animation _animationIdle;

        public Animation CurrentAnimation { get; set; }

        public MobAnimationController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public override void Initialize(AssetManager assetManager) {
            base.Initialize(assetManager);

            _mobEntity = (MobEntity) Entity;
            _mobId = _mobEntity.MobId;

            const int idleFrameCount = 2;
            _animationIdle = new Animation(AssetManager.DarkMageIdle, TimeSpan.FromMilliseconds(500),
                                           AssetManager.TileSize, idleFrameCount);

            const int clickedFrameCount = 14;
            _animationClicked = new Animation(AssetManager.DarkMageClicked, TimeSpan.FromMilliseconds(70),
                                              AssetManager.TileSize, clickedFrameCount);
            _animationClicked.AnimationDone += () => CurrentAnimation = _animationIdle;

            CurrentAnimation = _animationIdle;
        }

        public override void Update(GameTime time) {
            base.Update(time);

            var mouseHex = Camera2D.Instance.MouseHex;
            if (_gameInstance.MobManager.MobInstances[_mobId].Coord.Equals(mouseHex)) {
                SwitchAnimation(_animationClicked);
            }

            _time += time.ElapsedGameTime;

            if (_time > CurrentAnimation.FrameTime) {
                CurrentAnimation.NextFrame();
                _time = TimeSpan.Zero;
            }
        }

        private void SwitchAnimation(Animation newAnimation) {
            if (CurrentAnimation != newAnimation) {
                _time = TimeSpan.Zero;
                CurrentAnimation.Reset();
                newAnimation.Reset();
                CurrentAnimation = newAnimation;
            }
        }
    }
}