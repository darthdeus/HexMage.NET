using System.Threading.Tasks;
using HexMage.Simulator.AI;

namespace HexMage.Simulator {
    public interface IGameEventSubscriber {
        void ActionApplied(UctAction action);
        Task SlowActionApplied(UctAction action);
    }
}