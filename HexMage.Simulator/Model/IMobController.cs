using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public interface IMobController {
        void FastPlayTurn(GameEventHub eventHub);
        Task SlowPlayTurn(GameEventHub eventHub);

        string Name { get; }
    }
}