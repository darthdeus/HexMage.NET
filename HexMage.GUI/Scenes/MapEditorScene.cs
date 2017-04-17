using HexMage.GUI.Core;
using HexMage.Simulator.Pathfinding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Label = HexMage.GUI.UI.Label;

namespace HexMage.GUI.Scenes {
    public class MapEditorScene : GameScene {
        private Map _map;

        public MapEditorScene(GameManager game) : base(game) {
            _map = new Map(5);
        }

        public override void Initialize() {
            Camera2D.Instance.Translate = new Vector3(600, 500, 0);

            var root = CreateRootEntity(Camera2D.SortUI);
            root.CustomBatch = true;

            const string helpText = "L ... load\n" +
                                    "S ... save\n" +
                                    "Q ... place red starting point\n" +
                                    "E ... place blue starting point\n" +
                                    "W or RMB ... toggle walls\n" +
                                    "1,2,3 ... reorder starting points\n" +
                                    "Space ... CONTINUE";


            root.AddChild(new Label(helpText, _assetManager.Font, Color.White));

            root.AddComponent(new MapEditor(() => _map, map => _map = map));
            root.Renderer = new MapEditorRenderer(() => _map);

            root.AddComponent(() => {
                if (InputManager.Instance.IsKeyJustPressed(Keys.Space)) {
                    LoadNewScene(new TeamSelectionScene(_game, _map.DeepCopy()));
                }
            });
        }

        public override void Cleanup() { }
    }
}