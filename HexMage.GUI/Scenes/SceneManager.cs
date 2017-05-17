using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Scenes {
    class SceneManager {
        public static bool RollbackToFirst = false;

        private readonly Stack<GameScene> _scenes = new Stack<GameScene>();

        public SceneManager(GameScene initialScene) {
            _scenes.Push(initialScene);
        }

        public void Initialize() {
            _scenes.Peek().Initialize();
            _scenes.Peek().InitializeRootEntities();
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
                    newScene.InitializeRootEntities();
                    _scenes.Push(newScene);
                    break;

                case SceneUpdateResult.Continue:
                    break;
            }

            if (RollbackToFirst) {
                while (_scenes.Count > 1) {
                    currentScene.Cleanup();
                    _scenes.Pop();
                }

                RollbackToFirst = false;
            }
        }

        public void Render(GameTime gameTime) {
            Debug.Assert(_scenes.Count > 0);
            _scenes.Peek().Render(gameTime);
        }

    }
}