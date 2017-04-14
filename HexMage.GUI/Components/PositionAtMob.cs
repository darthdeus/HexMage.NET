using HexMage.GUI.Core;
using HexMage.Simulator;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    public class PositionAtMob : Component {
        private readonly int _mobId;
        private readonly GameInstance _gameInstance;

        public PositionAtMob(int mobId, GameInstance gameInstance) {
            _mobId = mobId;
            _gameInstance = gameInstance;
        }

        public override void Update(GameTime time) {
            base.Update(time);

            Entity.Position = Camera2D.Instance.HexToPixel(_gameInstance.State.MobInstances[_mobId].Coord);
        }
    }
}