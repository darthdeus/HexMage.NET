using System.Diagnostics;
using System.Threading.Tasks;
using HexMage.GUI.Scenes;
using HexMage.Simulator;
using HexMage.Simulator.Model;

namespace HexMage.GUI.Core {
    internal class PlayerController : IMobController {
        private readonly ArenaScene _arenaScene;
        private readonly GameInstance _gameInstance;

        public PlayerController(ArenaScene arenaScene, GameInstance gameInstance) {
            _arenaScene = arenaScene;
            _gameInstance = gameInstance;
        }

        public DefenseDesire FastRequestDesireToDefend(Mob mob, Ability ability) {
            throw new System.NotImplementedException();
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            throw new System.NotImplementedException();
        }

        public void FastRandomAction(GameEventHub eventHub) {
            throw new System.NotImplementedException();
        }

        public Task<DefenseDesire> RequestDesireToDefend(Mob mob, Ability ability) {
            return _arenaScene.RequestDesireToDefend(mob, ability);
        }

        private TaskCompletionSource<bool> _tcs;

        public Task<bool> PlayTurn(GameEventHub eventHub) {
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
            return new AiRandomController(_gameInstance).PlayTurn(eventHub);
        }

        public string Name => nameof(PlayerController);
    }
}