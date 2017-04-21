using System.IO;
using HexMage.GUI.Core;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Color = Microsoft.Xna.Framework.Color;
using Label = HexMage.GUI.UI.Label;

namespace HexMage.GUI.Scenes {
    public class MapEditorScene : GameScene {
        private Map _map;

        public MapEditorScene(GameManager gameManager) : base(gameManager) {
            if (File.Exists("data/map.json")) {
                _map = Map.Load("data/map.json");
            } else {
                _map = new Map(5);
            }
        }

        public override void Initialize() {
            Camera2D.Instance.Translate = new Vector3(600, 500, 0);

            var root = CreateRootEntity(Camera2D.SortUI);
            root.CustomBatch = true;

            var bg = CreateRootEntity(Camera2D.SortBackground);
            bg.Renderer = new SpriteRenderer(_assetManager[AssetManager.MapEditorBg]);
            bg.Position = Vector2.Zero;

            root.AddComponent(new MapEditor(() => _map, map => _map = map));
            root.Renderer = new MapEditorRenderer(() => _map);

            root.AddComponent(() => {
                if (InputManager.Instance.IsKeyJustPressed(Keys.Space)) {
                    LoadNewScene(new TeamSelectionScene(_gameManager, _map.DeepCopy()));
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.F11)) {
                    LoadWorldFromSave();
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.F12)) {
                    LoadEvolutionSave(1);
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.F6)) {
                    LoadEvolutionSave(666);
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.T)) {
                    var replay = ReplayRecorder.Instance.Load(0);

                    var game = replay.Game;

                    game.MobManager.Teams[TeamColor.Red] = new AiRuleBasedController(game);
                    game.MobManager.Teams[TeamColor.Blue] = new AiRuleBasedController(game);

                    LoadNewScene(new ArenaScene(_gameManager, game));
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.R)) {
                    var replay = ReplayRecorder.Instance.Load(0);

                    LoadNewScene(new ArenaScene(_gameManager, replay));
                }
            });
        }

        public override void Cleanup() { }


        public void LoadWorldFromSave() {
            using (var reader = new StreamReader(GameInstance.MapSaveFilename))
            using (var mobReader = new StreamReader(GameInstance.MobsSaveFilename)) {
                var mapRepr = JsonConvert.DeserializeObject<MapRepresentation>(reader.ReadToEnd());

                var game = new GameInstance(mapRepr.Size);
                mapRepr.UpdateMap(game.Map);

                var mobManager = JsonConvert.DeserializeObject<MobManager>(mobReader.ReadToEnd());
                game.MobManager = mobManager;

                // TODO: potrebuju InitializeState????
                game.PrepareEverything();
                game.Reset();

                var arenaScene = new ArenaScene(_gameManager, game);

                //game.MobManager.Teams[TeamColor.Red] = new PlayerController(arenaScene, game);
                //game.MobManager.Teams[TeamColor.Blue] = new PlayerController(arenaScene, game);
                game.MobManager.Teams[TeamColor.Red] = new MctsController(game, 1);
                game.MobManager.Teams[TeamColor.Blue] = new MctsController(game, 1);

                game.PrepareEverything();

                LoadNewScene(arenaScene);
            }
        }

        public void LoadEvolutionSave(int index) {
            var lines = File.ReadAllLines(Constants.BuildEvoSavePath(index));

            var d1 = DNA.FromSerializableString(lines[0]);
            var d2 = DNA.FromSerializableString(lines[1]);

            var map = Map.Load("data/map.json");
            var game = GameSetup.GenerateFromDna(d1, d2, map);

            var arenaScene = new ArenaScene(_gameManager, game);

            game.MobManager.Teams[TeamColor.Red] = new PlayerController(arenaScene, game);
            game.MobManager.Teams[TeamColor.Blue] = new MctsController(game, 1000);


            LoadNewScene(arenaScene);
        }

        public void LoadMapEditor() {
            LoadNewScene(new MapEditorScene(_gameManager));
        }
    }
}