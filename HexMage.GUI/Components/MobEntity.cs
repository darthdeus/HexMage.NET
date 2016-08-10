using HexMage.Simulator;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    public class MobEntity : Entity {
        private readonly GameInstance _gameInstance;
        public Mob Mob { get; set; }

        public MobEntity(Mob mob, GameInstance gameInstance) {
            _gameInstance = gameInstance;
            Mob = mob;
        }

        protected override void Update(GameTime time) {
            base.Update(time);
        }
    }
}