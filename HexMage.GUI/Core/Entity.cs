using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.GUI.Components;
using HexMage.GUI.Renderers;
using HexMage.GUI.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace HexMage.GUI.Core {
    public class Entity {
        public Func<Matrix> Transform { get; set; } = () => Matrix.Identity;
        public Matrix RenderTransform { get; set; }
        public bool Hidden { get; set; } = false;

        public bool DebugMode { get; set; } = false;
        // Arbitrary metadata that can be stored with an entity
        public object Metadata { get; set; }

        internal bool _sortOrderSet;
        private int _sortOrder;

        public int SortOrder {
            get { return _sortOrderSet ? _sortOrder : Parent.SortOrder; }
            set {
                _sortOrderSet = true;
                _sortOrder = value;
            }
        }

        // Setting this to true will cause the generic render lifecycle to not start
        // a new batch when rendering this entity, but *only* if the entity is root.
        public bool CustomBatch = false;

        private GameScene _scene;

        public GameScene Scene {
            get { return _scene ?? Parent.Scene; }
            set { _scene = value; }
        }

        public float Rotation = 0;
        public Vector2 Position { get; set; }
        public Vector2 RenderPosition { get; set; }
        public Rectangle AABB => new Rectangle(RenderPosition.ToPoint(), LayoutSize.ToPoint());

        public bool Active = true;

        public Vector4 Padding;
        public Vector2 PaddingOffset => new Vector2(Padding.W, Padding.X);
        public Vector2 PaddingSizeIncrease => new Vector2(Padding.Y + Padding.W, Padding.X + Padding.Z);

        public Entity Parent { get; set; }
        public List<Entity> Children { get; } = new List<Entity>();
        public Func<Vector2> SizeFunc { get; set; }

        protected List<Component> Components { get; } = new List<Component>();

        public IRenderer Renderer { get; set; }

        public IEnumerable<Entity> ActiveChildren => Children.Where(x => x.Active);

        public T GetComponent<T>() where T : Component {
            return (T) Components.FirstOrDefault(c => c is T);
        }

        public void AddComponent<T>(T component) where T : Component {
            component.Entity = this;
            Components.Add(component);
        }

        public void AddComponent(Action<GameTime> componentFunc) {
            var component = new LambdaComponent(componentFunc) {
                Entity = this
            };
            Components.Add(component);
        }

        public void AddComponent(Action componentFunc) {
            var component = new LambdaComponent(t => componentFunc()) {
                Entity = this
            };
            Components.Add(component);
        }

        public T AddChild<T>(T entity) where T : Entity {
            Debug.Assert(entity != this);
            Children.Add(entity);
            entity.Parent = this;
            return entity;
        }

        public Vector2 LayoutSize { get; set; }

        public void InitializeEntity(AssetManager assetManager) {
            foreach (var component in Components) {
                component.Initialize(assetManager);
            }

            Initialize();

            foreach (var entity in Children) {
                entity.InitializeEntity(assetManager);
            }
        }

        public void Initialize() {}

        public void LayoutEntity() {
            foreach (var entity in ActiveChildren) {
                entity.LayoutEntity();
            }

            Layout();
        }

        protected virtual void Layout() {
            if (SizeFunc != null) {
                LayoutSize = SizeFunc() + PaddingSizeIncrease;
            } else {
                LayoutSize = Children.LastOrDefault()?.LayoutSize ?? Vector2.Zero;
                LayoutSize += PaddingSizeIncrease;
            }
        }

        protected virtual void Update(GameTime time) {}

        public void UpdateEntity(GameTime time) {
            if (!Active) return;

            foreach (var component in Components) {
                component.Update(time);
            }

            Update(time);

            foreach (var entity in ActiveChildren) {
                entity.UpdateEntity(time);
            }
        }

        public void Render(SpriteBatch batch, AssetManager assetManager) {
            RenderPosition = Position
                             + (Parent?.RenderPosition ?? Vector2.Zero);

            RenderTransform = (Parent?.RenderTransform ?? Matrix.Identity)*Transform();

            if (!CustomBatch) batch.Begin(transformMatrix: RenderTransform);
            Renderer?.Render(this, batch, assetManager);
            if (!CustomBatch) batch.End();

            foreach (var entity in ActiveChildren.Where(x => !x.Hidden)) {
                entity.Render(batch, assetManager);
            }
        }

        public Entity CreateChild() {
            var entity = new Entity();
            AddChild(entity);
            return entity;
        }

        public void RemoveEntity(Entity childEntity) {
            Children.Remove(childEntity);
        }

        public void EnqueueClickEvent(Action action) {
            Scene.EnqueueClickEvent(new ClickEvent(this, action));
        }
    }

    public class SpriteRenderer : IRenderer {
        private readonly Func<Texture2D> _texFunc;
        public Texture2D Tex;

        public SpriteRenderer(Func<Texture2D> texFunc) {
            _texFunc = texFunc;
        }

        public SpriteRenderer(Texture2D tex) {
            Tex = tex;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            batch.Draw(_texFunc?.Invoke() ?? Tex, entity.RenderPosition);
        }
    }

    public class AnimatedSpriteRenderer : IRenderer {
        public readonly Texture2D Tex;
        private readonly Func<Rectangle> _spriteSelector;

        public AnimatedSpriteRenderer(Texture2D tex, Func<Rectangle> spriteSelector) {
            Tex = tex;
            _spriteSelector = spriteSelector;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            batch.Draw(Tex, entity.RenderPosition, _spriteSelector(), Color.White);
        }
    }
}