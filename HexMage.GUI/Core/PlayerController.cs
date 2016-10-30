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

        public Task<DefenseDesire> RequestDesireToDefend(int mobId, Ability ability) {
            return _arenaScene.RequestDesireToDefend(mobId, ability);
        }

        private TaskCompletionSource<bool> _tcs;
        private readonly AiRandomController _aiRandomController;

        public Task<bool> PlayTurn(GameEventHub eventHub) {
            Debug.Assert(_tcs == null);
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return _tcs.Task;
        }

        public Task<DefenseDesire> SlowRequestDesireToDefend(int targetId, int abilityId) {
            return _arenaScene.RequestDesireToDefend(targetId, _gameInstance.MobManager.AbilityForId(abilityId));
        }

        public Task SlowPlayTurn(GameEventHub eventHub) {
            Debug.Assert(_tcs == null, "Starting a new turn while there's an existing TCS");
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return _tcs.Task;
        }

        public void PlayerEndedTurn() {
            Debug.Assert(_tcs != null, "PlayerController.TaskCompletionSource wasn't properly initialized.");
            var tcs = _tcs;
            _tcs = null;
            tcs.SetResult(true);
        }

        public void RandomAction(GameEventHub eventHub) {
            _aiRandomController.FastPlayTurn(eventHub);
        }

        public DefenseDesire FastRequestDesireToDefend(int mob, int abilityId) {
            return DefenseDesire.Pass;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            Debug.Assert(_tcs == null);
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _tcs.Task.Wait(Program.CancellationToken);
        }

        public void FastRandomAction(GameEventHub eventHub) {
            throw new System.NotImplementedException();
        }

        public string Name => nameof(PlayerController);
    }
}