using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    public enum SceneUpdateResult {
        Terminate,
        Continue,
        NewScene
    }

    public abstract class GameScene {
        protected readonly GameManager _gameManager;
        protected readonly List<Entity> _rootEntities = new List<Entity>();
        protected Camera2D _camera => _gameManager.Camera;
        protected InputManager _inputManager => _gameManager.InputManager;
        protected AssetManager _assetManager => _gameManager.AssetManager;
        protected SpriteBatch _spriteBatch => _gameManager.SpriteBatch;

        protected GameScene(GameManager gameManager) {
            _gameManager = gameManager;
        }


        public abstract void Initialize();

        public void InitializeRootEntities() {
            foreach (var entity in _rootEntities) {
                entity.InitializeEntity();
            }
        }

        public void UpdateRootEntities(GameTime gameTime) {
            foreach (var entity in _rootEntities) {
                entity.LayoutEntity();
            }

            // The update is deliberately done for all entities after they've been
            // laid out, in case there are inter-entity connections that depend
            // on layout being finished.
            foreach (var entity in _rootEntities) {
                entity.UpdateEntity(gameTime);
            }
        }

        public void RenderRootEntities() {
            // TODO - remove this and force all renderers to batch themselves
            _spriteBatch.Begin();

            foreach (var entity in _rootEntities.OrderBy(x => x.SortOrder).Where(x => x.Active)) {
                entity.Render(_spriteBatch, _assetManager);
            }

            _spriteBatch.End();
        }

        protected Entity CreateRootEntity() {
            var entity = new Entity();
            _rootEntities.Add(entity);
            return entity;
        }

        public abstract void Cleanup();

        private GameScene _newScene = null;

        public void LoadNewScene(GameScene scene) {
            _newScene = scene;
        }

        private bool _shouldTerminate = false;

        public void Terminate() {
            _shouldTerminate = true;
        }

        public SceneUpdateResult Update(GameTime gameTime, ref GameScene newScene) {
            UpdateRootEntities(gameTime);
            if (_newScene != null) {
                newScene = _newScene;
                _newScene = null;
                return SceneUpdateResult.NewScene;
            } else if (_shouldTerminate) {
                return SceneUpdateResult.Terminate;
            } else {
                return SceneUpdateResult.Continue;
            }
        }

        public void Render(GameTime gameTime) {
            // TODO - figure out if GameTime would be useful to pass here?
            RenderRootEntities();
        }
    }
}