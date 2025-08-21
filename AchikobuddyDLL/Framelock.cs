using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        private DateTime _lastLogTime = DateTime.MinValue;

        private Framelock()
        {
            _reader = new InProcessMemoryReader(System.Diagnostics.Process.GetCurrentProcess());
        }

        public void Initialize()
        {
            if (_isHooked) return;
            try
            {
                Log("Attempting to find D3D9 device [Critical]");
                IntPtr device = GetD3D9Device();
                Log($"D3D9 device pointer: 0x{device.ToInt64():X8} [Critical]");
                if (device == IntPtr.Zero)
                    throw new Exception("Failed to find D3D9 device");

                Log("Reading D3D9 vtable [Critical]");
                IntPtr vtable = _reader.Read<IntPtr>(device);
                Log($"Vtable pointer: 0x{vtable.ToInt64():X8} [Critical]");
                IntPtr endSceneAddr = _reader.Read<IntPtr>(vtable + 42 * 4); // Index 42, x86
                Log($"EndScene address: 0x{endSceneAddr.ToInt64():X8} [Critical]");

                Log("Creating EndScene hook [Critical]");
                _originalEndScene = Marshal.GetDelegateForFunctionPointer<EndSceneDelegate>(endSceneAddr);
                _endSceneHook = LocalHook.Create(endSceneAddr, new EndSceneDelegate(MyEndScene), null);
                _endSceneHook.ThreadACL.SetInclusiveACL(new int[] { 0 });
                _isHooked = true;
                Log($"EndScene hooked at 0x{endSceneAddr.ToInt64():X8} [Critical]");
            }
            catch (Exception ex)
            {
                Log($"EndScene hook failed: {ex.Message} [Critical]");
            }
        }

        private IntPtr GetD3D9Device()
        {
            const uint baseAddr = 0x00C5DF88; // 1.12.1 D3D9 device pointer
            Log($"Reading D3D9 device pointer at 0x{baseAddr:X8} [Critical]");
            IntPtr device = _reader.Read<IntPtr>(new IntPtr(baseAddr));
            if (device != IntPtr.Zero)
            {
                Log($"Found D3D9 device at 0x{device.ToInt64():X8} [Critical]");
                return device;
            }

            Log("D3D9 device not found at base address, attempting pattern scan [Critical]");
            byte[] pattern = { 0x8B, 0x0D, 0x88, 0xDF, 0xC5, 0x00 }; // mov ecx, [0xC5DF88]
            IntPtr scanResult = MemoryScanner.Scan(_reader, new IntPtr(0x00400000), new IntPtr(0x00800000), pattern);
            Log($"Pattern scan result: 0x{scanResult.ToInt64():X8} [Critical]");
            return scanResult != IntPtr.Zero ? _reader.Read<IntPtr>(scanResult + 2) : IntPtr.Zero;
        }

        private int MyEndScene(IntPtr device)
        {
            try
            {
                // Throttle logging to once every 1000ms
                if ((DateTime.Now - _lastLogTime).TotalMilliseconds >= 1000)
                {
                    IntPtr namePtr = _reader.Read<IntPtr>(new IntPtr(Offsets.Player.Name));
                    byte[] nameBytes = _reader.ReadBytes(namePtr, 64);
                    string playerName = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                    Log($"EndScene called, Player: {playerName} [Critical]");
                    _lastLogTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Log($"EndScene error: {ex.Message} [Critical]");
            }
            return _originalEndScene(device);
        }

        public void Dispose()
        {
            if (_isHooked)
            {
                _endSceneHook?.Dispose();
                _isHooked = false;
                Log("EndScene unhooked [Critical]");
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