using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace HexMage.GUI {
    public class NetworkLoggerServer {
        private readonly CancellationToken _cancellationToken;
        private readonly TcpListener _listener;

        public NetworkLoggerServer(CancellationToken cancellationToken, int port) {
            _cancellationToken = cancellationToken;
            _listener = new TcpListener(IPAddress.Any, port);
        }

        private async void StartAccepting() {
            try {
                _listener.Start();

                while (!_cancellationToken.IsCancellationRequested) {
                    try {
                        using (var socket = await _listener.AcceptSocketAsync()) {
                            while (!_cancellationToken.IsCancellationRequested) {
                                var stream = new NetworkStream(socket);
                                var reader = new StreamReader(stream);

                                var msg = await reader.ReadLineAsync();
                                var parts = msg.Split('|');

                                if (parts.Length == 3) {
                                    string level = parts[0];
                                    string owner = parts[1];
                                    string message = parts[2];

                                    Console.WriteLine(
                                        $"[NETWORK LOG][{level}][{owner}]: {message}");
                                }
                            }
                        }
                    } catch (Exception e) {
                        PrintException(e, "client handling");
                    }
                }
            } catch (Exception e) {
                PrintException(e, "listen start");
            }
        }

        private void PrintException(Exception e, string location) {
            Console.WriteLine($"ERROR {nameof(NetworkLoggerServer)} {location} failed: {e}");
            Console.WriteLine("------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(e.StackTrace);
            Console.WriteLine();
            Console.WriteLine("------------------------------------------");
        }

        public void StartWorkerThread() {
            new Thread(StartAccepting).Start();
        }
    }
}