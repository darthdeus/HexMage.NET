using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    public class Component {
        public Entity Entity { get; set; }
    }

    public abstract class RenderableComponent : Component {
        public abstract void Render(SpriteBatch batch);
        public abstract void Update();
    }

    public class Entity {
        public Vector2 Position { get; set; }
        public Vector2 RenderPosition { get; set; }

        public Entity Parent { get; set; }
        public List<Entity> Children { get; } = new List<Entity>();
        private List<Component> Components { get; } = new List<Component>();

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

        public void Render(SpriteBatch batch) {
            RenderPosition = Position + (Parent?.Position ?? Vector2.Zero);

            GetComponent<RenderableComponent>()?.Render(batch);

            foreach (var entity in Children) {
                entity.Render(batch);
            }
        }
    }

    public class SpriteRenderer : RenderableComponent {
        private Texture2D _tex;

        public SpriteRenderer(Texture2D tex) {
            _tex = tex;
        }

        public override void Render(SpriteBatch batch) {
            Debug.Assert(Entity != null);
            batch.Draw(_tex, Entity.RenderPosition);
        }
    }
}