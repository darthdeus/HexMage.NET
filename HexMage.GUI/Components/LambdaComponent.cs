using System;
using HexMage.GUI.Components;
using Microsoft.Xna.Framework;

namespace HexMage.GUI {
    public class LambdaComponent : Component {
        private readonly Action<GameTime> _updateFunc;
        public LambdaComponent(Action<GameTime> updateFunc) {
            _updateFunc = updateFunc;
        }

        public override void Update(GameTime time) {
            base.Update(time);
            _updateFunc(time);
        }
    }
}