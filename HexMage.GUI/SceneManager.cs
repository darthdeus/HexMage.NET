using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace HexMage.GUI {
    class SceneManager {
        private readonly Stack<GameScene> _scenes = new Stack<GameScene>();

        public SceneManager(GameScene initialScene) {
            _scenes.Push(initialScene);
            initialScene.Initialize();
            initialScene.InitializeRootEntities();
        }

        public void Update(GameTime gameTime) {
            Debug.Assert(_scenes.Count > 0);

            var currentScene = _scenes.Peek();

            GameScene newScene = null;

            var result = currentScene.Update(gameTime, ref newScene);

            switch (result) {
                case SceneUpdateResult.Terminate:
                    currentScene.Cleanup();
                    _scenes.Pop();
                    break;

                case SceneUpdateResult.NewScene:
                    Debug.Assert(newScene != null);
                    newScene.Initialize();
                    _scenes.Push(newScene);
                    break;

                case SceneUpdateResult.Continue:
                    break;
            }
        }

        public void Render(GameTime gameTime) {
            Debug.Assert(_scenes.Count > 0);
            _scenes.Peek().Render(gameTime);
        }
    }
}