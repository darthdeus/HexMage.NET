using System;
using HexMage.GUI.UI;
using Microsoft.Xna.Framework;

namespace HexMage.GUI {
    public class MapSelectionScene : GameScene {
        private ImGui _gui;
        private Element _rootElement;

        public MapSelectionScene(GameManager gameManager) : base(gameManager) {}

        public override void Initialize() {
            _gui = new ImGui(_inputManager, _assetManager.Font);
            _rootElement = new Element();
            _rootElement.Position = new Vector2(50, 50);

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

        public override void Cleanup() {}

        public override SceneUpdateResult Update(GameTime gameTime, ref GameScene newScene) {
            _rootElement.Layout();
            if (_gui.Button("Start game", new Point(20, 20))) {
                newScene = new ArenaScene(_gameManager);
                return SceneUpdateResult.NewScene;
            } else {
                return SceneUpdateResult.Continue;
            }
        }

        public override void Draw(GameTime gameTime) {
            var mages = new Entity();

            var entity = mages.CreateChild();
            entity.Renderer = new SpriteRenderer(_assetManager[AssetManager.MobTexture]);
            entity.Position = new Vector2(150, 150);

            var second = entity.CreateChild();
            second.Renderer = new SpriteRenderer(_assetManager[AssetManager.MobTexture]);
            second.Position = new Vector2(5, 5);

            _spriteBatch.Begin();
            mages.Render(_spriteBatch, _assetManager);
            _rootElement.Render(_spriteBatch, _assetManager);
            _spriteBatch.End();
            Console.WriteLine();

            _gui.Draw(_assetManager[AssetManager.GrayTexture], _spriteBatch);
        }
    }
}