using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using HexMage.AI;
using HexMage.GUI.Core;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Scenes {
    public class TeamSelectionScene : GameScene {
        private readonly Map _map;
        private readonly GameInstance _gameInstance;
        private IMobController _leftController;
        private IMobController _rightController;
        private readonly ArenaScene _arenaScene;
        private List<IMobController> _controllerList;
        private readonly MobManager _mobManager = new MobManager();

        public TeamSelectionScene(GameManager gameManager, Map map) : base(gameManager) {
            _map = map;
            _gameInstance = new GameInstance(_map, _mobManager);
            _arenaScene = new ArenaScene(_gameManager, _gameInstance);

            _controllerList = new List<IMobController> {
                new PlayerController(_arenaScene, _gameInstance),
                new AiRandomController(_gameInstance),
                new DoNothingController()
            };
        }

        private const int MinTeamSize = 3;
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

           
            var t1slider = left.AddChild(new Slider(MinTeamSize, MaxTeamSize, new Point(100, 10)));
            var t2slider = right.AddChild(new Slider(MinTeamSize, MaxTeamSize, new Point(100, 10)));

            t1slider.OnChange += value => RegenerateTeams(t1slider.Value, t2slider.Value);
            t2slider.OnChange += value => RegenerateTeams(t1slider.Value, t2slider.Value);

            foreach (var controller in _controllerList)
            {
                var btnLeft = left.AddChild(new TextButton(controller.Name, _assetManager.Font));
                btnLeft.OnClick += _ => {
                    _leftController = controller;
                    RegenerateTeams(t1slider.Value, t2slider.Value);
                };

                var btnRight = right.AddChild(new TextButton(controller.Name, _assetManager.Font));
                btnRight.OnClick += _ => {
                    _rightController = controller;
                    RegenerateTeams(t1slider.Value, t2slider.Value);
                };
            }


            var btnStart = new TextButton("Start game", _assetManager.Font) {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(250, 20)
            };

            btnStart.OnClick += _ => { LoadNewScene(_arenaScene); };

            AddAndInitializeRootEntity(btnStart, _assetManager);

            AddAndInitializeRootEntity(left, _assetManager);
            AddAndInitializeRootEntity(right, _assetManager);
        }

        private void RegenerateTeams(int t1size, int t2size) {
            if (_leftController == null || _rightController == null) {
                Utils.Log(LogSeverity.Warning, nameof(TeamSelectionScene),
                          "Generating teams while no controllers are selected.");
                return;
            }

            Utils.Log(LogSeverity.Info, nameof(TeamSelectionScene), $"Genearting mobs, {t1size} vs {t2size}");

            _mobManager.Clear();

            const TeamColor t1 = TeamColor.Red;
            const TeamColor t2 = TeamColor.Blue;

            _mobManager.Teams[t1] = _leftController;
            _mobManager.Teams[t2] = _rightController;

            for (int i = 0; i < t1size; i++) {
                var mob = Generator.RandomMob(t1, _map.Size, c =>
                                                      _mobManager.AtCoord(c) == null && _map[c] == HexType.Empty);

                _mobManager.AddMob(mob);
            }

            for (int i = 0; i < t2size; i++) {
                var mob = Generator.RandomMob(t2, _map.Size, c =>
                                                      _mobManager.AtCoord(c) == null && _map[c] == HexType.Empty);

                _mobManager.AddMob(mob);
            }
        }

        public override void Cleanup() {}
    }
}