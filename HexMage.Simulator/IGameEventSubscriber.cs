using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IGameEventSubscriber {
        Task<bool> EventAbilityUsed(Mob mob, Mob target, UsableAbility ability);
        Task<bool> EventMobMoved(Mob mob, AxialCoord pos);
        Task<bool> EventDefenseDesireAcquired(Mob mob, DefenseDesire defenseDesireResult);
    }
}