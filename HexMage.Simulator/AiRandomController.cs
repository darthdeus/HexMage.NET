using System;
using System.Threading.Tasks;
using HexMage.Simulator;

namespace HexMage.Simulator {
    public class AiRandomController : IMobController {
        public Task<DefenseDesire> RequestDesireToDefend(Mob mob, Ability ability) {
            return Task.FromResult(DefenseDesire.Block);
        }
    }
}