using Microsoft.Xna.Framework;

namespace HexMage.GUI
{
    public class MapSelectionScene : GameScene
    {
        private ImGui _gui;

        public MapSelectionScene(GameManager gameManager) : base(gameManager)
        {
        }

        public override void Initialize() {
            _gui = new ImGui(_inputManager, _assetManager.Font);
        }

        public override void Cleanup()
        {
        }

        public override Either<GameScene, SceneUpdateResult> Update(GameTime gameTime)
        {
            if (_gui.Button("Start game", new Point(20, 20))) {
                return Either<GameScene, SceneUpdateResult>.Left(new ArenaScene(_gameManager));
            } else {
                return Either<GameScene, SceneUpdateResult>.Right(SceneUpdateResult.Continue);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();
            _spriteBatch.DrawString(_assetManager.Font, "fajdio jadsio fjiosad fdsjaio fjdoajfdsajioafji\nnfadsnjkfdsanjk", Vector2.Zero, Color.Black);
            _spriteBatch.End();

            _gui.Draw(_assetManager[AssetManager.GrayTexture], _spriteBatch);
        }
    }
}