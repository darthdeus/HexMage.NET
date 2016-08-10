using System;
using System.Timers;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Components {
    public class ProjectileEntity : Entity {
        private readonly TimeSpan _duration;
        private readonly AxialCoord _source;
        private readonly AxialCoord _destination;

        public event Action TargetHit;

        public ProjectileEntity(TimeSpan duration, AxialCoord source, AxialCoord destination) {
            _duration = duration;
            _source = source;
            _destination = destination;
        }

        private bool _initialized = false;
        private TimeSpan _startTime;
        private TimeSpan _elapsedTime = TimeSpan.Zero;
        private Vector2 _sourceWorld;
        private Vector2 _dstWorld;
        private Vector2 _directionWorld;

        protected override void Update(GameTime time) {
            base.Update(time);

            if (!_initialized) {
                _initialized = true;
                _startTime = time.TotalGameTime;
            }

            float percent = (float) (_elapsedTime.TotalMilliseconds/_duration.TotalMilliseconds);

            if (percent > 0.99f) {
                // TODO - destroy the entity and all of its components :)
                Active = false;
                TargetHit?.Invoke();
            }

            _sourceWorld = Camera2D.Instance.HexToPixel(_source);
            _dstWorld = Camera2D.Instance.HexToPixel(_destination);
            _directionWorld = _dstWorld - _sourceWorld;

            Vector2 normDir = _directionWorld;
            normDir.Normalize();

            var acos = Vector2.Dot(new Vector2(1, 0), normDir);
            double flip = 1;
            if (normDir.Y < 0) flip = -1;

            Rotation = (float) (flip * Math.Acos(acos));

            var originOffset = new Vector2(16, 16);
            Position = _sourceWorld + _directionWorld*percent + originOffset;

            _elapsedTime = time.TotalGameTime - _startTime;
        }
    }
}