using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MotionCtrl;
using System.IO;
using UI;
using System.Threading;
using UI.Class;

namespace UI.Compment
{
    public partial class TestWorkStation : UserControl
    {
        public WSFEED ws_feed = null;
        public bool bon_zk = true;
        public bool bon_power = true;
        private bool getdata = true;
        public TestWorkStation()
        {
            InitializeComponent();
           

        }      
        public void UpdateShow()
        {
            LoadCfg();           
        }

        public void PmsEn(User.PERMISSION pms)
        {
            if (pms <= User.PERMISSION.Operator)
            {
                getdata = false;
                this.Enabled = false;
            }
            else
            {
                this.Enabled = true;
                if (pms >= User.PERMISSION.Engineer)
                {
                    //for (int i = 0; i < wsdgv.ColumnCount; i++)
                    //    wsdgv.Columns[i].ReadOnly = true;
                   // wsdgv.Columns["enable"].ReadOnly = false;//使用
                    getdata = false;
                }

                if (pms >= User.PERMISSION.Admin)
                {
                    //wsdgv.Columns["Y"].ReadOnly = false;
                    //wsdgv.Columns["U"].ReadOnly = false;
                    //wsdgv.Columns["Z1"].ReadOnly = false;
                    //wsdgv.Columns["X"].ReadOnly = false;
                    //wsdgv.Columns["Z2"].ReadOnly = false;
                    getdata = true;
                }

                if (pms >= User.PERMISSION.SuperAdmin)
                {
                    //wsdgv.Columns["pc"].ReadOnly = false;
                    //wsdgv.Columns["textbox"].ReadOnly = false;
                    //wsdgv.Columns["SN"].ReadOnly = false;
                }
            }
            if (pms <= User.PERMISSION.Operator)
            {
                this.Enabled = false;
            }
            else
            {
                this.Enabled = true;
                if (pms >= User.PERMISSION.Engineer)
                {

                    for (int i = 0; i < dgv.ColumnCount; i++)
                        dgv.Columns[i].ReadOnly = true;
                    dgv.Columns["XD"].ReadOnly = false;
                    dgv.Columns["YD"].ReadOnly = false;
                    dgv.Columns["UD"].ReadOnly = false;
                    dgv.Columns["Z1D"].ReadOnly = false;
                    dgv.Columns["Z2D"].ReadOnly = false;
                    dgv.Columns["AutoCheckEnable"].ReadOnly = false;
                    dgv.Columns["ActualDistanceParam"].ReadOnly = false;
                    dgv.Columns["DistanceThreshold"].ReadOnly = false;
                    dgv.Columns["DistanceComp"].ReadOnly = false;
                    dgv.Columns["ActualLuxParam"].ReadOnly = false;
                    dgv.Columns["LuxThreshold"].ReadOnly = false;
                    dgv.Columns["ActualCctParam"].ReadOnly = false;
                    dgv.Columns["CctThreshold"].ReadOnly = false;
                    light_btn_del.Enabled = false;
                    light_btn_add.Enabled = false;
                    light_btn_load.Enabled = true;
                    light_btn_save.Enabled = true;                    
                }

                if (pms >= User.PERMISSION.Admin)
                {
                    for (int i = 0; i < dgv.ColumnCount; i++)
                        dgv.Columns[i].ReadOnly = false;
                    light_btn_del.Enabled = true;
                    light_btn_add.Enabled = true;
                }
            }
        }
        

        public bool GetRowData(ref WSFEED ws)
        {
            double x, y, z;
             //ws_feed.issfr=chk_SFR.Checked ;
            try
            {
                //string str = wsdgv.Rows[0].Cells["Y"].Value == null ? "" : wsdgv.Rows[0].Cells["Y"].Value.ToString();
                //ws.feedpos.y = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[0].Cells["U"].Value == null ? "" : wsdgv.Rows[0].Cells["U"].Value.ToString();
                //ws.feedpos.a = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[0].Cells["Z1"].Value == null ? "" : wsdgv.Rows[0].Cells["Z1"].Value.ToString();
                //ws.feedpos.z = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[0].Cells["X"].Value == null ? "" : wsdgv.Rows[0].Cells["X"].Value.ToString();
                //ws.feedpos.x = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;

                //str = wsdgv.Rows[1].Cells["Y"].Value == null ? "" : wsdgv.Rows[1].Cells["Y"].Value.ToString();
                //ws.safeuturnpos.y = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[1].Cells["U"].Value == null ? "" : wsdgv.Rows[1].Cells["U"].Value.ToString();
                //ws.safeuturnpos.a = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[1].Cells["Z1"].Value == null ? "" : wsdgv.Rows[1].Cells["Z1"].Value.ToString();
                //ws.safeuturnpos.z = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[1].Cells["X"].Value == null ? "" : wsdgv.Rows[1].Cells["X"].Value.ToString();
                //ws.safeuturnpos.x = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;


                //str = wsdgv.Rows[2].Cells["Y"].Value == null ? "" : wsdgv.Rows[2].Cells["Y"].Value.ToString();
                //ws.testpos.y = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[2].Cells["U"].Value == null ? "" : wsdgv.Rows[2].Cells["U"].Value.ToString();
                //ws.testpos.a = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[2].Cells["Z1"].Value == null ? "" : wsdgv.Rows[2].Cells["Z1"].Value.ToString();
                //ws.testpos.z = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[2].Cells["X"].Value == null ? "" : wsdgv.Rows[2].Cells["X"].Value.ToString();
                //ws.testpos.x = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;


                //str = wsdgv.Rows[3].Cells["Y"].Value == null ? "" : wsdgv.Rows[3].Cells["Y"].Value.ToString();
                //ws.sfrtestpos.y = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[3].Cells["U"].Value == null ? "" : wsdgv.Rows[3].Cells["U"].Value.ToString();
                //ws.sfrtestpos.a = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[3].Cells["Z2"].Value == null ? "" : wsdgv.Rows[3].Cells["Z2"].Value.ToString();
                //ws.sfrtestpos.z = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[3].Cells["X"].Value == null ? "" : wsdgv.Rows[3].Cells["X"].Value.ToString();
                //ws.sfrtestpos.x = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;

                //str = wsdgv.Rows[4].Cells["Y"].Value == null ? "" : wsdgv.Rows[4].Cells["Y"].Value.ToString();
                //ws.sfrtestpos2.y = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[4].Cells["U"].Value == null ? "" : wsdgv.Rows[4].Cells["U"].Value.ToString();
                //ws.sfrtestpos2.a = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[4].Cells["Z2"].Value == null ? "" : wsdgv.Rows[4].Cells["Z2"].Value.ToString();
                //ws.sfrtestpos2.z = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                //str = wsdgv.Rows[4].Cells["X"].Value == null ? "" : wsdgv.Rows[4].Cells["X"].Value.ToString();
                //ws.sfrtestpos2.x = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        bool GetRowData(int RowID, ref WSFEED.PosDef PosDef)
        {
            if (RowID < 0 || dgv.Rows == null || RowID >= dgv.Rows.Count) return false;
            try
            {
                string str = dgv.Rows[RowID].Cells["Num"].Value == null
                    ? ""
                    : dgv.Rows[RowID].Cells["Num"].Value.ToString();
                PosDef.ID = str.Length > 0 ? Convert.ToInt16(str) : -1;

                str = dgv.Rows[RowID].Cells["Disc"].Value == null ? "" : dgv.Rows[RowID].Cells["Disc"].Value.ToString();
                PosDef.Name = str;

                str = dgv.Rows[RowID].Cells["XD"].Value == null ? "" : dgv.Rows[RowID].Cells["XD"].Value.ToString();
                PosDef.X = str.Length > 0 ? Convert.ToDouble(str) : double.MaxValue;

                str = dgv.Rows[RowID].Cells["YD"].Value == null ? "" : dgv.Rows[RowID].Cells["YD"].Value.ToString();
                PosDef.Y = str.Length > 0 ? Convert.ToDouble(str) : double.MaxValue;

                str = dgv.Rows[RowID].Cells["UD"].Value == null ? "" : dgv.Rows[RowID].Cells["UD"].Value.ToString();
                PosDef.U = str.Length > 0 ? Convert.ToDouble(str) : double.MaxValue;

                str = dgv.Rows[RowID].Cells["Z1D"].Value == null ? "" : dgv.Rows[RowID].Cells["Z1D"].Value.ToString();
                PosDef.Z1 = str.Length > 0 ? Convert.ToDouble(str) : double.MaxValue;

                str = dgv.Rows[RowID].Cells["Z2D"].Value == null ? "" : dgv.Rows[RowID].Cells["Z2D"].Value.ToString();
                PosDef.Z2 = str.Length > 0 ? Convert.ToDouble(str) : double.MaxValue;

                str = dgv.Rows[RowID].Cells["Channel"].Value == null
                    ? ""
                    : dgv.Rows[RowID].Cells["Channel"].Value.ToString();
                PosDef.Channel = str.Length > 0 ? Convert.ToInt32(str) : int.MaxValue;

                str = dgv.Rows[RowID].Cells["Delay"].Value == null
                    ? ""
                    : dgv.Rows[RowID].Cells["Delay"].Value.ToString();
                PosDef.Delay = str.Length > 0 ? Convert.ToInt32(str) : 0;

                object checkValue = dgv.Rows[RowID].Cells["AutoCheckEnable"].Value;
                PosDef.AutoCheckEnable = checkValue != null && Convert.ToBoolean(checkValue);

                str = dgv.Rows[RowID].Cells["ActualDistanceParam"].Value == null ? "" : dgv.Rows[RowID].Cells["ActualDistanceParam"].Value.ToString();
                PosDef.ActualDistanceParam = str.Length > 0 ? Convert.ToDouble(str) : 0;

                str = dgv.Rows[RowID].Cells["DistanceThreshold"].Value == null ? "" : dgv.Rows[RowID].Cells["DistanceThreshold"].Value.ToString();
                PosDef.DistanceThreshold = str.Length > 0 ? Convert.ToDouble(str) : 0;

                str = dgv.Rows[RowID].Cells["DistanceComp"].Value == null ? "" : dgv.Rows[RowID].Cells["DistanceComp"].Value.ToString();
                PosDef.DistanceComp = str.Length > 0 ? Convert.ToDouble(str) : 0;

                str = dgv.Rows[RowID].Cells["ActualLuxParam"].Value == null ? "" : dgv.Rows[RowID].Cells["ActualLuxParam"].Value.ToString();
                PosDef.ActualLuxParam = str.Length > 0 ? Convert.ToDouble(str) : 0;

                str = dgv.Rows[RowID].Cells["LuxThreshold"].Value == null ? "" : dgv.Rows[RowID].Cells["LuxThreshold"].Value.ToString();
                PosDef.LuxThreshold = str.Length > 0 ? Convert.ToDouble(str) : 0;

                str = dgv.Rows[RowID].Cells["ActualCctParam"].Value == null ? "" : dgv.Rows[RowID].Cells["ActualCctParam"].Value.ToString();
                PosDef.ActualCctParam = str.Length > 0 ? Convert.ToDouble(str) : 0;

                str = dgv.Rows[RowID].Cells["CctThreshold"].Value == null ? "" : dgv.Rows[RowID].Cells["CctThreshold"].Value.ToString();
                PosDef.CctThreshold = str.Length > 0 ? Convert.ToDouble(str) : 0;

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        public bool SetRowData(WSFEED ws)
        {

            //chk_SFR.Checked = ws_feed.issfr;
            //chk_w.Checked = !ws_feed.issfr;
            try
            {
                //wsdgv.Rows.Clear();
                //wsdgv.Rows.Add(3);

                //wsdgv.Rows[0].Cells["位置"].Value = "上料位";
                //wsdgv.Rows[1].Cells["位置"].Value = "翻转位";
                //wsdgv.Rows[2].Cells["位置"].Value = "测试位";
                ////wsdgv.Rows[3].Cells["位置"].Value = "SFR测试位1";
                ////wsdgv.Rows[4].Cells["位置"].Value = "SFR测试位2";

                //wsdgv.Rows[0].Cells["Y"].Value = ws.feedpos.y;
                //wsdgv.Rows[0].Cells["U"].Value = ws.feedpos.a;
                //wsdgv.Rows[0].Cells["Z1"].Value = ws.feedpos.z;
                //wsdgv.Rows[0].Cells["Z2"].Value = ws.feedpos.z;
                //wsdgv.Rows[0].Cells["X"].Value = ws.feedpos.x;

                //wsdgv.Rows[1].Cells["Y"].Value = ws.safeuturnpos.y;
                //wsdgv.Rows[1].Cells["U"].Value = ws.safeuturnpos.a;
                //wsdgv.Rows[1].Cells["Z1"].Value = ws.safeuturnpos.z;
                //wsdgv.Rows[1].Cells["Z2"].Value = ws.safeuturnpos.z;
                //wsdgv.Rows[1].Cells["X"].Value = ws.safeuturnpos.x;

                //wsdgv.Rows[2].Cells["Y"].Value = ws.testpos.y;
                //wsdgv.Rows[2].Cells["U"].Value = ws.testpos.a;
                //wsdgv.Rows[2].Cells["Z1"].Value = ws.testpos.z;
                //wsdgv.Rows[2].Cells["Z2"].Value = 0;
                //wsdgv.Rows[2].Cells["X"].Value = ws.testpos.x;

                //wsdgv.Rows[3].Cells["Y"].Value = ws.sfrtestpos.y;
                //wsdgv.Rows[3].Cells["U"].Value = ws.sfrtestpos.a;
                //wsdgv.Rows[3].Cells["Z2"].Value = ws.sfrtestpos.z;
                //wsdgv.Rows[3].Cells["Z1"].Value = 0;
                //wsdgv.Rows[3].Cells["X"].Value = ws.sfrtestpos.x;

                //wsdgv.Rows[4].Cells["Y"].Value = ws.sfrtestpos2.y;
                //wsdgv.Rows[4].Cells["U"].Value = ws.sfrtestpos2.a;
                //wsdgv.Rows[4].Cells["Z2"].Value = ws.sfrtestpos2.z;
                //wsdgv.Rows[4].Cells["Z1"].Value = 0;
                //wsdgv.Rows[4].Cells["X"].Value = ws.sfrtestpos2.x;

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }

        }

        bool SetRowData(int RowID, WSFEED.PosDef PosDef)
        {
            if (RowID < 0 || dgv.Rows == null || RowID >= dgv.Rows.Count) return false;
            try
            {
                dgv.Rows[RowID].Cells["Num"].Value = PosDef.ID;
                dgv.Rows[RowID].Cells["Disc"].Value = PosDef.Name;
                dgv.Rows[RowID].Cells["XD"].Value = Math.Abs(PosDef.X) < 10000 ? PosDef.X.ToString("F3") : "";
                dgv.Rows[RowID].Cells["YD"].Value = Math.Abs(PosDef.Y) < 10000 ? PosDef.Y.ToString("F3") : "";
                dgv.Rows[RowID].Cells["UD"].Value = Math.Abs(PosDef.U) < 10000 ? PosDef.U.ToString("F3") : "";
                dgv.Rows[RowID].Cells["Z1D"].Value = Math.Abs(PosDef.Z1) < 10000 ? PosDef.Z1.ToString("F3") : "";
                dgv.Rows[RowID].Cells["Z2D"].Value = Math.Abs(PosDef.Z2) < 10000 ? PosDef.Z2.ToString("F3") : "";
                dgv.Rows[RowID].Cells["Channel"].Value =
                    Math.Abs(PosDef.Channel) < 100000 ? PosDef.Channel.ToString() : "";
                dgv.Rows[RowID].Cells["Delay"].Value = Math.Abs(PosDef.Delay) < 100000 ? PosDef.Delay.ToString() : "0";
                dgv.Rows[RowID].Cells["AutoCheckEnable"].Value = PosDef.AutoCheckEnable;
                dgv.Rows[RowID].Cells["ActualDistanceParam"].Value = Math.Abs(PosDef.ActualDistanceParam) < 10000 ? PosDef.ActualDistanceParam.ToString("F3") : "";
                dgv.Rows[RowID].Cells["DistanceThreshold"].Value = Math.Abs(PosDef.DistanceThreshold) < 10000 ? PosDef.DistanceThreshold.ToString("F3") : "";
                dgv.Rows[RowID].Cells["DistanceComp"].Value = Math.Abs(PosDef.DistanceComp) < 10000 ? PosDef.DistanceComp.ToString("F3") : "";
                dgv.Rows[RowID].Cells["ActualLuxParam"].Value = Math.Abs(PosDef.ActualLuxParam) < 100000 ? PosDef.ActualLuxParam.ToString("F3") : "";
                dgv.Rows[RowID].Cells["LuxThreshold"].Value = Math.Abs(PosDef.LuxThreshold) < 100000 ? PosDef.LuxThreshold.ToString("F3") : "";
                dgv.Rows[RowID].Cells["ActualCctParam"].Value = Math.Abs(PosDef.ActualCctParam) < 100000 ? PosDef.ActualCctParam.ToString("F3") : "";
                dgv.Rows[RowID].Cells["CctThreshold"].Value = Math.Abs(PosDef.CctThreshold) < 100000 ? PosDef.CctThreshold.ToString("F3") : "";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        private void FillTableWithPosDef(WSFEED.PosDef PosDef, int row = -2)
        {
            if (ws_feed == null || PosDef == null || PosDef.Name.Length < 1) return;
            //if empty or add mode then add
            //if (dgv.Rows.Count == 0|| row == -2) row = dgv.Rows.Add();
            if (dgv.Rows.Count == 0 || row < 0 || row >= dgv.Rows.Count) row = dgv.Rows.Add();
            //the last row
            else if (row < 0) row = dgv.Rows.Count - 1;

            SetRowData(row, PosDef);
        }

        public void UpdateShow(bool bdgvupdate = true)
        {

            if (ws_feed == null || ws_feed.ListPos.Count == 0)
            {
                dgv.Rows.Clear();
                return;
            }

            if (bdgvupdate)
            {
                if (dgv.Rows.Count != ws_feed.ListPos.Count)
                    dgv.Rows.Clear();

                for (int r = 0; r < ws_feed.ListPos.Count; r++)
                {
                    FillTableWithPosDef(ws_feed.ListPos.ElementAt(r), r);
                }
            }

            ////补偿
            //if (dgv.CurrentRow != null && dgv.CurrentRow.Cells[0] != null && dgv.CurrentRow.Cells[0].Value != null)
            //{
            //    int num = (int)dgv.CurrentRow.Cells["Num"].Value;
            //    ws_feed.PosDef PosDef = ws_feed.ListPos.Find(delegate (ws_feed.PosDef x) { return x.ID == num; });
            //    if (PosDef != null)
            //    {
            //        dgvcmp.Rows.Clear();
            //        for (int n = 0; n < PosDef.Cmp.Length; n++)
            //        {
            //            int RowID = dgvcmp.Rows.Add();
            //            dgvcmp.Columns[0].HeaderText = VAR.IsChinese ? string.Format("位置[{0}]的补偿", PosDef.ID) : string.Format("Pos[{0}]Offset", PosDef.ID);
            //            dgvcmp.Rows[RowID].Cells["cmp_ws"].Value = VAR.IsChinese ? string.Format("工站{0}", n) : string.Format("POS{0}", n);
            //            dgvcmp.Rows[RowID].Cells["cmp_x1"].Value = Math.Abs(PosDef.Cmp[n].X1) < 10000
            //                ? PosDef.Cmp[n].X1.ToString("F3")
            //                : "";
            //            dgvcmp.Rows[RowID].Cells["cmp_x2"].Value = Math.Abs(PosDef.Cmp[n].X2) < 10000
            //                ? PosDef.Cmp[n].X2.ToString("F3")
            //                : "";
            //            dgvcmp.Rows[RowID].Cells["cmp_y1"].Value = Math.Abs(PosDef.Cmp[n].Y1) < 10000
            //                ? PosDef.Cmp[n].Y1.ToString("F3")
            //                : "";
            //            dgvcmp.Rows[RowID].Cells["cmp_z1"].Value = Math.Abs(PosDef.Cmp[n].Z1) < 10000
            //                ? PosDef.Cmp[n].Z1.ToString("F3")
            //                : "";
            //            dgvcmp.Rows[RowID].Cells["cmp_z2"].Value = Math.Abs(PosDef.Cmp[n].Z2) < 10000
            //                ? PosDef.Cmp[n].Z2.ToString("F3")
            //                : "";
            //        }
            //    }
            //}
            dgv.Update();
        }

        public EM_RES LoadCfg()
        {
            if (ws_feed == null) return EM_RES.ERR;
            EM_RES res = EM_RES.OK;
            res = ws_feed.LoadCfgws(VAR.gsys_set.cur_product_name);
            if (res != EM_RES.OK)
            {
                MessageBox.Show("加载失败!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return res;
            }
            SetRowData(ws_feed);          
            return EM_RES.OK;
        }
 
        private void Test_WorkStation_Load(object sender, EventArgs e)
        {
            LoadCfg();
            this.dgv.ShowCellToolTips = true;
        }      

        private void button1_Click(object sender, EventArgs e)
        {
            if (ws_feed == null) return;
            EM_RES res = EM_RES.OK;
            GetRowData(ref ws_feed);
           res = ws_feed.SaveCfgws(VAR.gsys_set.cur_product_name);
            if (res != EM_RES.OK)
            {
                MessageBox.Show("保存失败!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                MessageBox.Show("保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.None);
            }
        }

        //private void wsdgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (ws_feed == null || e.RowIndex < 0 || e.RowIndex > 5) return;
        //    int id = 0;
        //    if (e.ColumnIndex == 7)
        //    {
        //        try
        //        {
        //            EM_RES res=EM_RES.OK;
        //            if (!GetRowData(ref ws_feed)) return;
        //            DialogResult result = MessageBox.Show(string.Format("确定要定位【{0}】?\n ", wsdgv.Rows[e.RowIndex].Cells["位置"].Value == null ? "" : wsdgv.Rows[e.RowIndex].Cells["位置"].Value), "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
        //                if (result == DialogResult.Yes);
        //            else return;
        //            if (e.RowIndex == 0)
        //            {
        //                res = ws_feed.SetupForFeed(ref VAR.gsys_set.bquit);
        //            }
        //            else if (e.RowIndex == 1)
        //            {
        //                res = ws_feed.ax_y.MoveTo(ref VAR.gsys_set.bquit, Convert.ToDouble(wsdgv.Rows[e.RowIndex].Cells["Y"].Value));
        //            }
        //            else if (e.RowIndex == 2)
        //            {
        //                res = ws_feed.MoveTocommonTestpos(ref VAR.gsys_set.bquit);
        //            }
        //            //else if (e.RowIndex == 3)
        //            //{
        //            //    res = ws_feed.MoveToSFRTestpos(ref VAR.gsys_set.bquit);
        //            //}
        //            //else if (e.RowIndex == 4)
        //            //{
        //            //    res = ws_feed.MoveToSFRTestpos2(ref VAR.gsys_set.bquit);
        //            //}
        //            if (res == EM_RES.OK)
        //                MessageBox.Show("定位成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //            else
        //                MessageBox.Show("定位失败!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show(ex.Message);
        //        }
        //    }
        //    else if(e.ColumnIndex == 6)
        //    {
        //        try
        //        {
        //            EM_RES res = EM_RES.OK;
        //            //wsdgv.Rows[e.RowIndex].Cells["Y"].Value = ws_feed.ax_y.enc_pos;
        //            //wsdgv.Rows[e.RowIndex].Cells["U"].Value = ws_feed.ax_u.enc_pos;
        //            //wsdgv.Rows[e.RowIndex].Cells["Z1"].Value = ws_feed.ax_z_fr.cmd_pos;
        //            //wsdgv.Rows[e.RowIndex].Cells["Z2"].Value = ws_feed.ax_z_bk.cmd_pos;
        //            //wsdgv.Rows[e.RowIndex].Cells["X"].Value =  ws_feed.ax_x.cmd_pos;
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show(ex.Message);
        //        }

        //    }

        //}
        public void Add(WSFEED.PosDef PosDef = null, int idx = -1)
        {
            if (ws_feed == null) return;
            if (ws_feed.ListPos == null) ws_feed.ListPos = new List<WSFEED.PosDef>();
            if (PosDef == null)
            {
                PosDef = new WSFEED.PosDef();
                WSFEED.PosDef maxid = ws_feed.ListPos.Count > 0 ? ws_feed.ListPos.OrderByDescending(t => t.ID).First(): null;
                PosDef.ID = maxid != null ? maxid.ID + 1 : 1;
                PosDef.Name = "名称" + dgv.Rows.Count;
                PosDef.X = ws_feed.ax_x != null ? ws_feed.ax_x.cmd_pos : double.MaxValue;
                PosDef.Y = ws_feed.ax_y != null ? ws_feed.ax_y.fenc_pos : double.MaxValue;
                PosDef.U = ws_feed.ax_u != null ? ws_feed.ax_u.cmd_pos : double.MaxValue;
                PosDef.Z1 = ws_feed.ax_z_fr != null ? ws_feed.ax_z_bk.fenc_pos : double.MaxValue;
                PosDef.Z2 = ws_feed.ax_z_bk != null ? ws_feed.ax_z_bk.fenc_pos : double.MaxValue;
                PosDef.Channel = int.MaxValue;
                PosDef.Delay = 0;
            }

            if (null == ws_feed.ListPos.Find(s => s.ID == PosDef.ID))
            {
                if (idx == -1 && dgv.CurrentRow != null) idx = dgv.CurrentRow.Index;
                if (idx >= 0 && (idx + 1) < ws_feed.ListPos.Count)
                    ws_feed.ListPos.Insert(idx + 1, PosDef);
                else ws_feed.ListPos.Add(PosDef);
                UpdateShow(true);

                //select add row
                if (idx > 0 && dgv.Rows != null && (idx + 1) < dgv.Rows.Count)
                    dgv.Rows[idx + 1].Selected = true;
            }
        }

        public void Del(int idx = -1)
        {
            if (ws_feed == null) return;
            if (ws_feed.ListPos == null) ws_feed.ListPos = new List<WSFEED.PosDef>();
            if (idx == -1 && dgv.CurrentRow != null) idx = dgv.CurrentRow.Index;

            if (idx >= 0 && idx < ws_feed.ListPos.Count)
            {
                ws_feed.ListPos.RemoveAt(idx);
                UpdateShow(true);
                if (idx > 0 && dgv.Rows != null && idx < dgv.Rows.Count)
                    dgv.Rows[idx].Selected = true;
            }
        }
        private void light_btn_add_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes !=
              MessageBox.Show(VAR.IsChinese ? "确定是否新增?" : "Are you sure you want to add it?\r\n确定是否新增?", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) return;
            Add();
            EM_RES res = ws_feed.SaveCfgws(VAR.gsys_set.cur_product_name);
            if (res != EM_RES.OK)
            {
                MessageBox.Show(VAR.IsChinese ? "保存失败!" : "Save failed!\r\n保存失败!", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            res = ws_feed.LoadCfgws(VAR.gsys_set.cur_product_name);
            if (res != EM_RES.OK)
            {
                MessageBox.Show(VAR.IsChinese ? "保存后加载失败!" : "Load failed after saving!", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void wsdgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            UpdateShow(false);
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (ws_feed == null || e.RowIndex < 0 || e.RowIndex > ws_feed.ListPos.Count) return;
            //定位
            if (e.ColumnIndex == dgv.Columns["btn_close"].Index)
            {
                try
                {
                    WSFEED.PosDef PosDef = new WSFEED.PosDef();
                    if (!GetRowData(e.RowIndex, ref PosDef)) return;
                    if (ws_feed == null)
                    {
                        MessageBox.Show(VAR.IsChinese ? "测试工站未知，禁止定位！" : "The position of the turntable is unknown, positioning of the light box is prohibited!\r\n转台位置未知，禁止光箱定位！", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                        return;
                    }
                    DialogResult result = MessageBox.Show(string.Format("确定要定位【{0}】?\n ", dgv.Rows[e.RowIndex].Cells["Disc"].Value == null ? "" : dgv.Rows[e.RowIndex].Cells["Disc"].Value), "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                    if (result == DialogResult.Yes) ;
                    else return;
                    ws_feed.gpio_openfr.SetOn();//真空打开
                    EM_RES res = ws_feed.Cyldown(ref VAR.gsys_set.bquit);
                    if (res != EM_RES.OK)
                    {
                        return ;
                    }
                    ws_feed.gpio_openfr.SetOff();//真空打开
                    if (!ws_feed.isUInTestPos)
                    {
                        res = ws_feed.MoveToUTestPos(ref VAR.gsys_set.bquit, true);
                        if (res!= EM_RES.OK)
                        {
                            return;
                        }
                    }
                    res = ws_feed.MoveTo(ref VAR.gsys_set.bquit, PosDef);
                    if (res == EM_RES.OK)
                        MessageBox.Show(VAR.IsChinese ? "定位成功!" : "Positioning success!\r\n定位成功!", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        MessageBox.Show(VAR.IsChinese ? "定位失败!" : "Positioning failed!\r\n定位失败!", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            //学习位置
            else if (e.ColumnIndex == dgv.Columns["btn_open"].Index)
            {
                try
                {
                    WSFEED.PosDef PosDef = new WSFEED.PosDef();
                    if (!GetRowData(e.RowIndex, ref PosDef)) return;
                    PosDef.X = ws_feed.ax_x != null ? ws_feed.ax_x.fcmd_pos : double.MaxValue;
                    PosDef.Y = ws_feed.ax_y != null ? ws_feed.ax_y.fenc_pos : double.MaxValue;
                    PosDef.U = ws_feed.ax_u != null ? ws_feed.ax_u.fcmd_pos : double.MaxValue;
                    PosDef.Z1 = ws_feed.ax_z_fr != null ? ws_feed.ax_z_fr.fenc_pos : double.MaxValue;
                    PosDef.Z2 = ws_feed.ax_z_bk != null ? ws_feed.ax_z_bk.fenc_pos : double.MaxValue;
                    SetRowData(e.RowIndex, PosDef);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void dgv_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != 0 || e.RowIndex < 0 || dgv.Rows.Count <= 0) return;
            string str = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == null
                ? ""
                : dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
            int val = str.Length > 0 ? Convert.ToInt32(str) : -10000;
            if (val >= 0)
                dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = "0x" + Convert.ToString(val, 16);
        }

        private void dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if ((e.RowIndex & 1) == 1) e.CellStyle.BackColor = Color.WhiteSmoke;
        }

        private void light_btn_load_Click(object sender, EventArgs e)
        {
            if (ws_feed == null) return;
            EM_RES res = ws_feed.LoadCfgws(VAR.gsys_set.cur_product_name);
            if (res != EM_RES.OK)
            {
                MessageBox.Show(VAR.IsChinese ? "加载失败!" : "Failed to load!", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            UpdateShow(true);
            MessageBox.Show(VAR.IsChinese ? "加载成功!" : "Loaded successfully!", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void light_btn_save_Click(object sender, EventArgs e)
        {
            if (ws_feed == null) return;
            GetRowData(ref ws_feed);
            List<WSFEED.PosDef> listtemp = new List<WSFEED.PosDef>();
            listtemp.Clear();
            foreach (DataGridViewRow row in dgv.Rows)
            {
                WSFEED.PosDef PosDef = new WSFEED.PosDef();
                if (GetRowData(row.Index, ref PosDef))
                {
                    listtemp.Add(PosDef);
                }
            }

            ws_feed.ListPos = listtemp;
            EM_RES res = ws_feed.SaveCfgws(VAR.gsys_set.cur_product_name);
            if (res != EM_RES.OK)
            {
                MessageBox.Show(VAR.IsChinese ? "保存失败!" : "Save failed!\r\n保存失败!", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            res = ws_feed.LoadCfgws(VAR.gsys_set.cur_product_name);
            if (res != EM_RES.OK)
            {
                MessageBox.Show(VAR.IsChinese ? "保存后加载失败!" : "Load failed after saving!\r\n保存后加载失败!", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UpdateShow(true);
            MessageBox.Show(VAR.IsChinese ? "保存成功!" : "Saved successfully!\r\n保存成功!", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void light_btn_del_Click(object sender, EventArgs e)
        {
            Del();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int sta = 0;
            ws_feed.StartTestFlow(0, WSFEED.Demo);
            ws_feed.WaitTestResult(ref sta, PT_SET.TestTime * 100000, WSFEED.Demo);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            int sta = 0;
            ws_feed.WaitTestResult(ref sta, PT_SET.TestTime * 100000, WSFEED.Demo);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int status = 0;
            VAR.gsys_set.bquit = false;
            ws_feed.WaitTestResult(ref status, PT_SET.TestTime * 100000);
            ws_feed.NextTest(status);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            EM_RES res = EM_RES.OK;
            VAR.gsys_set.bquit = false;
            UpDownLoad.bquit = false;
            UpDownLoad.wsfeed1openchart = true;
            UpDownLoad.wsfeed2openchart = true;
            ws_feed.TestStatus = WSFEED.EM_TEST_STA.UNTEST;
            res = ws_feed.SetupForTest(ref VAR.gsys_set.bquit);
            if (res != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}去往测试位置失败！", ws_feed.disc));
                return;
            }
            UpDownLoad.wsfeed1openchart = false;
            UpDownLoad.wsfeed2openchart = false;
            ws_feed.RunTestTask();
            ws_feed.WaitTestTask(ref VAR.gsys_set.bquit);
            res = ws_feed.SetupForFeed(ref VAR.gsys_set.bquit,true);
            if (res != EM_RES.OK) return;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!PT_SET.bG4C)
            {
                bool res;
                if  (ws_feed.id == 1)//轩十佳光源OTP
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "进入轩仕佳");
                    //MT.COM3.XSJSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    res = MT.COM3.SetChannelXSJ(1);

                }
                else
                {
                  
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "进入轩仕佳");
                    //MT.COM4.XSJSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    res = MT.COM4.SetChannelXSJ(1);

                }
                if (res)
                {
                    MessageBox.Show("切换成功");
                }
                 else
                {
                    MessageBox.Show("切换失败");
                }
            }
            else
            {
                if (ws_feed.id == 1)
                {
                    MT.COM10.G4CSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    MT.COM10.SetChannelG4C(0);
                }
                else
                {
                    MT.COM11.G4CSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    MT.COM11.SetChannelG4C(0);
                }
            }    
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (!PT_SET.bG4C)
            {
                bool res;
                if (ws_feed.id == 1)//轩十佳光源OTP
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "进入轩仕佳");
                    //MT.COM3.XSJSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    res = MT.COM3.SetChannelXSJ(2);

                }
                else
                {

                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "进入轩仕佳");
                    //MT.COM4.XSJSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    res = MT.COM4.SetChannelXSJ(2);

                }
                if (res)
                {
                    MessageBox.Show("切换成功");
                }
                else
                {
                    MessageBox.Show("切换失败");
                }
            }
            else
            {
                if (ws_feed.id == 1)
                {
                    MT.COM10.G4CSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    MT.COM10.SetChannelG4C(1);
                }
                else
                {
                    MT.COM11.G4CSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    MT.COM11.SetChannelG4C(1);
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (!PT_SET.bG4C)
            {
                bool res;
                if (ws_feed.id == 1)//轩十佳光源OTP
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "进入轩仕佳");
                    //MT.COM3.XSJSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    res = MT.COM3.SetChannelXSJ(3);

                }
                else
                {

                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "进入轩仕佳");
                    //MT.COM4.XSJSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    res = MT.COM4.SetChannelXSJ(3);

                }
                if (res)
                {
                    MessageBox.Show("切换成功");
                }
                else
                {
                    MessageBox.Show("切换失败");
                }
            }
            else
            {
                if (ws_feed.id == 1)
                {
                    MT.COM10.G4CSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    MT.COM10.SetChannelG4C(2);
                }
                else
                {
                    MT.COM11.G4CSatrt();
                    Thread.Sleep(200);
                    Application.DoEvents();
                    MT.COM11.SetChannelG4C(2);
                }
            }

           
        }
    }
}
