using System;
using System.Collections.Generic;
using System.Linq;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using HexMage.GUI.UI;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Scenes {
    public class ArenaScene : GameScene {
        private readonly GameInstance _game;
        public readonly Dictionary<int, MobEntity> MobEntities = new Dictionary<int, MobEntity>();

        private GameBoardController _gameBoardController;
        private readonly Replay _replay;
        public Action GameFinishedCallback;

        public ArenaScene(GameManager gameManager, Replay replay) : base(gameManager) {
            _replay = replay;

            _game = replay.Game;

            _game.MobManager.Teams[TeamColor.Red] = new ReplayController();
            _game.MobManager.Teams[TeamColor.Blue] = new ReplayController();

            Constants.RecordReplays = false;
        }

        public ArenaScene(GameManager gameManager, GameInstance game) : base(gameManager) {
            _game = game;
        }

        public override void Initialize() {
            HistoryLog.Initialize(_assetManager.Font, 400, _assetManager);

            HistoryLog.Instance.SortOrder = Camera2D.SortUI + 100;
            HistoryLog.Instance.Hidden = false;
            AddAndInitializeRootEntity(HistoryLog.Instance, _assetManager);

            var cursorSprite = _assetManager[AssetManager.CrosshairCursor];

            var crosshairCursor = CreateRootEntity(Camera2D.SortUI + 10000);
            crosshairCursor.Renderer = new SpriteRenderer(cursorSprite);
            crosshairCursor.AddComponent(() => {
                crosshairCursor.Position = InputManager.Instance.MousePosition.ToVector2() -
                                           cursorSprite.Bounds.Size.ToVector2() / 2;
            });

            Camera2D.Instance.Translate = new Vector3(600, 400, 0);

            var gameBoardEntity = CreateRootEntity(Camera2D.SortBackground);

            gameBoardEntity.AddComponent(() => {
                if (InputManager.Instance.IsKeyJustPressed(Keys.P)) {
                    Terminate();
                }
            });

            _gameBoardController = new GameBoardController(_game,
                                                           crosshairCursor,
                                                           this,
                                                           _replay);

            _gameBoardController.GameFinishedCallback = () => GameFinishedCallback?.Invoke();

            gameBoardEntity.AddComponent(_gameBoardController);
            gameBoardEntity.CustomBatch = true;

            BuildUi();

            foreach (var mobId in _game.MobManager.Mobs) {
                var mobAnimationController = new MobAnimationController(_game);
                var mobEntity = new MobEntity(mobId, _game) {
                    SortOrder = Camera2D.SortMobs,
                    Renderer = new MobRenderer(_game, mobId, mobAnimationController),
                    Transform = () => Camera2D.Instance.Transform
                };
                mobEntity.AddComponent(mobAnimationController);

                AddAndInitializeRootEntity(mobEntity, _assetManager);
                MobEntities[mobId] = mobEntity;
            }
        }

        public override void Cleanup() { }

        private void BuildUi() {
            BuildMouseDetailUi();

            const int abilitySpacing = 70;
            var currentLayout = new VerticalLayout {
                Spacing = abilitySpacing,
                Position = new Vector2(0, 0),
                SortOrder = Camera2D.SortUI + 1
            };

            AddAndInitializeRootEntity(currentLayout, _assetManager);

            var hoverLayout = new VerticalLayout {
                Spacing = abilitySpacing,
                Position = new Vector2(1130, 0),
                SortOrder = Camera2D.SortUI + 1
            };
            AddAndInitializeRootEntity(hoverLayout, _assetManager);

            Func<GameInstance> gameFunc = () => _game;
            Func<CachedMob> currentMobFunc = () => _game.CurrentCachedMob;
            Func<CachedMob> hoverMobFunc = () => {
                var mouseHex = Camera2D.Instance.MouseHex;
                if (_game.Pathfinder.IsValidCoord(mouseHex)) {
                    var mid = _game.State.AtCoord(mouseHex, true);

                    if (mid.HasValue) {
                        return _game.CachedMob(mid.Value);
                    } else {
                        return null;
                    }
                } else {
                    return null;
                }
            };

            for (int i = 0; i < 6; i++) {
                currentLayout.AddChild(AbilityDetail(gameFunc, currentMobFunc, i,
                                                     ParticleEffectSettings.HighlightParticles));
                hoverLayout.AddChild(AbilityDetail(gameFunc, hoverMobFunc, i, ParticleEffectSettings.NoParticles));
            }

            var teamLabel = CreateRootEntity(Camera2D.SortUI - 10);
            teamLabel.Renderer = new SpriteRenderer(() => {
                var team = _game.CurrentTeam;
                if (team.HasValue && !_game.IsFinished) {
                    if (team.Value == TeamColor.Red) {
                        return _assetManager[AssetManager.TitleRedTeam];
                    } else {
                        return _assetManager[AssetManager.TitleBlueTeam];
                    }
                } else {
                    return _assetManager[AssetManager.TitleGameFinished];
                }
            });
            teamLabel.Position = new Vector2(1280 / 2 - 40, 5);
        }

        private void BuildMouseDetailUi() {
            var detail = CreateRootEntity(Camera2D.SortUI + 100);
            var bg = _assetManager[AssetManager.MobDetailBg];
            detail.Renderer = new SpriteRenderer(bg);
            detail.AddComponent(() => detail.Position = new Vector2(bg.Width, 1024 - bg.Height));

            var labelHp = detail.AddChild(new Label(() => {
                var mob = Camera2D.Instance.CachedMouseMob(_game);
                if (mob != null) {
                    return $"{mob.MobInstance.Hp}/{mob.MobInfo.MaxHp}";
                } else {
                    return "NOMOB";
                }
            }, _assetManager.AbilityFont, Color.White));

            labelHp.Position = new Vector2(95, 60);

            var labelAp = detail.AddChild(new Label(() => {
                var mob = Camera2D.Instance.CachedMouseMob(_game);
                if (mob != null) {
                    return $"{mob.MobInstance.Ap}/{mob.MobInfo.MaxAp}";
                } else {
                    return "NOMOB";
                }
            }, _assetManager.AbilityFont, Color.White));

            labelAp.Position = new Vector2(95, 138);

            var labelBuff = detail.AddChild(new Label(() => {
                var mob = Camera2D.Instance.CachedMouseMob(_game);
                if (mob != null) {
                    return
                        $"{mob.MobInstance.Buff.HpChange}/{mob.MobInstance.Buff.ApChange} ({mob.MobInstance.Buff.Lifetime} turns)";
                } else {
                    return "NOMOB";
                }
            }, _assetManager.AbilityFont, Color.White));

            labelBuff.Position = new Vector2(300, 95);

            var labelAreaBuff = detail.AddChild(new Label(() => {
                var mob = Camera2D.Instance.CachedMouseMob(_game);
                if (mob != null) {
                    var areaBuffs = _game.State.BuffsAt(mob.MobInstance.Coord);
                    var areaBuff = areaBuffs.Aggregate(Buff.ZeroBuff(), Buff.Combine);

                    return
                        $"{areaBuff.HpChange}/{areaBuff.ApChange} ({areaBuff.Lifetime} turns)";
                } else {
                    return "NOMOB";
                }
            }, _assetManager.AbilityFont, Color.White));

            labelAreaBuff.Position = new Vector2(300, 143);

            var labelCoord = detail.AddChild(new Label(() => Camera2D.Instance.MouseHex.ToString(),
                                                       _assetManager.AbilityFont, Color.White));

            labelCoord.Position = new Vector2(530, 150);

            var labelDistance = detail.AddChild(new Label(() => {
                var mob = _game.CurrentCachedMob;
                var mouseHex = Camera2D.Instance.MouseHex;
                if (mob != null && _game.Pathfinder.IsValidCoord(mouseHex)) {
                    return _game.Pathfinder.Distance(mob.MobInstance.Coord, mouseHex).ToString();
                } else {
                    return "";
                }
            }, _assetManager.AbilityFont, Color.White));

            labelDistance.Position = new Vector2(130, 95);

            var labelEmptyAreaBuff = detail.AddChild(new Label(() => {
                var mouseHex = Camera2D.Instance.MouseHex;
                if (_game.Pathfinder.IsValidCoord(mouseHex)) {
                    var areaBuffs = _game.State.BuffsAt(mouseHex);
                    var areaBuff = areaBuffs.Aggregate(Buff.ZeroBuff(), Buff.Combine);

                    return
                        $"{areaBuff.HpChange}/{areaBuff.ApChange} ({areaBuff.Lifetime} turns)";
                } else {
                    return "INVALID COORD";
                }
            }, _assetManager.AbilityFont, Color.White));

            labelEmptyAreaBuff.Position = new Vector2(323, 120);

            detail.AddComponent(() => {
                var mouseHex = Camera2D.Instance.MouseHex;
                if (_game.Pathfinder.IsValidCoord(mouseHex)) {
                    var mobId = _game.State.AtCoord(mouseHex, true);

                    if (mobId == null) {
                        labelHp.Active = false;
                        labelAp.Active = false;
                        labelBuff.Active = false;
                        labelAreaBuff.Active = false;

                        if (_game.Map[mouseHex] == HexType.Wall) {
                            labelEmptyAreaBuff.Active = false;
                            labelDistance.Active = false;
                            detail.Renderer = new SpriteRenderer(_assetManager[AssetManager.MobDetailBgStone]);
                        } else {
                            labelEmptyAreaBuff.Active = true;
                            labelDistance.Active = true;
                            detail.Renderer = new SpriteRenderer(_assetManager[AssetManager.MobDetailBgEmpty]);
                        }
                    } else {
                        labelHp.Active = true;
                        labelAp.Active = true;
                        labelBuff.Active = true;
                        labelAreaBuff.Active = true;
                        labelEmptyAreaBuff.Active = false;
                        labelDistance.Active = false;
                        detail.Renderer = new SpriteRenderer(_assetManager[AssetManager.MobDetailBg]);
                    }
                } else {
                    labelEmptyAreaBuff.Active = false;
                    labelDistance.Active = false;
                    detail.Renderer = new SpriteRenderer(_assetManager[AssetManager.MobDetailBgStone]);
                    labelHp.Active = false;
                    labelAp.Active = false;
                    labelBuff.Active = false;
                    labelAreaBuff.Active = false;
                }
            });
        }

        private Entity AbilityDetail(Func<GameInstance> gameFunc, Func<CachedMob> mobFunc, int abilityIndex,
                                     ParticleEffectSettings particleEffectSettings) {
            var abilityDetailWrapper = new Entity {
                SizeFunc = () => new Vector2(120, 80),
                Hidden = true
            };

            abilityDetailWrapper.AddComponent(_ => { abilityDetailWrapper.Hidden = mobFunc() == null; });

            var abilityDetail = new Entity() {
                Renderer = new SpellRenderer(_game, _gameBoardController, mobFunc, abilityIndex),
                CustomBatch = true,
                SizeFunc = () => new Vector2(150, 150)
            };
            abilityDetail.AddComponent(() => { abilityDetail.LayoutEntity(); });
            abilityDetailWrapper.AddChild(abilityDetail);

            var abilityDetailNotEnoughAp = new Entity() {
                Renderer = new SpriteRenderer(_assetManager[AssetManager.SpellBgNotEnoughAp]),
                SortOrder = Camera2D.SortUI + 10000
            };

            abilityDetailNotEnoughAp.AddComponent(() => {
                var mob = mobFunc();
                if (mob != null) {
                    if (abilityIndex < mob.MobInfo.Abilities.Count) {
                        var abilityId = mob.MobInfo.Abilities[abilityIndex];
                        var ability = _game.MobManager.Abilities[abilityId];

                        bool onCooldown = _game.State.Cooldowns[abilityId] > 0;
                        abilityDetailNotEnoughAp.Hidden = ability.Cost <= mob.MobInstance.Ap;

                        if (abilityDetailNotEnoughAp.Hidden == false && onCooldown) {
                            abilityDetailNotEnoughAp.Hidden = true;
                        }
                    } else {
                        abilityDetailNotEnoughAp.Hidden = true;
                    }
                } else {
                    abilityDetailNotEnoughAp.Hidden = true;
                }
            });

            var abilityDetailCooldown = new Entity() {
                Renderer = new SpriteRenderer(_assetManager[AssetManager.SpellBgCooldown]),
                SortOrder = Camera2D.SortUI + 10000
            };

            abilityDetailCooldown.AddComponent(() => {
                var mob = mobFunc();
                if (mob != null) {
                    if (abilityIndex < mob.MobInfo.Abilities.Count) {
                        var abilityId = mob.MobInfo.Abilities[abilityIndex];

                        abilityDetailCooldown.Hidden = _game.State.Cooldowns[abilityId] == 0;
                    } else {
                        abilityDetailCooldown.Hidden = true;
                    }
                } else {
                    abilityDetailCooldown.Hidden = true;
                }
            });

            var dmgLabel = new Label(_assetManager.AbilityFont) {
                Position = new Vector2(53, 12)
            };
            abilityDetailWrapper.AddChild(dmgLabel);

            var apLabel = new Label(_assetManager.AbilityFont) {
                Position = new Vector2(119, 12)
            };
            abilityDetailWrapper.AddChild(apLabel);

            var rangeLabel = new Label(_assetManager.AbilityFont) {
                Position = new Vector2(58, 55)
            };
            abilityDetailWrapper.AddChild(rangeLabel);

            var buffLabel = new Label(_assetManager.AbilityFontSmall) {
                Position = new Vector2(38, 106)
            };
            abilityDetailWrapper.AddChild(buffLabel);

            var areaBuffLabel = new Label(_assetManager.AbilityFontSmall) {
                Position = new Vector2(38, 124)
            };
            abilityDetailWrapper.AddChild(areaBuffLabel);

            var cooldownLabel = new Label(_assetManager.AbilityFont) {
                Position = new Vector2(119, 55)
            };
            abilityDetailWrapper.AddChild(cooldownLabel);

            abilityDetailWrapper.AddChild(abilityDetailNotEnoughAp);
            abilityDetailWrapper.AddChild(abilityDetailCooldown);

            const float speed = 1;
            const float horizontalOffset = 6;

            Func<Random, Vector2> offsetFunc = rnd =>
                new Vector2(
                    (float) rnd.NextDouble() * horizontalOffset * 2 - horizontalOffset, 0);

            Func<Random, Vector2> velocityFunc = rnd =>
                new Vector2((float) rnd.NextDouble() - 0.2f,
                            (float) rnd.NextDouble() * speed - speed / 2);

            const int maximumNumberOfParticles = 200;
            const int particlesPerSecond = 20;

            if (particleEffectSettings == ParticleEffectSettings.HighlightParticles) {
                var particles = new ParticleSystem(maximumNumberOfParticles,
                                                   particlesPerSecond,
                                                   new Vector2(0, -1), speed,
                                                   _assetManager[AssetManager.ParticleSprite],
                                                   0.01f, offsetFunc, velocityFunc);

                particles.CustomBatch = true;
                particles.Position = new Vector2(60, 120);

                particles.ColorFunc = () => {
                    var mob = mobFunc();
                    if (_gameBoardController.SelectedAbilityIndex.HasValue && mob != null) {
                        return Color.Red;
                    } else {
                        return Color.White;
                    }
                };

                abilityDetail.AddComponent(_ => {
                    particles.Active = _gameBoardController.SelectedAbilityIndex == abilityIndex;
                });

                abilityDetailWrapper.AddChild(particles);
            }

            var abilityUpdater = new AbilityUpdater(gameFunc,
                                                    mobFunc,
                                                    abilityIndex,
                                                    dmgLabel,
                                                    apLabel,
                                                    rangeLabel,
                                                    buffLabel,
                                                    cooldownLabel,
                                                    areaBuffLabel);
            abilityDetail.AddComponent(abilityUpdater);

            abilityUpdater.OnClick += index => {
                if (_game.CurrentController is PlayerController) {
                    _gameBoardController.SelectAbility(index);
                }
            };

            return abilityDetailWrapper;
        }
    }
}