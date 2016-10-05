using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IGameEventSubscriber {
        void EventAbilityUsed(Mob mob, Mob target, Ability ability);
        void EventMobMoved(Mob mob, AxialCoord pos);
        void EventDefenseDesireAcquired(Mob mob, DefenseDesire defenseDesireResult);
    }
}