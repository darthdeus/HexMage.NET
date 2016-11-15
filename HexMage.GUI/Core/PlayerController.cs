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
            _aiRandomController = new AiRandomController(_gameInstance);
        }

        private TaskCompletionSource<bool> _tcs;
        private readonly AiRandomController _aiRandomController;

        public Task<bool> PlayTurn(GameEventHub eventHub) {
            Debug.Assert(_tcs == null);
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return _tcs.Task;
        }

        public Task SlowPlayTurn(GameEventHub eventHub) {
            Debug.Assert(_tcs == null, "Starting a new turn while there's an existing TCS");
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            eventHub.State = GameEventState.TurnInProgress;
            return _tcs.Task;
        }

        public void PlayerEndedTurn(GameEventHub eventHub) {
            Debug.Assert(_tcs != null, "PlayerController.TaskCompletionSource wasn't properly initialized.");
            var tcs = _tcs;           
            eventHub.State = GameEventState.SettingUpTurn; 
            _tcs = null;
            tcs.SetResult(true);
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            Debug.Assert(_tcs == null);
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _tcs.Task.Wait(Program.CancellationToken);
        }

        public string Name => nameof(PlayerController);
    }
}