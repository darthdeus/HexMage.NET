using System;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    /// <summary>
    /// Handles animation frame updating for a given animation
    /// </summary>
    public class AnimationController : Component {
        private readonly Animation _animation;
        public AnimationController(Animation animation) {
            _animation = animation;
        }

        private TimeSpan _time;

        public override void Update(GameTime time) {
            base.Update(time);

            _time += time.ElapsedGameTime;

            if (_time > _animation.FrameTime) {
                _animation.NextFrame();
                _time = TimeSpan.Zero;                
            }
        }
    }
}