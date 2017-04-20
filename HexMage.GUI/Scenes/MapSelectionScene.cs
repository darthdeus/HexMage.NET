﻿using System;
using System.IO;
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
using Newtonsoft.Json;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Scenes {
    public class MapSelectionScene : GameScene {
        private const int DefaultMapSize = 5;
        private const int MinSize = 4;
        private const int MaxSize = 12;
        private int? _selectedSize;

        private readonly MapGenerator _mapGenerator = new MapGenerator();
        private MapSeed _currentSeed = MapSeed.CreateRandom();

        private Map _currentMap;
        private VerticalLayout _seedHistory;

        public MapSelectionScene(GameManager game) : base(game) {
            _currentMap = _mapGenerator.Generate(DefaultMapSize, _currentSeed);
        }

        public override void Initialize() {
            var rootElement = CreateRootEntity(Camera2D.SortUI);
            rootElement.Position = new Vector2(50, 50);

            var leftColumn = rootElement.AddChild(new VerticalLayout {Spacing = 5});
            var middleColumn = rootElement.CreateChild();

            leftColumn.Position = new Vector2(40, 40);

            var seedLabel = new Label("Map seed:", _assetManager.Font, Color.White);
            seedLabel.AddComponent(_ => seedLabel.Text = $"Map seed: {_currentSeed.ToString()}");
            leftColumn.AddChild(seedLabel);

            var btnGenerateMap = new TextButton("Generate new map", _assetManager.Font);
            btnGenerateMap.OnClick += BtnGenerateMapOnOnClick;
            leftColumn.AddChild(btnGenerateMap);

            leftColumn.AddChild(new Label("Seed history:", _assetManager.Font, Color.White));

            _seedHistory = new VerticalLayout {
                Spacing = 3
            };
            leftColumn.AddChild(_seedHistory);

            middleColumn.Position = new Vector2(400, 40);
            var btnStartGame = new TextButton("Start game using current map", _assetManager.Font);
            btnStartGame.OnClick += _ => { DoContinue(); };
            middleColumn.AddChild(btnStartGame);

            var sizeSlider = new Slider(MinSize, MaxSize, new Point(100, 20));
            sizeSlider.OnChange += size => {
                _selectedSize = size;
                GenerateMap();
            };

            var sizeSliderLayout = new VerticalLayout {
                Spacing = 5,
                Position = new Vector2(300, 0)
            };

            sizeSliderLayout.AddChild(new Label("Change map size:", _assetManager.Font, Color.White));
            sizeSliderLayout.AddChild(sizeSlider);

            middleColumn.AddChild(sizeSliderLayout);

            var mapPreview = new Entity {
                Renderer = new MapPreviewRenderer(() => _currentMap, 0.55f)
            };

            Action positionUpdater =
                () =>
                    mapPreview.Position =
                        new Vector2(30 + 10 * (_selectedSize ?? MinSize), 120 + 30 * (_selectedSize ?? MinSize));

            positionUpdater();

            mapPreview.AddComponent(_ => positionUpdater());

            middleColumn.AddChild(mapPreview);

            var menuBar = new HorizontalLayout();

            rootElement.AddChild(menuBar);

            var loaderEntity = new Entity {
                SortOrder = Camera2D.SortUI
            };

            loaderEntity.AddComponent(() => {
                if (InputManager.Instance.IsKeyJustReleased(Keys.F11)) {
                    LoadWorldFromSave();
                }

                if (InputManager.Instance.IsKeyJustReleased(Keys.F12)) {
                    LoadEvolutionSave(1);
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.F6)) {
                    LoadEvolutionSave(666);
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.Space)) {
                    DoContinue();
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.E)) {
                    LoadMapEditor();
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.T)) {
                    var replay = ReplayRecorder.Instance.Load(0);

                    var game = replay.Game;

                    game.MobManager.Teams[TeamColor.Red] = new AiRuleBasedController(game);
                    game.MobManager.Teams[TeamColor.Blue] = new AiRuleBasedController(game);

                    LoadNewScene(new ArenaScene(_game, game));
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.R)) {
                    var replay = ReplayRecorder.Instance.Load(0);

                    LoadNewScene(new ArenaScene(_game, replay));
                }
            });

            AddAndInitializeRootEntity(loaderEntity, _assetManager);
        }

        private void DoContinue() {
            LoadNewScene(new TeamSelectionScene(_game, _currentMap.DeepCopy()));
        }

        private void BtnGenerateMapOnOnClick(TextButton btn) {
            var item = new HorizontalLayout {
                Spacing = 3,
                Metadata = _currentSeed
            };

            var selectButton = new TextButton(_currentSeed.ToString(), _assetManager.Font);
            selectButton.OnClick += e => {
                _currentSeed = (MapSeed) item.Metadata;
                GenerateMap();
            };

            var removeButton = new TextButton("x", _assetManager.Font);
            removeButton.OnClick += e => _seedHistory.RemoveEntity(item);

            item.AddChild(selectButton);
            item.AddChild(removeButton);

            _seedHistory.AddChild(item);
            _currentSeed = MapSeed.CreateRandom();
            GenerateMap();
        }

        private void GenerateMap() {
            _currentMap = _mapGenerator.Generate(_selectedSize ?? DefaultMapSize, _currentSeed);
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

                var arenaScene = new ArenaScene(_game, game);

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

            var arenaScene = new ArenaScene(_game, game);

            game.MobManager.Teams[TeamColor.Red] = new PlayerController(arenaScene, game);
            game.MobManager.Teams[TeamColor.Blue] = new MctsController(game, 1000);


            LoadNewScene(arenaScene);
        }

        public void LoadMapEditor() {
            LoadNewScene(new MapEditorScene(_game));
        }
    }
}