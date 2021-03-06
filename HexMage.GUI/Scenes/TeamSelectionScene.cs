﻿using System;
using System.Collections.Generic;
using System.Text;
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
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Scenes {
    public class TeamSelectionScene : GameScene {
        private readonly Map _map;

        private GameInstance _game;

        delegate IMobController ControllerFactory(GameInstance game, ArenaScene arena);

        private readonly List<Tuple<string, ControllerFactory>> _controllerFactories;

        private Tuple<string, ControllerFactory> _leftController;
        private Tuple<string, ControllerFactory> _rightController;

        private VerticalLayout _teamPreviewLayout;
        private HorizontalLayout _t1Preview;
        private HorizontalLayout _t2Preview;

        private int _teamSize = 2; // Initial team size

        public TeamSelectionScene(GameManager gameManager, Map map) : base(gameManager) {
            _map = map;

            _controllerFactories = new List<Tuple<string, ControllerFactory>> {
                Tuple.Create<string, ControllerFactory>("Rule", (game, _) => new AiRuleBasedController(game)),
                Tuple.Create<string, ControllerFactory>("Random", (game, _) => new AiRandomController(game)),
                Tuple.Create<string, ControllerFactory>("MCTS#1000", (game, _) => new MctsController(game, 1000)),
                Tuple.Create<string, ControllerFactory>("Player", (game, arena) => new PlayerController(arena, game)),
                Tuple.Create<string, ControllerFactory>("MCTS#100", (game, _) => new MctsController(game, 100)),
                Tuple.Create<string, ControllerFactory>("MCTS#10000", (game, _) => new MctsController(game, 10000))
            };
        }

        public override void Initialize() {
            var bg = CreateRootEntity(Camera2D.SortBackground);
            bg.Renderer = new SpriteRenderer(_assetManager[AssetManager.TeamSelectionBg]);
            bg.Position = Vector2.Zero;

            var aiListOffset = new Vector2(400, 200);

            var left = new VerticalLayout {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(50, 50) + aiListOffset
            };

            var right = new VerticalLayout {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(400, 50) + aiListOffset
            };

            left.AddChild(new Separator(5));
            right.AddChild(new Separator(5));

            left.AddChild(new Label(() => _leftController?.Item1 ?? "", _assetManager.AbilityFont, Color.White));
            right.AddChild(new Label(() => _rightController?.Item1 ?? "", _assetManager.AbilityFont, Color.White));

            left.AddChild(new Separator(5));
            right.AddChild(new Separator(5));

            foreach (var pair in _controllerFactories) {
                var btnLeft = left.AddChild(new TextButton(pair.Item1, _assetManager.Font));
                btnLeft.OnClick += _ => {
                    _leftController = pair;
                    RegenerateTeams();
                };

                var btnRight = right.AddChild(new TextButton(pair.Item1, _assetManager.Font));
                btnRight.OnClick += _ => {
                    _rightController = pair;
                    RegenerateTeams();
                };
            }

            var hotkeyManager = new Entity() {SortOrder = Camera2D.SortUI};

            bool first = true;
            hotkeyManager.AddComponent(() => {
                if (InputManager.Instance.IsKeyJustPressed(Keys.R) && Keyboard.GetState().IsKeyDown(Keys.LeftControl)) {
                    SceneManager.RollbackToFirst = true;
                    return;
                }

                var dict = new Dictionary<Keys, Tuple<string, ControllerFactory>> {
                    {Keys.A, _controllerFactories[0]},
                    {Keys.S, _controllerFactories[1]},
                    {Keys.D, _controllerFactories[2]},
                    {Keys.F, _controllerFactories[3]},
                    {Keys.G, _controllerFactories[4]},
                    {Keys.H, _controllerFactories[5]}
                };

                bool handPickedTeam = false;
                foreach (var pair in dict) {
                    if (InputManager.Instance.IsKeyJustPressed(pair.Key)) {
                        if (first) {
                            _leftController = pair.Value;
                        } else {
                            _rightController = pair.Value;
                        }
                        handPickedTeam = true;
                        break;
                    }
                }

                if (handPickedTeam) {
                    RegenerateTeams();
                    first = !first;
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.D1)) {
                    _teamSize = 1;
                    RegenerateTeams();
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.D2)) {
                    _teamSize = 2;
                    RegenerateTeams();
                } else if (InputManager.Instance.IsKeyJustPressed(Keys.D3)) {
                    _teamSize = 3;
                    RegenerateTeams();
                }

                if (InputManager.Instance.IsKeyJustPressed(Keys.Space)) {
                    DoContinue();
                }
            });

            AddAndInitializeRootEntity(hotkeyManager, _assetManager);

            AddAndInitializeRootEntity(left, _assetManager);
            AddAndInitializeRootEntity(right, _assetManager);

            _teamPreviewLayout = new VerticalLayout() {
                SortOrder = Camera2D.SortUI,
                Position = new Vector2(100, 300) + aiListOffset,
                Spacing = 40
            };

            _t1Preview = new HorizontalLayout {Spacing = 10};
            _t2Preview = new HorizontalLayout {Spacing = 10};

            _teamPreviewLayout.AddChild(_t1Preview);
            _teamPreviewLayout.AddChild(_t2Preview);

            AddAndInitializeRootEntity(_teamPreviewLayout, _assetManager);
        }

        private void DoContinue() {
            if (_game == null) {
                Utils.Log(LogSeverity.Warning, nameof(TeamSelectionScene),
                          "Failed to start a game, no controllers selected.");
                return;
            }

            if (_teamSize > Math.Min(_game.Map.RedStartingPoints.Count, _game.Map.BlueStartingPoints.Count)) {
                Utils.Log(LogSeverity.Error, nameof(TeamSelectionScene),
                          "Not enough starting positions, decrease team size or add them in the map editor (Ctrl-R).");
                return;
            }

            if (_leftController != null && _rightController != null) {
                var arena = new ArenaScene(_gameManager, _game);

                const TeamColor t1 = TeamColor.Red;
                const TeamColor t2 = TeamColor.Blue;

                _game.MobManager.Teams[t1] = _leftController.Item2(_game, arena);
                _game.MobManager.Teams[t2] = _rightController.Item2(_game, arena);

                _game.PrepareEverything();

                LoadNewScene(arena);
            } else {
                Utils.Log(LogSeverity.Warning, nameof(TeamSelectionScene),
                          "Failed to start a game, no controllers selected.");
            }
        }

        private void RegenerateTeams() {
            Utils.Log(LogSeverity.Info, nameof(TeamSelectionScene), $"Genearting teams of size {_teamSize}.");

            _t1Preview.ClearChildren();
            _t2Preview.ClearChildren();

            var dna = new DNA(_teamSize, 2);
            dna.Randomize();

            var game = GameSetup.GenerateFromDna(dna, dna.Clone(), _map, false);

            foreach (var mobId in game.MobManager.Mobs) {
                var mobInfo = game.MobManager.MobInfos[mobId];
                if (mobInfo.Team == TeamColor.Red) {
                    _t1Preview.AddChild(BuildMobPreview(game, () => mobId));
                } else {
                    _t2Preview.AddChild(BuildMobPreview(game, () => mobId));
                }
            }

            _game = game;
        }

        public Entity BuildMobPreview(GameInstance game, Func<int> mobFunc) {
            Func<string> textFunc = () => {
                var builder = new StringBuilder();

                var mobId = mobFunc();

                builder.AppendLine(mobId.ToString());

                var mobInfo = game.MobManager.MobInfos[mobId];

                foreach (var abilityId in mobInfo.Abilities) {
                    var ability = game.MobManager.Abilities[abilityId];
                    builder.AppendLine("-----");
                    builder.AppendLine($"DMG {ability.Dmg}, Range {ability.Range}");

                    if (!ability.Buff.IsZero) {
                        var buff = ability.Buff;
                        builder.AppendLine("Buffs:");
                        builder.AppendLine($"Hp {buff.HpChange}, Ap {buff.ApChange}");
                    }
                }

                return builder.ToString();
            };

            var wrapper = new Entity {
                Renderer = new ColorRenderer(Color.White)
            };

            wrapper.AddChild(new Label(textFunc, _assetManager.Font));

            return wrapper;
        }

        public override void Cleanup() { }
    }
}