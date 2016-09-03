using System.Threading.Tasks;
using HexMage.Simulator;

namespace HexMage.GUI {
    internal class PlayerController : IMobController {
        private readonly ArenaScene _arenaScene;
        private readonly GameInstance _gameInstance;

        public PlayerController(ArenaScene arenaScene, GameInstance gameInstance) {
            _arenaScene = arenaScene;
            _gameInstance = gameInstance;
        }

        public Task<DefenseDesire> RequestDesireToDefend(Mob mob, Ability ability) {
            return _arenaScene.RequestDesireToDefend(mob, ability);
        }
    }
}