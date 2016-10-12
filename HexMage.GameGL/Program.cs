using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;

namespace HexMage.GameGL {
    class Game1 : Nez.Core {
        //private GraphicsDeviceManager _graphics;

        //public Game1() {
        //    _graphics = new GraphicsDeviceManager(this) {
        //        PreferredBackBufferWidth = 1280,
        //        PreferredBackBufferHeight = 1024
        //    };
        //}

        protected override void Initialize() {
            base.Initialize();

            // create our Scene with the DefaultRenderer and a clear color of CornflowerBlue
            var myScene = Scene.createWithDefaultRenderer(Color.CornflowerBlue);

            // load a Texture. Note that the Texture is loaded via the scene.content class. This works just like the standard MonoGame Content class
            // with the big difference being that it is tied to a Scene. When the Scene is unloaded so too is all the content loaded via myScene.content.
            var texture = myScene.content.Load<Texture2D>("mobs/dark-mage-idle");

            // setup our Scene by adding some Entities
            var entityOne = myScene.createEntity("entity-one");
            entityOne.addComponent(new Sprite(texture));

            var entityTwo = myScene.createEntity("entity-two");
            entityTwo.addComponent(new Sprite(texture));

            // move entityTwo to a new location so it isn't overlapping entityOne
            entityTwo.transform.position = new Vector2(200, 200);

            // set the scene so Nez can take over
            scene = myScene;
        }
    }

    public static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            using (var game = new Game1())
                game.Run();
        }
    }
}