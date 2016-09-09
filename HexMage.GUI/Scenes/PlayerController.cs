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

        private TaskCompletionSource<bool> _tcs;

        public Task<bool> PlayTurn(GameEventHub eventHub) {
#warning TOOD - je tohle spravne?
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Utils.ThreadLog("Player turn failing");
            return _tcs.Task;
        }

        public void PlayerEndedTurn() {
            _tcs.SetResult(true);
        }

        public Task<bool> RandomAction(GameEventHub eventHub) {
            throw new System.NotImplementedException();
        }
    }
}