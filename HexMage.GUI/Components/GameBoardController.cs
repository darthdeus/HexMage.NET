using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using HexMage.GUI.Scenes;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Components {
    public class GameBoardController : Component, IGameEventSubscriber {
        private readonly GameEventHub _eventHub;
        private readonly ArenaScene _arenaScene;
        private readonly GameInstance _gameInstance;

        private readonly Vector2 _mouseHoverPopoverOffset = new Vector2(
            0.5f*AssetManager.TileSize, -0.5f*AssetManager.TileSize);

        private readonly Vector2 _usedAbilityOffset = new Vector2(80, -20);
        private AssetManager _assetManager;
        private DateTime _displayMessageBoxUntil = DateTime.Now;
        private Label _emptyHexLabel;
        private Entity _emptyHexPopover;
        private VerticalLayout _messageBox;
        private Label _messageBoxLabel;
        private Label _mobHealthLabel;
        private VerticalLayout _mobPopover;
        private Vector4 _popoverPadding;

        public int? SelectedAbilityIndex;

        public GameBoardController(GameInstance gameInstance, GameEventHub eventHub, ArenaScene arenaScene) {
            _gameInstance = gameInstance;
            _eventHub = eventHub;
            _arenaScene = arenaScene;
        }

#warning TODO - async void with no error handling
        public async void EventAbilityUsed(MobId mobId, MobId targetId, Ability ability) {
            Utils.Log(LogSeverity.Info, nameof(GameBoardController), "EventAbilityUsed");

            var mobInstance = _gameInstance.MobManager.MobInstanceForId(mobId);
            var targetInstance = _gameInstance.MobManager.MobInstanceForId(targetId);

            BuildUsedAbilityPopover(mobId, ability).LogContinuation();

            var projectileSprite = AssetManager.ProjectileSpriteForElement(ability.Element);

            var projectileAnimation = new Animation(projectileSprite,
                                                    TimeSpan.FromMilliseconds(50),
                                                    AssetManager.TileSize,
                                                    4);

            projectileAnimation.Origin = new Vector2(AssetManager.TileSize/2, AssetManager.TileSize/2);

            var projectile = new ProjectileEntity(
                TimeSpan.FromMilliseconds(1500),
                mobInstance.Coord,
                targetInstance.Coord) {
                Renderer = new AnimationRenderer(projectileAnimation),
                SortOrder = Camera2D.SortProjectiles,
                Transform = () => Camera2D.Instance.Transform
            };

            projectile.AddComponent(new AnimationController(projectileAnimation));

            Entity.Scene.AddAndInitializeNextFrame(projectile);

            await projectile.Task;

            var explosion = new Entity {
                Transform = () => Camera2D.Instance.Transform,
                SortOrder = Camera2D.SortProjectiles
            };

            explosion.AddComponent(new PositionAtMob(_gameInstance.MobManager, targetId));

            var explosionSprite = AssetManager.ProjectileExplosionSpriteForElement(ability.Element);

            var explosionAnimation = new Animation(
                explosionSprite,
                TimeSpan.FromMilliseconds(350),
                AssetManager.TileSize,
                4);

            explosionAnimation.AnimationDone += () => { Entity.Scene.DestroyEntity(explosion); };

            explosion.Renderer = new AnimationRenderer(explosionAnimation);
            explosion.AddComponent(new AnimationController(explosionAnimation));

            Entity.Scene.AddAndInitializeNextFrame(explosion);

            Entity.Scene.DestroyEntity(projectile);
        }

        public async void EventMobMoved(MobId mob, AxialCoord pos) {
            var mobEntity = _arenaScene.MobEntities[mob];
            Debug.Assert(mobEntity != null, "Trying to move a mob without an associated entity.");

            var path = _gameInstance.Pathfinder.PathTo(pos).Reverse();
            foreach (var coord in path) {
                await mobEntity.MoveTo(_gameInstance.MobManager.MobInstanceForId(mob).Coord, coord);
            }
        }

        public void EventDefenseDesireAcquired(MobId mob, DefenseDesire defenseDesireResult) {
            ShowMessage($"{nameof(GameBoardController)} got defense {defenseDesireResult}");
        }

        public override void Initialize(AssetManager assetManager) {
            AssertNotInitialized();
            _assetManager = assetManager;

            BuildPopovers();

            new Thread(() => { _eventHub.FastMainLoop(TimeSpan.FromMilliseconds(500)); }).Start();
        }

        public Task ShowMessage(string message, int displayForSeconds = 5) {
            _messageBoxLabel.Text = message;
            _displayMessageBoxUntil = DateTime.Now.Add(TimeSpan.FromSeconds(displayForSeconds));

            return Task.Delay(TimeSpan.FromSeconds(displayForSeconds));
        }

        public override void Update(GameTime time) {
            HandleKeyboardAbilitySelect();

            UnselectAbilityIfNeeded();

            var inputManager = InputManager.Instance;
            var mouseHex = Camera2D.Instance.MouseHex;

            if (inputManager.IsKeyJustPressed(Keys.Pause)) {
                if (_eventHub.IsPaused) {
                    ShowMessage("Game resumed.");
                } else {
                    ShowMessage("Game paused.");
                }
                _eventHub.IsPaused = !_eventHub.IsPaused;
            }

            var controller = _gameInstance.TurnManager.CurrentController as PlayerController;
            if (controller != null) {
                if (inputManager.JustRightClicked())
                    if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                        _gameInstance.Map.Toogle(mouseHex);

                        // TODO - podivat se na generickou implementaci pathfinderu
                        // TODO - pathfindovani ze zdi najde cesty
                        _gameInstance.Pathfinder.PathfindFromCurrentMob(_gameInstance.TurnManager);
                    }

                if (inputManager.IsKeyJustPressed(Keys.Space)) {
                    controller.PlayerEndedTurn();
                    ShowMessage("Starting new turn!");
                }

                HandleUserTurnInput(inputManager);
            }

            UpdatePopovers(time, mouseHex);
        }

        private void UnselectAbilityIfNeeded() {
            var mobId = _gameInstance.TurnManager.CurrentMob;
            if (mobId != null && SelectedAbilityIndex.HasValue) {
                var mobInfo = _gameInstance.MobManager.MobInfoForId(mobId.Value);
                var selectedAbility = mobInfo.Abilities[SelectedAbilityIndex.Value];

                if (!_gameInstance.IsAbilityUsable(mobId.Value, selectedAbility)) {
                    SelectedAbilityIndex = null;
                }
            }
        }

        private void HandleUserTurnInput(InputManager inputManager) {
            if (inputManager.JustLeftClickReleased()) {
                EnqueueClickEvent(HandleLeftClick);
            } else if (inputManager.IsKeyJustReleased(Keys.R)) {
#warning TODO - implement this
                //_gameInstance.TurnManager.CurrentController.RandomAction(_eventHub);
            }
        }

        private void HandleKeyboardAbilitySelect() {
            var inputManager = InputManager.Instance;

            if (inputManager.IsKeyJustReleased(Keys.D1)) SelectAbility(0);
            else if (inputManager.IsKeyJustReleased(Keys.D2)) SelectAbility(1);
            else if (inputManager.IsKeyJustReleased(Keys.D3)) SelectAbility(2);
            else if (inputManager.IsKeyJustReleased(Keys.D4)) SelectAbility(3);
            else if (inputManager.IsKeyJustReleased(Keys.D5)) SelectAbility(4);
            else if (inputManager.IsKeyJustReleased(Keys.D6)) SelectAbility(5);

            if (inputManager.IsKeyJustReleased(Keys.F1)) {
                ((GameBoardRenderer) Entity.Renderer).Mode = BoardRenderMode.Default;
            } else if (inputManager.IsKeyJustReleased(Keys.F2)) {
                ((GameBoardRenderer) Entity.Renderer).Mode = BoardRenderMode.HoverHeatmap;
            } else if (inputManager.IsKeyJustReleased(Keys.F3)) {
                ((GameBoardRenderer) Entity.Renderer).Mode = BoardRenderMode.GlobalHeatmap;
            }
        }

        public void SelectAbility(int index) {
            var currentMob = _gameInstance.TurnManager.CurrentMob;
            if (currentMob == null) return;

            var mobInfo = _gameInstance.MobManager.MobInfoForId(currentMob.Value);
            var ability = mobInfo.Abilities[index];

            if (SelectedAbilityIndex.HasValue) {
                if (SelectedAbilityIndex.Value == index) {
                    SelectedAbilityIndex = null;
                } else if (_gameInstance.IsAbilityUsable(currentMob.Value, ability)) {
                    SelectedAbilityIndex = index;
                }
            } else if (_gameInstance.IsAbilityUsable(currentMob.Value, ability)) {
                SelectedAbilityIndex = index;
            }
        }

        private void AttackMob(MobId targetId) {
            Debug.Assert(SelectedAbilityIndex != null,
                         "_gameInstance.TurnManager.SelectedAbilityIndex != null");

            var abilityIndex = SelectedAbilityIndex.Value;
            var mobId = _gameInstance.TurnManager.CurrentMob;

            Debug.Assert(mobId != null);
            var abilityId = _gameInstance.MobManager.MobInfos[mobId.Value].Abilities[abilityIndex].Id;

            _eventHub.BroadcastAbilityUsed(new MobId(mobId.Value), targetId, new AbilityId(abilityId));
        }

        private void HandleLeftClick() {
            var abilitySelected = SelectedAbilityIndex.HasValue;

            var mouseHex = Camera2D.Instance.MouseHex;
            var currentMob = _gameInstance.TurnManager.CurrentMob;

            if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                var targetId = _gameInstance.MobManager.AtCoord(mouseHex);
                if (targetId != null) {
                    if (targetId == currentMob) {
                        ShowMessage("You can't target yourself.");
                    } else {
                        var mobInfo = _gameInstance.MobManager.MobInfoForId(currentMob.Value);
                        var targetInstance = _gameInstance.MobManager.MobInstanceForId(targetId.Value);
                        var targetInfo = _gameInstance.MobManager.MobInfoForId(targetId.Value);

                        if (targetInstance.Hp == 0) {
                            ShowMessage("This mob is already dead.");
                        } else {
                            if (mobInfo.Team == targetInfo.Team) {
                                ShowMessage("You can't target your team.");
                            } else if (SelectedAbilityIndex.HasValue) {
                                if (abilitySelected) {
                                    AttackMob(targetId.Value);
                                } else {
                                    ShowMessage("You can't move here.");
                                }
                            }
                        }
                    }
                } else {
                    if (abilitySelected) {
                        ShowMessage("You can't cast spells on the ground.");
                    } else {
                        var mobInstance = _gameInstance.MobManager.MobInstanceForId(currentMob.Value);
                        var mobInfo = _gameInstance.MobManager.MobInfoForId(currentMob.Value);

                        if (_gameInstance.Map[mouseHex] == HexType.Empty) {
                            var distance = mobInstance.Coord.Distance(mouseHex);

                            if (distance > mobInstance.Ap) {
                                ShowMessage("You don't have enough AP.");
                            } else {
                                _eventHub.BroadcastMobMoved(currentMob.Value, mouseHex);
                            }
                        } else {
                            ShowMessage("You can't walk into a wall.");
                        }
                    }
                }
            }
        }

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
                var mobId = _gameInstance.MobManager.AtCoord(mouseHex);

                if (mobId == null) {
                    var map = _gameInstance.Map;

                    var labelText = new StringBuilder();

                    switch (map[mouseHex]) {
                        case HexType.Empty:
                            _emptyHexPopover.Active = true;
                            labelText.AppendLine("Empty hex");
                            labelText.AppendLine($"Coord: {mouseHex}");
                            break;

                        case HexType.Wall:
                            _emptyHexPopover.Active = true;
                            labelText.AppendLine("Indestructible wall");
                            labelText.AppendLine($"Coord: {mouseHex}");
                            break;
                    }

                    var buffs = map.BuffsAt(mouseHex);
                    Debug.Assert(buffs != null,
                                 "Buffs can't be null since we're only using valid map coords (and those are all initialized).");

                    foreach (var buff in buffs)
                        labelText.AppendLine($"{buff.HpChange}/{buff.ApChange} for {buff.Lifetime} turns");

                    _emptyHexLabel.Text = labelText.ToString();
                } else {
                    _mobPopover.Active = true;
                    var mobTextBuilder = new StringBuilder();
                    var mobInstance = _gameInstance.MobManager.MobInstanceForId(mobId.Value);
                    var mobInfo = _gameInstance.MobManager.MobInfoForId(mobId.Value);

                    mobTextBuilder.AppendLine(
                        $"HP {mobInstance.Hp}/{mobInfo.MaxHp}\nAP {mobInstance.Ap}/{mobInfo.MaxAp}");
                    mobTextBuilder.AppendLine($"Iniciative: {mobInfo.Iniciative}");
                    mobTextBuilder.AppendLine();

                    mobTextBuilder.AppendLine("Buffs:");
                    foreach (var buff in mobInstance.Buffs)
                        mobTextBuilder.AppendLine(
                            $"  {buff.Element} - {buff.HpChange}/{buff.ApChange} for {buff.Lifetime} turns");

                    mobTextBuilder.AppendLine();
                    mobTextBuilder.AppendLine("Area buffs:");

                    foreach (var buff in _gameInstance.Map.BuffsAt(mobInstance.Coord)) {
                        mobTextBuilder.AppendLine(
                            $"  {buff.Element} - {buff.HpChange}/{buff.ApChange} for {buff.Lifetime} turns");
                    }

                    mobTextBuilder.AppendLine();
                    mobTextBuilder.AppendLine($"Coord {mobInstance.Coord}");

                    _mobHealthLabel.Text = mobTextBuilder.ToString();
                }
            } else {
                _emptyHexPopover.Active = false;
                _mobPopover.Active = false;
            }

            _messageBox.Active = _displayMessageBoxUntil >= DateTime.Now;
        }

        private void BuildPopovers() {
            _popoverPadding = new Vector4(20, 10, 20, 10);

            {
                _messageBox = new VerticalLayout {
                    Renderer = new ColorRenderer(Color.White),
                    Padding = _popoverPadding,
                    SortOrder = Camera2D.SortUI,
                    Position = new Vector2(500, 50)
                };

                _messageBoxLabel = _messageBox.AddChild(new Label("Message Box", _assetManager.Font));
                Entity.Scene.AddAndInitializeRootEntity(_messageBox, _assetManager);
            }

            {
                _emptyHexPopover = new VerticalLayout {
                    Renderer = new ColorRenderer(Color.LightGray),
                    Padding = _popoverPadding,
                    SortOrder = Camera2D.SortUI
                };

                _emptyHexLabel = _emptyHexPopover.AddChild(new Label("Just an empty hex", _assetManager.Font));
                Entity.Scene.AddAndInitializeRootEntity(_emptyHexPopover, _assetManager);
            }

            {
                _mobPopover = new VerticalLayout {
                    Renderer = new ColorRenderer(Color.LightGray),
                    Padding = _popoverPadding,
                    SortOrder = Camera2D.SortUI
                };

                _mobHealthLabel = _mobPopover.AddChild(new Label("Mob health", _assetManager.Font));
                Entity.Scene.AddAndInitializeRootEntity(_mobPopover, _assetManager);
            }
        }

        private readonly TimeSpan _abilityPopoverDisplayTime = TimeSpan.FromSeconds(3);

        private async Task BuildUsedAbilityPopover(MobId mobId, Ability ability) {
            var result = new VerticalLayout {
                Renderer = new ColorRenderer(Color.LightGray),
                Padding = _popoverPadding,
                SortOrder = Camera2D.SortUI + 1000,
                Hidden = false
            };

            var camera = Camera2D.Instance;

            result.AddComponent(
                () => {
                    result.Position = camera.HexToPixelWorld(_gameInstance.MobManager.MobInstanceForId(mobId).Coord) +
                                      _usedAbilityOffset;
                });

            string labelText = $"{ability.Dmg}DMG cost {ability.Cost}";
            result.AddChild(new Label(labelText, _assetManager.Font));

            Entity.Scene.AddAndInitializeRootEntity(result, _assetManager);

            await Task.Delay(_abilityPopoverDisplayTime);

            Entity.Scene.DestroyEntity(result);
        }
    }
}