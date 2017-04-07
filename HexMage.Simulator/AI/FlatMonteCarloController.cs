using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    public class FlatMonteCarloController : IMobController {
        private readonly GameInstance _game;

        public FlatMonteCarloController(GameInstance game) {
            _game = game;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            do {
                var action = FlatMonteCarlo.Search(_game).Action;

                if (action.Type == UctActionType.EndTurn) break;

                ActionEvaluator.FNoCopy(_game, action);
            } while (!_game.IsFinished);
        }

        public async Task SlowPlayTurn(GameEventHub eventHub) {
            do
            {
                var action = FlatMonteCarlo.Search(_game).Action;

                if (action.Type == UctActionType.EndTurn) break;

                await eventHub.SlowPlayAction(_game, action);
            } while (!_game.IsFinished);
        }

        public string Name => "FlatMC";

        public override string ToString() {
            return "FlatMC";
        }
    }
}