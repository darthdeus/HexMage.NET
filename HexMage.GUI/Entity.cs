using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    public class Component {
        public Entity Entity { get; set; }
        public virtual void Update(GameTime time) {}
    }

    public interface IRenderer {
        void Render(Entity entity, SpriteBatch batch, AssetManager assetManager);
    }

    public class Entity {
        public Vector2 Position { get; set; }
        public Vector2 RenderPosition { get; set; }

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
            Children.Add(entity);
            entity.Parent = this;
        }

        public void Render(SpriteBatch batch, AssetManager assetManager) {
            RenderPosition = Position + (Parent?.RenderPosition ?? Vector2.Zero);

            Renderer?.Render(this, batch, assetManager);

            foreach (var entity in Children) {
                entity.Render(batch, assetManager);
            }
        }

        protected virtual void Update(GameTime time) { }

        public void UpdateEntity(GameTime time) {
            foreach (var component in Components) {
                component.Update(time);
            }

            Update(time);

            foreach (var entity in Children) {
                entity.UpdateEntity(time);
            }
        }

        public Entity CreateChild() {
            var entity = new Entity();
            AddChild(entity);
            return entity;
        }
    }

    public class SpriteRenderer : IRenderer {
        private readonly Texture2D _tex;

        public SpriteRenderer(Texture2D tex) {
            _tex = tex;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            batch.Draw(_tex, entity.RenderPosition);
        }

    }
}