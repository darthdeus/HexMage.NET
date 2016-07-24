using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    public class Component {
        public Entity Entity { get; set; }
        public virtual void Initialize() {}
        public virtual void Update(GameTime time) {}
    }

    public interface IRenderer {
        void Render(Entity entity, SpriteBatch batch, AssetManager assetManager);
    }

    public class Entity {
        public Vector2 Position { get; set; }
        public Vector2 RenderPosition { get; set; }
        public Rectangle AABB => new Rectangle(RenderPosition.ToPoint(), CachedSize.ToPoint());

        public Entity Parent { get; set; }
        public List<Entity> Children { get; } = new List<Entity>();
        protected List<Component> Components { get; } = new List<Component>();
        public IRenderer Renderer { get; set; }

        public T GetComponent<T>() where T : Component {
            return (T) Components.FirstOrDefault(c => c is T);
        }

        public void AddComponent<T>(T component) where T : Component {
            component.Entity = this;
            Components.Add(component);
        }

        public void AddChild(Entity entity) {
            Debug.Assert(entity != this);
            Children.Add(entity);
            entity.Parent = this;
        }


        public Vector2 CachedSize { get; set; }

        public void InitializeEntity() {
            foreach (var component in Components) {
                component.Initialize();
            }

            Initialize();

            foreach (var entity in Children) {
                entity.InitializeEntity();
            }
        }

        public void Initialize() {}

        public void LayoutEntity() {
            foreach (var entity in Children) {
                entity.LayoutEntity();
            }

            Layout();
        }

        protected virtual void Layout() {
            CachedSize = Children.LastOrDefault()?.CachedSize ?? Vector2.Zero;
        }

        protected virtual void Update(GameTime time) {}

        public void UpdateEntity(GameTime time) {
            foreach (var component in Components) {
                component.Update(time);
            }

            Update(time);

            foreach (var entity in Children) {
                entity.UpdateEntity(time);
            }
        }

        public void Render(SpriteBatch batch, AssetManager assetManager) {
            RenderPosition = Position + (Parent?.RenderPosition ?? Vector2.Zero);

            Renderer?.Render(this, batch, assetManager);

            foreach (var entity in Children) {
                entity.Render(batch, assetManager);
            }
        }


        public Entity CreateChild() {
            var entity = new Entity();
            AddChild(entity);
            return entity;
        }
    }

    public class SpriteRenderer : IRenderer {
        public readonly Texture2D Tex;

        public SpriteRenderer(Texture2D tex) {
            Tex = tex;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            batch.Draw(Tex, entity.RenderPosition);
        }
    }
}