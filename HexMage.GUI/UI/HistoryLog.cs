using System.Collections.Generic;
using System.Diagnostics;
using HexMage.GUI.Core;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HexMage.GUI.UI {
    public class HistoryLog : Entity {
        private const int MaxHistorySize = 10;
        private readonly List<HistoryLogEntry> _log = new List<HistoryLogEntry>();
        private readonly SpriteFont _font;
        private readonly int _width;
        private readonly AssetManager _assetManager;
        private readonly VerticalLayout _childrenPlaceholder;
        private readonly Vector2 _textOffset = Vector2.Zero;
        private int ActionCount = 1;

        private static HistoryLog _instance;

        public static HistoryLog Instance {
            get {
                Debug.Assert(_instance != null, "Trying to access LogBox before it was initialized.");
                return _instance;
            }
        }

        public static void Initialize(SpriteFont font, int width, AssetManager assetManager) {
            _instance = new HistoryLog(font, width, assetManager);
        }

        public HistoryLog(SpriteFont font, int width, AssetManager assetManager) {
            _font = font;
            _width = width;
            _assetManager = assetManager;

            _childrenPlaceholder = AddChild(new VerticalLayout());
            _childrenPlaceholder.Position = _textOffset;
            _childrenPlaceholder.Renderer = new SpriteRenderer(_assetManager[AssetManager.HistoryLogBg]);
            _childrenPlaceholder.Padding = new Vector4(35, 20, 20, 20);

            for (int i = 0; i < MaxHistorySize; i++) {
                var entry = new HistoryLogEntry(-1, TeamColor.Red, UctAction.NullAction(), null, null, null, null,
                                                assetManager);
                _log.Add(entry);
                _childrenPlaceholder.AddChild(entry);
            }

        }

        public void Log(TeamColor currentTeam, UctAction action, CachedMob mob, CachedMob target,
                        AbilityInfo abilityInfo, int? moveCost) {
            GameManager.CurrentSynchronizationContext.Post(_ => {
                var entry = new HistoryLogEntry(ActionCount++,
                                                currentTeam,
                                                action,
                                                mob,
                                                target,
                                                abilityInfo,
                                                moveCost,
                                                _assetManager);

                _log.Add(entry);
                _childrenPlaceholder.AddChild(entry);

                if (_log.Count > MaxHistorySize) {
                    _log.RemoveAt(0);
                    _childrenPlaceholder.Children.RemoveAt(0);
                }
            }, null);
        }

        protected override void Update(GameTime time) {
            if (InputManager.Instance.IsKeyJustReleased(Keys.OemTilde)) {
                Hidden = !Hidden;
            }

            Position = new Vector2(0, 1024 - LayoutSize.Y);
        }
    }
}