using System;
using Microsoft.Xna.Framework;

namespace HexMage.GUI {
    public class Component {
        public bool IsInitialized { get; private set; } = false;
        public Entity Entity { get; set; }
        public virtual void Initialize(AssetManager assetManager) {}
        public virtual void Update(GameTime time) {}

        // TODO - find a better name for this
        protected void AssertNotInitialized() {
            if (IsInitialized) {
                Console.WriteLine($"Component {this} for {Entity} already initialized.");
            } else {
                IsInitialized = true;
            }
        }

        protected void EnqueueClickEvent(Action action) {
            Entity.EnqueueClickEvent(action);
        }
    }
}