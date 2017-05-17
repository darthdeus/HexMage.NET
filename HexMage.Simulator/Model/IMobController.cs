using System.Threading.Tasks;

namespace HexMage.Simulator.Model {
    public interface IMobController {
        void FastPlayTurn(GameEventHub eventHub);
        Task SlowPlayTurn(GameEventHub eventHub);

        string Name { get; }
    }
}