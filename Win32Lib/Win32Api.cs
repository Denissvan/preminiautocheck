using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Win32Lib
{
    public class Win32Api
    {
        public static int WM_COPYDATA = 0x004A;

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref CopyDataStruct lParam);

        [DllImport("Kernel32.dll", EntryPoint = "GetSystemTime", SetLastError = true)]
        private static extern void Win32GetSystemTime(ref SystemTime sysTime);

        [DllImport("Kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
        private static extern bool Win32SetSystemTime(ref SystemTime sysTime);
      
        [DllImport("Kernel32.dll")]
        private static extern bool SetLocalTime(ref SystemTime sysTime);

        [DllImport("Kernel32.dll")]
        private static extern void GetLocalTime(ref SystemTime sysTime);

        [DllImport("Kernel32.dll")]
        private static extern uint GetLastError();

        [DllImport("User32.dll")]
        private static extern bool ChangeWindowMessageFilter(uint msg, int flags);
       
        public static void SendToSecsApp(string msg)
        {
            IntPtr hWnd = FindWindow(null, "SECS上位程序");
            //Process[] procs = Process.GetProcesses();
            //var app = procs.FirstOrDefault(i => i.ProcessName.Contains("SecsApp"));
            if (hWnd != IntPtr.Zero)
            //if (app != null)
            {
                //IntPtr hWnd = app.MainWindowHandle;
                byte[] sarr = System.Text.Encoding.Default.GetBytes(msg);
                int len = sarr.Length;
                CopyDataStruct data;
                data.DwData = (IntPtr)0;
                data.CbData = len + 1;
                data.LpData = msg;
                // 发送消息
                var ret =Win32Api.SendMessage(hWnd, Win32Api.WM_COPYDATA, IntPtr.Zero, ref data);
            }
        }    
        
        public static void SendToMiniApp(string msg,string name = "WLTmini")
        {
            Process[] procs = Process.GetProcesses();
            var app = procs.FirstOrDefault(i => i.ProcessName.Contains(name));
            if (app != null)
            {
                IntPtr hWnd = app.MainWindowHandle;
                byte[] sarr = System.Text.Encoding.Default.GetBytes(msg);
                int len = sarr.Length;
                CopyDataStruct data;
                data.DwData = (IntPtr)0;
                data.CbData = len + 1;
                data.LpData = msg;
                // 发送消息
                var ret =Win32Api.SendMessage(hWnd, Win32Api.WM_COPYDATA, IntPtr.Zero, ref data);
            }
        }

        public static void ChangeSecsWindowMessageFilter()
        {
            ChangeWindowMessageFilter((uint)WM_COPYDATA, 1);
        }

        public static bool SetLocalTime(DateTime dateTime)
        {
            SystemTime st = new SystemTime();
            st.Year = (ushort)dateTime.Year; // must be short
            st.Month = (ushort)dateTime.Month;
            st.Day = (ushort)dateTime.Day;
            st.Hour = (ushort)dateTime.Hour;
            st.Minute = (ushort)dateTime.Minute;
            st.Second = (ushort)dateTime.Second;
            var ret = SetLocalTime(ref st);
            //Console.WriteLine("SetSystemTime return : " + ret);
            //Console.WriteLine("Last error : " + GetLastError());
            return ret;
        }
    }

    public struct CopyDataStruct
    {
        public IntPtr DwData;
        public int CbData;
        [MarshalAs(UnmanagedType.LPStr)]
        public string LpData;
    }

    public struct SystemTime
    {
        public ushort Year;
        public ushort Month;
        public ushort DayOfWeek;
        public ushort Day;
        public ushort Hour;
        public ushort Minute;
        public ushort Second;
        public ushort Millisecond;
    };
}
