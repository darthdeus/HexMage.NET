using System;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    internal class HoverUpdater : Component {
        private readonly Action<bool> _action;

        public HoverUpdater(Action<bool> action) {
            _action = action;
        }

        public override void Update(GameTime time) {
            base.Update(time);

            _action(Entity.AABB.Contains(InputManager.Instance.MousePosition));
        }
    }
}