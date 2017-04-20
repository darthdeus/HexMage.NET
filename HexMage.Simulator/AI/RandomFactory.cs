namespace HexMage.Simulator.AI {
    public class RandomFactory : IAiFactory {
        public IMobController Build(GameInstance gameInstance) {
            return new AiRandomController(gameInstance);
        }
    }
}