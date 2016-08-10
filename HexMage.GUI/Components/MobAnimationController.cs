using System;
using HexMage.Simulator;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    public class MobAnimationController : Component {
        private MobEntity _mobEntity;
        private Mob _mob;
        private MobRenderer _mobRenderer;

        public override void Initialize(AssetManager assetManager) {
            base.Initialize(assetManager);

            _mobEntity = (MobEntity)Entity;
            _mob = _mobEntity.Mob;
            _mobRenderer = (MobRenderer)_mobEntity.Renderer;
        }

        private double _time;
        private double _frameTime = 0.1f;

        public override void Update(GameTime time) {
            base.Update(time);
            _time += time.ElapsedGameTime.TotalSeconds;

            if (_time > _frameTime) {
                _mobRenderer.AnimationFrame++;
                _time = 0;
            }

            _mobRenderer.AnimationFrame %= _mobRenderer.TotalFrames;
        }
    }
}