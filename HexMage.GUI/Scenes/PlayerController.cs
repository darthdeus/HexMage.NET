using System.Diagnostics;
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
            Debug.Assert(_tcs == null);            
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return _tcs.Task;
        }

        public void PlayerEndedTurn() {
            Debug.Assert(_tcs != null, "PlayerController.TaskCompletionSource wasn't properly initialized.");
            _tcs.SetResult(true);
            _tcs = null;
        }

        public Task<bool> RandomAction(GameEventHub eventHub) {
#warning TODO - perhaps there's a better way to handle this?
            return new AiRandomController(_gameInstance).PlayTurn(eventHub);
        }
    }
}