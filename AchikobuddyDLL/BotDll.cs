using GreyMagic;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AchikobuddyDll
{
    public class BotDll
    {
        private static InProcessMemoryReader _reader;
        private static Framelock _framelock;

        [DllExport("EntryPoint", CallingConvention = CallingConvention.Cdecl)]
        public static void EntryPoint()
        {
            try
            {
                _reader = new InProcessMemoryReader(System.Diagnostics.Process.GetCurrentProcess());
                _framelock = Framelock.Instance;
                _framelock.Initialize();
                File.AppendAllText("achikobuddy.log", $"{DateTime.Now:HH:mm:ss}: DLL injected, InProcessMemoryReader and Framelock initialized [Critical]\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("achikobuddy.log", $"{DateTime.Now:HH:mm:ss}: DLL init failed: {ex.Message} [Critical]\n");
            }
        }
    }
}