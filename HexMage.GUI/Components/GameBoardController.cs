﻿using System;
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
        private readonly GameInstance _gameInstance;

        private readonly Vector2 _mouseHoverPopoverOffset = new Vector2(
            0.5f * AssetManager.TileSize, -0.5f * AssetManager.TileSize);

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

        private Replay _replay;

        public int? SelectedAbilityIndex;

        public GameBoardController(GameInstance gameInstance, GameEventHub eventHub, ArenaScene arenaScene,
                                   Replay replay = null) {
            _gameInstance = gameInstance;
            _eventHub = eventHub;
            _arenaScene = arenaScene;
            _replay = replay;
        }

        public void ActionApplied(UctAction action) {
            Debug.Assert(_gameInstance.CurrentTeam.HasValue, "_gameInstance.CurrentTeam.HasValue");
            var mob = _gameInstance.CachedMob(action.MobId);
            var target = _gameInstance.CachedMob(action.TargetId);

            AbilityInfo abilityInfo = null;
            if (action.Type == UctActionType.AttackMove || action.Type == UctActionType.AbilityUse) {
                abilityInfo = _gameInstance.MobManager.Abilities[action.AbilityId];
            }

            int? moveCost = null;

            if (action.Type == UctActionType.AttackMove || action.Type == UctActionType.DefensiveMove
                || action.Type == UctActionType.Move) {
                moveCost = _gameInstance.Pathfinder.Distance(mob.MobInstance.Coord, action.Coord);
            }

            HistoryLog.Instance.Log(_gameInstance.CurrentTeam.Value, action, mob, target, abilityInfo, moveCost);
        }

        public async Task SlowEventMobMoved(int mobId, AxialCoord pos) {
            var mobEntity = _arenaScene.MobEntities[mobId];
            Debug.Assert(mobEntity != null, "Trying to move a mob without an associated entity.");

            var from = _gameInstance.State.MobInstances[mobId].Coord;
            var path = _gameInstance.Pathfinder.PathTo(from, pos);
            path.Reverse();
            foreach (var coord in path) {
                await mobEntity.MoveTo(from, coord);
                from = coord;
            }
        }

        public async Task SlowEventAbilityUsed(int mobId, int targetId, AbilityInfo abilityInfo) {
            var sound = abilityInfo.Dmg > 18
                               ? AssetManager.SoundEffectFireballLarge
                               : AssetManager.SoundEffectFireballSmall;
            _assetManager.LoadSoundEffect(sound).Play();

            var mobInstance = _gameInstance.State.MobInstances[mobId];
            var targetInstance = _gameInstance.State.MobInstances[targetId];

            //BuildUsedAbilityPopover(mobId, abilityInfo).LogContinuation();

            var projectileSprite = AssetManager.ProjectileSpriteForElement(abilityInfo.Element);

            var projectileAnimation = new Animation(projectileSprite,
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

            explosion.AddComponent(new PositionAtMob(_gameInstance.MobManager, targetId, _gameInstance));

            var explosionSprite = AssetManager.ProjectileExplosionSpriteForElement(abilityInfo.Element);

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

        public override void Initialize(AssetManager assetManager) {
            AssertNotInitialized();
            _assetManager = assetManager;

            BuildPopovers();

            if (_replay == null) {
                _eventHub.SlowMainLoop(TimeSpan.FromMilliseconds(500))
                         .LogContinuation();
            } else {
                _eventHub.PlayReplay(_replay.Actions)
                         .LogContinuation();
            }
        }

        public override void Update(GameTime time) {
            UnselectAbilityIfNeeded();

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

            var controller = _gameInstance.CurrentController as PlayerController;
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

            if (inputManager.IsKeyJustReleased(Keys.F10)) {
                var repr = new MapRepresentation(_gameInstance.Map);

                using (var writer = new StreamWriter(GameInstance.MapSaveFilename))
                using (var mobWriter = new StreamWriter(GameInstance.MobsSaveFilename)) {
                    writer.Write(JsonConvert.SerializeObject(repr));
                    mobWriter.Write(JsonConvert.SerializeObject(_gameInstance.MobManager));
                }
            }

            if (inputManager.IsKeyJustReleased(Keys.F11)) {
                using (var reader = new StreamReader(GameInstance.MapSaveFilename))
                using (var mobReader = new StreamReader(GameInstance.MobsSaveFilename)) {
                    var mapRepr = JsonConvert.DeserializeObject<MapRepresentation>(reader.ReadToEnd());
                    mapRepr.UpdateMap(_gameInstance.Map);

                    var mobManager = JsonConvert.DeserializeObject<MobManager>(mobReader.ReadToEnd());
                    Console.WriteLine(mobManager);
                }
            }

            if (inputManager.IsKeyJustReleased(Keys.F12)) {
                foreach (var mobId in _gameInstance.MobManager.Mobs) {
                    var mobInfo = _gameInstance.MobManager.MobInfos[mobId];
                    var mobInstance = _gameInstance.State.MobInstances[mobId];
                    Console.WriteLine($"#{mobId} {mobInstance.Coord} {mobInfo}");
                }
            }

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
            } else if (inputManager.IsKeyJustReleased(Keys.F4)) {
                ((GameBoardRenderer) Entity.Renderer).Mode = BoardRenderMode.VisibilityMap;
            }
        }

        public void SelectAbility(int index) {
            var currentMob = _gameInstance.CurrentMob;
            if (currentMob == null) return;

            var mobInfo = _gameInstance.MobManager.MobInfos[currentMob.Value];
            if (index >= mobInfo.Abilities.Count) {
                Utils.Log(LogSeverity.Info, nameof(GameBoardController),
                          "Trying to select an ability index higher than the number of abilities.");
                return;
            }
            var ability = mobInfo.Abilities[index];

            if (SelectedAbilityIndex.HasValue) {
                if (SelectedAbilityIndex.Value == index) {
                    SelectedAbilityIndex = null;
                } else if (GameInvariants.IsAbilityUsableNoTarget(_gameInstance, currentMob.Value, ability)) {
                    SelectedAbilityIndex = index;
                }
            } else if (GameInvariants.IsAbilityUsableNoTarget(_gameInstance, currentMob.Value, ability)) {
                SelectedAbilityIndex = index;
            }
        }

        private void AttackMob(int targetId) {
            Debug.Assert(SelectedAbilityIndex != null,
                         "_gameInstance.TurnManager.SelectedAbilityIndex != null");

            var abilityIndex = SelectedAbilityIndex.Value;
            var mobId = _gameInstance.CurrentMob;

            Debug.Assert(mobId != null);
            var abilityId = _gameInstance.MobManager.MobInfos[mobId.Value].Abilities[abilityIndex];

            var visibilityPath =
                _gameInstance.Map.AxialLinedraw(_gameInstance.State.MobInstances[mobId.Value].Coord,
                                                _gameInstance.State.MobInstances[targetId].Coord);

            bool isVisible = true;
            foreach (var coord in visibilityPath) {
                if (_gameInstance.Map[coord] == HexType.Wall) {
                    isVisible = false;
                }
            }

            if (isVisible) {
                bool withinRange = (visibilityPath.Count - 1) <= _gameInstance.MobManager.Abilities[abilityId].Range;
                if (withinRange) {
                    InputManager.Instance.UserInputEnabled = false;
                    _eventHub.SlowBroadcastAbilityUsed(mobId.Value, targetId, abilityId)
                             .ContinueWith(t => { InputManager.Instance.UserInputEnabled = true; })
                             .LogTask();
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
            var currentMob = _gameInstance.CurrentMob;

            if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                var targetId = _gameInstance.State.AtCoord(mouseHex, true);
                if (targetId != null) {
                    if (targetId == currentMob) {
                        ShowMessage("You can't target yourself.");
                    } else {
                        var mobInfo = _gameInstance.MobManager.MobInfos[currentMob.Value];
                        var targetInstance = _gameInstance.State.MobInstances[targetId.Value];
                        var targetInfo = _gameInstance.MobManager.MobInfos[targetId.Value];

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
                        var mobInstance = _gameInstance.State.MobInstances[currentMob.Value];

                        if (_gameInstance.Map[mouseHex] == HexType.Empty) {
                            var distance = _gameInstance.Pathfinder.Distance(mobInstance.Coord, mouseHex);

                            if (distance == int.MaxValue) {
                                ShowMessage("Target is unreachable");
                            } else if (distance > mobInstance.Ap) {
                                ShowMessage("You don't have enough AP.");
                            } else {
                                InputManager.Instance.UserInputEnabled = false;
                                _eventHub.SlowBroadcastMobMoved(currentMob.Value, mouseHex)
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
            _emptyHexPopover.Position = new Vector2(900, 800);
            _mobPopover.Position = new Vector2(900, 800);

            _emptyHexPopover.Active = false;
            _mobPopover.Active = false;

            if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                var mobId = _gameInstance.State.AtCoord(mouseHex, true);

                if (mobId == null) {
                    var map = _gameInstance.Map;

                    var labelText = new StringBuilder();

                    // If there's no mob we can't calculate a distance from it
                    if (_gameInstance.CurrentMob.HasValue) {
                        var mobInstance = _gameInstance.State.MobInstances[_gameInstance.CurrentMob.Value];
                        labelText.AppendLine(
                            $"Distance: {_gameInstance.Pathfinder.Distance(mobInstance.Coord, mouseHex)}");
                    }

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

                    var buffs = _gameInstance.State.BuffsAt(mouseHex);
                    Debug.Assert(buffs != null,
                                 "Buffs can't be null since we're only using valid map coords (and those are all initialized).");

                    foreach (var buff in buffs)
                        labelText.AppendLine($"{buff.HpChange}/{buff.ApChange} for {buff.Lifetime} turns");

                    _emptyHexLabel.Text = labelText.ToString();
                } else {
                    _mobPopover.Active = true;
                    var mobTextBuilder = new StringBuilder();
                    var mobInstance = _gameInstance.State.MobInstances[mobId.Value];
                    var mobInfo = _gameInstance.MobManager.MobInfos[mobId.Value];

                    mobTextBuilder.AppendLine(
                        $"HP {mobInstance.Hp}/{mobInfo.MaxHp}\nAP {mobInstance.Ap}/{mobInfo.MaxAp}");
                    mobTextBuilder.AppendLine($"Iniciative: {mobInfo.Iniciative}");
                    mobTextBuilder.AppendLine();

                    mobTextBuilder.AppendLine("Buffs:");
                    if (!mobInstance.Buff.IsZero) {
                        var buff = mobInstance.Buff;
                        mobTextBuilder.AppendLine(
                            $"  {buff.Element} - {buff.HpChange}/{buff.ApChange} for {buff.Lifetime} turns");
                    }


                    mobTextBuilder.AppendLine();
                    mobTextBuilder.AppendLine("Area buffs:");

                    foreach (var buff in _gameInstance.State.BuffsAt(mobInstance.Coord)) {
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

        private async Task BuildUsedAbilityPopover(int mobId, AbilityInfo abilityInfo) {
            var result = new VerticalLayout {
                Renderer = new ColorRenderer(Color.LightGray),
                Padding = _popoverPadding,
                SortOrder = Camera2D.SortUI + 1000,
                Hidden = false
            };

            var camera = Camera2D.Instance;

            result.AddComponent(
                () => {
                    result.Position =
                        camera.HexToPixelWorld(_gameInstance.State.MobInstances[mobId].Coord) +
                        _usedAbilityOffset;
                });

            string labelText = $"{abilityInfo.Dmg}DMG cost {abilityInfo.Cost}";
            result.AddChild(new Label(labelText, _assetManager.Font));

            Entity.Scene.AddAndInitializeRootEntity(result, _assetManager);

            await Task.Delay(_abilityPopoverDisplayTime);

            Entity.Scene.DestroyEntity(result);
        }

        private void UnselectAbilityIfNeeded() {
            var mobId = _gameInstance.CurrentMob;
            if (mobId == null || !SelectedAbilityIndex.HasValue) return;

            var mobInfo = _gameInstance.MobManager.MobInfos[mobId.Value];
            var selectedAbility = mobInfo.Abilities[SelectedAbilityIndex.Value];

            if (!GameInvariants.IsAbilityUsableNoTarget(_gameInstance, mobId.Value, selectedAbility)) {
                SelectedAbilityIndex = null;
            }
        }

        public Task ShowMessage(string message, int displayForSeconds = 5) {
            _messageBoxLabel.Text = message;
            _displayMessageBoxUntil = DateTime.Now.Add(TimeSpan.FromSeconds(displayForSeconds));

            return Task.Delay(TimeSpan.FromSeconds(displayForSeconds));
        }
    }
}