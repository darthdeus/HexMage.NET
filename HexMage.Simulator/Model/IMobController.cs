using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IMobController {
        DefenseDesire FastRequestDesireToDefend(MobId mobId, AbilityId abilityId);
        void FastPlayTurn(GameEventHub eventHub);
        void FastRandomAction(GameEventHub eventHub);

        string Name { get; }
        Task<DefenseDesire> SlowRequestDesireToDefend(MobId targetId, AbilityId abilityId);
        Task SlowPlayTurn(GameEventHub eventHub);
    }
}