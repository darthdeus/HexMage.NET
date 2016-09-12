using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.UI {
    public class LogEntry {
        public LogSeverity LogLevel { get; private set; }
        public string Owner { get; private set; }
        public int ThreadId { get; private set; }
        public string Message { get; private set; }
        public StackTrace StackTrace { get; private set; }

        public LogEntry(LogSeverity logLevel, string owner, int threadId, string message) {
            LogLevel = logLevel;
            Owner = owner;
            ThreadId = threadId;
            Message = message;
            StackTrace = new StackTrace();
        }
    }

    public class LogBox : Entity, IRenderer, ILogger {
        private readonly List<LogEntry> _log = new List<LogEntry>();
        private readonly SpriteFont _font;
        private readonly int _width;
        private readonly Vector2 _textOffset = new Vector2(20, 20);
        private readonly Entity _childrenPlaceholder;

        private static LogBox _instance;

        public static LogBox Instance {
            get {
                Debug.Assert(_instance != null, "Trying to access LogBox before it was initialized.");
                return _instance;
            }
        }

        public static void Initialize(SpriteFont font, int width) {
            _instance = new LogBox(font, width);
        }

        public LogBox(SpriteFont font, int width) {
            _font = font;
            _width = width;
            Renderer = new ColorRenderer(Color.Black);

            _childrenPlaceholder = AddChild(new Entity());
            _childrenPlaceholder.Position = _textOffset;
            _childrenPlaceholder.Renderer = this;
        }

        public void Log(string owner, string message) {
            Log(LogSeverity.Warning, owner, message);
        }

        public void Log(LogSeverity logLevel, string owner, string message) {
            var tid = Thread.CurrentThread.ManagedThreadId;
            _log.Add(new LogEntry(logLevel, owner, tid, message));
        }

        protected override void Layout() {
            var measured = _log.Select(e => _font.MeasureString(e.Message).Y + _font.MeasureString(e.StackTrace?.ToString() ?? "").Y);

            var height = measured.Aggregate(0, (acc, m) => (int) m + acc);

            var size = new Vector2(_width, height);
            LayoutSize = size + _textOffset + new Vector2(0, _textOffset.Y);
            Console.WriteLine(LayoutSize);
            _childrenPlaceholder.LayoutSize = size;
        }

        protected override void Update(GameTime time) {
            if (InputManager.Instance.IsKeyJustReleased(Keys.OemTilde)) {
                Hidden = !Hidden;
            }
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var offset = Vector2.Zero;

            foreach (var logEntry in _log) {
                var pos = offset + RenderPosition + _textOffset;
                string logLevelMsg = $"[{logEntry.LogLevel}]";
                string tidMsg = $"[TID#{logEntry.ThreadId}]";
                string ownerMsg = $"[{logEntry.Owner}]:";

                const float separatorSize = 3;

                float messageHeight = _font.MeasureString(logEntry.Message).Y;

                float levelWidth = _font.MeasureString(logLevelMsg).X;
                float tidWidth = _font.MeasureString(tidMsg).X;
                float ownerWidth = _font.MeasureString(ownerMsg).X;

                batch.DrawString(_font, logLevelMsg, pos, Color.Gray);
                batch.DrawString(_font, tidMsg, pos + new Vector2(levelWidth + separatorSize, 0), Color.Yellow);
                batch.DrawString(_font, $"[{logEntry.Owner}]",
                                 pos + new Vector2(levelWidth + tidWidth + 2*separatorSize, 0),
                                 Color.Red);
                batch.DrawString(_font, logEntry.Message,
                                 pos + new Vector2(levelWidth + tidWidth + ownerWidth + separatorSize, 0),
                                 Color.White);

                if (logEntry.LogLevel == LogSeverity.Error) {
                    var stacktraceStr = new StackTrace().ToString();

                    var stacktraceHeight = _font.MeasureString(stacktraceStr).Y;

                    batch.DrawString(_font, stacktraceStr, pos + new Vector2(0, messageHeight), Color.White);
                    offset += new Vector2(0, stacktraceHeight);
                }

                offset += new Vector2(0, messageHeight);
            }
        }
    }
}