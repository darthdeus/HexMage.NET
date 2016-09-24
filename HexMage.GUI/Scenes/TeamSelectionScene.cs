using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using HexMage.GUI.Core;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.PCG;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Scenes {
    public class TeamSelectionScene : GameScene {
        private readonly Map _map;
        private GameInstance _gameInstance;
        private IMobController _leftController;
        private IMobController _rightController;
        private ArenaScene _arenaScene;

        public TeamSelectionScene(GameManager gameManager, Map map) : base(gameManager) {
            _map = map;
            _gameInstance = new GameInstance(_map);
            _arenaScene = new ArenaScene(_gameManager, _map);
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

            var leftAiBtn = left.AddChild(new TextButton("AI", _assetManager.Font));
            var leftPlayerBtn = left.AddChild(new TextButton("Player", _assetManager.Font));

            var rightAiBtn = right.AddChild(new TextButton("AI", _assetManager.Font));
            var rightPlayerBtn = right.AddChild(new TextButton("Player", _assetManager.Font));

            leftAiBtn.OnClick += _ => _leftController = new AiRandomController(_gameInstance);
            leftPlayerBtn.OnClick += _ => _leftController = new PlayerController(_arenaScene, _gameInstance);

            rightAiBtn.OnClick += _ => _rightController = new AiRandomController(_gameInstance);
            rightPlayerBtn.OnClick += _ => _rightController = new PlayerController(_arenaScene, _gameInstance);

            var t1slider = left.AddChild(new Slider(MinTeamSize, MaxTeamSize, new Point(100, 10)));
            var t2slider = right.AddChild(new Slider(MinTeamSize, MaxTeamSize, new Point(100, 10)));

            t1slider.OnChange += value => RegenerateTeams(t1slider.Value, t2slider.Value);
            t2slider.OnChange += value => RegenerateTeams(t1slider.Value, t2slider.Value);

            var btnStart = new TextButton("Start game", _assetManager.Font) {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(250, 20)
            };

            btnStart.OnClick += _ => {
                LoadNewScene(new ArenaScene(_gameManager, _map));
            };

            AddAndInitializeRootEntity(btnStart, _assetManager);

            AddAndInitializeRootEntity(left, _assetManager);
            AddAndInitializeRootEntity(right, _assetManager);
        }

        private MobManager RegenerateTeams(int t1size, int t2size) {
            var mobs = new MobManager();

            var t1 = mobs.AddTeam(TeamColor.Red, _leftController);
            var t2 = mobs.AddTeam(TeamColor.Blue, _rightController);

            for (int i = 0; i < t1size; i++) {
                Generator.RandomMob(t1, _map.Size, c => mobs.AtCoord(c) == null && _map[c] == HexType.Empty);
            }

            for (int i = 0; i < t2size; i++) {
                Generator.RandomMob(t2, _map.Size, c => mobs.AtCoord(c) == null && _map[c] == HexType.Empty);
            }

            return mobs;
        }

        public override void Cleanup() {}
    }
}