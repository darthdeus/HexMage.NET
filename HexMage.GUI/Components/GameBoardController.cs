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
        private Entity _emptyHexPopover;
        private VerticalLayout _mobPopover;
        private Label _emptyHexLabel;
        private Label _mobHealthLabel;

        public GameBoardController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public override void Initialize(AssetManager assetManager) {
            AssertNotInitialized();

            {
                _emptyHexPopover = new VerticalLayout {
                    Renderer = new ColorRenderer(Color.LightGray),
                    Padding = new Vector4(20, 10, 20, 10),
                    SortOrder = 1000
                };

                _emptyHexLabel = _emptyHexPopover.AddChild(new Label("Just an empty hex", assetManager.Font));

                Entity.Scene.AddRootEntity(_emptyHexPopover);
                _emptyHexPopover.InitializeEntity(assetManager);
            }

            {
                _mobPopover = new VerticalLayout {
                    Renderer = new ColorRenderer(Color.LightGray),
                    Padding = new Vector4(20, 10, 20, 10),
                    SortOrder = 1000
                };

                _mobHealthLabel = _mobPopover.AddChild(new Label("Mob health", assetManager.Font));

                Entity.Scene.AddRootEntity(_mobPopover);
                _mobPopover.InitializeEntity(assetManager);
            }
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

            var position = Camera2D.Instance.HexToPixel(mouseHex) + new Vector2(30, -40);
            var sin = (float) Math.Sin(time.TotalGameTime.TotalSeconds*2);
            var offset = sin*sin*new Vector2(0, -3);

            _emptyHexPopover.Position = position + offset;
            _mobPopover.Position = position + offset;

            _emptyHexPopover.Active = false;
            _mobPopover.Active = false;

            if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                var mob = _gameInstance.MobManager.AtCoord(mouseHex);

                if (mob == null) {
                    switch (_gameInstance.Map[mouseHex]) {
                        case HexType.Empty:
                            _emptyHexPopover.Active = true;
                            _emptyHexLabel.Text = "Just an empty hex.";
                            break;

                        case HexType.Wall:
                            _emptyHexPopover.Active = true;
                            _emptyHexLabel.Text = "Indestructible wall";
                            break;
                    }
                } else {
                    _mobPopover.Active = true;
                    _mobHealthLabel.Text = $"HP {mob.HP}/{mob.MaxHP}\nAP {mob.AP}/{mob.MaxAP}";
                }
            } else {
                _emptyHexPopover.Active = false;
                _mobPopover.Active = false;
            }
        }
    }
}