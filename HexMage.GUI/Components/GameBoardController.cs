using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using HexMage.GUI.Scenes;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Components {
    public class GameBoardController : Component, IGameEventSubscriber {
        private readonly GameEventHub _eventHub;
        private readonly ArenaScene _arenaScene;
        private readonly Entity _crosshairCursor;
        private readonly GameInstance _game;

        private readonly Vector2 _mouseHoverPopoverOffset = new Vector2(
            0.5f * AssetManager.TileSize, -0.5f * AssetManager.TileSize);

        private readonly Vector2 _usedAbilityOffset = new Vector2(80, -20);
        private AssetManager _assetManager;
        private DateTime _displayMessageBoxUntil = DateTime.Now;
        private VerticalLayout _messageBox;
        private Label _messageBoxLabel;
        private Vector4 _popoverPadding;
        public Action GameFinishedCallback;

        private readonly Replay _replay;

        private readonly TimeSpan _abilityPopoverDisplayTime = TimeSpan.FromSeconds(3);

        public int? SelectedAbilityIndex;

        public GameBoardController(GameInstance game, Entity crosshairCursor,
                                   ArenaScene arenaScene, Replay replay = null) {
            _game = game;
            _arenaScene = arenaScene;
            _crosshairCursor = crosshairCursor;
            _replay = replay;
            _eventHub = new GameEventHub(game);
            _eventHub.AddSubscriber(this);
        }

        public void ActionApplied(UctAction action) {
            var mob = _game.CachedMob(action.MobId);
            var target = _game.CachedMob(action.TargetId);

            AbilityInfo abilityInfo = null;
            if (action.Type == UctActionType.AttackMove || action.Type == UctActionType.AbilityUse) {
                abilityInfo = _game.MobManager.Abilities[action.AbilityId];
            }

            int? moveCost = null;

            if (action.Type == UctActionType.AttackMove || action.Type == UctActionType.DefensiveMove
                || action.Type == UctActionType.Move) {
                moveCost = _game.Pathfinder.Distance(mob.MobInstance.Coord, action.Coord);
            }

            Debug.Assert(_game.CurrentTeam != null, "_gameInstance.CurrentTeam != null");

            HistoryLog.Instance.Log(_game.CurrentTeam.Value, action, mob, target, abilityInfo, moveCost);
        }

        public async Task SlowActionApplied(UctAction action) {
            if (action.Type == UctActionType.Move) {
                await SlowEventMobMoved(action.MobId, action.Coord);
            } else if (action.Type == UctActionType.AbilityUse) {
                await SlowEventAbilityUsed(action.MobId, action.TargetId, _game.MobManager.Abilities[action.AbilityId]);
            } else {
                throw new ArgumentException(
                    $"{nameof(GameBoardController)} only supports {nameof(UctActionType.Move)} and {nameof(UctActionType.AbilityUse)} actions.");
            }
        }

        public override void Initialize(AssetManager assetManager) {
            AssertNotInitialized();
            _assetManager = assetManager;

            Debug.Assert(Entity.Renderer == null);
            Entity.Renderer = new GameBoardRenderer(_game, this, _eventHub);

            BuildPopovers();
            var turnEndSound = _assetManager.LoadSoundEffect(AssetManager.SoundEffectEndTurn);

            if (_replay == null) {
                _eventHub.SlowMainLoop(() => turnEndSound.Play(0.3f, 0, 0), () => GameFinishedCallback?.Invoke())
                         .LogContinuation();
            } else {                
                _eventHub.PlayReplay(_replay.Actions, () => turnEndSound.Play(0.3f, 0, 0))
                         .LogContinuation();
            }
        }

        public override void Update(GameTime time) {
            UnselectAbilityIfNeeded();

            _crosshairCursor.Hidden = !SelectedAbilityIndex.HasValue;
            HexMageGame.Instance.IsMouseVisible = _crosshairCursor.Hidden;

            var inputManager = InputManager.Instance;
            var mouseHex = Camera2D.Instance.MouseHex;

            if (inputManager.IsKeyJustPressed(Keys.R) && Keyboard.GetState().IsKeyDown(Keys.LeftControl)) {
                SceneManager.RollbackToFirst = true;
                return;
            }

            if (inputManager.IsKeyJustPressed(Keys.Pause)) {
                // TODO - pausing
                if (_eventHub.IsPaused) {
                    ShowMessage("Game resumed.");
                } else {
                    ShowMessage("Game paused.");
                }
                _eventHub.IsPaused = !_eventHub.IsPaused;
            }

            var controller = _game.CurrentController as PlayerController;
            if (controller != null && inputManager.UserInputEnabled &&
                _eventHub.State == GameEventState.TurnInProgress) {
                HandleKeyboardAbilitySelect();

                if (inputManager.IsKeyJustPressed(Keys.Space)) {
                    controller.PlayerEndedTurn(_eventHub);
                    SelectedAbilityIndex = null;
                    ShowMessage("Starting new turn!");
                }

                if (inputManager.JustLeftClickReleased()) {
                    EnqueueClickEvent(HandleLeftClick);
                }
            }

            UpdatePopovers(time, mouseHex);
        }

        private void HandleKeyboardAbilitySelect() {
            var inputManager = InputManager.Instance;

            if (inputManager.IsKeyJustPressed(Keys.F10)) {
                var repr = new MapRepresentation(_game.Map);

                using (var writer = new StreamWriter(GameInstance.MapSaveFilename))
                using (var mobWriter = new StreamWriter(GameInstance.MobsSaveFilename)) {
                    writer.Write(JsonConvert.SerializeObject(repr));
                    mobWriter.Write(JsonConvert.SerializeObject(_game.MobManager));
                }
            }

            if (inputManager.IsKeyJustPressed(Keys.F11)) {
                using (var reader = new StreamReader(GameInstance.MapSaveFilename))
                using (var mobReader = new StreamReader(GameInstance.MobsSaveFilename)) {
                    var mapRepr = JsonConvert.DeserializeObject<MapRepresentation>(reader.ReadToEnd());
                    mapRepr.UpdateMap(_game.Map);

                    var mobManager = JsonConvert.DeserializeObject<MobManager>(mobReader.ReadToEnd());
                    Console.WriteLine(mobManager);
                }
            }

            if (inputManager.IsKeyJustPressed(Keys.F12)) {
                foreach (var mobId in _game.MobManager.Mobs) {
                    var mobInfo = _game.MobManager.MobInfos[mobId];
                    var mobInstance = _game.State.MobInstances[mobId];
                    Console.WriteLine($"#{mobId} {mobInstance.Coord} {mobInfo}");
                }
            }

            if (inputManager.IsKeyJustPressed(Keys.D1)) SelectAbility(0);
            else if (inputManager.IsKeyJustPressed(Keys.D2)) SelectAbility(1);
            else if (inputManager.IsKeyJustPressed(Keys.D3)) SelectAbility(2);
            else if (inputManager.IsKeyJustPressed(Keys.D4)) SelectAbility(3);
            else if (inputManager.IsKeyJustPressed(Keys.D5)) SelectAbility(4);
            else if (inputManager.IsKeyJustPressed(Keys.D6)) SelectAbility(5);

            if (inputManager.IsKeyJustPressed(Keys.F1)) {
                ((GameBoardRenderer) Entity.Renderer).Mode = BoardRenderMode.Default;
            } else if (inputManager.IsKeyJustPressed(Keys.F2)) {
                ((GameBoardRenderer) Entity.Renderer).Mode = BoardRenderMode.HoverHeatmap;
            } else if (inputManager.IsKeyJustPressed(Keys.F3)) {
                ((GameBoardRenderer) Entity.Renderer).Mode = BoardRenderMode.GlobalHeatmap;
            } else if (inputManager.IsKeyJustPressed(Keys.F4)) {
                ((GameBoardRenderer) Entity.Renderer).Mode = BoardRenderMode.VisibilityMap;
            }
        }

        public void SelectAbility(int index) {
            var currentMob = _game.CurrentMob;
            if (currentMob == null) return;

            var mobInfo = _game.MobManager.MobInfos[currentMob.Value];
            if (index >= mobInfo.Abilities.Count) {
                Utils.Log(LogSeverity.Info, nameof(GameBoardController),
                          "Trying to select an ability index higher than the number of abilities.");
                return;
            }
            var ability = mobInfo.Abilities[index];

            if (SelectedAbilityIndex.HasValue) {
                if (SelectedAbilityIndex.Value == index) {
                    SelectedAbilityIndex = null;
                } else if (GameInvariants.IsAbilityUsableNoTarget(_game, currentMob.Value, ability)) {
                    SelectedAbilityIndex = index;
                }
            } else if (GameInvariants.IsAbilityUsableNoTarget(_game, currentMob.Value, ability)) {
                SelectedAbilityIndex = index;
            }
        }

        private void AttackMob(int targetId) {
            Debug.Assert(SelectedAbilityIndex != null,
                         "_gameInstance.TurnManager.SelectedAbilityIndex != null");

            var abilityIndex = SelectedAbilityIndex.Value;
            var mobId = _game.CurrentMob;

            Debug.Assert(mobId != null);
            var abilityId = _game.MobManager.MobInfos[mobId.Value].Abilities[abilityIndex];

            var visibilityPath =
                _game.Map.AxialLinedraw(_game.State.MobInstances[mobId.Value].Coord,
                                        _game.State.MobInstances[targetId].Coord);

            bool isVisible = true;
            foreach (var coord in visibilityPath) {
                if (_game.Map[coord] == HexType.Wall) {
                    isVisible = false;
                }
            }

            if (isVisible) {
                bool withinRange = (visibilityPath.Count - 1) <= _game.MobManager.Abilities[abilityId].Range;
                if (withinRange) {
                    InputManager.Instance.UserInputEnabled = false;
                    _eventHub.SlowPlayAction(_game, UctAction.AbilityUseAction(abilityId, mobId.Value, targetId))
                             .ContinueWith(t => { InputManager.Instance.UserInputEnabled = true; })
                             .LogContinuation();
                } else {
                    ShowMessage("Target is outside the range of the currently selected ability.");
                }
            } else {
                ShowMessage("The target is not visible.");
            }
        }

        private void HandleLeftClick() {
            var abilitySelected = SelectedAbilityIndex.HasValue;

            var mouseHex = Camera2D.Instance.MouseHex;
            var currentMob = _game.CurrentMob;

            if (_game.Pathfinder.IsValidCoord(mouseHex)) {
                var targetId = _game.State.AtCoord(mouseHex, true);
                if (targetId != null) {
                    if (targetId == currentMob) {
                        ShowMessage("You can't target yourself.");
                    } else {
                        var mobInfo = _game.MobManager.MobInfos[currentMob.Value];
                        var targetInstance = _game.State.MobInstances[targetId.Value];
                        var targetInfo = _game.MobManager.MobInfos[targetId.Value];

                        if (targetInstance.Hp == 0) {
                            ShowMessage("This mob is already dead.");
                        } else {
                            if (mobInfo.Team == targetInfo.Team) {
                                ShowMessage("You can't target your team.");
                            } else if (SelectedAbilityIndex.HasValue) {
                                // TODO - tohle by se melo kontrolovat mnohem driv
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
                        var mobInstance = _game.State.MobInstances[currentMob.Value];

                        if (_game.Map[mouseHex] == HexType.Empty) {
                            var distance = _game.Pathfinder.Distance(mobInstance.Coord, mouseHex);

                            if (distance == int.MaxValue) {
                                ShowMessage("Target is unreachable");
                            } else if (distance > mobInstance.Ap) {
                                ShowMessage("You don't have enough AP.");
                            } else {
                                InputManager.Instance.UserInputEnabled = false;
                                _eventHub.SlowPlayAction(_game, UctAction.MoveAction(currentMob.Value, mouseHex))
                                         .ContinueWith(t => { InputManager.Instance.UserInputEnabled = true; })
                                         .LogContinuation();
                            }
                        } else {
                            ShowMessage("You can't walk into a wall.");
                        }
                    }
                }
            }
        }

        private void UpdatePopovers(GameTime time, AxialCoord mouseHex) {
            _messageBox.Active = _displayMessageBoxUntil >= DateTime.Now;
        }

        private void BuildPopovers() {
            _popoverPadding = new Vector4(20, 10, 20, 10);

            {
                _messageBox = new VerticalLayout {
                    Renderer = new ColorRenderer(Color.White),
                    Padding = _popoverPadding,
                    SortOrder = Camera2D.SortUI + 1000,
                    Position = new Vector2(500, 50)
                };

                _messageBoxLabel = _messageBox.AddChild(new Label("Message Box", _assetManager.Font));
                Entity.Scene.AddAndInitializeRootEntity(_messageBox, _assetManager);
            }
        }

        private void UnselectAbilityIfNeeded() {
            var mobId = _game.CurrentMob;
            if (mobId == null || !SelectedAbilityIndex.HasValue) return;

            var mobInfo = _game.MobManager.MobInfos[mobId.Value];
            var selectedAbility = mobInfo.Abilities[SelectedAbilityIndex.Value];

            if (!GameInvariants.IsAbilityUsableNoTarget(_game, mobId.Value, selectedAbility)) {
                SelectedAbilityIndex = null;
            }
        }

        public Task ShowMessage(string message, int displayForSeconds = 5) {
            _messageBoxLabel.Text = message;
            _displayMessageBoxUntil = DateTime.Now.Add(TimeSpan.FromSeconds(displayForSeconds));

            return Task.Delay(TimeSpan.FromSeconds(displayForSeconds));
        }


        private async Task SlowEventMobMoved(int mobId, AxialCoord pos) {
            var mobEntity = _arenaScene.MobEntities[mobId];
            Debug.Assert(mobEntity != null, "Trying to move a mob without an associated entity.");

            var from = _game.State.MobInstances[mobId].Coord;
            var path = _game.Pathfinder.PathTo(from, pos);
            path.Reverse();
            foreach (var coord in path) {
                await mobEntity.MoveTo(from, coord);
                from = coord;
            }
        }

        private async Task SlowEventAbilityUsed(int mobId, int targetId, AbilityInfo abilityInfo) {
            var sound = abilityInfo.Dmg > 18
                            ? AssetManager.SoundEffectFireballLarge
                            : AssetManager.SoundEffectFireballSmall;
            _assetManager.LoadSoundEffect(sound).Play();

            var mobInstance = _game.State.MobInstances[mobId];
            var targetInstance = _game.State.MobInstances[targetId];

            //BuildUsedAbilityPopover(mobId, abilityInfo).LogContinuation();

            var projectileAnimation = new Animation(AssetManager.FireballSprite,
                                                    TimeSpan.FromMilliseconds(50),
                                                    AssetManager.TileSize,
                                                    4);

            projectileAnimation.Origin = new Vector2(AssetManager.TileSize / 2, AssetManager.TileSize / 2);

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

            _assetManager.LoadSoundEffect(AssetManager.SoundEffectSpellHit).Play();

            var explosion = new Entity {
                Transform = () => Camera2D.Instance.Transform,
                SortOrder = Camera2D.SortProjectiles
            };

            explosion.AddComponent(new PositionAtMob(targetId, _game));

            var explosionAnimation = new Animation(
                AssetManager.FireballExplosionSprite,
                TimeSpan.FromMilliseconds(350),
                AssetManager.TileSize,
                4);

            explosionAnimation.AnimationDone += () => { Entity.Scene.DestroyEntity(explosion); };

            explosion.Renderer = new AnimationRenderer(explosionAnimation);
            explosion.AddComponent(new AnimationController(explosionAnimation));

            Entity.Scene.AddAndInitializeNextFrame(explosion);

            Entity.Scene.DestroyEntity(projectile);
        }
    }
}