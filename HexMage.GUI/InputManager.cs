using HexMage.Simulator;
using Microsoft.Xna.Framework.Input;

namespace HexMage.GUI
{
    public class InputManager
    {
        private KeyboardState _lastState;

        public bool IsKeyJustPressed(Keys key) {
            var current = Keyboard.GetState();
            return current.IsKeyDown(key) && _lastState.IsKeyUp(key);
        }

        public bool IsKeyJustReleased(Keys key) {
            var current = Keyboard.GetState();            
            return current.IsKeyUp(key) && _lastState.IsKeyDown(key);
        }

        public void Refresh() {
            _lastState = Keyboard.GetState();
        }

        public PixelCoord MousePosition => new PixelCoord(Mouse.GetState().X, Mouse.GetState().Y);
    }

    public class GameInputManager
    {
        private GameInstance _game;
        private TurnManager _turnManager;
        private Pathfinder _pathfinder;

        public GameInputManager(GameInstance game) {
            _game = game;
            _turnManager = game.TurnManager;
            _pathfinder = game.Pathfinder;
        }
    }
}