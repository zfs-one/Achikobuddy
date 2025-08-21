using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;
using DllExporterNet4;
using GreyMagic;

namespace AchikobuddyDll
{
    public static class BotDll
    {
        private static InProcessMemoryReader _reader;
        private static Framelock _framelock;

        [DllExport]
        public static void EntryPoint()
        {
            Log("EntryPoint invoked [Critical]");
            try
            {
                Log("Attempting to initialize process [Critical]");
                var proc = Process.GetCurrentProcess();
                Log($"EntryPoint called in process {proc.ProcessName} ({proc.Id}) [Critical]");

                Log("Initializing InProcessMemoryReader [Critical]");
                _reader = new InProcessMemoryReader(proc);
                Log("InProcessMemoryReader initialized [Critical]");

                Log("Creating Framelock instance [Critical]");
                _framelock = Framelock.Instance;
                Log("Framelock instance created [Critical]");

                Log("Initializing Framelock [Critical]");
                _framelock.Initialize();
                Log("DLL injected, InProcessMemoryReader and Framelock initialized [Critical]");
            }
            catch (Exception ex)
            {
                Log($"DLL init failed: {ex.Message} [Critical]");
            }
        }

        private static void Log(string message)
        {
            string timestampedMessage = $"{DateTime.Now:HH:mm:ss}: {message}";
            bool loggedToPipe = false;
            for (int i = 0; i < 3; i++) // Retry 3 times
            {
                try
                {
                    using (var pipe = new NamedPipeClientStream(".", "AchikobuddyLogPipe", PipeDirection.Out))
                    {
                        pipe.Connect(1000);
                        using (var writer = new StreamWriter(pipe))
                        {
                            writer.WriteLine(timestampedMessage);
                            writer.Flush();
                        }
                        loggedToPipe = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Thread.Sleep(100); // Wait before retry
                    if (i == 2) // Log to file on final failure
                    {
                        try
                        {
                            File.AppendAllText(@"C:\dll_log.txt", $"{timestampedMessage}, Pipe error: {ex.Message}\n");
                        }
                        catch (Exception fileEx)
                        {
                            // Fallback to temp directory if C:\ fails
                            try
                            {
                                string tempPath = Path.Combine(Path.GetTempPath(), "dll_log.txt");
                                File.AppendAllText(tempPath,
                                    $"{timestampedMessage}, Pipe error: {ex.Message}, C:\\ error: {fileEx.Message}\n");
                            }
                            catch (Exception tempEx)
                            {
                                // Last resort: try current directory
                                try
                                {
                                    File.AppendAllText("dll_log.txt",
                                        $"{timestampedMessage}, Pipe error: {ex.Message}, C:\\ error: {fileEx.Message}, Temp error: {tempEx.Message}\n");
                                }
                                catch { /* Silent fail */ }
                            }
                        }
                    }
                }
            }
            if (loggedToPipe)
                Thread.Sleep(100); // Throttle to prevent pipe overload
        }
    }
}