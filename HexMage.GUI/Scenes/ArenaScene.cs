using System;
using System.Collections.Generic;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Scenes {
    public class ArenaScene : GameScene {
        private readonly GameInstance _gameInstance;
        private readonly GameEventHub _gameEventHub;
        public readonly Dictionary<int, MobEntity> MobEntities = new Dictionary<int, MobEntity>();

        private GameBoardController _gameBoardController;
        private readonly Replay _replay;

        public ArenaScene(GameManager game, Replay replay) : base(game) {
            _replay = replay;

            _gameInstance = replay.Game;

            _gameInstance.MobManager.Teams[TeamColor.Red] = new ReplayController();
            _gameInstance.MobManager.Teams[TeamColor.Blue] = new ReplayController();

            Constants.RecordReplays = false;
            _gameEventHub = new GameEventHub(_gameInstance);
        }

        public ArenaScene(GameManager game, GameInstance gameInstance) : base(game) {
            _gameInstance = gameInstance;
            _gameEventHub = new GameEventHub(_gameInstance);
        }

        public override void Initialize() {
            HistoryLog.Initialize(_assetManager.Font, 400, _assetManager);

            HistoryLog.Instance.SortOrder = Camera2D.SortUI + 100;
            HistoryLog.Instance.Hidden = false;
            AddAndInitializeRootEntity(HistoryLog.Instance, _assetManager);

            var detail = CreateRootEntity(Camera2D.SortUI + 100);
            var bg = _assetManager[AssetManager.MobDetailBg];
            detail.Renderer = new SpriteRenderer(bg);
            detail.AddComponent(() => detail.Position = new Vector2(bg.Width, 1024 - bg.Height));

            var cursorSprite = _assetManager[AssetManager.CrosshairCursor];

            var crosshairCursor = CreateRootEntity(Camera2D.SortUI);
            crosshairCursor.Renderer = new SpriteRenderer(cursorSprite);
            crosshairCursor.AddComponent(() => {
                crosshairCursor.Position = InputManager.Instance.MousePosition.ToVector2() -
                                           cursorSprite.Bounds.Size.ToVector2() / 2;
            });

            Camera2D.Instance.Translate = new Vector3(600, 400, 0);

            var gameBoardEntity = CreateRootEntity(Camera2D.SortBackground);

            gameBoardEntity.AddComponent(() => {
                if (InputManager.Instance.IsKeyJustPressed(Keys.P)) {
                    Terminate();
                }
            });

            _gameBoardController = new GameBoardController(_gameInstance, _gameEventHub, crosshairCursor, this,
                                                           _replay);

            gameBoardEntity.AddComponent(_gameBoardController);
            gameBoardEntity.Renderer =
                new GameBoardRenderer(_gameInstance, _gameBoardController, _gameEventHub, _camera);
            gameBoardEntity.CustomBatch = true;

            _gameEventHub.AddSubscriber(_gameBoardController);

            BuildUi();

            foreach (var mobId in _gameInstance.MobManager.Mobs) {
                var mobAnimationController = new MobAnimationController(_gameInstance);
                var mobEntity = new MobEntity(mobId, _gameInstance) {
                    SortOrder = Camera2D.SortMobs,
                    // TODO - fetch the animation controller via GetComponent<T>
                    Renderer = new MobRenderer(_gameInstance, mobId, mobAnimationController),
                    Transform = () => Camera2D.Instance.Transform
                };
                mobEntity.AddComponent(mobAnimationController);

                AddAndInitializeRootEntity(mobEntity, _assetManager);
                MobEntities[mobId] = mobEntity;
            }
        }

        public override void Cleanup() { }

        private void BuildUi() {
            var leftBg = CreateRootEntity(Camera2D.SortUI - 10);
            leftBg.Position = Vector2.Zero;
            leftBg.Renderer = new SpriteRenderer(_assetManager[AssetManager.UiLeftBg]);

            var rightBg = CreateRootEntity(Camera2D.SortUI - 10);
            rightBg.Position = new Vector2(1280 - 150, 0);
            rightBg.Renderer = new SpriteRenderer(_assetManager[AssetManager.UiRightBg]);

            const int abilitySpacing = 70;
            var currentLayout = new VerticalLayout {
                Spacing = abilitySpacing,
                Position = new Vector2(0, 0),
                SortOrder = Camera2D.SortUI + 1
            };

            AddAndInitializeRootEntity(currentLayout, _assetManager);

            var hoverLayout = new VerticalLayout {
                Spacing = abilitySpacing,
                Position = new Vector2(1130, 0),
                SortOrder = Camera2D.SortUI + 1
            };
            AddAndInitializeRootEntity(hoverLayout, _assetManager);

#warning TODO - this shouldn't be a func, but rater pass it directly
            Func<GameInstance> gameFunc = () => _gameInstance;
            Func<int?> currentMobFunc = () => _gameInstance.CurrentMob;
            Func<int?> hoverMobFunc = () => {
                var mouseHex = Camera2D.Instance.MouseHex;
                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    return _gameInstance.State.AtCoord(mouseHex, true);
                } else {
                    return null;
                }
            };

#warning TODO: 6 je fuj
            for (int i = 0; i < 6; i++) {
                currentLayout.AddChild(AbilityDetail(gameFunc, currentMobFunc, i,
                                                     ParticleEffectSettings.HighlightParticles));
                hoverLayout.AddChild(AbilityDetail(gameFunc, hoverMobFunc, i, ParticleEffectSettings.NoParticles));
            }

            var topBar = CreateRootEntity(Camera2D.SortUI - 10);
            topBar.Renderer = new SpriteRenderer(_assetManager[AssetManager.TitleBg]);
            topBar.Position = new Vector2(150, 0);

            var teamLabel = CreateRootEntity(Camera2D.SortUI - 10);
            teamLabel.Renderer = new SpriteRenderer(() => {
                var team = _gameInstance.CurrentTeam;
                if (team.HasValue) {
                    if (team.Value == TeamColor.Red) {
                        return _assetManager[AssetManager.TitleRedTeam];
                    } else {
                        return _assetManager[AssetManager.TitleBlueTeam];
                    }
                } else {
                    return _assetManager[AssetManager.TitleGameFinished];
                }
            });
            teamLabel.Position = new Vector2(1280 / 2 - 40, 5);
        }

        private Entity AbilityDetail(Func<GameInstance> gameFunc, Func<int?> mobFunc, int abilityIndex,
                                     ParticleEffectSettings particleEffectSettings) {
            var abilityDetailWrapper = new Entity {
                SizeFunc = () => new Vector2(120, 80),
                Hidden = true
            };

            abilityDetailWrapper.AddComponent(_ => { abilityDetailWrapper.Hidden = mobFunc() == null; });

            var abilityDetail = new VerticalLayout {
                Padding = new Vector4(10, 10, 10, 10),
                Renderer = new SpellRenderer(_gameInstance, _gameBoardController, mobFunc, abilityIndex),
                CustomBatch = true
            };

            abilityDetailWrapper.AddChild(abilityDetail);

            var dmgLabel = new Label(_assetManager.Font);
            abilityDetail.AddChild(dmgLabel);

            var rangeLabel = new Label(_assetManager.Font);
            abilityDetail.AddChild(rangeLabel);

            var elementLabel = new Label(_assetManager.Font);
            abilityDetail.AddChild(elementLabel);

            var cooldownLabel = new Label(_assetManager.Font);
            abilityDetail.AddChild(cooldownLabel);

            var buffsLabel = new Label(_assetManager.Font);
            abilityDetail.AddChild(buffsLabel);

            const float speed = 1;
            const float horizontalOffset = 6;

            Func<Random, Vector2> offsetFunc = rnd =>
                new Vector2(
                    (float) rnd.NextDouble() * horizontalOffset * 2 - horizontalOffset, 0);

            Func<Random, Vector2> velocityFunc = rnd =>
                new Vector2((float) rnd.NextDouble() - 0.2f,
                            (float) rnd.NextDouble() * speed - speed / 2);

            const int maximumNumberOfParticles = 200;
            const int particlesPerSecond = 20;

            if (particleEffectSettings == ParticleEffectSettings.HighlightParticles) {
                var particles = new ParticleSystem(maximumNumberOfParticles,
                                                   particlesPerSecond,
                                                   new Vector2(0, -1), speed,
                                                   _assetManager[AssetManager.ParticleSprite],
                                                   0.01f, offsetFunc, velocityFunc);

                particles.CustomBatch = true;
                particles.Position = new Vector2(60, 120);

                particles.ColorFunc = () => {
                    var mob = mobFunc();
                    if (_gameBoardController.SelectedAbilityIndex.HasValue && mob != null) {
                        int index = _gameBoardController.SelectedAbilityIndex.Value;
                        var ability =
                            _gameInstance.MobManager.AbilityForId(
                                _gameInstance.MobManager.MobInfos[mob.Value].Abilities[index]);
                        return ElementColor(ability.Element);
                    } else {
                        return Color.White;
                    }
                };

                abilityDetail.AddComponent(
                    _ => { particles.Active = _gameBoardController.SelectedAbilityIndex == abilityIndex; });

                abilityDetailWrapper.AddChild(particles);
            }

            var abilityUpdater = new AbilityUpdater(gameFunc,
                                                    mobFunc,
                                                    abilityIndex,
                                                    dmgLabel,
                                                    rangeLabel,
                                                    elementLabel,
                                                    cooldownLabel,
                                                    buffsLabel);
            abilityDetail.AddComponent(abilityUpdater);

            abilityUpdater.OnClick += index => {
                if (_gameInstance.CurrentController is PlayerController) {
                    _gameBoardController.SelectAbility(index);
                }
            };

            return abilityDetailWrapper;
        }

        private Color ElementColor(AbilityElement element) {
            switch (element) {
                case AbilityElement.Earth:
                    return Color.Orange;
                case AbilityElement.Fire:
                    return Color.Red;
                case AbilityElement.Air:
                    return Color.Gray;
                case AbilityElement.Water:
                    return Color.Blue;
                default:
                    throw new ArgumentException("Invalid element", nameof(element));
            }
        }
    }
}