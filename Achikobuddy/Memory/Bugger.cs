using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Achikobuddy.Memory
{
    public static class Bugger
    {
        private static readonly List<string> _logMessages = new List<string>();
        private static bool _isLoggingEnabled = false;

        private static CancellationTokenSource _pipeCts;

        public static void EnableLogging(bool enable)
        {
            _isLoggingEnabled = enable;
            Log($"Logging {(enable ? "enabled" : "disabled")} [Critical]");
        }

        public static void Log(string message)
        {
            if (!_isLoggingEnabled) return;
            string timestampedMessage = $"{DateTime.Now:HH:mm:ss}: {message}";
            lock (_logMessages)
            {
                _logMessages.Add(timestampedMessage);
                if (_logMessages.Count > 1000) // Prevent memory bloat
                    _logMessages.RemoveAt(0);
            }
        }

        public static List<string> GetLogMessages()
        {
            lock (_logMessages)
            {
                return new List<string>(_logMessages);
            }
        }

        public static void ClearLogs()
        {
            lock (_logMessages)
            {
                _logMessages.Clear();
            }
        }

        // ---------------- Pipe server ----------------
        public static void StartPipeServer()
        {
            if (_pipeCts != null) return; // already running
            _pipeCts = new CancellationTokenSource();
            Task.Run(() => RunPipeServer(_pipeCts.Token));
            Log("Pipe server started [Critical]");
        }

        public static void StopPipeServer()
        {
            if (_pipeCts == null) return;
            _pipeCts.Cancel();
            _pipeCts = null;
            Log("Pipe server stopped [Critical]");
        }

        private static async Task RunPipeServer(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (var pipe = new NamedPipeServerStream("AchikobuddyLogPipe",
                        PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                    {
                        await pipe.WaitForConnectionAsync(token);
                        Log("Pipe server connected to client [Critical]");
                        using (var reader = new StreamReader(pipe))
                        {
                            while (pipe.IsConnected && !token.IsCancellationRequested)
                            {
                                var line = await reader.ReadLineAsync();
                                if (line == null) break;
                                Log(line);
                            }
                        }
                        Log("Pipe server disconnected [Critical]");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Pipe server error: {ex.Message} [Critical]");
                    await Task.Delay(1000, token); // backoff
                }
            }
        }
    }
}
