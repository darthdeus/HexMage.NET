using System.Threading.Tasks;
using HexMage.Simulator;
using HexMage.Simulator.Model;

namespace HexMage.AI {
    public class DoNothingController : IMobController {
        private readonly GameInstance _gameInstance;

        public DoNothingController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public Task<DefenseDesire> RequestDesireToDefend(Mob mob, Ability ability) {
            return Task.FromResult(DefenseDesire.Pass);
        }

        public Task<bool> PlayTurn(GameEventHub eventHub) {
            return Task.FromResult(true);
        }

        public Task<bool> RandomAction(GameEventHub eventHub) {
            return Task.FromResult(true);
        }

        public string Name => nameof(DoNothingController);
    }


    public class DoNothingController2 : IMobController {
        private readonly GameInstance _gameInstance;

        public DoNothingController2(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public Task<DefenseDesire> RequestDesireToDefend(Mob mob, Ability ability) {
            return Task.FromResult(DefenseDesire.Pass);
        }

        public Task<bool> PlayTurn(GameEventHub eventHub) {
            return Task.FromResult(true);
        }

        public Task<bool> RandomAction(GameEventHub eventHub) {
            return Task.FromResult(true);
        }

        public string Name => nameof(DoNothingController2);
    }
}