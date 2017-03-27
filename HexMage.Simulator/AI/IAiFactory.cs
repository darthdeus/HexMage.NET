namespace HexMage.Simulator.AI {
    public interface IAiFactory {
        IMobController Build(GameInstance gameInstance);
    }
}