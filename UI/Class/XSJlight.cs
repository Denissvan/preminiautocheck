using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace UI.Class
{
    public class XSJlight
    {

        const string DllName = "E:\\light_control.dll";//E:\\
        //                                               // 声明 DLL 中的函数

        // 指定DLL文件的名称和函数的入口点  

        //[DllImport("light_control.dll", EntryPoint = "open_light_source", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //public static extern int open_light_source(ref IntPtr hSerial, string serial_port, UInt32 baudrate);

        //[DllImport("light_control.dll", EntryPoint = "set_light_source", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //public static extern int set_light_source(IntPtr hSerial, UInt32 light_index);


        //[DllImport("light_control.dll", EntryPoint = "close_light_source", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //public static extern int close_light_source(IntPtr hSerial);

        //[DllImport("light_control.dll", EntryPoint = "get_light_count", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //public static extern Int32 get_light_count();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 open_light_source(ref IntPtr hSerial, string serial_port, UInt32 baudrate);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 set_light_source(IntPtr hSerial, UInt32 light_index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 close_light_source(IntPtr hSerial);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]

        public static extern Int32 get_light_count();

        public static IntPtr hSerial = IntPtr.Zero;
        public static string serial_port = "COM4";
        public static UInt32 baudrate = 9600;
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
       

        public static bool Changenel1(int channelid)
        {
            uint light_index = (uint)channelid;
            int result = set_light_source(hSerial, light_index);
            if (result==0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool OpenXsjlight()
        {
            Int32 result/* = get_light_count()*/;
            // 调用 DLL 中的函数
            result = open_light_source(ref hSerial, serial_port, baudrate);
            if (result == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool CloseXsjlight()
        {

            Int32 result/* = get_light_count()*/;
            result = close_light_source(hSerial);
            if (result == 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

    }
}
