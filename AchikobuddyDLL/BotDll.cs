using System;
using System.IO;
using System.Diagnostics;
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
            File.AppendAllText("achikobuddy.log", $"{DateTime.Now:HH:mm:ss}: EntryPoint invoked [Critical]\n");

            try
            {
                var proc = Process.GetCurrentProcess();
                File.AppendAllText("achikobuddy.log",
                    $"{DateTime.Now:HH:mm:ss}: EntryPoint called in process {proc.ProcessName} ({proc.Id}) [Critical]\n");

                _reader = new InProcessMemoryReader(proc);
                File.AppendAllText("achikobuddy.log",
                    $"{DateTime.Now:HH:mm:ss}: InProcessMemoryReader initialized [Critical]\n");

                _framelock = Framelock.Instance;
                File.AppendAllText("achikobuddy.log",
                    $"{DateTime.Now:HH:mm:ss}: Framelock instance created [Critical]\n");

                _framelock.Initialize();
                File.AppendAllText("achikobuddy.log",
                    $"{DateTime.Now:HH:mm:ss}: DLL injected, InProcessMemoryReader and Framelock initialized [Critical]\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("achikobuddy.log",
                    $"{DateTime.Now:HH:mm:ss}: DLL init failed: {ex.Message} [Critical]\n");
            }
        }
    }
}