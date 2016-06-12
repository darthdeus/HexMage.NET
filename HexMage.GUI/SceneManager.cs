using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace HexMage.GUI
{
    class SceneManager
    {
        private readonly Stack<GameScene> _scenes = new Stack<GameScene>();

        public SceneManager(GameScene initialScene) {
            _scenes.Push(initialScene);
        }

        public void Update(GameTime gameTime) {
            Debug.Assert(_scenes.Count > 0);

            var currentScene = _scenes.Peek();

            var result = currentScene.Update(gameTime);
            if (result.IsRight) {
                if (result.RightValue == SceneUpdateResult.Terminate) {
                    currentScene.Cleanup();
                    _scenes.Pop();
                }
            } else {
                var newScene = result.LeftValue;
                newScene.Initialize();
                _scenes.Push(newScene);
            }
        }

        public void Draw(GameTime gameTime) {
            Debug.Assert(_scenes.Count > 0);
            _scenes.Peek().Draw(gameTime);
        }
    }
}