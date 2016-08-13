using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace HexMage.GUI {
    public class InputManager {
        public static readonly InputManager Instance = new InputManager();

        private MouseState _lastMouseState;
        private MouseState _currentMouseState;

        private KeyboardState _lastKeyboardState;
        private KeyboardState _currentKeyboardState;

        public bool IsKeyJustPressed(Keys key) {
            return _lastKeyboardState.IsKeyUp(key) && _currentKeyboardState.IsKeyDown(key);
        }

        public bool IsKeyJustReleased(Keys key) {
            return _lastKeyboardState.IsKeyDown(key) && _currentKeyboardState.IsKeyUp(key);
        }

        public void Refresh() {
            _lastMouseState = _currentMouseState;
            _currentMouseState = Mouse.GetState();

            _lastKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();
        }

        public Point MousePosition => new Point(Mouse.GetState().X, Mouse.GetState().Y);

        public bool JustLeftClicked() {
            return _lastMouseState.LeftButton == ButtonState.Released &&
                   _currentMouseState.LeftButton == ButtonState.Pressed;
        }

        public bool JustLeftClickReleased() {
            return _lastMouseState.LeftButton == ButtonState.Pressed &&
                   _currentMouseState.LeftButton == ButtonState.Released;
        }

        public bool JustRightClicked() {
            return _lastMouseState.RightButton == ButtonState.Released &&
                   _currentMouseState.RightButton == ButtonState.Pressed;
        }

        public bool JustRightClickReleased() {
            return _lastMouseState.RightButton == ButtonState.Pressed &&
                   _currentMouseState.RightButton == ButtonState.Released;
        }

        public bool JustMiddleClicked() {
            return _lastMouseState.MiddleButton == ButtonState.Released &&
                   _currentMouseState.MiddleButton == ButtonState.Pressed;
        }

        public bool JustMiddleClickReleased() {
            return _lastMouseState.MiddleButton == ButtonState.Pressed &&
                   _currentMouseState.MiddleButton == ButtonState.Released;
        }


    }

    public class GameInputManager {
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