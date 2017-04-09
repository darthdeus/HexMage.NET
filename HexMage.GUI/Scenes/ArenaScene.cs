using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Scenes {
    public class ArenaScene : GameScene {
        private readonly GameInstance _gameInstance;
        private readonly Entity _defenseModal;
        private readonly GameEventHub _gameEventHub;
        public readonly Dictionary<int, MobEntity> MobEntities = new Dictionary<int, MobEntity>();

        private GameBoardController _gameBoardController;
        private Replay _replay;

        public ArenaScene(GameManager gameManager, Replay replay) : base(gameManager) {
            _replay = replay;

            _gameInstance = new GameInstance(replay.Map, replay.MobManager);
            _gameInstance.PrepareEverything();
            _gameInstance.Reset();

            _gameInstance.MobManager.Teams[TeamColor.Red] = new ReplayController();
            _gameInstance.MobManager.Teams[TeamColor.Blue] = new ReplayController();

            Constants.RecordReplays = false;
            _gameEventHub = new GameEventHub(_gameInstance);
        }

        public ArenaScene(GameManager gameManager, GameInstance gameInstance) : base(gameManager) {
            _gameInstance = gameInstance;

            _defenseModal = new VerticalLayout {
                SortOrder = Camera2D.SortUI + 1000,
                Padding = new Vector4(20),
                Renderer = new ColorRenderer(Color.LightGray),
                Position = new Vector2(500, 250),
                Active = false
            };

            _gameEventHub = new GameEventHub(_gameInstance);
        }

        public override void Initialize() {
            //_gameInstance.MobManager.Reset();
            //_gameInstance.Map.PrecomputeCubeLinedraw();
            //_gameInstance.Pathfinder.PathfindDistanceAll();

            //// TODO - vyresit kdy presne volat presort (mozna nejaky unifikovany initialize?)
            //_gameInstance.TurnManager.PresortTurnOrder();
            //_gameInstance.TurnManager.StartNextTurn(_gameInstance.Pathfinder);

            Camera2D.Instance.Translate = new Vector3(600, 500, 0);

            var gameBoardEntity = CreateRootEntity(Camera2D.SortBackground);

            _gameBoardController = new GameBoardController(_gameInstance, _gameEventHub, this, _replay);

            gameBoardEntity.AddComponent(_gameBoardController);
            gameBoardEntity.Renderer =
                new GameBoardRenderer(_gameInstance, _gameBoardController, _gameEventHub, _camera);
            gameBoardEntity.CustomBatch = true;

            _gameEventHub.AddSubscriber(_gameBoardController);

            BuildUi();

            foreach (var mobId in _gameInstance.MobManager.Mobs) {
                var mobAnimationController = new MobAnimationController(_gameInstance);
                var mobEntity = new MobEntity(mobId, _gameInstance) {
                    SortOrder = Camera2D.SortUI,
                    // TODO - fetch the animation controller via GetComponent<T>
                    Renderer = new MobRenderer(_gameInstance, mobId, mobAnimationController),
                    Transform = () => Camera2D.Instance.Transform
                };
                mobEntity.AddComponent(mobAnimationController);

                AddAndInitializeRootEntity(mobEntity, _assetManager);
                MobEntities[mobId] = mobEntity;
            }
        }

        private enum ParticleEffectSettings {
            HighlightParticles,
            NoParticles
        }

        public override void Cleanup() { }

        private void BuildUi() {
            Func<string> gameStateTextFunc = () => _gameInstance.IsFinished ? "Game finished" : "Game in progress";
            var gameStateLabel = new Label(gameStateTextFunc, _assetManager.Font) {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(400, 50)
            };

            AddAndInitializeRootEntity(gameStateLabel, _assetManager);

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

            for (int i = 0; i < MobInfo.NumberOfAbilities; i++) {
                currentLayout.AddChild(AbilityDetail(gameFunc, currentMobFunc, i,
                                                     ParticleEffectSettings.HighlightParticles));
                hoverLayout.AddChild(AbilityDetail(gameFunc, hoverMobFunc, i, ParticleEffectSettings.NoParticles));
            }
        }

        private Entity AbilityDetail(Func<GameInstance> gameFunc, Func<int?> mobFunc, int abilityIndex,
                                     ParticleEffectSettings particleEffectSettings) {
            var abilityDetailWrapper = new Entity {
                SizeFunc = () => new Vector2(120, 80)
            };

            abilityDetailWrapper.Hidden = true;
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
                var particles = new ParticleSystem(maximumNumberOfParticles, particlesPerSecond,
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

            abilityUpdater.OnClick += index => { _gameBoardController.SelectAbility(index); };

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