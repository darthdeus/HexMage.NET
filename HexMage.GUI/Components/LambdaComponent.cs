using System;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    /// <summary>
    /// For internal usage only, wraps a lambda function into a component.
    /// </summary>
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