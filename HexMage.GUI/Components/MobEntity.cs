using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using HexMage.GUI.Core;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    public class MobEntity : Entity {
        private readonly GameInstance _gameInstance;
        private float _moveProgress;
        private AxialCoord _destination;
        private AxialCoord _source;
        private bool _animateMovement;
        public int MobId { get; set; }

        public MobEntity(int mobId, GameInstance gameInstance) {
            _gameInstance = gameInstance;
            MobId = mobId;
        }

        protected override void Update(GameTime time) {
            base.Update(time);

            var camera = Camera2D.Instance;

            if (_animateMovement) {
                Debug.Assert(_tcs != null, "MobEntity.TaskCompletionSource hasn't been properly initialized.");
                var sourcePos = camera.HexToPixel(_source);
                var destinationPos = camera.HexToPixel(_destination);

                // TODO - do this using proper fixed timestep, rather than per-frame percentage
                _moveProgress += 0.06f;

                if (_moveProgress >= 1.0f) {
                    _moveProgress = 1.0f;
                    _animateMovement = false;
                    Console.WriteLine("Move finished");
                    _tcs.SetResult(true);
                    _tcs = null;
                }

                Position = sourcePos + _moveProgress*(destinationPos - sourcePos);
            } else {
                var posBefore = Position;

                var coord = _gameInstance.MobManager.MobInstanceForId(MobId).Coord;
                Position = camera.HexToPixel(coord);
            }
        }

        private TaskCompletionSource<bool> _tcs;

        public Task<bool> MoveTo(AxialCoord source, AxialCoord destination) {
            Debug.Assert(!_animateMovement, "Movement already in progress, can't move until it finishes.");
            Debug.Assert(_tcs == null, "_tcs != null when trying to re-initialize it.");

            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _moveProgress = 0.0f;
            Console.WriteLine($"Moving from {source} to {destination}");
            _destination = destination;
            _source = source;
            _animateMovement = true;

            return _tcs.Task;
        }
    }
}