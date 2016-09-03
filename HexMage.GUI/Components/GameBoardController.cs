using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                };

                _mobHealthLabel = _mobPopover.AddChild(new Label("Mob health", assetManager.Font));

                Entity.Scene.AddRootEntity(_mobPopover);
                _mobPopover.InitializeEntity(assetManager);
            }

            CreateMobEntities(assetManager);
        }

        private void CreateMobEntities(AssetManager assetManager) {
            foreach (var mob in _gameInstance.MobManager.Mobs) {
                var mobAnimationController = new MobAnimationController();

                var mobEntity = new MobEntity(mob, _gameInstance) {
                    Renderer = new MobRenderer(_gameInstance, mob, mobAnimationController),
                    SortOrder = Camera2D.SortMobs,
                    Transform = () => Camera2D.Instance.Transform
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
            HandleKeyboardAbilitySelect();

            var inputManager = InputManager.Instance;
            var mouseHex = Camera2D.Instance.MouseHex;

            if (inputManager.JustRightClicked()) {
                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    _gameInstance.Map.Toogle(mouseHex);

                    // TODO - podivat se na generickou implementaci pathfinderu
                    // TODO - pathfindovani ze zdi najde cesty
                    _gameInstance.Pathfinder.PathfindFrom(_gameInstance.TurnManager.CurrentMob.Coord);
                }
            }

            if (inputManager.IsKeyJustPressed(Keys.Space)) {
                _gameInstance.TurnManager.NextMobOrNewTurn();
                // TODO - fix this, it's ugly
                _gameInstance.Pathfinder.PathfindFrom(_gameInstance.TurnManager.CurrentMob.Coord);
            }

            HandleUserTurnInput(inputManager, mouseHex);

            UpdatePopovers(time, mouseHex);
        }

        private void HandleUserTurnInput(InputManager inputManager, AxialCoord mouseHex) {
            bool abilitySelected = _gameInstance.TurnManager.SelectedAbilityIndex.HasValue;

            var currentMob = _gameInstance.TurnManager.CurrentMob;
            if (inputManager.JustLeftClickReleased()) {
                EnqueueClickEvent(() => {
                    Console.WriteLine($"MOB EVENT, time {DateTime.Now.Millisecond}");
                    if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                        var mob = _gameInstance.MobManager.AtCoord(mouseHex);
                        if (mob != null) {
                            if (mob == currentMob) {
                                ShowMessage("You can't target yourself.");
                            } else {
                                if (mob.Team.Color == currentMob.Team.Color) {
                                    ShowMessage("You can't target your team.");
                                } else if (_gameInstance.TurnManager.SelectedAbilityIndex.HasValue) {
                                    if (abilitySelected) {
                                        AttackMob(mob);
                                    } else {
                                        ShowMessage("You can't move here.");
                                    }
                                }
                            }
                        } else {
                            if (abilitySelected) {
                                ShowMessage("Select an ability to use first.");
                            } else {
                                MoveTo(currentMob, mouseHex);
                            }
                        }
                    }
                });
            }
        }

        private readonly Vector2 _mouseHoverPopoverOffset = new Vector2(
            0.5f*AssetManager.TileSize, -0.5f*AssetManager.TileSize);

        private void UpdatePopovers(GameTime time, AxialCoord mouseHex) {
            var camera = Camera2D.Instance;
            var position = camera.MousePixelPos + _mouseHoverPopoverOffset;
            var sin = (float) Math.Sin(time.TotalGameTime.TotalSeconds);
            var offset = sin*sin*new Vector2(0, -5);

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
        }

        private void HandleKeyboardAbilitySelect() {
            var inputManager = InputManager.Instance;

            if (inputManager.IsKeyJustReleased(Keys.D1)) {
                SelectAbility(0);
            } else if (inputManager.IsKeyJustReleased(Keys.D2)) {
                SelectAbility(1);
            } else if (inputManager.IsKeyJustReleased(Keys.D3)) {
                SelectAbility(2);
            } else if (inputManager.IsKeyJustReleased(Keys.D4)) {
                SelectAbility(3);
            } else if (inputManager.IsKeyJustReleased(Keys.D5)) {
                SelectAbility(4);
            } else if (inputManager.IsKeyJustReleased(Keys.D6)) {
                SelectAbility(5);
            }
        }

        private void SelectAbility(int index) {
            var currentMob = _gameInstance.TurnManager.CurrentMob;
            var ability = currentMob.Abilities[index];

            if (_gameInstance.IsAbilityUsable(currentMob, ability)) {
                _gameInstance.TurnManager.ToggleAbilitySelected(index);
            }
        }

        private void MoveTo(Mob currentMob, AxialCoord pos) {
            var distance = currentMob.Coord.Distance(pos);
            if (distance <= currentMob.AP) {
                var mobEntity = (MobEntity) currentMob.Metadata;
                mobEntity.MoveTo(pos);

                currentMob.AP -= distance;
                currentMob.Coord = pos;
                _gameInstance.Pathfinder.PathfindFrom(pos);
            }
        }

        private void AttackMob(Mob mob) {
            _gameInstance.TurnManager.CurrentTarget = mob;

            var usableAbilities = _gameInstance.UsableAbilities(
                _gameInstance.TurnManager.CurrentMob,
                mob);

            Debug.Assert(_gameInstance.TurnManager.SelectedAbilityIndex != null,
                         "_gameInstance.TurnManager.SelectedAbilityIndex != null");

            var abilityIndex = _gameInstance.TurnManager.SelectedAbilityIndex.Value;
            var ability = _gameInstance.TurnManager.CurrentMob.Abilities[abilityIndex];

            var usableAbility = usableAbilities.FirstOrDefault(ua => ua.Ability == ability);
            if (usableAbility != null) {
                var projectileSprite = AssetManager.ProjectileSpriteForElement(ability.Element);

                const int numberOfFrames = 4;
                var projectileAnimation = new Animation(projectileSprite,
                                                        TimeSpan.FromMilliseconds(50),
                                                        AssetManager.TileSize,
                                                        numberOfFrames);

                projectileAnimation.Origin = new Vector2(16, 16);

                var projectile = new ProjectileEntity(
                    TimeSpan.FromMilliseconds(1500),
                    _gameInstance.TurnManager.CurrentMob.Coord,
                    _gameInstance.TurnManager.CurrentTarget.Coord) {
                        Renderer = new AnimationRenderer(projectileAnimation),
                        SortOrder = Camera2D.SortProjectiles,
                        Transform = () => Camera2D.Instance.Transform
                    };

                projectile.AddComponent(new AnimationController(projectileAnimation));

                var target = _gameInstance.TurnManager.CurrentTarget;

                projectile.TargetHit += async () => {
                    await usableAbility.Use();

                    var explosion = new Entity() {
                        Transform = () => Camera2D.Instance.Transform,
                        SortOrder = Camera2D.SortProjectiles
                    };

                    explosion.AddComponent(new PositionAtMob(target));

                    const int totalAnimationFrames = 4;

                    var explosionSprite = AssetManager.ProjectileExplosionSpriteForElement(ability.Element);

                    var explosionAnimation = new Animation(
                        explosionSprite,
                        TimeSpan.FromMilliseconds(350),
                        AssetManager.TileSize,
                        totalAnimationFrames);

                    explosionAnimation.AnimationDone += () => { Entity.Scene.DestroyEntity(explosion); };

                    explosion.Renderer = new AnimationRenderer(explosionAnimation);
                    explosion.AddComponent(new AnimationController(explosionAnimation));

                    Entity.Scene.AddAndInitializeNextFrame(explosion);

                    Entity.Scene.DestroyEntity(projectile);
                };

                Entity.Scene.AddAndInitializeNextFrame(projectile);
            } else {
                ShowMessage("You can't use the selected ability on that target.");
            }
        }
    }
}