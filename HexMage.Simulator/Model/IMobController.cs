using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IMobController {
        DefenseDesire FastRequestDesireToDefend(Mob mob, AbilityInfo ability);
        void FastPlayTurn(GameEventHub eventHub);
        void FastRandomAction(GameEventHub eventHub);


        Task<DefenseDesire> RequestDesireToDefend(Mob mob, AbilityInfo ability);
        Task<bool> PlayTurn(GameEventHub eventHub);
        Task<bool> RandomAction(GameEventHub eventHub);

        string Name { get; }
    }
}