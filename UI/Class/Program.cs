using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UI
{
    public enum MachineStartupMode
    {
        Normal = 0,
        TrayBoxSwapped = 1
    }

    public static class RuntimeMachineMode
    {
        public static MachineStartupMode Current { get; private set; } = MachineStartupMode.Normal;

        public static bool IsTrayBoxSwapped => Current == MachineStartupMode.TrayBoxSwapped;

        public static void Set(MachineStartupMode mode)
        {
            Current = mode;
        }
    }

    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (!SelectMachineMode()) return;

            Process.Start(AppDomain.CurrentDomain.BaseDirectory + @"\Release\SecsApp.exe");
            //启动程序
            System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcessesByName(System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            if (ps.Length <= 1)
            {
                ImageSaveQueue.gInit();
                Application.Run(new FrMain());
            }
            else
            {
                MessageBox.Show("程序已运行!");
            }
        }

        private static bool SelectMachineMode()
        {
            DialogResult result = MessageBox.Show(
                "请选择机台版本:\r\n是：普通版本\r\n否：料仓互换版\r\n取消：退出程序",
                "机台版本选择",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (result == DialogResult.Cancel) return false;

            RuntimeMachineMode.Set(result == DialogResult.No
                ? MachineStartupMode.TrayBoxSwapped
                : MachineStartupMode.Normal);
            return true;
        }
    }
}
