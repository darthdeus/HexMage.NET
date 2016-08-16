using HexMage.Simulator;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    public class MobEntity : Entity {
        private readonly GameInstance _gameInstance;
        private float _moveProgress;
        private AxialCoord _destination;
        private AxialCoord _source;
        private bool _animateMovement;
        public Mob Mob { get; set; }

        public MobEntity(Mob mob, GameInstance gameInstance) {
            _gameInstance = gameInstance;
            Mob = mob;
        }

        protected override void Update(GameTime time) {
            base.Update(time);

            var camera = Camera2D.Instance;

            if (_animateMovement) {
                var sourcePos = camera.HexToPixel(_source);
                var destinationPos = camera.HexToPixel(_destination);

                // TODO - do this using proper fixed timestep, rather than per-frame percentage
                _moveProgress += 0.03f;

                if (_moveProgress >= 1.0f) {
                    _moveProgress = 1.0f;
                    _animateMovement = false;
                }

                Position = sourcePos + _moveProgress*(destinationPos - sourcePos);
            } else {
                Position = camera.HexToPixel(Mob.Coord);
            }
        }

        public void MoveTo(AxialCoord coord) {
            
            _moveProgress = 0.0f;
            _destination = coord;
            _source = Mob.Coord;
            _animateMovement = true;
        }
    }
}