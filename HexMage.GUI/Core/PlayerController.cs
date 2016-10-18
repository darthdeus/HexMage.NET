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

        public Task<DefenseDesire> RequestDesireToDefend(MobId mobId, Ability ability) {
            return _arenaScene.RequestDesireToDefend(mobId, ability);
        }

        private TaskCompletionSource<bool> _tcs;

        public Task<bool> PlayTurn(GameEventHub eventHub) {
            Debug.Assert(_tcs == null);
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return _tcs.Task;
        }

        public Task<DefenseDesire> SlowRequestDesireToDefend(MobId targetId, AbilityId abilityId) {
            return _arenaScene.RequestDesireToDefend(targetId, _gameInstance.MobManager.AbilityForId(abilityId));
        }

        public Task SlowPlayTurn(GameEventHub eventHub) {
            Debug.Assert(_tcs == null);
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return _tcs.Task;
        }

        public void PlayerEndedTurn() {
            Debug.Assert(_tcs != null, "PlayerController.TaskCompletionSource wasn't properly initialized.");
            _tcs.SetResult(true);
            _tcs = null;
        }

        public void RandomAction(GameEventHub eventHub) {
            new AiRandomController(_gameInstance).FastPlayTurn(eventHub);
        }

        public DefenseDesire FastRequestDesireToDefend(MobId mob, AbilityId abilityId) {
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