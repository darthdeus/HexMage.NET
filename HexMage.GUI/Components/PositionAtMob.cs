using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    public class PositionAtMob : Component {
        private readonly Mob _mob;

        public PositionAtMob(Mob mob) {
            _mob = mob;
        }

        public override void Update(GameTime time) {
            base.Update(time);

            Entity.Position = Camera2D.Instance.HexToPixel(_mob.Coord);
        }
    }
}