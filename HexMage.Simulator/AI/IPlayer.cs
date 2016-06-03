namespace HexMage.Simulator
{
    public interface IPlayer
    {
        bool IsAI();
        void ActionTo(PixelCoord c, GameInstance gameInstance, Mob mob);
        void AnyAction(GameInstance gameInstance, Mob mob);
    }
}