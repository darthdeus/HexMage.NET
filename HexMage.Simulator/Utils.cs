using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HexMage.Simulator {
    public enum LogSeverity
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public interface ILogger {
        void Log(LogSeverity logLevel, string owner, string message);
    }

    public class FileLogger : ILogger {
        private string _filename;

        public FileLogger(string filename) {
            _filename = filename;
        }


        public void Log(LogSeverity logLevel, string owner, string message) {
            if (logLevel == LogSeverity.Error) {
                using (var writer = File.AppendText(_filename)) {
                    writer.WriteLine($"[{logLevel}][TID#{Thread.CurrentThread.ManagedThreadId}][{owner}] {message}");
                    writer.WriteLine();

                    writer.WriteLine(new StackTrace());
                    writer.WriteLine();
                    writer.WriteLine();
                }
            }
        }
    }

    public static class Utils {
        private static readonly List<ILogger> _loggers = new List<ILogger>();

        public static void RegisterLogger(ILogger logger) {
            _loggers.Add(logger);
        }

        public static void Log(LogSeverity logLevel, string owner, string message) {
            foreach (var logger in _loggers) {
                logger.Log(logLevel, owner, message);
            }
        }

        public static void LogContinuation<T>(Task<T> task) {
            if (task.IsFaulted) {
                Log(LogSeverity.Error, nameof(task), $"Task {task} failed.");
            } else {
                Log(LogSeverity.Info, nameof(task), $"Task {task} complete, result: {task.Result}");
            }
        }
    }
}