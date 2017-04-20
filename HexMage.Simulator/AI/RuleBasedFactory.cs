namespace HexMage.Simulator.AI {
    public class RuleBasedFactory : IAiFactory {
        public IMobController Build(GameInstance gameInstance) {
            return new AiRuleBasedController(gameInstance);
        }
    }
}