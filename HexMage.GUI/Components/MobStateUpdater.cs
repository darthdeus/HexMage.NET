using System;
using HexMage.Simulator;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    public class MobStateUpdater : Component {
        private readonly Mob _mob;
        private Camera2D _camera;
        private InputManager _inputManager;


        public MobStateUpdater(Mob mob) {
            _mob = mob;
        }

        public override void Initialize(AssetManager assetManager) {
            base.Initialize(assetManager);
            _camera = Camera2D.Instance;
            _inputManager = InputManager.Instance;
        }

        public override void Update(GameTime time) {
            base.Update(time);

            if (_mob.Coord.Equals(_camera.MouseHex)) {}
        }
    }
}