using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.GUI.Renderers;
using HexMage.Simulator;

namespace HexMage.GUI.Scenes {
    public class MapEditorScene : GameScene {
        private Map _map;

        public MapEditorScene(GameManager gameManager) : base(gameManager) {
            _map = new Map(5);
        }


        public override void Initialize() {
            var root = CreateRootEntity(Camera2D.SortUI);

            root.Renderer = new MapPreviewRenderer(() => _map, 1);
        }

        public override void Cleanup() {
            throw new NotImplementedException();
        }
    }
}