using HexMage.Simulator;

namespace HexMage.GUI.Components {
    public class MobEntity : Entity {
        public Mob Mob { get; set; }

        public MobEntity(Mob mob) {
            Mob = mob;
        }
    }
}