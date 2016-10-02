using System.Threading.Tasks;
using HexMage.Simulator;
using HexMage.Simulator.Model;

namespace HexMage.AI
{
    public class DoNothingController : IMobController
    {
        public DefenseDesire FastRequestDesireToDefend(Mob mob, AbilityInfo abilityInfo) {
            throw new System.NotImplementedException();
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            throw new System.NotImplementedException();
        }

        public void FastRandomAction(GameEventHub eventHub) {
            throw new System.NotImplementedException();
        }

        public Task<DefenseDesire> RequestDesireToDefend(Mob mob, AbilityInfo abilityInfo) {
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
