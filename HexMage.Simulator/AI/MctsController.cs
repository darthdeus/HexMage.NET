using System.Threading.Tasks;

namespace HexMage.Simulator
{
    public class MctsController : IMobController {
        private readonly GameInstance _gameInstance;

        public MctsController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            var uct = new UctAlgorithm();
            var node = uct.UctSearch(_gameInstance);

            if (node.Action is AbilityUseAction abilityAction) {
                _gameInstance.FastUse(abilityAction.AbilityId, abilityAction.MobId, abilityAction.TargetId);
            } else if (node.Action is MoveAction moveAction) {
                _gameInstance.FastMove(moveAction.MobId, moveAction.Coord);
            }
        }

        public Task SlowPlayTurn(GameEventHub eventHub) {
            FastPlayTurn(eventHub);
            return Task.CompletedTask;
        }

        public string Name => "MctsController";
    }
}