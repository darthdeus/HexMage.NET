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
    public class LogEntry : Entity, IRenderer {
        // TODO - remove this
        private readonly AssetManager _assetManager;
        public LogSeverity LogLevel { get; private set; }
        public string Owner { get; private set; }
        public int ThreadId { get; private set; }
        public string Message { get; private set; }
        public StackTrace StackTrace { get; private set; }

        public LogEntry(LogSeverity logLevel, string owner, int threadId, string message, AssetManager assetManager) {
            _assetManager = assetManager;
            LogLevel = logLevel;
            Owner = owner;
            ThreadId = threadId;
            Message = message;
            StackTrace = new StackTrace();
            Renderer = this;
        }

        protected override void Layout() {
            var font = _assetManager.Font;
            var height = font.MeasureString(Message).Y;
            if (LogLevel == LogSeverity.Error) {
                height += font.MeasureString(StackTrace.ToString()).Y + 2*height;
            }

            // TODO - calculate the width properly
            LayoutSize = new Vector2(800, height);
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var font = assetManager.Font;
            var pos = RenderPosition;
            string logLevelMsg = $"[{LogLevel}]";
            string tidMsg = $"[TID#{ThreadId}]";
            string ownerMsg = $"[{Owner}]:";

            const float separatorSize = 3;

            float messageHeight = font.MeasureString(Message).Y;

            float levelWidth = font.MeasureString(logLevelMsg).X;
            float tidWidth = font.MeasureString(tidMsg).X;
            float ownerWidth = font.MeasureString(ownerMsg).X;

            batch.DrawString(font, logLevelMsg, pos, LogLevelColor(LogLevel));
            batch.DrawString(font, tidMsg, pos + new Vector2(levelWidth + separatorSize, 0), Color.Yellow);
            batch.DrawString(font, $"[{Owner}]",
                             pos + new Vector2(levelWidth + tidWidth + 2*separatorSize, 0),
                             Color.Pink);
            batch.DrawString(font, Message,
                             pos + new Vector2(levelWidth + tidWidth + ownerWidth + separatorSize, 0),
                             Color.White);

            if (LogLevel == LogSeverity.Error) {
                var stacktraceStr = new StackTrace().ToString();

                batch.DrawString(font, stacktraceStr, pos + new Vector2(0, messageHeight), Color.White);
            }
        }

        private Color LogLevelColor(LogSeverity logLevel) {
            switch (logLevel) {
                case LogSeverity.Debug:
                    return Color.LightBlue;
                case LogSeverity.Info:
                    return Color.LightGreen;
                case LogSeverity.Warning:
                    return Color.Yellow;
                case LogSeverity.Error:
                    return Color.Red;
                default:
                    throw new InvalidOperationException($"Invalid log level '{logLevel}'");
            }
        }
    }

    public class LogBox : Entity, ILogger {
        private readonly List<LogEntry> _log = new List<LogEntry>();
        private readonly SpriteFont _font;
        private readonly int _width;
        private readonly AssetManager _assetManager;
        private readonly VerticalLayout _childrenPlaceholder;
        private readonly Vector2 _textOffset = new Vector2(20, 20);

        private static LogBox _instance;

        public static LogBox Instance {
            get {
                Debug.Assert(_instance != null, "Trying to access LogBox before it was initialized.");
                return _instance;
            }
        }

        public static void Initialize(SpriteFont font, int width, AssetManager assetManager) {
            _instance = new LogBox(font, width, assetManager);
        }

        public LogBox(SpriteFont font, int width, AssetManager assetManager) {
            _font = font;
            _width = width;
            _assetManager = assetManager;

            _childrenPlaceholder = AddChild(new VerticalLayout());
            _childrenPlaceholder.Position = _textOffset;
            _childrenPlaceholder.Renderer = new ColorRenderer(Color.Black);
            _childrenPlaceholder.Padding = new Vector4(20, 20, 20, 20);
        }

        public void Log(string owner, string message) {
            Log(LogSeverity.Warning, owner, message);
        }

        public void Log(LogSeverity logLevel, string owner, string message) {
            var tid = Thread.CurrentThread.ManagedThreadId;
            var entry = new LogEntry(logLevel, owner, tid, message, _assetManager);
            _log.Add(entry);
            _childrenPlaceholder.AddChild(entry);
        }

        protected override void Update(GameTime time) {
            if (InputManager.Instance.IsKeyJustReleased(Keys.OemTilde)) {
                Hidden = !Hidden;
            }
        }
    }
}