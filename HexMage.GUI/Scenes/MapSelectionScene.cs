using HexMage.GUI.UI;
using Microsoft.Xna.Framework;

namespace HexMage.GUI
{
    public class MapSelectionScene : GameScene
    {
        private ImGui _gui;
        private Element _rootElement;

        public MapSelectionScene(GameManager gameManager) : base(gameManager)
        {
        }

        public override void Initialize() {
            _gui = new ImGui(_inputManager, _assetManager.Font);
            _rootElement = new Element();

            var btn1 = new TextButton("click!", _assetManager.Font);
            var lbl1 = new Label("label1", _assetManager.Font);
            var lbl2 = new Label("label2", _assetManager.Font);
            var btn2 = new TextButton("me!", _assetManager.Font);


            var menuBar = new HorizontalLayout();

            var vertical = new VerticalLayout();
            vertical.Add(lbl1);
            vertical.Add(lbl2);

            menuBar.Add(btn1);
            menuBar.Add(vertical);
            menuBar.Add(btn2);                      

            _rootElement.Add(menuBar);
        }

        public override void Cleanup()
        {
        }

        public override Either<GameScene, SceneUpdateResult> Update(GameTime gameTime)
        {
            _rootElement.Layout();
            if (_gui.Button("Start game", new Point(20, 20))) {
                return Either<GameScene, SceneUpdateResult>.Left(new ArenaScene(_gameManager));
            } else {
                return Either<GameScene, SceneUpdateResult>.Right(SceneUpdateResult.Continue);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            var entity = new Entity();
            entity.AddComponent(new SpriteRenderer(_assetManager[AssetManager.MobTexture]));
            entity.Position = new Vector2(50, 50);

            var second = new Entity();
            second.AddComponent(new SpriteRenderer(_assetManager[AssetManager.MobTexture]));
            second.Position = new Vector2(5, 5);

            entity.AddChild(second);

            _spriteBatch.Begin();
            entity.Render(_spriteBatch);
            //_spriteBatch.DrawString(_assetManager.Font, "fajdio jadsio fjiosad fdsjaio fjdoajfdsajioafji\nnfadsnjkfdsanjk", Vector2.Zero, Color.Black);
            _spriteBatch.End();

            _gui.Draw(_assetManager[AssetManager.GrayTexture], _spriteBatch);

            _spriteBatch.Begin();
            _rootElement.Render(new Vector2(50, 50), _assetManager, _spriteBatch);
            _spriteBatch.End();
        }
    }
}