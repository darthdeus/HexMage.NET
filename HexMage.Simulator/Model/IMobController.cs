using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IMobController {
        Task<DefenseDesire> RequestDesireToDefend(Mob mob, Ability ability);
        Task<bool> PlayTurn(GameEventHub eventHub);
        Task<bool> RandomAction(GameEventHub eventHub);

        string Name { get; }
    }
}