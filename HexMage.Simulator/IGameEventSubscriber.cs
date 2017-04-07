using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IGameEventSubscriber {
        void EventAbilityUsed(int mobId, int targetId, AbilityInfo abilityInfo);
        void EventMobMoved(int mobId, AxialCoord pos);
        Task SlowEventMobMoved(int mobId, AxialCoord pos);
        Task SlowEventAbilityUsed(int mobId, int targetId, AbilityInfo abilityInfo);
    }
}