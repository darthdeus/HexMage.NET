using System;
using System.Collections.Generic;
using System.IO;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;

namespace HexMage.GUI.Scenes {
    public class MapSelectionScene : GameScene {
        private const int DefaultMapSize = 7;
        private const int MinSize = 4;
        private const int MaxSize = 12;
        private int? _selectedSize;

        private MapGenerator _mapGenerator = new MapGenerator();
        private MapSeed _currentSeed = MapSeed.CreateRandom();

        private Map _currentMap;
        private VerticalLayout _seedHistory;

        public MapSelectionScene(GameManager gameManager) : base(gameManager) {
            _currentMap = _mapGenerator.Generate(DefaultMapSize, _currentSeed);
        }

        public override void Initialize() {
            var rootElement = CreateRootEntity(Camera2D.SortUI);
            rootElement.Position = new Vector2(50, 50);

            var leftColumn = rootElement.AddChild(new VerticalLayout {Spacing = 5});
            var middleColumn = rootElement.CreateChild();

            leftColumn.Position = new Vector2(40, 40);

            var seedLabel = new Label("Map seed:", _assetManager.Font);
            seedLabel.AddComponent(_ => seedLabel.Text = $"Map seed: {_currentSeed.ToString()}");
            leftColumn.AddChild(seedLabel);

            var btnGenerateMap = new TextButton("Generate new map", _assetManager.Font);
            btnGenerateMap.OnClick += BtnGenerateMapOnOnClick;
            leftColumn.AddChild(btnGenerateMap);

            leftColumn.AddChild(new Label("Seed history:", _assetManager.Font));

            _seedHistory = new VerticalLayout {
                Spacing = 3
            };
            leftColumn.AddChild(_seedHistory);

            middleColumn.Position = new Vector2(400, 40);
            var btnStartGame = new TextButton("Start game using current map", _assetManager.Font);
            btnStartGame.OnClick += _ => { LoadNewScene(new TeamSelectionScene(_gameManager, _currentMap.DeepCopy())); };
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

            sizeSliderLayout.AddChild(new Label("Change map size:", _assetManager.Font));
            sizeSliderLayout.AddChild(sizeSlider);

            middleColumn.AddChild(sizeSliderLayout);

            var mapPreview = new Entity {
                Renderer = new MapPreviewRenderer(() => _currentMap, 0.55f)
            };

            Action positionUpdater =
                () =>
                    mapPreview.Position =
                        new Vector2(30 + 10*(_selectedSize ?? MinSize), 120 + 30*(_selectedSize ?? MinSize));

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
                                      });

            AddAndInitializeRootEntity(loaderEntity, _assetManager);
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

        public override void Cleanup() {}

        public void LoadWorldFromSave() {
            using (var reader = new StreamReader(GameInstance.MapSaveFilename))
            using (var mobReader = new StreamReader(GameInstance.MobsSaveFilename)) {
                var mapRepr = JsonConvert.DeserializeObject<MapRepresentation>(reader.ReadToEnd());

                var game = new GameInstance(mapRepr.Size);
                mapRepr.UpdateMap(game.Map);

                var mobManager = JsonConvert.DeserializeObject<MobManager>(mobReader.ReadToEnd());
                game.MobManager = mobManager;

                var arenaScene = new ArenaScene(_gameManager, game);

                game.MobManager.Teams[TeamColor.Red] = new PlayerController(arenaScene, game);
                game.MobManager.Teams[TeamColor.Blue] = new PlayerController(arenaScene, game);

                game.PrepareEverything();

                new FlatMonteCarlo().Run(game);

                LoadNewScene(arenaScene);
            }
        }
    }
}