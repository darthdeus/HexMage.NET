using System;
using System.Collections.Generic;
using System.Linq;
using HexMage.GUI.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    public enum SceneUpdateResult {
        Terminate,
        Continue,
        NewScene
    }

    public class ClickEvent {
        public readonly Entity Target;
        public readonly Action Event;

        public ClickEvent(Entity target, Action eventAction) {
            Target = target;
            Event = eventAction;
        }
    }

    public abstract class GameScene {
        protected readonly GameManager _gameManager;
        private readonly List<Entity> _rootEntities = new List<Entity>();

        protected Camera2D _camera => _gameManager.Camera;
        protected InputManager _inputManager => _gameManager.InputManager;
        protected AssetManager _assetManager => _gameManager.AssetManager;
        protected SpriteBatch _spriteBatch => _gameManager.SpriteBatch;

        private readonly List<Action> _afterUpdateActions = new List<Action>();
        // Root entities to be initialized on the next frame
        private readonly List<Entity> _toInitializeEntities = new List<Entity>();
        // Entities to be destroyed at the end of the frame
        private readonly List<Entity> _toDestroyEntities = new List<Entity>();

        private readonly List<ClickEvent> _clickEvents = new List<ClickEvent>();

        private readonly SortedList<DateTime, Action> _delayedActions = new SortedList<DateTime, Action>();

        protected GameScene(GameManager gameManager) {
            _gameManager = gameManager;
        }

        public abstract void Initialize();

        public void InitializeRootEntities() {
            var currentRootEntities = _rootEntities.ToList();
            foreach (var entity in currentRootEntities) {
                entity.InitializeEntity(_assetManager);
            }
        }

        public void UpdateRootEntities(GameTime gameTime) {
            foreach (var entity in _toInitializeEntities) {
                AddRootEntity(entity);
                entity.InitializeEntity(_assetManager);
            }

            // Entities enqueued to be destroyed in the previous frame are now
            // removed. Root entities get removed from the list, while nested ones
            // are detached from their parent and GCd.
            foreach (var entity in _toDestroyEntities) {
                if (entity.Parent != null) {
                    entity.Parent.RemoveEntity(entity);
                } else {
                    _rootEntities.Remove(entity);
                }
            }

            // Handle all click events
            var clickEvent = _clickEvents.OrderBy(x => x.Target.SortOrder).Reverse().FirstOrDefault();
            clickEvent?.Event();
            _clickEvents.Clear();

            // Delayed actions which are due are executed at once and removed from the queue.
            var pendingActions = _delayedActions
                .TakeWhile(x => x.Key < DateTime.Now)
                .ToList();

            foreach (var pair in pendingActions) {
                pair.Value.Invoke();
                _delayedActions.Remove(pair.Key);
            }

            // Layout of all root entities is always done before Update() is called
            foreach (var entity in _rootEntities) {
                entity.LayoutEntity();
            }

            // The update is deliberately done for all entities after they've been
            // laid out, in case there are inter-entity connections that depend
            // on layout being finished.
            foreach (var entity in _rootEntities) {
                entity.UpdateEntity(gameTime);
            }

            foreach (var action in _afterUpdateActions) {
                action.Invoke();
            }
            _afterUpdateActions.Clear();
        }

        public void RenderRootEntities() {
            foreach (var entity in _rootEntities.OrderBy(x => x.SortOrder).Where(x => x.Active)) {
                entity.Render(_spriteBatch, _assetManager);
            }
        }

        protected Entity CreateRootEntity() {
            var entity = new Entity {Scene = this};
            _rootEntities.Add(entity);
            return entity;
        }

        public abstract void Cleanup();

        public void AddRootEntity(Entity entity) {
            _rootEntities.Add(entity);
            entity.Scene = this;
        }

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

        public void AfterUpdate(Action action) {
            _afterUpdateActions.Add(action);
        }

        public void AddAndInitializeNextFrame(Entity entity) {
            _toInitializeEntities.Add(entity);
        }

        public void DestroyEntity(Entity entity) {
            _toDestroyEntities.Add(entity);
        }

        public void DelayFor(TimeSpan delay, Action action) {
            _delayedActions.Add(DateTime.Now.Add(delay), action);
        }

        public void EnqueueClickEvent(ClickEvent clickEvent) {
            _clickEvents.Add(clickEvent);
        }
    }
}