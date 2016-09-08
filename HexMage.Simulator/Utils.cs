using System;
using System.Threading;

namespace HexMage.Simulator {
    public static class Utils {
        public static void ThreadLog(string message) {
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {message}");
        }
    }
}