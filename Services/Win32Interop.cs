using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Yorvis.Services
{
    public static class Win32Interop
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        public static TimeSpan GetIdleTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);
            uint elapsedTicks = (uint)Environment.TickCount - lastInputInfo.dwTime;
            return TimeSpan.FromMilliseconds(elapsedTicks);
        }

        public static (string ProcessName, string WindowTitle) GetActiveWindowInfo()
        {
            IntPtr handle = GetForegroundWindow();
            if (handle == IntPtr.Zero) return ("Idle", "No Active Window");

            string process = GetProcessName(handle) ?? "Unknown";
            string title = GetWindowTitle(handle) ?? string.Empty;

            // Handle common system windows that shouldn't disrupt tracking
            if (process == "explorer" && (string.IsNullOrEmpty(title) || title == "Start" || title == "Task Switching"))
            {
                return ("Desktop", "System Shell");
            }

            return (process, title);
        }

        private static string GetWindowTitle(IntPtr handle)
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            if (GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            return null;
        }

        private static string GetProcessName(IntPtr handle)
        {
            uint processId;
            GetWindowThreadProcessId(handle, out processId);
            try
            {
                Process p = Process.GetProcessById((int)processId);
                return p.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }

        public static string GetActiveWindowTitle() => GetActiveWindowInfo().WindowTitle;
        public static string GetActiveProcessName() => GetActiveWindowInfo().ProcessName;
    }
}
