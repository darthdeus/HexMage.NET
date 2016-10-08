using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IGameEventSubscriber {
        void EventAbilityUsed(MobId mobId, MobId targetId, Ability ability);
        void EventMobMoved(MobId mobId, AxialCoord pos);
        void EventDefenseDesireAcquired(MobId mobId, DefenseDesire defenseDesireResult);
    }
}