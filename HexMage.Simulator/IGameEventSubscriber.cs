using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IGameEventSubscriber {
        Task SlowEventMobMoved(int mobId, AxialCoord pos);
        Task SlowEventAbilityUsed(int mobId, int targetId, AbilityInfo abilityInfo);
    }
}