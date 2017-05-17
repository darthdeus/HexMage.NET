using System;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Renderers {
    public class AnimationRenderer : IRenderer {
        private readonly Animation _animation;

        public AnimationRenderer(Animation animation) {
            _animation = animation;
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            _animation.RenderFrame(entity, entity.RenderPosition, batch, assetManager);
        }
    }
}