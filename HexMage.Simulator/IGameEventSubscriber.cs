using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IGameEventSubscriber {
        void EventAbilityUsed(int mobId, int targetId, Ability ability);
        void EventMobMoved(int mobId, AxialCoord pos);
        void EventDefenseDesireAcquired(int mobId, DefenseDesire defenseDesireResult);
        Task SlowEventMobMoved(int mobId, AxialCoord pos);
        Task SlowEventAbilityUsed(int mobId, int targetId, Ability ability);
    }
}