using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace HexMage.GUI.Components {
    public class GameBoardController : Component {
        private readonly GameInstance _gameInstance;

        public GameBoardController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public override void Update(GameTime time) {
            var inputManager = InputManager.Instance;
            if (inputManager.JustRightClicked()) {
                var mouseHex = Camera2D.Instance.MouseHex;

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
        }
    }
}