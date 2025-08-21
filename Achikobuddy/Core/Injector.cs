using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Achikobuddy.Memory;

namespace Achikobuddy.Core
{
    public static class Injector
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        public static bool InjectDll(int pid, string dllPath)
        {
            Bugger.Log($"Attempting to inject DLL: {dllPath} into PID {pid} [Critical]");
            if (!File.Exists(dllPath))
            {
                Bugger.Log($"DLL not found at {dllPath} [Critical]");
                return false;
            }

            try
            {
                Process process = Process.GetProcessById(pid);
                IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
                if (hProcess == IntPtr.Zero)
                {
                    Bugger.Log($"Failed to open process PID {pid}: Error {Marshal.GetLastWin32Error()} [Critical]");
                    return false;
                }

                byte[] dllPathBytes = Encoding.ASCII.GetBytes(dllPath + "\0");
                IntPtr allocAddr = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllPathBytes.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                if (allocAddr == IntPtr.Zero)
                {
                    Bugger.Log($"Failed to allocate memory in PID {pid}: Error {Marshal.GetLastWin32Error()} [Critical]");
                    CloseHandle(hProcess);
                    return false;
                }

                IntPtr bytesWritten;
                if (!WriteProcessMemory(hProcess, allocAddr, dllPathBytes, (uint)dllPathBytes.Length, out bytesWritten))
                {
                    Bugger.Log($"Failed to write DLL path to PID {pid}: Error {Marshal.GetLastWin32Error()} [Critical]");
                    CloseHandle(hProcess);
                    return false;
                }

                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    Bugger.Log($"Failed to get LoadLibraryA address: Error {Marshal.GetLastWin32Error()} [Critical]");
                    CloseHandle(hProcess);
                    return false;
                }

                IntPtr threadHandle = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocAddr, 0, out _);
                if (threadHandle == IntPtr.Zero)
                {
                    Bugger.Log($"Failed to create remote thread in PID {pid}: Error {Marshal.GetLastWin32Error()} [Critical]");
                    CloseHandle(hProcess);
                    return false;
                }

                CloseHandle(threadHandle);
                CloseHandle(hProcess);
                Bugger.Log($"DLL injected into PID {pid} [Critical]");
                return true;
            }
            catch (Exception ex)
            {
                Bugger.Log($"Injection failed for PID {pid}: {ex.Message} [Critical]");
                return false;
            }
        }
    }
}