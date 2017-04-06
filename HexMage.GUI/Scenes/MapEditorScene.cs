using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.Simulator;
using HexMage.Simulator.Pathfinding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Label = HexMage.GUI.UI.Label;

namespace HexMage.GUI.Scenes {
    public class MapEditorScene : GameScene {
        private Map _map;

        public MapEditorScene(GameManager gameManager) : base(gameManager) {
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
                                    "W or RMB... toggle";
            root.AddChild(new Label(helpText, _assetManager.Font));

            root.AddComponent(new MapEditor(() => _map, map => _map = map));
            root.Renderer = new MapEditorRenderer(() => _map);
        }

        public override void Cleanup() {}
    }
}