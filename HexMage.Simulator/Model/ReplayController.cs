using System;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class ReplayController : IMobController {
        private static readonly string ErrorMessage =
            "ReplayController is only a placeholder intended to assert that the replay is not being played by an AI.";

        public void FastPlayTurn(GameEventHub eventHub) {
            throw new NotImplementedException(ErrorMessage);
        }

        public Task SlowPlayTurn(GameEventHub eventHub) {
            throw new NotImplementedException(ErrorMessage);
        }

        public string Name => "Replay";
    }
}