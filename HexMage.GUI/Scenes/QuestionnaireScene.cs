using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.GUI.Core;
using HexMage.GUI.UI;

namespace HexMage.GUI.Scenes
{
    public class QuestionnaireScene : GameScene
    {
        public QuestionnaireScene(GameManager gameManager) : base(gameManager) { }
        public override void Initialize() {
            var rootLayout = new VerticalLayout {
                SortOrder = Camera2D.SortUI
            };
            foreach (var fileName in Directory.EnumerateFiles("data/questionnaire")) {
                rootLayout.AddChild(new Label(fileName, _assetManager.Font));
            }
        }

        public override void Cleanup() {
            
        }
    }
}
