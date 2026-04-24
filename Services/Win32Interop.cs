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

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            return null;
        }

        public static string GetActiveProcessName()
        {
            IntPtr handle = GetForegroundWindow();
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
    }
}
