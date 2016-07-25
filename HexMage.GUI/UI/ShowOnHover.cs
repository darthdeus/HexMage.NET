using Microsoft.Xna.Framework;

namespace HexMage.GUI.UI {
    public class ShowOnHover : Component {
        private readonly Entity _entityToShow;

        public ShowOnHover(Entity entityToShow) {
            _entityToShow = entityToShow;
        }

        public override void Update(GameTime time) {
            var pos = InputManager.Instance.MousePosition;
            if (Entity.AABB.Contains(pos)) {
                _entityToShow.Active = true;
                _entityToShow.Position = pos.ToVector2();
            } else {
                _entityToShow.Active = false;
            }
        }
    }
}