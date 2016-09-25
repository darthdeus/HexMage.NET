using System.Threading.Tasks;
using HexMage.Simulator;
using HexMage.Simulator.Model;

namespace HexMage.AI
{
    public class DoNothingController : IMobController
    {
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
}
