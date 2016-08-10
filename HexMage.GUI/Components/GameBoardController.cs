using System;
using HexMage.GUI.Renderers;
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
        private VerticalLayout _messageBox;
        private Label _messageBoxLabel;
        private DateTime _displayMessageBoxUntil = DateTime.Now;
        private AssetManager _assetManager;

        public GameBoardController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public override void Initialize(AssetManager assetManager) {
            AssertNotInitialized();
            _assetManager = assetManager;

            {
                _messageBox = new VerticalLayout {
                    Renderer = new ColorRenderer(Color.White),
                    Padding = new Vector4(20, 10, 20, 10),
                    SortOrder = Camera2D.SortUI,
                    //Projection = () => Camera2D.Instance.Projection,
                    Position = new Vector2(500, 50)
                };

                _messageBoxLabel = _messageBox.AddChild(new Label("Message Box", assetManager.Font));

                Entity.Scene.AddRootEntity(_messageBox);
                _messageBox.InitializeEntity(assetManager);
            }

            {
                _emptyHexPopover = new VerticalLayout {
                    Renderer = new ColorRenderer(Color.LightGray),
                    Padding = new Vector4(20, 10, 20, 10),
                    SortOrder = Camera2D.SortUI,
                    Projection = () => Camera2D.Instance.Projection
                };

                _emptyHexLabel = _emptyHexPopover.AddChild(new Label("Just an empty hex", assetManager.Font));

                Entity.Scene.AddRootEntity(_emptyHexPopover);
                _emptyHexPopover.InitializeEntity(assetManager);
            }

            {
                _mobPopover = new VerticalLayout {
                    Renderer = new ColorRenderer(Color.LightGray),
                    Padding = new Vector4(20, 10, 20, 10),
                    SortOrder = Camera2D.SortUI,
                    Projection = () => Camera2D.Instance.Projection
                };

                _mobHealthLabel = _mobPopover.AddChild(new Label("Mob health", assetManager.Font));

                Entity.Scene.AddRootEntity(_mobPopover);
                _mobPopover.InitializeEntity(assetManager);
            }

            foreach (var mob in _gameInstance.MobManager.Mobs) {
                var mobAnimationController = new MobAnimationController();

                var mobEntity = new MobEntity(mob, _gameInstance) {
                    //Renderer = new SpriteRenderer(assetManager[AssetManager.MobTexture]),
                    Renderer = new MobRenderer(_gameInstance, mob, mobAnimationController),
                    SortOrder = Camera2D.SortMobs,
                    Projection = () => Camera2D.Instance.Projection
                };
                mob.Metadata = mobEntity;
                mobEntity.AddComponent(mobAnimationController);
                mobEntity.AddComponent(new MobStateUpdater(mob));

                Entity.Scene.AddRootEntity(mobEntity);
                mobEntity.InitializeEntity(assetManager);
            }
        }

        public void ShowMessage(string message, int displayForSeconds = 5) {
            _messageBoxLabel.Text = message;
            _displayMessageBoxUntil = DateTime.Now.Add(TimeSpan.FromSeconds(displayForSeconds));
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

            if (inputManager.JustLeftClickReleased()) {
                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    var mob = _gameInstance.MobManager.AtCoord(mouseHex);
                    if (mob != null) {
                        if (mob == _gameInstance.TurnManager.CurrentMob) {
                            ShowMessage("You can't target yourself.");
                        } else {
                            if (mob.Team.Color == _gameInstance.TurnManager.CurrentMob.Team.Color) {
                                ShowMessage("You can't target your team.");
                            } else {
                                _gameInstance.TurnManager.CurrentTarget = mob;

                                var fireballAnimation = new Animation(AssetManager.FireballSprite,
                                    TimeSpan.FromMilliseconds(50),
                                    32,
                                    4);

                                fireballAnimation.Origin = new Vector2(16, 16);

                                var fireball = new ProjectileEntity(
                                    TimeSpan.FromMilliseconds(1500),
                                    _gameInstance.TurnManager.CurrentMob.Coord,
                                    _gameInstance.TurnManager.CurrentTarget.Coord) {
                                        Renderer = new AnimationRenderer(fireballAnimation),
                                        SortOrder = Camera2D.SortProjectiles,
                                        Projection = () => Camera2D.Instance.Projection
                                    };

                                fireball.AddComponent(new AnimationController(fireballAnimation));

                                Entity.Scene.AddAndInitializeNextFrame(fireball);
                            }
                        }
                    }
                }
            }

            var position = Camera2D.Instance.HexToPixel(mouseHex) + new Vector2(40, -15);
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

            if (_displayMessageBoxUntil < DateTime.Now) {
                _messageBox.Active = false;
            } else {
                _messageBox.Active = true;
            }

            UpdateMobPositions();
        }

        private void UpdateMobPositions() {
            foreach (var mob in _gameInstance.MobManager.Mobs) {
                var mobEntity = (MobEntity) mob.Metadata;
                mobEntity.Position = Camera2D.Instance.HexToPixel(mob.Coord);
            }
        }
    }
}