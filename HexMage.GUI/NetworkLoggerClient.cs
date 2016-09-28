using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HexMage.Simulator;

namespace HexMage.GUI {
    public class NetworkLoggerClient : ILogger {
        private readonly IPEndPoint _endpoint;
        private readonly CancellationToken _cancellationToken;

        private readonly ConcurrentQueue<Tuple<LogSeverity, string, string>> _queue =
            new ConcurrentQueue<Tuple<LogSeverity, string, string>>();


        public NetworkLoggerClient(IPEndPoint endpoint, CancellationToken cancellationToken) {
            _endpoint = endpoint;
            _cancellationToken = cancellationToken;
        }

        public void StartWorkerThread() {
            new Thread(() => ClientLoop(_endpoint)).Start();
        }

        private async void ClientLoop(IPEndPoint endpoint) {
            try {
                using (var client = new TcpClient()) {
                    client.Connect(endpoint.Address, endpoint.Port);
                    var stream = client.GetStream();

                    while (!_cancellationToken.IsCancellationRequested) {
                        try {
                            Tuple<LogSeverity, string, string> msg;
                            if (_queue.TryDequeue(out msg)) {
                                var str = $"{msg.Item1}|{msg.Item2}|{msg.Item3}\n";
                                var payload = Encoding.UTF8.GetBytes(str);

                                await stream.WriteAsync(payload, 0, payload.Length, _cancellationToken);
                            } else {
                                await Task.Delay(TimeSpan.FromMilliseconds(500), _cancellationToken);
                            }
                        } catch (Exception e) {
                            if (e is TaskCanceledException) {
                                Console.WriteLine(
                                    $"{nameof(NetworkLoggerClient)} network logger interrupted while shutting down, pending tasks canceled.");
                            } else {
                                Console.WriteLine(
                                    $"{nameof(NetworkLoggerClient)} error while processing event from queue: {e}");
                            }
                        }
                    }
                }
            } catch (Exception e) {
                Console.WriteLine($"{nameof(NetworkLoggerClient)} failed: {e}");
            }
        }

        public void Log(LogSeverity logLevel, string owner, string message) {
            _queue.Enqueue(Tuple.Create(logLevel, owner, message));
        }
    }
}