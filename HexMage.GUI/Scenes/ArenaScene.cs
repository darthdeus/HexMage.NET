using System;
using System.Diagnostics;
using HexMage.GUI.Components;
using HexMage.GUI.Renderers;
using HexMage.GUI.UI;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            var layout = new HorizontalLayout();
            layout.Position = new Vector2(0, 850);
            AddRootEntity(layout);

            var mob = _gameInstance.TurnManager.CurrentMob;

            for (int i = 0; i < Mob.NumberOfAbilities; i++) {
                layout.AddChild(AbilityDetail(_gameInstance.TurnManager, i));
            }

            return layout;
        }

        private Entity AbilityDetail(TurnManager turnManager, int abilityIndex) {
            var abilityDetail = new VerticalLayout {
                Padding = new Vector4(10, 10, 10, 10),
                Renderer = new ColorRenderer(Color.LightCyan)
            };

            var changer = new ColorChanger(Color.Cyan);
            changer.OnHover += () => {
                if (abilityIndex == turnManager.SelectedAbilityIndex.GetValueOrDefault(-1)) {
                    return Color.MistyRose;
                } else {
                    return Color.Cyan;
                }
            };

            changer.RegularColorFunc += () => {
                if (abilityIndex == turnManager.SelectedAbilityIndex.GetValueOrDefault(-1)) {
                    return Color.MistyRose;
                } else {
                    return Color.LightCyan;
                }
            };

            abilityDetail.AddComponent(changer);

            var dmgLabel = new Label(_assetManager.Font);
            abilityDetail.AddChild(dmgLabel);

            var rangeLabel = new Label(_assetManager.Font);
            abilityDetail.AddChild(rangeLabel);

            var elementLabel = new Label(_assetManager.Font);
            abilityDetail.AddChild(elementLabel);

            var abilityUpdater = new AbilityUpdater(turnManager,
                abilityIndex,
                dmgLabel,
                rangeLabel,
                elementLabel);
            abilityDetail.AddComponent(abilityUpdater);

            abilityUpdater.OnClick += index => {
                turnManager.SelectedAbilityIndex = index;
            };

            return abilityDetail;
        }

        private class AbilityUpdater : Component {
            private readonly TurnManager _turnManager;
            private readonly int _abilityIndex;
            private readonly Label _dmgLabel;
            private readonly Label _rangeLabel;
            private readonly Label _elementLabel;
            private Ability _ability;

            public event Action<int> OnClick;

            public AbilityUpdater(TurnManager turnManager, int abilityIndex, Label dmgLabel, Label rangeLabel,
                                  Label elementLabel) {
                _turnManager = turnManager;
                _abilityIndex = abilityIndex;
                _dmgLabel = dmgLabel;
                _rangeLabel = rangeLabel;
                _elementLabel = elementLabel;
            }

            public override void Update(GameTime time) {
                var mob = _turnManager.CurrentMob;
                if (mob != null) {
                    Debug.Assert(mob.Abilities.Count == Mob.AbilityCount);
                    Debug.Assert(_abilityIndex < mob.Abilities.Count);

                    _ability = mob.Abilities[_abilityIndex];

                    var inputManager = InputManager.Instance;
                    var aabb = new Rectangle(Entity.RenderPosition.ToPoint(),
                        Entity.CachedSize.ToPoint());

                    if (inputManager.JustLeftClicked()) {
                        if (aabb.Contains(inputManager.MousePosition)) {
                            OnClick?.Invoke(_abilityIndex);
                        }
                    }
                    
                    _dmgLabel.Text = $"DMG {_ability.Dmg}, Cost {_ability.Cost}";
                    _rangeLabel.Text = $"Range {_ability.Range}";
                    _elementLabel.Text = _ability.Element.ToString();
                }
            }
        }
    }
}