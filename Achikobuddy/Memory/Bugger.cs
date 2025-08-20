using System;
using System.Collections.Generic;

namespace Achikobuddy.Memory
{
    public static class Bugger
    {
        private static readonly List<string> _logMessages = new List<string>();
        private static bool _isLoggingEnabled = false;

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
    }
}