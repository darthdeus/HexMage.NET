using System;
using System.Diagnostics;
using System.Threading;
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
        private readonly ReplayRecorder _replayRecorder;

        public ArenaScene(GameManager gameManager, GameInstance gameInstance) : base(gameManager) {
            _gameInstance = gameInstance;

            _replayRecorder = new ReplayRecorder();

            _defenseModal = new VerticalLayout {
                SortOrder = Camera2D.SortUI,
                Padding = new Vector4(20),
                Renderer = new ColorRenderer(Color.LightGray),
                Position = new Vector2(500, 250),
                Active = false
            };
            
            _gameEventHub = new GameEventHub(_gameInstance);
        }

        public override void Initialize() {
            _gameInstance.TurnManager.StartNextTurn(_gameInstance.Pathfinder);

            Camera2D.Instance.Translate = new Vector3(600, 500, 0);

            _logBox = LogBox.Instance;
            _logBox.Hidden = true;
            _logBox.SortOrder = Camera2D.SortUI + 100;

            AddAndInitializeRootEntity(_logBox, _assetManager);

            var buttons = new Panel();

            var btnYes = new TextButton("Yes", _assetManager.Font) {
                Position = new Vector2(40, 10)
            };
            var btnNo = new TextButton("No", _assetManager.Font) {
                Position = new Vector2(100, 10)
            };

            btnYes.OnClick += _ => FinalizeDefenseModal(DefenseDesire.Block);
            btnNo.OnClick += _ => FinalizeDefenseModal(DefenseDesire.Pass);

            buttons.AddChild(btnYes);
            buttons.AddChild(btnNo);

            _defenseModal.AddChild(new Label("Do you want to defend?", _assetManager.Font));
            _defenseModal.AddChild(buttons);

            AddAndInitializeRootEntity(_defenseModal, _assetManager);

            var gameBoardEntity = CreateRootEntity(Camera2D.SortBackground);
            var gameBoardController = new GameBoardController(_gameInstance, _gameEventHub, _replayRecorder, this);
            _gameBoardController = gameBoardController;
            gameBoardEntity.AddComponent(gameBoardController);
            gameBoardEntity.Renderer = new GameBoardRenderer(_gameInstance, gameBoardController, _camera);
            gameBoardEntity.CustomBatch = true;

            _gameEventHub.AddSubscriber(gameBoardController);
            _gameEventHub.AddSubscriber(_replayRecorder);

            BuildUi();
        }

        private enum ParticleEffectSettings {
            HighlightParticles,
            NoParticles
        }

        // TODO - sort out where else cleanup needs to be called
        public override void Cleanup() {}

        private void BuildUi() {
            Func<string> gameStateTextFunc = () => _gameInstance.IsFinished() ? "Game finished" : "Game in progress";
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

            Func<Mob> currentMobFunc = () => _gameInstance.TurnManager.CurrentMob;
            Func<Mob> hoverMobFunc = () => {
                var mouseHex = Camera2D.Instance.MouseHex;
                if (_gameInstance.Pathfinder.IsValidCoord(mouseHex)) {
                    return _gameInstance.MobManager.AtCoord(mouseHex);
                } else {
                    return null;
                }
            };

            for (int i = 0; i < Mob.NumberOfAbilities; i++) {
                currentLayout.AddChild(AbilityDetail(currentMobFunc, i, ParticleEffectSettings.HighlightParticles));
                hoverLayout.AddChild(AbilityDetail(hoverMobFunc, i, ParticleEffectSettings.NoParticles));
            }
        }

        private Entity AbilityDetail(Func<Mob> mobFunc, int abilityIndex, ParticleEffectSettings particleEffectSettings) {
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
                    (float) rnd.NextDouble()*horizontalOffset*2 - horizontalOffset, 0);

            Func<Random, Vector2> velocityFunc = rnd =>
                new Vector2((float) rnd.NextDouble() - 0.2f,
                            (float) rnd.NextDouble()*speed - speed/2);

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
                        return ElementColor(mob.Abilities[index].Element);
                    } else {
                        return Color.White;
                    }
                };

                abilityDetail.AddComponent(
                    _ => { particles.Active = _gameBoardController.SelectedAbilityIndex == abilityIndex; });

                abilityDetailWrapper.AddChild(particles);
            }

            var abilityUpdater = new AbilityUpdater(mobFunc,
                                                    abilityIndex,
                                                    dmgLabel,
                                                    rangeLabel,
                                                    elementLabel,
                                                    cooldownLabel,
                                                    buffsLabel);
            abilityDetail.AddComponent(abilityUpdater);

            abilityUpdater.OnClick += index => {
                Console.WriteLine($"ABILITY EVENT, time {DateTime.Now.Millisecond}");

                _gameBoardController.ToggleAbilitySelected(index);
            };

            return abilityDetailWrapper;
        }

        private void FinalizeDefenseModal(DefenseDesire desire) {
            Debug.Assert(_defenseDesireSource != null, "Defense desire modal wasn't properly initialized");
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] - Result set");
            _defenseDesireSource.SetResult(desire);
            _defenseDesireSource = null;
            _defenseModal.Active = false;
        }

        private TaskCompletionSource<DefenseDesire> _defenseDesireSource;
        private GameBoardController _gameBoardController;
        private LogBox _logBox;

        public Task<DefenseDesire> RequestDesireToDefend(Mob mob, Ability ability) {
            _defenseModal.Active = true;
            _defenseDesireSource = new TaskCompletionSource<DefenseDesire>();

            return _defenseDesireSource.Task;
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