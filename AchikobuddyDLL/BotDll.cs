using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms; // ✅ WinForms for MessageBox
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
            try
            {
                Log("EntryPoint invoked [Critical]");

                var proc = Process.GetCurrentProcess();
                Log($"EntryPoint called in process {proc.ProcessName} ({proc.Id}) [Critical]");

                // ✅ Sanity check messagebox
                MessageBox.Show(
                    $"Achikobuddy DLL injected into {proc.ProcessName} (PID: {proc.Id})",
                    "Achikobuddy DLL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

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
                catch
                {
                    Thread.Sleep(100);
                }
            }

            if (!loggedToPipe)
            {
                try { File.AppendAllText(@"C:\dll_log.txt", timestampedMessage + Environment.NewLine); }
                catch { /* ignore if logging fails completely */ }
            }
        }
    }
}
