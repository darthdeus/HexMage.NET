using Microsoft.Xna.Framework;

namespace HexMage.GUI
{
    enum SceneUpdateResult
    {
        Terminate,
        Continue
    }

    abstract class GameScene
    {
        public abstract void Initialize();
        public abstract void Cleanup();
        public abstract Either<GameScene, SceneUpdateResult> Update(GameTime gameTime);
        public abstract void Draw(GameTime gameTime);
    }

    
}