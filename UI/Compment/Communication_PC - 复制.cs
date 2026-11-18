using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MotionCtrl;
using System.IO;
using System.Threading;
using System.IO.Ports;

namespace UI.Compment
{
    public partial class Communication_PC : UserControl
    {

        public static bool b_testpc_quit = false;
        AXIS ax = null;
        AXIS ay = null;
        AXIS az = null;
        public WS ws=null;

        public Communication_PC()
        {
            InitializeComponent();              
        }   
        public EM_RES LoadCfg_Test_WS()
        {
            string section;
            IniFile inf = new IniFile(Path.GetFullPath("..") + "\\syscfg\\syscfg_Test_WorkStation.ini");
            section = "WS1";
            COM.ws1.start_pos.x = inf.ReadDouble(section, "WS1_X", double.MaxValue);
            COM.ws1.start_pos.y = inf.ReadDouble(section, "WS1_Y", double.MaxValue);
            COM.ws1.start_pos.z = inf.ReadDouble(section, "WS1_Z", double.MaxValue);
            COM.ws1.testposleft    = inf.ReadDouble(section, "WS_X1_left", double.MaxValue);
            COM.ws1.testposright   = inf.ReadDouble(section, "WS_X1_right", double.MaxValue);
            COM.ws1.testpossafe = inf.ReadDouble(section, "WS_X1_safe", double.MaxValue);
            section = "WS2";
            COM.ws2.start_pos.x = inf.ReadDouble(section, "WS2_X", double.MaxValue);
            COM.ws2.start_pos.y = inf.ReadDouble(section, "WS2_Y", double.MaxValue);
            COM.ws2.start_pos.z = inf.ReadDouble(section, "WS2_Z", double.MaxValue);
            COM.ws2.testposleft = inf.ReadDouble(section, "WS_X1_left", double.MaxValue);
            COM.ws2.testposright = inf.ReadDouble(section, "WS_X1_right", double.MaxValue);
            COM.ws2.testpossafe = inf.ReadDouble(section, "WS_X1_safe", double.MaxValue);
            section = "WS3";
            COM.ws3.start_pos.x = inf.ReadDouble(section, "WS3_X", double.MaxValue);
            COM.ws3.start_pos.y = inf.ReadDouble(section, "WS3_Y", double.MaxValue);
            COM.ws3.start_pos.z = inf.ReadDouble(section, "WS3_Z", double.MaxValue);
            COM.ws3.testposleft = inf.ReadDouble(section, "WS_X1_left", double.MaxValue);
            COM.ws3.testposright = inf.ReadDouble(section, "WS_X1_right", double.MaxValue);
            COM.ws3.testpossafe = inf.ReadDouble(section, "WS_X1_safe", double.MaxValue);
            section = "WS4";
            COM.ws4.start_pos.x = inf.ReadDouble(section, "WS4_X", double.MaxValue);
            COM.ws4.start_pos.y = inf.ReadDouble(section, "WS4_Y", double.MaxValue);
            COM.ws4.start_pos.z = inf.ReadDouble(section, "WS4_Z", double.MaxValue);
            COM.ws4.testposleft = inf.ReadDouble(section, "WS_X1_left", double.MaxValue);
            COM.ws4.testposright = inf.ReadDouble(section, "WS_X1_right", double.MaxValue);
            COM.ws4.testpossafe = inf.ReadDouble(section, "WS_X1_safe", double.MaxValue);
            section = "COM";
            VAR.gsys_set.str_w1_com = inf.ReadString(section, "W1_COM", comboBox1.Text);
            VAR.gsys_set.str_w2_com = inf.ReadString(section, "W2_COM", comboBox2.Text);
            nud_ws1_x.Value = (decimal)COM.ws1.start_pos.x;
            nud_ws1_y.Value = (decimal)COM.ws1.start_pos.y;
            nud_ws1_z.Value = (decimal)COM.ws1.start_pos.z;
            nud_ws2_x.Value = (decimal)COM.ws2.start_pos.x;
            nud_ws2_y.Value = (decimal)COM.ws2.start_pos.y;
            nud_ws2_z.Value = (decimal)COM.ws2.start_pos.z;
            nud_ws3_x.Value = (decimal)COM.ws3.start_pos.x;
            nud_ws3_y.Value = (decimal)COM.ws3.start_pos.y;
            nud_ws3_z.Value = (decimal)COM.ws3.start_pos.z;
            nud_ws4_x.Value = (decimal)COM.ws4.start_pos.x;
            nud_ws4_y.Value = (decimal)COM.ws4.start_pos.y;
            nud_ws4_z.Value = (decimal)COM.ws4.start_pos.z;
            nud_ws_x1_left.Value = (decimal)COM.ws1.testposleft;
            nud_ws_x1_right.Value = (decimal)COM.ws1.testposright;
            nud_ws_x1_safe.Value = (decimal)COM.ws1.testpossafe;
           comboBox1.Text = VAR.gsys_set.str_w1_com;
           comboBox2.Text = VAR.gsys_set.str_w2_com;
            return EM_RES.OK;
        }
        public EM_RES SaveCfg__Test_WS()
        {
            string section;
            IniFile inf = new IniFile(Path.GetFullPath("..") + "\\syscfg\\syscfg_Test_WorkStation.ini");
            section = "WS1";
            inf.WriteString(section, "WS1_X", nud_ws1_x.Value.ToString());
            inf.WriteString(section, "WS1_Y", nud_ws1_y.Value.ToString());
            inf.WriteString(section, "WS1_Z", nud_ws1_z.Value.ToString());
            inf.WriteString(section, "WS_X1_left", nud_ws_x1_left.Value.ToString());
            inf.WriteString(section, "WS_X1_right", nud_ws_x1_right.Value.ToString());
            inf.WriteString(section, "WS_X1_safe", nud_ws_x1_safe.Value.ToString());
            section = "WS2";
            inf.WriteString(section, "WS2_X", nud_ws2_x.Value.ToString());
            inf.WriteString(section, "WS2_Y", nud_ws2_y.Value.ToString());
            inf.WriteString(section, "WS2_Z", nud_ws2_z.Value.ToString());
            inf.WriteString(section, "WS_X1_left", nud_ws_x1_left.Value.ToString());
            inf.WriteString(section, "WS_X1_right", nud_ws_x1_right.Value.ToString());
            inf.WriteString(section, "WS_X1_safe", nud_ws_x1_safe.Value.ToString());
            section = "WS3";
            inf.WriteString(section, "WS3_X", nud_ws3_x.Value.ToString());
            inf.WriteString(section, "WS3_Y", nud_ws3_y.Value.ToString());
            inf.WriteString(section, "WS3_Z", nud_ws3_z.Value.ToString());
            inf.WriteString(section, "WS_X1_left", nud_ws_x1_left.Value.ToString());
            inf.WriteString(section, "WS_X1_right", nud_ws_x1_right.Value.ToString());
            inf.WriteString(section, "WS_X1_safe", nud_ws_x1_safe.Value.ToString());
            section = "WS4";
            inf.WriteString(section, "WS4_X", nud_ws4_x.Value.ToString());
            inf.WriteString(section, "WS4_Y", nud_ws4_y.Value.ToString());
            inf.WriteString(section, "WS4_Z", nud_ws4_z.Value.ToString());
            inf.WriteString(section, "WS_X1_left", nud_ws_x1_left.Value.ToString());
            inf.WriteString(section, "WS_X1_right", nud_ws_x1_right.Value.ToString());
            inf.WriteString(section, "WS_X1_safe", nud_ws_x1_safe.Value.ToString());
            section = "COM";
            inf.WriteString(section, "W1_COM", comboBox1.Text);
            inf.WriteString(section, "W2_COM", comboBox2.Text);
            return EM_RES.OK;
        }




        public void btn_save_pctest_Click(object sender, EventArgs e)
        {
            EM_RES res;
            res= SaveCfg__Test_WS();
            LoadCfg_Test_WS();          
            if ( res== EM_RES.OK)
            {
                MessageBox.Show("测试工位参数保存成功");
            }            
        }

        private void axTcpServer_PC_OnErrorMsg(string msg)
        {
            MessageBox.Show(msg);
        }

        public void ShowTestData()
        {
            LoadCfg_Test_WS();
        }
        private void button1_Click_1(object sender, EventArgs e)
        {
            //LoadCfg_Test_WS();
            WS ws = null;
            switch (((Button) sender).Name)
            {
                case "get_ws1_startpos":
                    ws = COM.ws1;
                    break;
                case "get_ws2_startpos":
                    ws = COM.ws2;
                    break;
                case "get_ws3_startpos":
                    ws = COM.ws3;
                    break;
                case "get_ws4_startpos":
                    ws = COM.ws4;
                    break;
                case "get_ws_leftpos":
                    ws = COM.ws1;
                    break;
                case "get_ws_rightpos":
                    ws = COM.ws1;
                    break;
                case "get_ws_safepos":
                    ws = COM.ws1;
                    break;
            }

            if (ws.ax_x.cmd_pos / ws.ax_x.pul_per_mm > 5000 || ws.ax_x.cmd_pos/ws.ax_x.pul_per_mm < -5000)
            {
                MessageBox.Show("当前X轴位置超出范围");
                return;
            }
            if (ws.ax_y.cmd_pos/ws.ax_y.pul_per_mm > 5000 || ws.ax_y.cmd_pos/ ws.ax_y.pul_per_mm < -5000)
            {
                MessageBox.Show("当前Y轴位置超出范围");
                return;
            }
            if (ws.ax_z.cmd_pos/ ws.ax_z.pul_per_mm > 5000 || ws.ax_z.cmd_pos/ ws.ax_z.pul_per_mm < -5000)
            {
                MessageBox.Show("当前Z轴位置超出范围");
                return;
            }
            if (ws.ax_x1.enc_pos/ ws.ax_x1.pul_per_mm > 5000 || ws.ax_x1.enc_pos/ ws.ax_x1.pul_per_mm < -5000)
            {
                MessageBox.Show("当前Z轴位置超出范围");
                return;
            }
            switch (((Button)sender).Name)
            {
                case "get_ws1_startpos":
                    nud_ws1_x.Value =(decimal)(ws.ax_x.cmd_pos / ws.ax_x.pul_per_mm);
                    nud_ws1_y.Value = (decimal)(ws.ax_y.cmd_pos / ws.ax_y.pul_per_mm);
                    nud_ws1_z.Value = (decimal)(ws.ax_z.cmd_pos / ws.ax_z.pul_per_mm);
                    break;
                case "get_ws2_startpos":
                    nud_ws2_x.Value = (decimal)(ws.ax_x.cmd_pos / ws.ax_x.pul_per_mm);
                    nud_ws2_y.Value = (decimal)(ws.ax_y.cmd_pos / ws.ax_y.pul_per_mm);
                    nud_ws2_z.Value = (decimal)(ws.ax_z.cmd_pos / ws.ax_z.pul_per_mm);
                    break;
                case "get_ws3_startpos":
                    nud_ws3_x.Value = (decimal)(ws.ax_x.cmd_pos / ws.ax_x.pul_per_mm);
                    nud_ws3_y.Value = (decimal)(ws.ax_y.cmd_pos / ws.ax_y.pul_per_mm);
                    nud_ws3_z.Value = (decimal)(ws.ax_z.cmd_pos / ws.ax_z.pul_per_mm);
                    break;
                case "get_ws4_startpos":
                    nud_ws4_x.Value = (decimal)(ws.ax_x.cmd_pos / ws.ax_x.pul_per_mm);
                    nud_ws4_y.Value = (decimal)(ws.ax_y.cmd_pos / ws.ax_y.pul_per_mm);
                    nud_ws4_z.Value = (decimal)(ws.ax_z.cmd_pos / ws.ax_z.pul_per_mm);
                    break;
                case "get_ws_leftpos":
                    nud_ws_x1_left.Value = (decimal)(ws.ax_x1.enc_pos / ws.ax_x1.pul_per_mm);                   
                    break;
                case "get_ws_rightpos":
                    nud_ws_x1_right.Value = (decimal)(ws.ax_x1.enc_pos / ws.ax_x1.pul_per_mm);                   
                    break;
                case "get_ws_safepos":
                    nud_ws_x1_safe.Value = (decimal)(ws.ax_x1.enc_pos / ws.ax_x1.pul_per_mm);
                    break;
            }           
        }
        private void goto_ws1_startpos_Click(object sender, EventArgs e)
        {            
            switch (((Button)sender).Name)
            {
                case "goto_ws1_startpos":
                    MT.Move(ref VAR.gsys_set.bquit, ref COM.ws1.ax_x, (double)nud_ws1_x.Value, ref COM.ws1.ax_y,
                        (double)nud_ws1_y.Value, ref COM.ws1.ax_z, (double)nud_ws1_z.Value);
                    break;
                case "goto_ws2_startpos":
                    MT.Move(ref VAR.gsys_set.bquit, ref COM.ws2.ax_x, (double)nud_ws2_x.Value, ref COM.ws2.ax_y,
                        (double)nud_ws2_y.Value, ref COM.ws2.ax_z, (double)nud_ws2_z.Value);
                    break;
                case "goto_ws3_startpos":
                    MT.Move(ref VAR.gsys_set.bquit, ref COM.ws3.ax_x, (double)nud_ws3_x.Value, ref COM.ws3.ax_y,
                        (double)nud_ws3_y.Value, ref COM.ws3.ax_z, (double)nud_ws3_z.Value);
                    
                    break;
                case "goto_ws4_startpos":
                    MT.Move(ref VAR.gsys_set.bquit, ref COM.ws4.ax_x, (double)nud_ws4_x.Value, ref COM.ws4.ax_y,
                        (double)nud_ws4_y.Value, ref COM.ws4.ax_z, (double)nud_ws4_z.Value);
                    
                    break;
                case "goto_ws_leftpos":
                    MT.Move(ref VAR.gsys_set.bquit, ref COM.ws1.ax_x1, (double)nud_ws_x1_left.Value);
                    break;
                case "goto_ws_rightpos":
                    MT.Move(ref VAR.gsys_set.bquit, ref COM.ws1.ax_x1, (double)nud_ws_x1_right.Value);
                    break;
                case "goto_ws_safepos":
                    MT.Move(ref VAR.gsys_set.bquit, ref COM.ws1.ax_x1, (double)nud_ws_x1_safe.Value);
                    break;
            }
        }

        private void Communication_PC_Load(object sender, EventArgs e)
        {
            ShowTestData();
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 1;
            }
            else
            {
                comboBox1.Text = "未检测到串口";
            }
            comboBox2.Items.AddRange(SerialPort.GetPortNames());
            if (comboBox2.Items.Count > 0)
            {
                comboBox2.SelectedIndex = 7;
            }
            else
            {
                comboBox2.Text = "未检测到串口";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!FrMain.frmain.SPort1.IsOpen)
            {
                try
                {

                    FrMain.frmain.SPort1.PortName = comboBox1.Text;
                    FrMain.frmain.SPort1.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("串口打开失败!");
                }
            }
                
            
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
          //  FrMain.frmain.SPort1.PortName = comboBox1.Text;
            FrMain.frmain.SPort1.Close();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!FrMain.frmain.SPort2.IsOpen)
            {
                try
                {

                    FrMain.frmain.SPort2.PortName = comboBox2.Text;
                    FrMain.frmain.SPort2.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("串口打开失败!");
                }
            }
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
          //  FrMain.frmain.SPort2.PortName = comboBox2.Text;
            FrMain.frmain.SPort2.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (FrMain.frmain.SPort1.IsOpen)
            {
                panel2.BackColor = Color.Green;
            }
            else
            {
                panel2.BackColor = Color.Red;
            }
            if (FrMain.frmain.SPort2.IsOpen)
            {
                panel3.BackColor = Color.Green;
            }
            else
            {
                panel3.BackColor = Color.Red;
            }
            textBox2.Text = FrMain.frmain.ReadStr1;
            textBox3.Text = FrMain.frmain.ReadStr2;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            EM_RES res;            
            res = LoadCfg_Test_WS();
            if (res == EM_RES.OK)
            {
                MessageBox.Show("测试工位参数获取成功");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            String curtext = null;
            TestPC testpc = new TestPC();
            byte[] curtexthex = null;
            if(textBox4.Text!=null)
            {
                curtext = textBox4.Text;
            }
            else
            {
                MessageBox.Show("文本未输入，请确认");
                return;
            }
            curtexthex = testpc.strToHexByte(curtext);
            if (!FrMain.frmain.SPort2.IsOpen)
            {
                MessageBox.Show("串口" + comboBox2.Text + "未打开，请确认");
                return;
            }
            else
            {
                FrMain.frmain.SPort2.Write(curtexthex, 0, curtexthex.Length);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            String curtext = null;
            TestPC testpc = new TestPC();           
            byte[] curtexthex = null;
            
            
            if (textBox1.Text != null)
            {
                curtext = textBox1.Text;
            }
            else
            {
                MessageBox.Show("文本未输入，请确认");
                return;
            }
            curtexthex = testpc.strToHexByte(curtext);
            if (!FrMain.frmain.SPort1.IsOpen)
            {
                MessageBox.Show("串口"+comboBox1.Text+"未打开，请确认");
                return;
            }
            else
            {
                FrMain.frmain.SPort1.Write(curtexthex, 0, curtexthex.Length);
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled == true)
            {
                timer1.Enabled = false;
            }
            else
            {
                timer1.Enabled = true;
            }
            
        }
    }
}
