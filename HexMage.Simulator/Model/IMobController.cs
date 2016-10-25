using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IMobController {
        DefenseDesire FastRequestDesireToDefend(int mobId, int abilityId);
        void FastPlayTurn(GameEventHub eventHub);
        void FastRandomAction(GameEventHub eventHub);

        string Name { get; }
        Task<DefenseDesire> SlowRequestDesireToDefend(int targetId, int abilityId);
        Task SlowPlayTurn(GameEventHub eventHub);
    }
}