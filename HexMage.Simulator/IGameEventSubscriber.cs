using System.Threading.Tasks;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IGameEventSubscriber {
        void ActionApplied(UctAction action);
        Task SlowEventMobMoved(int mobId, AxialCoord pos);
        Task SlowEventAbilityUsed(int mobId, int targetId, AbilityInfo abilityInfo);
    }
}