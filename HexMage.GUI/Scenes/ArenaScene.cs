using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HexMage.GUI.Components;
using HexMage.GUI.Renderers;
using HexMage.GUI.UI;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI {
    internal class HoverUpdater : Component {
        private readonly Action<bool> _action;

        public HoverUpdater(Action<bool> action) {
            _action = action;
        }

        public override void Update(GameTime time) {
            base.Update(time);

            _action(Entity.AABB.Contains(InputManager.Instance.MousePosition));
        }
    }

    internal class ArenaScene : GameScene {
        private readonly GameInstance _gameInstance;
        private readonly Entity _defenseModal;
        public bool HoveringOverUi { get; private set; } = false;
        private readonly GameEventHub _gameEventHub;

        public ArenaScene(GameManager gameManager, Map map) : base(gameManager) {
            _gameInstance = new GameInstance(map.Size, map);
            
              _defenseModal = new VerticalLayout() {
                SortOrder = Camera2D.SortUI,
                Padding = new Vector4(20),
                Renderer = new ColorRenderer(Color.LightGray),
                Position = new Vector2(500, 250),
                Active = false
            };

            var aiController = new AiRandomController(_gameInstance);

            var t1 = _gameInstance.MobManager.AddTeam(TeamColor.Red, new PlayerController(this, _gameInstance));
            var t2 = _gameInstance.MobManager.AddTeam(TeamColor.Blue, aiController);

            _gameEventHub = new GameEventHub();
            _gameEventHub.AddSubscriber(aiController);

            for (int team = 0; team < 2; team++) {
                for (int mobI = 0; mobI < 2; mobI++) {
                    var mob = Generator.RandomMob(team%2 == 0 ? t1 : t2, _gameInstance.Size,
                                                  c => _gameInstance.MobManager.AtCoord(c) == null);

                    _gameInstance.MobManager.AddMob(mob);
                }
            }
            _gameInstance.TurnManager.StartNextTurn();
            _gameInstance.Pathfinder.PathfindFrom(_gameInstance.TurnManager.CurrentMob.Coord);
        }

        public override void Initialize() {
            Camera2D.Instance.Translate = new Vector3(600, 500, 0);

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

            AddRootEntity(_defenseModal);
            _defenseModal.InitializeEntity(_assetManager);

            var gameBoardEntity = CreateRootEntity(Camera2D.SortBackground);
            var gameBoardController = new GameBoardController(_gameInstance, _gameEventHub);
            gameBoardEntity.AddComponent(gameBoardController);
            gameBoardEntity.Renderer = new GameBoardRenderer(_gameInstance, _camera);
            gameBoardEntity.CustomBatch = true;

            _gameEventHub.AddSubscriber(gameBoardController);

            var uiEntity = BuildUi();
            uiEntity.SortOrder = Camera2D.SortBackground + 1;
        }


        // TODO - sort out where else cleanup needs to be called
        public override void Cleanup() {            
        }

        private Entity BuildUi() {
            var layout = new HorizontalLayout {
                Spacing = 40,
                Position = new Vector2(0, 850),
                SortOrder = Camera2D.SortUI,
            };

            AddRootEntity(layout);

            for (int i = 0; i < Mob.NumberOfAbilities; i++) {
                layout.AddChild(AbilityDetail(_gameInstance.TurnManager, i));
            }

            return layout;
        }

        private Entity AbilityDetail(TurnManager turnManager, int abilityIndex) {
            var abilityDetailWrapper = new Entity {
                SizeFunc = () => new Vector2(120, 80)
            };

            var abilityDetail = new VerticalLayout() {
                Padding = new Vector4(10, 10, 10, 10),
                Renderer = new SpellRenderer(_gameInstance, turnManager, abilityIndex),
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

            var particles = new ParticleSystem(maximumNumberOfParticles, particlesPerSecond,
                                               new Vector2(0, -1), speed,
                                               _assetManager[AssetManager.ParticleSprite],
                                               0.01f, offsetFunc, velocityFunc);

            particles.CustomBatch = true;
            particles.Position = new Vector2(60, 120);

            particles.ColorFunc = () => {
                if (turnManager.SelectedAbilityIndex.HasValue) {
                    int index = turnManager.SelectedAbilityIndex.Value;
                    switch (turnManager.CurrentMob.Abilities[index].Element) {
                        case AbilityElement.Earth:
                            return Color.Orange;
                        case AbilityElement.Fire:
                            return Color.Red;
                        case AbilityElement.Air:
                            return Color.Gray;
                        case AbilityElement.Water:
                            return Color.Blue;
                        default:
                            return Color.White;
                    }
                } else {
                    return Color.White;
                }
            };

            abilityDetail.AddComponent(_ => { particles.Active = turnManager.SelectedAbilityIndex == abilityIndex; });

            abilityDetailWrapper.AddChild(particles);

            var abilityUpdater = new AbilityUpdater(turnManager,
                                                    abilityIndex,
                                                    dmgLabel,
                                                    rangeLabel,
                                                    elementLabel,
                                                    cooldownLabel,
                                                    buffsLabel);
            abilityDetail.AddComponent(abilityUpdater);

            abilityUpdater.OnClick += index => {
                Console.WriteLine($"ABILITY EVENT, time {DateTime.Now.Millisecond}");

                turnManager.ToggleAbilitySelected(index);
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

        public Task<DefenseDesire> RequestDesireToDefend(Mob mob, Ability ability) {
            _defenseModal.Active = true;
            _defenseDesireSource = new TaskCompletionSource<DefenseDesire>();

            return _defenseDesireSource.Task;
        }
    }
}