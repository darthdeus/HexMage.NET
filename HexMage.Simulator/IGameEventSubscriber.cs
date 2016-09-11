using System.Threading.Tasks;

namespace HexMage.Simulator {
    public interface IGameEventSubscriber {
        Task<bool> EventAbilityUsed(Mob mob, Mob target, UsableAbility ability);
        Task<bool> EventMobMoved(Mob mob, AxialCoord pos);
    }
}