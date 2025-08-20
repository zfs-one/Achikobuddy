using System;
using System.IO;
using System.Diagnostics;
using DllExporterNet4; // attribute lives here (from the NuGet package)
using GreyMagic;

namespace AchikobuddyDll
{
    public static class BotDll
    {
        private static InProcessMemoryReader _reader;
        private static Framelock _framelock;

        // NOTE: DllExporterNet4 expects the attribute with no ctor args
        [DllExport]
        public static void EntryPoint()
        {
            try
            {
                var proc = Process.GetCurrentProcess();

                // basic logging to help debug injection
                File.AppendAllText("achikobuddy.log",
                    $"{DateTime.Now:HH:mm:ss}: EntryPoint called in process {proc.ProcessName} ({proc.Id})\n");

                // initialize GreyMagic in-proc reader
                _reader = new InProcessMemoryReader(proc);

                // initialize framelock (if your GreyMagic build has it)
                _framelock = Framelock.Instance;
                _framelock.Initialize();

                File.AppendAllText("achikobuddy.log",
                    $"{DateTime.Now:HH:mm:ss}: DLL injected, InProcessMemoryReader and Framelock initialized\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("achikobuddy.log",
                    $"{DateTime.Now:HH:mm:ss}: DLL init failed: {ex}\n");
            }
        }
    }
}
