using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HexMage.GUI.Core;
using HexMage.GUI.UI;
using HexMage.Simulator.AI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Label = HexMage.GUI.UI.Label;

namespace HexMage.GUI.Scenes {
    public class QuestionnaireScene : GameScene {
        private VerticalLayout _rootLayout;
        public QuestionnaireScene(GameManager gameManager) : base(gameManager) { }

        public override void Initialize() {
            var bg = CreateRootEntity(Camera2D.SortBackground);
            bg.Renderer = new SpriteRenderer(_assetManager[AssetManager.QuestionnaireBg]);
            bg.Position = Vector2.Zero;

            _rootLayout = new VerticalLayout {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(530, 250)
            };

            GenerateChildren();

            AddAndInitializeRootEntity(_rootLayout, _assetManager);

            _rootLayout.AddComponent(() => {
                if (InputManager.Instance.IsKeyJustPressed(Keys.Space)) {
                    RunNextExperiment();
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.E)) {
                    LoadNewScene(new MapEditorScene(_gameManager));
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.Q) && Keyboard.GetState().IsKeyDown(Keys.LeftControl)) {
                    foreach (var file in AllFiles()) {
                        if (IsDone(file)) {
                            MarkUndone(file);
                        }
                    }

                    DelayFor(TimeSpan.Zero, GenerateChildren);
                }
            });
        }

        private void RunNextExperiment() {
            var experimentFile = AllFiles().FirstOrDefault(name => !IsDone(name));
            if (experimentFile == null) {
                MessageBox.Show($"Experiment je hotovy, dekujeme za ucast");
                return;
            }

            var game = MapEditorScene.LoadEvolutionSaveFile(experimentFile);

            var arena = new ArenaScene(_gameManager, game);
            arena.GameFinishedCallback = () => {
                MarkDone(experimentFile);
                MessageBox.Show(
                    "Konec hry, prosím vyplňte automaticky otevřený dotazník a poté restartujte hru pro pokračování.");
                var url =
                    $"https://docs.google.com/forms/d/e/1FAIpQLSeSebCnhzSgrgoaNBf5xaJLegkT-Oir0t2RTHBj3ektaOYZJQ/viewform?usp=pp_url&entry.627128578&entry.1173552081={experimentFile}";
                System.Diagnostics.Process.Start(url);
            };

            game.AssignAiControllers(new PlayerController(arena, game),
                                     new MctsController(game, 1000));

            LoadNewScene(arena);
        }

        private void GenerateChildren() {
            _rootLayout.ClearChildren();

            foreach (var fileName in AllFiles()) {
                if (IsDone(fileName)) {
                    _rootLayout.AddChild(new Label($"- DONE {fileName}", _assetManager.AbilityFontSmall, Color.Gray));
                } else {
                    _rootLayout.AddChild(new Label($"- TODO {fileName}", _assetManager.AbilityFontSmall, Color.White));
                }
            }
        }

        private static void MarkDone(string fileName) {
            File.Move(fileName, fileName + ".done");
        }

        private static void MarkUndone(string fileName) {
            File.Move(fileName, fileName.Replace(".done", ""));
        }

        private static bool IsDone(string fileName) {
            return fileName.EndsWith(".done");
        }

        private IEnumerable<string> AllFiles() {
            return Directory.EnumerateFiles("data/questionnaire");
        }

        public override void Cleanup() { }
    }
}