using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    public class MctsFactory : IAiFactory {
        private readonly int _time;

        public MctsFactory(int time) {
            _time = time;
        }

        public IMobController Build(GameInstance gameInstance) {
            return new MctsController(gameInstance, _time);
        }
    }
}