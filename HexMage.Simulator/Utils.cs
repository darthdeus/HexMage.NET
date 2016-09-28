using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HexMage.Simulator {
    public enum LogSeverity {
        Debug,
        Info,
        Warning,
        Error
    }

    public interface ILogger {
        void Log(LogSeverity logLevel, string owner, string message);
    }

    public class StdoutLogger : ILogger {
        public void Log(LogSeverity logLevel, string owner, string message) {
            Console.ForegroundColor = LogLevelColor(logLevel);
            Console.Write($"[{logLevel}]");

            int currentThread = Thread.CurrentThread.ManagedThreadId;
            if (Utils.MainThreadId != currentThread) {
                Console.ForegroundColor = ConsoleColor.Red;
            } else {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            }

            Console.Write($"[{Thread.CurrentThread.ManagedThreadId}]");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"[{owner}]");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);

            if (logLevel == LogSeverity.Error) {
                Console.WriteLine(new StackTrace());
            }

            Console.ForegroundColor = ConsoleColor.White;
        }


        private ConsoleColor LogLevelColor(LogSeverity logLevel) {
            switch (logLevel) {
                case LogSeverity.Debug:
                    return ConsoleColor.Blue;
                case LogSeverity.Info:
                    return ConsoleColor.Green;
                case LogSeverity.Warning:
                    return ConsoleColor.Yellow;
                case LogSeverity.Error:
                    return ConsoleColor.Red;
                default:
                    throw new InvalidOperationException($"Invalid log level '{logLevel}'");
            }
        }
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
        public static int MainThreadId = -1;

        public static void InitializeLoggerMainThread() {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static void RegisterLogger(ILogger logger) {
            _loggers.Add(logger);
        }

        public static void Log(LogSeverity logLevel, string owner, string message) {
            foreach (var logger in _loggers) {
                logger.Log(logLevel, owner, message);
            }
        }

        public static void LogTask(this Task task) {
            if (task.IsFaulted) {
                Log(LogSeverity.Error, nameof(task), $"Task {task} failed, exception: {task.Exception}.");

                SynchronizationContext.Current.Post(_ => { throw task.Exception; }, null);
                throw task.Exception;
            } else {
                Log(LogSeverity.Info, nameof(task),
                    $"Task {task} complete: {task.IsCompleted}, faulted: {task.IsFaulted}");
            }
        }

        public static void LogTask<T>(this Task<T> task) {
            if (task.IsFaulted) {
                Log(LogSeverity.Error, nameof(task), $"Task<T> {task} failed, exception {task.Exception}.");

                SynchronizationContext.Current.Post(_ => { throw task.Exception; }, null);
                throw task.Exception;
            } else {
                Log(LogSeverity.Info, nameof(task),
                    $"Task<T> {task} complete: {task.IsCompleted}, result: {task.Result}, faulted: {task.IsFaulted}");
            }
        }

        public static void LogContinuation(this Task task) {
            task.ContinueWith(t => { task.LogTask(); }, TaskContinuationOptions.LongRunning);
        }

        public static void LogContinuation<T>(this Task<T> task) {
            task.ContinueWith(t => { task.LogTask(); }, TaskContinuationOptions.LongRunning);
        }
    }
}