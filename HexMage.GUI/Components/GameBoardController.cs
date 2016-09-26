using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        private readonly ReplayRecorder _replayRecorder;
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

        public GameBoardController(GameInstance gameInstance, GameEventHub eventHub, ReplayRecorder replayRecorder,
                                   ArenaScene arenaScene) {
            _gameInstance = gameInstance;
            _eventHub = eventHub;
            _replayRecorder = replayRecorder;
            _arenaScene = arenaScene;
        }

        public async Task<bool> EventAbilityUsed(Mob mob, Mob target, UsableAbility usableAbility) {
            var ability = usableAbility.Ability;

            Utils.Log(LogSeverity.Info, nameof(GameBoardController), "EventAbilityUsed");

            BuildUsedAbilityPopover(mob, usableAbility.Ability)
                .LogContinuation();

            var projectileSprite = AssetManager.ProjectileSpriteForElement(ability.Element);

            var projectileAnimation = new Animation(projectileSprite,
                                                    TimeSpan.FromMilliseconds(50),
                                                    AssetManager.TileSize,
                                                    4);

            projectileAnimation.Origin = new Vector2(AssetManager.TileSize/2, AssetManager.TileSize/2);

            var projectile = new ProjectileEntity(
                TimeSpan.FromMilliseconds(1500),
                mob.Coord,
                target.Coord) {
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

            explosion.AddComponent(new PositionAtMob(target));

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

            return true;
        }

        public async Task<bool> EventMobMoved(Mob mob, AxialCoord pos) {
            Debug.Assert(mob.Metadata != null, "Trying to move a mob without an associated entity.");
            var entity = (MobEntity) mob.Metadata;

            var path = _gameInstance.Pathfinder.PathTo(pos).Reverse().Skip(1);
            AxialCoord source = mob.Coord;
            foreach (var destination in path) {
                await entity.MoveTo(source, destination);
                source = destination;
            }

            return true;
        }

        public Task<bool> EventDefenseDesireAcquired(Mob mob, DefenseDesire defenseDesireResult) {
            return Task.FromResult(true);
        }

        public override void Initialize(AssetManager assetManager) {
            AssertNotInitialized();
            _assetManager = assetManager;

            BuildPopovers();
            CreateMobEntities(assetManager);

            _eventHub.MainLoop(TimeSpan.FromMilliseconds(200))
                     .ContinueWith(async t => {
                                       Utils.Log(LogSeverity.Info, nameof(GameBoardController),
                                                 "Finished waiting for main loop to exit");
                                       t.LogTask();

                                       await ShowMessage("Game complete");
                                   });
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

            if (inputManager.IsKeyJustPressed(Keys.P)) {
                _replayRecorder.DumpReplay(Console.Out);
            }

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
            var mob = _gameInstance.TurnManager.CurrentMob;
            if (mob != null && SelectedAbilityIndex.HasValue) {
                var selectedAbility = mob.Abilities[SelectedAbilityIndex.Value];

                if (selectedAbility.Cost > mob.Ap) {
                    SelectedAbilityIndex = null;
                }
            }
        }

        private void HandleUserTurnInput(InputManager inputManager) {
            if (inputManager.JustLeftClickReleased()) EnqueueClickEvent(HandleLeftClick);
            else if (inputManager.IsKeyJustReleased(Keys.R))
                _gameInstance.TurnManager.CurrentController.RandomAction(_eventHub);
        }

        private void HandleKeyboardAbilitySelect() {
            var inputManager = InputManager.Instance;

            if (inputManager.IsKeyJustReleased(Keys.D1)) SelectAbility(0);
            else if (inputManager.IsKeyJustReleased(Keys.D2)) SelectAbility(1);
            else if (inputManager.IsKeyJustReleased(Keys.D3)) SelectAbility(2);
            else if (inputManager.IsKeyJustReleased(Keys.D4)) SelectAbility(3);
            else if (inputManager.IsKeyJustReleased(Keys.D5)) SelectAbility(4);
            else if (inputManager.IsKeyJustReleased(Keys.D6)) SelectAbility(5);
        }

        public void SelectAbility(int index) {
            var currentMob = _gameInstance.TurnManager.CurrentMob;
            var ability = currentMob.Abilities[index];

            if (SelectedAbilityIndex.HasValue) {
                if (SelectedAbilityIndex.Value == index) {
                    SelectedAbilityIndex = null;
                } else if (_gameInstance.IsAbilityUsable(currentMob, ability)) {
                    SelectedAbilityIndex = index;
                }
            } else if (_gameInstance.IsAbilityUsable(currentMob, ability)) {
                    SelectedAbilityIndex = index;
            }
        }

        private void AttackMob(Mob target) {
            var usableAbilities = _gameInstance.UsableAbilities(
                _gameInstance.TurnManager.CurrentMob,
                target);

            Debug.Assert(SelectedAbilityIndex != null,
                         "_gameInstance.TurnManager.SelectedAbilityIndex != null");

            var abilityIndex = SelectedAbilityIndex.Value;
            var ability = _gameInstance.TurnManager.CurrentMob.Abilities[abilityIndex];

            var usableAbility = usableAbilities.FirstOrDefault(ua => ua.Ability == ability);
            if (usableAbility != null) {
                _eventHub.BroadcastAbilityUsed(_gameInstance.TurnManager.CurrentMob, target, usableAbility)
                         .LogContinuation();
            } else {
                ShowMessage("You can't use the selected ability on that target.");
            }
        }

        private void HandleLeftClick() {
            var abilitySelected = SelectedAbilityIndex.HasValue;

            var mouseHex = Camera2D.Instance.MouseHex;
            var currentMob = _gameInstance.TurnManager.CurrentMob;

            if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                var mob = _gameInstance.MobManager.AtCoord(mouseHex);
                if (mob != null) {
                    if (mob == currentMob) {
                        ShowMessage("You can't target yourself.");
                    } else {
                        if (mob.Hp == 0) {
                            ShowMessage("This mob is already dead.");
                        } else {
                            if (mob.Team == currentMob.Team) {
                                ShowMessage("You can't target your team.");
                            } else if (SelectedAbilityIndex.HasValue) {
                                if (abilitySelected) {
                                    AttackMob(mob);
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
                        if (_gameInstance.Map[mouseHex] == HexType.Empty) {
                            var distance = currentMob.Coord.Distance(mouseHex);

                            if (distance > currentMob.Ap) {
                                ShowMessage("You don't have enough AP.");
                            } else {
                                _eventHub.BroadcastMobMoved(currentMob, mouseHex)
                                         .ContinueWith((t, o) => t.LogContinuation(),
                                                       TaskContinuationOptions.LongRunning,
                                                       TaskScheduler.Default);
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
                var mob = _gameInstance.MobManager.AtCoord(mouseHex);

                if (mob == null) {
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
                    mobTextBuilder.AppendLine($"HP {mob.Hp}/{mob.MaxHp}\nAP {mob.Ap}/{mob.MaxAp}");
                    mobTextBuilder.AppendLine($"Iniciative: {mob.Iniciative}");
                    mobTextBuilder.AppendLine();

                    mobTextBuilder.AppendLine("Buffs:");
                    foreach (var buff in mob.Buffs)
                        mobTextBuilder.AppendLine(
                            $"  {buff.Element} - {buff.HpChange}/{buff.ApChange} for {buff.Lifetime} turns {buff.MoveSpeedModifier}spd");

                    mobTextBuilder.AppendLine();
                    mobTextBuilder.AppendLine("Area buffs:");

                    var areaBuffs = _gameInstance.Map.BuffsAt(mob.Coord);
                    foreach (var buff in areaBuffs)
                        mobTextBuilder.AppendLine(
                            $"  {buff.Element} - {buff.HpChange}/{buff.ApChange} for {buff.Lifetime} turns {buff.MoveSpeedModifier}spd");

                    mobTextBuilder.AppendLine();
                    mobTextBuilder.AppendLine($"Coord {mob.Coord}");

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

        private async Task BuildUsedAbilityPopover(Mob mob, Ability ability) {
            var result = new VerticalLayout {
                Renderer = new ColorRenderer(Color.LightGray),
                Padding = _popoverPadding,
                SortOrder = Camera2D.SortUI + 1000,
                Hidden = false
            };

            var camera = Camera2D.Instance;

            result.AddComponent(
                () => { result.Position = camera.HexToPixelWorld(mob.Coord) + _usedAbilityOffset; });

            string labelText = $"{ability.Dmg}DMG cost {ability.Cost}";
            result.AddChild(new Label(labelText, _assetManager.Font));

            Entity.Scene.AddAndInitializeRootEntity(result, _assetManager);

            await Task.Delay(_abilityPopoverDisplayTime);

            Entity.Scene.DestroyEntity(result);
        }
    }
}