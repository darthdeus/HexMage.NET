using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.GUI.UI;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Components {
    public class GameBoardController : Component {
        private readonly GameInstance _gameInstance;
        private Entity _popover;

        public GameBoardController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public override void Initialize(AssetManager assetManager) {
            AssertNotInitialized();
            _popover = new VerticalLayout();
            _popover.Renderer = new ColorRenderer(Color.LightGray);
            _popover.Padding = new Vector4(20, 10, 20, 10);
            _popover.SortOrder = 1000;
            
            _popover.AddChild(new Label("Future popover", assetManager.Font));
                        
            Entity.Scene.AddRootEntity(_popover);           
            _popover.InitializeEntity(assetManager);
        }

        public override void Update(GameTime time) {
            var inputManager = InputManager.Instance;
            var mouseHex = Camera2D.Instance.MouseHex;
            if (inputManager.JustRightClicked()) {

                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    _gameInstance.Map.Toogle(mouseHex);

                    // TODO - podivat se na generickou implementaci pathfinderu
                    // TODO - pathfindovani ze zdi najde cesty
                    _gameInstance.Pathfinder.PathfindFrom(new AxialCoord(0, 0));
                }
            }

            if (inputManager.IsKeyJustPressed(Keys.Space)) {
                _gameInstance.TurnManager.NextMobOrNewTurn();
                // TODO - fix this, it's ugly
                _gameInstance.Pathfinder.PathfindFrom(_gameInstance.TurnManager.CurrentMob.Coord);
            }

            if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                _popover.Active = true;
                _popover.Position = Camera2D.Instance.HexToPixel(mouseHex) + new Vector2(30, -40);
            }
        }
    }
}