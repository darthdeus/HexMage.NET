using System;
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
            var t1 = _gameInstance.MobManager.AddTeam();
            var t2 = _gameInstance.MobManager.AddTeam();

            for (int team = 0; team < 2; team++) {
                for (int mobI = 0; mobI < 5; mobI++) {
                    var mob = Generator.RandomMob(team%2 == 0 ? t1 : t2, _gameInstance.Size,
                                                  c => _gameInstance.MobManager.AtCoord(c) == null);

                    _gameInstance.MobManager.AddMob(mob);
                }
            }

            _gameInstance.TurnManager.StartNextTurn();
        }


        public override void Initialize() {
            var gameBoardEntity = CreateRootEntity();
            gameBoardEntity.AddComponent(new GameBoardController(_gameInstance));
            gameBoardEntity.Renderer = new GameBoardRenderer(_gameInstance, _camera);
            gameBoardEntity.SortOrder = 1;

            var uiEntity = BuildUI();
            uiEntity.SortOrder = 2;
        }

        public override void Cleanup() {}

        private Entity BuildUI() {
            var layout = new HorizontalLayout();
            layout.Position = new Vector2(0, 850);
            _rootEntities.Add(layout);

            foreach (var mob in _gameInstance.MobManager.Teams[0].Mobs) {
                var mobDetail = new VerticalLayout() {
                    Padding = new Vector4(10, 10, 10, 10)
                };
                mobDetail.Renderer = new ColorRenderer(Color.LightCyan);
                mobDetail.AddComponent(new ColorChanger(Color.DarkCyan));

                mobDetail.Position = new Vector2(0, 400);

                mobDetail.AddChild(new SpriteElement(_assetManager[AssetManager.MobTexture]));

                var hpBg = mobDetail.CreateChild();
                hpBg.Renderer = new ColorRenderer(Color.DarkRed);
                hpBg.AddComponent(new ColorChanger(Color.Red));

                hpBg.AddChild(new Label($"HP: {mob.HP}/{mob.MaxHP}", _assetManager.Font) {
                    TextColor = Color.White
                });
                mobDetail.AddChild(hpBg);

                var apBg = mobDetail.CreateChild();
                apBg.Renderer = new ColorRenderer(Color.DarkBlue);
                apBg.AddComponent(new ColorChanger(Color.Blue));

                apBg.AddChild(new Label($"AP: {mob.AP}/{mob.MaxAP}", _assetManager.Font) {
                    TextColor = Color.White
                });
                mobDetail.AddChild(apBg);

                mobDetail.AddChild(new TextButton("Kill me", _assetManager.Font));

                layout.AddChild(mobDetail);
            }

            return layout;
        }
    }
}