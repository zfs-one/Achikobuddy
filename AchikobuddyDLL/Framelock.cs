using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using EasyHook;
using GreyMagic;

namespace AchikobuddyDll
{
    public sealed class Framelock
    {
        private static readonly Lazy<Framelock> _instance = new Lazy<Framelock>(() => new Framelock());
        public static Framelock Instance => _instance.Value;

        private LocalHook _endSceneHook;
        private bool _isHooked;
        private delegate int EndSceneDelegate(IntPtr device);
        private EndSceneDelegate _originalEndScene;
        private readonly InProcessMemoryReader _reader;

        private Framelock()
        {
            _reader = new InProcessMemoryReader(System.Diagnostics.Process.GetCurrentProcess());
        }

        public void Initialize()
        {
            if (_isHooked) return;
            try
            {
                IntPtr device = GetD3D9Device();
                if (device == IntPtr.Zero)
                    throw new Exception("Failed to find D3D9 device");

                IntPtr vtable = _reader.Read<IntPtr>(device);
                IntPtr endSceneAddr = _reader.Read<IntPtr>(vtable + 42 * 4); // Index 42, x86
                _originalEndScene = Marshal.GetDelegateForFunctionPointer<EndSceneDelegate>(endSceneAddr);
                _endSceneHook = LocalHook.Create(endSceneAddr, new EndSceneDelegate(MyEndScene), null);
                _endSceneHook.ThreadACL.SetInclusiveACL(new int[] { 0 });
                _isHooked = true;
                File.AppendAllText("achikobuddy.log", $"{DateTime.Now:HH:mm:ss}: EndScene hooked at 0x{endSceneAddr.ToInt64():X8} [Critical]\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("achikobuddy.log", $"{DateTime.Now:HH:mm:ss}: EndScene hook failed: {ex.Message} [Critical]\n");
            }
        }

        private IntPtr GetD3D9Device()
        {
            const uint baseAddr = 0x00C5DF88; // 1.12.1 D3D9 device pointer
            IntPtr device = _reader.Read<IntPtr>(new IntPtr(baseAddr)); // Cast uint to IntPtr
            if (device != IntPtr.Zero)
                return device;

            // Fallback: Pattern scan inspired by ZzukBot4's GetEndScene.cs
            byte[] pattern = { 0x8B, 0x0D, 0x88, 0xDF, 0xC5, 0x00 }; // mov ecx, [0xC5DF88]
            IntPtr scanResult = MemoryScanner.Scan(_reader, new IntPtr(0x00400000), new IntPtr(0x00800000), pattern);
            return scanResult != IntPtr.Zero ? _reader.Read<IntPtr>(scanResult + 2) : IntPtr.Zero;
        }

        private int MyEndScene(IntPtr device)
        {
            try
            {
                // Read the pointer to the player name
                IntPtr namePtr = _reader.Read<IntPtr>(new IntPtr(Offsets.Player.Name)); // Cast uint to IntPtr
                // Read up to 64 bytes (adjust as needed) and convert to string
                byte[] nameBytes = _reader.ReadBytes(namePtr, 64);
                string playerName = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                File.AppendAllText("achikobuddy.log", $"{DateTime.Now:HH:mm:ss}: EndScene called, Player: {playerName} [Critical]\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("achikobuddy.log", $"{DateTime.Now:HH:mm:ss}: EndScene error: {ex.Message} [Critical]\n");
            }
            return _originalEndScene(device);
        }

        public void Dispose()
        {
            if (_isHooked)
            {
                _endSceneHook?.Dispose();
                _isHooked = false;
                File.AppendAllText("achikobuddy.log", $"{DateTime.Now:HH:mm:ss}: EndScene unhooked [Critical]\n");
            }
        }
    }

    public static class MemoryScanner
    {
        public static IntPtr Scan(InProcessMemoryReader reader, IntPtr start, IntPtr end, byte[] pattern)
        {
            for (IntPtr addr = start; addr.ToInt64() < end.ToInt64(); addr = new IntPtr(addr.ToInt64() + 4))
            {
                byte[] bytes = reader.ReadBytes(addr, pattern.Length);
                if (bytes != null && MatchPattern(bytes, pattern))
                    return addr;
            }
            return IntPtr.Zero;
        }

        private static bool MatchPattern(byte[] bytes, byte[] pattern)
        {
            if (bytes.Length != pattern.Length) return false;
            for (int i = 0; i < bytes.Length; i++)
                if (bytes[i] != pattern[i])
                    return false;
            return true;
        }
    }
}