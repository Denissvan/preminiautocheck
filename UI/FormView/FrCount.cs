using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MotionCtrl;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using Cognex.VisionPro.Exceptions;

namespace UI
{
    public partial class FrCount : Form
    {
        public class VNC_DAT
        {
            public string disc = "";
            public string ip ="";
            public string password = "";
            public void LoadCfg(int ws_id, int pc_id)
            {
                IniFile inf = new IniFile(VAR.gsys_set.GetSysCfgPath + "syscfg.ini");
                string str = inf.ReadString("TEST_PC", string.Format("WS{0}_PC{1}", ws_id+1, pc_id), "");
                string[] str_array = str.Split(',');
                if (str_array.Length > 0) ip = str_array[0];
                if (str_array.Length > 1) password = str_array[1];
                disc = string.Format("工站{0}/PC{1}[{2}]", ws_id + 1, pc_id, ip);
            }
        }
        List<VNC_DAT> ListVNC = new List<VNC_DAT>();

        public FrCount()
        {
            InitializeComponent();
        }


        private void FrCount_Load(object sender, EventArgs e)
        {
            sqlSelector_product.HandlerSelect = product_select;
            sqlSelector_count_data.HandlerSelect = data_select;
            sqlSelector_count_soket.HandlerSelect = soket_select;
            sqlSelector_count_ng.HandlerSelect = ng_select;
            sqlSelector_err_soket.HandlerSelect = err_select;
            Product.Tray tray = new Product.Tray(COM.traybox_ng.strCfgPath, Product.EM_CM_RES.OK);
            tray.NGDef = COM.NGDef;
            nGcode.TrayDat = tray;
        }


        private void ctb_product_SelectedIndexChanged(object sender, EventArgs e)
        {
            lb_SaveCsv.Text = VAR.IsChinese?"提示:":"Tip:";
            switch (((CTabControl)sender).SelectedTab.Name)
            {
                case "tp_test_pc":
                    int sel = cb_test_pc.SelectedIndex;
                    cb_test_pc.Items.Clear();
                    ListVNC.Clear();
                    foreach (WSFEED w in COM.list_wsfeed)
                    {
                        List<WSFEED.PCDat> ls = w.list_pc_dat;
                        foreach (WSFEED.PCDat pc in ls)
                        {
                            VNC_DAT vnc = new VNC_DAT();
                            vnc.LoadCfg(w.id, pc.ID);
                            ListVNC.Add(vnc);
                            cb_test_pc.Items.Add(vnc.disc);
                        }
                    }
                    cb_test_pc.SelectedIndex = sel;
                    break;
                default:
                    if (rd.IsConnected) rd.Disconnect();
                    break;
            }
        }

        private void tmr_update_Tick(object sender, EventArgs e)
        {
            switch (ctb_count.SelectedTab.Name)
            {
                default:
                    break;
            }
            tmr_update.Interval = 500;
        }

        private bool bproduct_select = true;

        private void product_select(object sender, EventArgs e)
        {
            if (bproduct_select)
            {
                bproduct_select = false;
                SQLData.TestDataProductCount(sqlSelector_product, chart_product);
                bproduct_select = true;
            }
        }
        private bool bdata_select = true;
        private void data_select(object sender, EventArgs e)
        {
            if (bdata_select)
            {
                bdata_select = false;
                SQLData.TestDataSelect(sqlSelector_count_data, dgv_count_data);
                bdata_select = true;
            }
        }

        private bool berrdata_select = true;
        private void err_select(object sender, EventArgs e)
        {
            if (berrdata_select)
            {
                berrdata_select = false;
                SQLData.TestDataerrSelect(sqlSelector_err_soket, data_err);
                berrdata_select = true;
            }
        }
        private bool bsoket_select = true;
        private void soket_select(object sender, EventArgs e)
        {
            if (bsoket_select)
            {
                bsoket_select = false;
                SQLData.TestDataSoketCount(sqlSelector_count_soket, chart_count_soket);
                bsoket_select = true;
            }
        }
        private bool bng_select = true;
        private void ng_select(object sender, EventArgs e)
        {
            if (bng_select)
            {
                bng_select = false;
                SQLData.TestNGCount(sqlSelector_count_ng, chart_count_ng);
                bng_select = true;
            }
        }

        private void dgv_count_data_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if ((e.RowIndex & 1) == 1)
            {
                e.CellStyle.BackColor = SystemColors.ControlLight;
            }
        }

        delegate void RdConnectDelegate(string ip, string pws);
        private void RdConnect(string ip, string pws)
        {
            if (true == rd.InvokeRequired)
            {
                RdConnectDelegate cn = new RdConnectDelegate(RdConnect);
                this.Invoke(cn, new object[] { ip,pws});
            }
            else
            {
                try
                {
                    if (rd.IsConnected) rd.Disconnect();
                    rd.GetPassword = () =>
                    {
                        return pws;
                    };
                    rd.Connect(ip, true, true);
                    rd.Focus();
                    rd.Refresh();
                    tbp_vnc.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }


         private void cb_test_pc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_test_pc.SelectedIndex < 0 || cb_test_pc.SelectedIndex >= ListVNC.Count) return;
            VNC_DAT vnc = ListVNC.ElementAt(cb_test_pc.SelectedIndex);
            Task t = new Task(() =>
            {
                RdConnect(vnc.ip, vnc.password);
            });
            t.Start();
        }

        private void btn_vnc_captrue_Click(object sender, EventArgs e)
        {
            if(rd.IsConnected)
            {
                Bitmap bmp = new Bitmap(rd.Width, rd.Height);//实例化一个和窗体一样大的bitmap
                Graphics g = Graphics.FromImage(bmp);
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.CopyFromScreen(rd.Left, rd.Top, 0, 0, new Size(rd.Width, rd.Height));
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "图片(*.png)|*.png";
                if(DialogResult.OK ==sfd.ShowDialog())
                {
                    if (sfd.FileName.Length > 0)
                    {
                        bmp.Save(sfd.FileName+".png");
                        MessageBox.Show(VAR.IsChinese?string.Format("截图保存成功!\r\n{0}.png", sfd.FileName): string.Format("Screenshot saved successfully!\r\n{0}.png\r\n截图保存成功!\r\n{0}.png", sfd.FileName), "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void ckb_rb_ctrl_CheckedChanged(object sender, EventArgs e)
        {
            if (rd.IsConnected)
            {
                rd.Enabled = !((CheckBox)sender).Checked;
                rd.ViewOnly = (((CheckBox)sender).Checked);
            }
        }

        private void rd_ConnectionLost(object sender, EventArgs e)
        {
            pnl_pc_sel.BackColor = Color.Gold;
            //MessageBox.Show("连接失败,请检查连接或密码！", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void rd_ConnectComplete(object sender, VncSharp.ConnectEventArgs e)
        {
            pnl_pc_sel.BackColor = Color.DodgerBlue;
        }

        public bool SaveCsv(ref string filename)
        {
            bool IsOk = false;
            String str;
              str = Path.GetFullPath("..") + "\\CsvData\\";
            //str = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\";
            if (!Directory.Exists(str))
            {
                //文件夹不存在则创建
                Directory.CreateDirectory(str);
            }
            filename = DateTime.Now.ToString("(yyyy-MM-dd)") + ".csv";
            str += filename;
            if (!File.Exists(str))
                File.Delete(str);
            IsOk = Utility.DataGridViewToCSV(str, dgv_count_data);
            return IsOk;
        }
        public bool SaveerrCsv(ref string filename)
        {
            bool IsOk = false;
            String str;
            //  str = Path.GetFullPath("..") + "\\CsvData\\";
            str = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\";
            if (!Directory.Exists(str))
            {
                //文件夹不存在则创建
                Directory.CreateDirectory(str);
            }
            filename = DateTime.Now.ToString("(yyyy-MM-dd)") + ".csv";
            str += filename;
            if (!File.Exists(str))
                File.Delete(str);
            IsOk = Utility.DataGridViewToCSV(str, data_err);
            return IsOk;
        }

        private void btn_SaveCsv_Click(object sender, EventArgs e)
        {
            bool IsOk = false;
            string filename = string.Empty;
            if (dgv_count_data.RowCount>50000)
            {
                lb_SaveCsv.Text = VAR.IsChinese?"提示:输出记录不能超50000条": "Tip:Output records cannot exceed 50000";
                return ;
            }
            try
            {
                btn_SaveCsv.Enabled = false;
                IsOk=SaveCsv(ref filename);
                if (IsOk)
                {
                    if(VAR.IsChinese)
                    lb_SaveCsv.Text="生成OK,路径:运行目录下CsvData\\"+ filename+"文件";
                    else
                    {
                        lb_SaveCsv.Text = "Generate OK, Path: under the running directory CsvData\\"+ filename + "file";
                    }
                }
                else
                {
                    if (VAR.IsChinese)
                        lb_SaveCsv.Text = "生成NG,提示:表格已打开或无数据,请选好<日期>后按<查询键>生成表格后再输出报表!";
                    else
                    {
                        lb_SaveCsv.Text = "Generate ng, tip: the table is open or has no data, please select the <date> and press the query key to generate the table and then output the report!";
                    }
                }
            }
            finally
            {
                btn_SaveCsv.Enabled = true;
            }
           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool IsOk = false;
            string filename = string.Empty;
            if (data_err.RowCount > 50000)
            {
                lb_errSaveCsv.Text = "提示:输出记录不能超50000条";
                return;
            }
            try
            {
                btn_SaveCsv.Enabled = false;
                IsOk = SaveerrCsv(ref filename);
                if (IsOk) lb_errSaveCsv.Text = "生成OK,路径:桌面" + filename + "文件";
                else lb_errSaveCsv.Text = "生成NG,提示:表格已打开或无数据,请选好<日期>后按<查询键>生成表格后再输出报表!";
            }
            finally
            {
                btn_SaveCsv.Enabled = true;
            }
        }
    }
}
