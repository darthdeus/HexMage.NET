using System;
using HexMage.GUI.Components;
using HexMage.GUI.Renderers;
using HexMage.GUI.UI;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI {
    internal class ArenaScene : GameScene {
        private readonly GameInstance _gameInstance = new GameInstance(20);
        private VerticalLayout _mobUI;

        public ArenaScene(GameManager gameManager) : base(gameManager) {
            var t1 = _gameInstance.MobManager.AddTeam(TeamColor.Red);
            var t2 = _gameInstance.MobManager.AddTeam(TeamColor.Blue);

            for (int team = 0; team < 2; team++) {
                for (int mobI = 0; mobI < 5; mobI++) {
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

            var gameBoardEntity = CreateRootEntity();
            gameBoardEntity.AddComponent(new GameBoardController(_gameInstance));
            gameBoardEntity.Renderer = new GameBoardRenderer(_gameInstance, _camera);
            gameBoardEntity.SortOrder = 1;
            gameBoardEntity.CustomBatch = true;

            var uiEntity = BuildUI();
            uiEntity.SortOrder = 2;
        }

        public override void Cleanup() {}

        private Entity BuildUI() {
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
                Renderer = new SpellRenderer(turnManager, abilityIndex),
                CustomBatch = true
            };

            abilityDetailWrapper.AddChild(abilityDetail);

            var dmgLabel = new Label(_assetManager.Font);
            abilityDetail.AddChild(dmgLabel);

            var rangeLabel = new Label(_assetManager.Font);
            abilityDetail.AddChild(rangeLabel);

            var elementLabel = new Label(_assetManager.Font);
            abilityDetail.AddChild(elementLabel);


            float speed = 1;

            float horizontalOffset = 6;
            Func<Random, Vector2> offsetFunc = rnd =>
                                               new Vector2(
                                                   (float) rnd.NextDouble()*horizontalOffset*2 - horizontalOffset, 0);

            Func<Random, Vector2> velocityFunc = rnd =>
                                                 new Vector2((float) rnd.NextDouble() - 0.2f, (float) rnd.NextDouble()*speed - speed/2);

            var particles = new ParticleSystem(200, 20, new Vector2(0, -1), speed,
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
                elementLabel);
            abilityDetail.AddComponent(abilityUpdater);

            abilityUpdater.OnClick += index => {
                Console.WriteLine($"ABILITY EVENT, time {DateTime.Now.Millisecond}");

                turnManager.ToggleAbilitySelected(index);
            };

            return abilityDetailWrapper;
        }
    }
}