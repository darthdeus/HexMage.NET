using System;
using System.Collections.Generic;
using System.Text;
using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;
using HexMage.Simulator.PCG;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Scenes {
    public class TeamSelectionScene : GameScene {
        // TODO - fuj, referencovat primo game instance
        private readonly Map _map;

        private readonly GameInstance _gameInstance;
        private IMobController _leftController;
        private IMobController _rightController;
        private ArenaScene _arenaScene;

        private List<IMobController> _controllerList;

        // TODO - fuj, referencovat primo game instance
        private readonly MobManager _mobManager = new MobManager();

        private VerticalLayout _teamPreviewLayout;
        private HorizontalLayout _t1Preview;
        private HorizontalLayout _t2Preview;

        public TeamSelectionScene(GameManager game, Map map) : base(game) {
            _map = map;
            _gameInstance = new GameInstance(_map, _mobManager);
            // TODO - tohle je fuj, inicializovat to poradne
            InitializeArenaScene();
        }

        public void InitializeArenaScene() {
            _arenaScene = new ArenaScene(_game, _gameInstance);

            _controllerList = new List<IMobController> {
                new AiRuleBasedController(_gameInstance),
                new AiRandomController(_gameInstance),
                new MctsController(_gameInstance, 1000),
                new PlayerController(_arenaScene, _gameInstance),
                new FlatMonteCarloController(_gameInstance),
                new MctsController(_gameInstance, 10000),
            };
        }

        private const int MinTeamSize = 1;
        private const int MaxTeamSize = 7;

        public override void Initialize() {
            var left = new VerticalLayout {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(50, 50)
            };

            var right = new VerticalLayout {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(400, 50)
            };

            var t1SliderLayout = left.AddChild(new VerticalLayout());
            var t2SliderLayout = right.AddChild(new VerticalLayout());

            var t1SizeLabel = t1SliderLayout.AddChild(new Label("Team 1 size:", _assetManager.Font, Color.White));
            var t2SizeLabel = t2SliderLayout.AddChild(new Label("Team 2 size:", _assetManager.Font, Color.White));

            var t1Slider = t1SliderLayout.AddChild(new Slider(MinTeamSize, MaxTeamSize, new Point(100, 10)));
            var t2Slider = t2SliderLayout.AddChild(new Slider(MinTeamSize, MaxTeamSize, new Point(100, 10)));

            t1Slider.OnChange += value => RegenerateTeams(t1Slider.Value, t2Slider.Value);
            t2Slider.OnChange += value => RegenerateTeams(t1Slider.Value, t2Slider.Value);

            t1SizeLabel.AddComponent(() => t1SizeLabel.Text = $"Team 1 size: {t1Slider.Value}");
            t2SizeLabel.AddComponent(() => t2SizeLabel.Text = $"Team 2 size: {t2Slider.Value}");

            left.AddChild(new Separator(5));
            right.AddChild(new Separator(5));

            left.AddChild(new Label(() => _leftController?.ToString() ?? "", _assetManager.Font));
            right.AddChild(new Label(() => _rightController?.ToString() ?? "", _assetManager.Font));

            left.AddChild(new Separator(5));
            right.AddChild(new Separator(5));

            foreach (var controller in _controllerList) {
                var btnLeft = left.AddChild(new TextButton(controller.Name, _assetManager.Font));
                btnLeft.OnClick += _ => {
                    _leftController = controller;
                    RegenerateTeams(t1Slider.Value, t2Slider.Value);
                };

                var btnRight = right.AddChild(new TextButton(controller.Name, _assetManager.Font));
                btnRight.OnClick += _ => {
                    _rightController = controller;
                    RegenerateTeams(t1Slider.Value, t2Slider.Value);
                };
            }

            var btnStart = new TextButton("Start game", _assetManager.Font) {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(250, 20)
            };

            btnStart.OnClick += _ => { DoContinue(); };

            var hotkeyManager = new Entity() {SortOrder = Camera2D.SortUI};

            bool first = true;
            hotkeyManager.AddComponent(() => {
                bool handPickedTeam = false;
                if (InputManager.Instance.IsKeyJustPressed(Keys.A)) {
                    if (first) {
                        _leftController = new AiRuleBasedController(_gameInstance);
                    } else {
                        _rightController = new AiRuleBasedController(_gameInstance);
                    }
                    handPickedTeam = true;
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.S)) {
                    if (first) {
                        _leftController = new AiRandomController(_gameInstance);
                    } else {
                        _rightController = new AiRandomController(_gameInstance);
                    }
                    handPickedTeam = true;
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.D)) {
                    if (first) {
                        _leftController = new MctsController(_gameInstance, 100);
                    } else {
                        _rightController = new MctsController(_gameInstance, 100);
                    }
                    handPickedTeam = true;
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.F)) {
                    if (first) {
                        _leftController = new PlayerController(_arenaScene, _gameInstance);
                    } else {
                        _rightController = new PlayerController(_arenaScene, _gameInstance);
                    }
                    handPickedTeam = true;
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.G)) {
                    if (first) {
                        _leftController = new FlatMonteCarloController(_gameInstance);
                    } else {
                        _rightController = new FlatMonteCarloController(_gameInstance);
                    }
                    handPickedTeam = true;
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.H)) {
                    if (first) {
                        _leftController = new MctsController(_gameInstance, 1000);
                    } else {
                        _rightController = new MctsController(_gameInstance, 1000);
                    }
                    handPickedTeam = true;
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.J)) {
                    if (first) {
                        _leftController = new MctsController(_gameInstance, 10000);
                    } else {
                        _rightController = new MctsController(_gameInstance, 10000);
                    }
                    handPickedTeam = true;
                }

                if (handPickedTeam) {
                    RegenerateTeams(t1Slider.Value, t2Slider.Value);
                    first = !first;
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.D1)) {
                    RegenerateTeams(1, 1);
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.D2)) {
                    RegenerateTeams(2, 2);
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.D3)) {
                    RegenerateTeams(3, 3);
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.D4)) {
                    RegenerateTeams(4, 4);
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.D5)) {
                    RegenerateTeams(5, 5);
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.Space)) {
                    DoContinue();
                }
            });

            AddAndInitializeRootEntity(hotkeyManager, _assetManager);

            var btnRegenerate = new TextButton("Regenerate teams", _assetManager.Font) {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(250, 40)
            };

            btnRegenerate.OnClick += _ => RegenerateTeams(t1Slider.Value, t2Slider.Value);

            AddAndInitializeRootEntity(btnStart, _assetManager);
            AddAndInitializeRootEntity(btnRegenerate, _assetManager);

            AddAndInitializeRootEntity(left, _assetManager);
            AddAndInitializeRootEntity(right, _assetManager);

            _teamPreviewLayout = new VerticalLayout() {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(100, 200),
                Spacing = 40
            };

            _t1Preview = new HorizontalLayout {Spacing = 10};
            _t2Preview = new HorizontalLayout {Spacing = 10};

            _teamPreviewLayout.AddChild(_t1Preview);
            _teamPreviewLayout.AddChild(_t2Preview);

            AddAndInitializeRootEntity(_teamPreviewLayout, _assetManager);
        }

        private void DoContinue() {
            if (_leftController != null && _rightController != null) {
                _gameInstance.PrepareEverything();
                InitializeArenaScene();
                LoadNewScene(_arenaScene);
            } else {
                Utils.Log(LogSeverity.Warning, nameof(TeamSelectionScene),
                          "Failed to start a game, no controllers selected.");
            }
        }

        private void RegenerateTeams(int t1size, int t2size) {
            if (_leftController == null || _rightController == null) {
                //Utils.Log(LogSeverity.Warning, nameof(TeamSelectionScene), "Generating teams while no controllers are selected.");
                return;
            }

            Utils.Log(LogSeverity.Info, nameof(TeamSelectionScene), $"Genearting mobs, {t1size} vs {t2size}");

            _mobManager.Clear();
            _gameInstance.State.Clear();

            const TeamColor t1 = TeamColor.Red;
            const TeamColor t2 = TeamColor.Blue;

            _mobManager.Teams[t1] = _leftController;
            _mobManager.Teams[t2] = _rightController;

            _t1Preview.ClearChildren();
            _t2Preview.ClearChildren();

            for (int i = 0; i < t1size; i++) {
                var mobInfo1 = Generator.RandomMob(_mobManager, t1, _gameInstance.State);
                var mobInfo2 = mobInfo1.DeepCopy();

                var m1 = _gameInstance.AddMobWithInfo(mobInfo1);
                var m2 = _gameInstance.AddMobWithInfo(mobInfo2);

                // Change team of m2
                var mi2 = _gameInstance.MobManager.MobInfos[m2];
                mi2.Team = t2;
                _gameInstance.MobManager.MobInfos[m2] = mi2;

                GameSetup.ResetPositions(_gameInstance);
                //Generator.RandomPlaceMob(_gameInstance.MobManager, m1, _map, _gameInstance.State);
                //Generator.RandomPlaceMob(_gameInstance.MobManager, m2, _map, _gameInstance.State);

                _t1Preview.AddChild(BuildMobPreview(() => m1));
                _t2Preview.AddChild(BuildMobPreview(() => m2));
            }


            //for (int i = 0; i < t1size; i++) {
            //    var mobInfo = Generator.RandomMob(_mobManager, t1, _gameInstance.State);
            //    var mobId = _gameInstance.AddMobWithInfo(mobInfo);
            //    Generator.RandomPlaceMob(_gameInstance.MobManager, mobId, _map, _gameInstance.State);

            //    _t1Preview.AddChild(BuildMobPreview(() => mobId));
            //}

            //for (int i = 0; i < t2size; i++) {
            //    var mobInfo = Generator.RandomMob(_mobManager, t2, _gameInstance.State);
            //    var mobId = _gameInstance.AddMobWithInfo(mobInfo);
            //    Generator.RandomPlaceMob(_gameInstance.MobManager, mobId, _map, _gameInstance.State);

            //    _t2Preview.AddChild(BuildMobPreview(() => mobId));
            //}

            _gameInstance.PrepareEverything();
        }

        public Entity BuildMobPreview(Func<int> mobFunc) {
            Func<string> textFunc = () => {
                var builder = new StringBuilder();

                var mobId = mobFunc();

                builder.AppendLine(mobId.ToString());

                var mobInfo = _gameInstance.MobManager.MobInfos[mobId];

                foreach (var abilityId in mobInfo.Abilities) {
                    var ability = _gameInstance.MobManager.AbilityForId(abilityId);
                    builder.AppendLine("-----");
                    builder.AppendLine($"{ability.Element}, DMG {ability.Dmg}, Range {ability.Range}");

                    if (!ability.Buff.IsZero) {
                        var buff = ability.Buff;
                        builder.AppendLine("Buffs:");
                        builder.AppendLine($"{buff.Element}, Hp {buff.HpChange}, Ap {buff.ApChange}");
                    }
                }

                return builder.ToString();
            };

            var wrapper = new Entity {
                Renderer = new ColorRenderer(Color.White)
            };

            wrapper.AddChild(new Label(textFunc, _assetManager.Font));

            return wrapper;
        }

        public override void Cleanup() { }
    }
}