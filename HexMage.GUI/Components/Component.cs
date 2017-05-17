using System;
using HexMage.GUI.Core;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    /// <summary>
    /// Represents a unit of behavior that can be attached to <code>Entity</code> instances.
    /// </summary>
    public class Component {
        public bool IsInitialized { get; private set; } = false;
        public Entity Entity { get; set; }
        public virtual void Initialize(AssetManager assetManager) {}
        public virtual void Update(GameTime time) {}

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