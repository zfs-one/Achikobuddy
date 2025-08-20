using System;
using System.IO;
using System.Runtime.InteropServices;
using EasyHook;

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

        private Framelock() { }

        public void Initialize()
        {
            if (_isHooked) return;
            try
            {
                // Placeholder: Get D3D9 device pointer (to be implemented)
                IntPtr device = IntPtr.Zero; // Replace with actual device lookup
                IntPtr vtable = Marshal.ReadIntPtr(device);
                IntPtr endSceneAddr = Marshal.ReadIntPtr(vtable + 42 * 4); // Index 42, 4 bytes for x86
                _originalEndScene = Marshal.GetDelegateForFunctionPointer<EndSceneDelegate>(endSceneAddr);
                _endSceneHook = LocalHook.Create(endSceneAddr, new EndSceneDelegate(MyEndScene), null);
                _endSceneHook.ThreadACL.SetInclusiveACL(new int[] { 0 }); // Hook all threads
                _isHooked = true;
                File.AppendAllText("achikobuddy.log", $"{DateTime.Now:HH:mm:ss}: EndScene hooked [Critical]\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("achikobuddy.log", $"{DateTime.Now:HH:mm:ss}: EndScene hook failed: {ex.Message} [Critical]\n");
            }
        }

        private int MyEndScene(IntPtr device)
        {
            // Placeholder: Run bot logic (e.g., read player position, call CTM)
            File.AppendAllText("achikobuddy.log", $"{DateTime.Now:HH:mm:ss}: EndScene called [Critical]\n");
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
}