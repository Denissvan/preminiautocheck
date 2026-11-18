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

namespace UI.Compment
{
    public partial class LightBoxDef : UserControl
    {
        public LightBox lightbox = null;
        public LightBoxDef()
        {
            InitializeComponent();
        }

        public void PmsEn(User.PERMISSION pms)
        {
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
                    dgv.Columns["X1"].ReadOnly = false;
                    dgv.Columns["X2"].ReadOnly = false;
                    dgv.Columns["Y1"].ReadOnly = false;
                    dgv.Columns["Z1"].ReadOnly = false;
                    dgv.Columns["Z2"].ReadOnly = false;
                    btn_del.Enabled = false;
                    btn_add.Enabled = false;
                    btn_load.Enabled = true;
                    btn_save.Enabled = true;
                    btn_stop.Enabled = true;
                }

                if (pms >= User.PERMISSION.Admin)
                {
                    for (int i = 0; i < dgv.ColumnCount; i++)
                        dgv.Columns[i].ReadOnly = false;
                    btn_del.Enabled = true;
                    btn_add.Enabled = true;
                }
            }
        }

        public void Add(LightBox.PosDef PosDef = null,int idx = -1)
        {
            if (lightbox == null ) return;
            if (lightbox.ListPos == null)lightbox.ListPos = new  List<LightBox.PosDef>();
            if(PosDef == null)
            {
                PosDef = new LightBox.PosDef();
                LightBox.PosDef maxid = lightbox.ListPos.Count>0?lightbox.ListPos.OrderByDescending(t => t.ID).First():null;
                PosDef.ID = maxid != null ? maxid.ID + 1 : 1;
                PosDef.Name = "名称" + dgv.Rows.Count;
                PosDef.X1 = lightbox.ax_x1 != null ? lightbox.ax_x1.fenc_pos : double.MaxValue;
                PosDef.X2 = lightbox.ax_x2 != null ? lightbox.ax_x2.fenc_pos : double.MaxValue;
                PosDef.Y1 = lightbox.ax_y1 != null ? lightbox.ax_y1.fenc_pos : double.MaxValue;
                PosDef.Z1 = lightbox.ax_z1 != null ? lightbox.ax_z1.fenc_pos : double.MaxValue;
                PosDef.Z2 = lightbox.ax_z2 != null ? lightbox.ax_z2.fenc_pos : double.MaxValue;
                PosDef.Channel = int.MaxValue;
                PosDef.Delay = 0;
            }
            if (null == lightbox.ListPos.Find(s => s.ID == PosDef.ID))
            {
                if (idx == -1 && dgv.CurrentRow != null) idx = dgv.CurrentRow.Index;
                if (idx >= 0 && (idx + 1) < lightbox.ListPos.Count)
                    lightbox.ListPos.Insert(idx + 1, PosDef);
                else lightbox.ListPos.Add(PosDef);
                UpdateShow();

                //select add row
                if (idx > 0 && dgv.Rows != null && (idx + 1) < dgv.Rows.Count)
                    dgv.Rows[idx + 1].Selected = true;
            }           
        }
        public void Del(int idx = -1)
        {
            if (lightbox == null) return;
            if (lightbox.ListPos == null) lightbox.ListPos = new List<LightBox.PosDef>();
            if (idx == -1 && dgv.CurrentRow != null) idx = dgv.CurrentRow.Index;

            if (idx >= 0 && idx < lightbox.ListPos.Count)
            {
                lightbox.ListPos.RemoveAt(idx);
                UpdateShow();
                if (idx > 0 && dgv.Rows != null && idx < dgv.Rows.Count)
                    dgv.Rows[idx].Selected = true;
            }            
        }

        bool GetRowData(int RowID, ref LightBox.PosDef PosDef)
        {
            if (RowID < 0 || dgv.Rows == null || RowID >= dgv.Rows.Count) return false;
            try
            {
                string str = dgv.Rows[RowID].Cells["Num"].Value==null?"": dgv.Rows[RowID].Cells["Num"].Value.ToString();
                PosDef.ID = str.Length>0? Convert.ToInt16(str) : -1;

                str = dgv.Rows[RowID].Cells["Disc"].Value == null ? "" : dgv.Rows[RowID].Cells["Disc"].Value.ToString();
                PosDef.Name = str;

                str = dgv.Rows[RowID].Cells["X1"].Value == null ? "" : dgv.Rows[RowID].Cells["X1"].Value.ToString();
                PosDef.X1 = str.Length > 0 ? Convert.ToDouble(str) : double.MaxValue;

                str = dgv.Rows[RowID].Cells["X2"].Value == null ? "" : dgv.Rows[RowID].Cells["X2"].Value.ToString();
                PosDef.X2 = str.Length > 0 ? Convert.ToDouble(str) : double.MaxValue;

                str = dgv.Rows[RowID].Cells["Y1"].Value == null ? "" : dgv.Rows[RowID].Cells["Y1"].Value.ToString();
                PosDef.Y1 = str.Length > 0 ? Convert.ToDouble(str) : double.MaxValue;

                str = dgv.Rows[RowID].Cells["Z1"].Value == null ? "" : dgv.Rows[RowID].Cells["Z1"].Value.ToString();
                PosDef.Z1 = str.Length > 0 ? Convert.ToDouble(str) : double.MaxValue;

                str = dgv.Rows[RowID].Cells["Z2"].Value == null ? "" : dgv.Rows[RowID].Cells["Z2"].Value.ToString();
                PosDef.Z2 = str.Length > 0 ? Convert.ToDouble(str) : double.MaxValue;

                str = dgv.Rows[RowID].Cells["Channel"].Value == null ? "" : dgv.Rows[RowID].Cells["Channel"].Value.ToString();
                PosDef.Channel = str.Length > 0 ? Convert.ToInt32(str) : int.MaxValue;

                str = dgv.Rows[RowID].Cells["Delay"].Value == null ? "" : dgv.Rows[RowID].Cells["Delay"].Value.ToString();
                PosDef.Delay = str.Length > 0 ? Convert.ToInt32(str) : 0;

                //补偿数据对应选择行
                if (dgv.CurrentRow != null && RowID == dgv.CurrentRow.Index)
                {
                    for (int n = 0; n < PosDef.Cmp.Length; n++)
                    {
                        PosDef.Cmp[n] = new LightBox.PosCmpDef();
                        str = dgvcmp.Rows[n].Cells["cmp_x1"].Value == null ? "" : dgvcmp.Rows[n].Cells["cmp_x1"].Value.ToString();
                        PosDef.Cmp[n].X1 = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;

                        str = dgvcmp.Rows[n].Cells["cmp_x2"].Value == null ? "" : dgvcmp.Rows[n].Cells["cmp_x2"].Value.ToString();
                        PosDef.Cmp[n].X2 = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;

                        str = dgvcmp.Rows[n].Cells["cmp_z1"].Value == null ? "" : dgvcmp.Rows[n].Cells["cmp_z1"].Value.ToString();
                        PosDef.Cmp[n].Z1 = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;

                        str = dgvcmp.Rows[n].Cells["cmp_z2"].Value == null ? "" : dgvcmp.Rows[n].Cells["cmp_z2"].Value.ToString();
                        PosDef.Cmp[n].Z2 = str.Length > 0 ? Convert.ToDouble(str) : int.MaxValue;
                    }
                }
                else
                {
                    //保存之前的补偿值
                    int id = PosDef.ID;
                    LightBox.PosDef postemp= lightbox.ListPos.Find(delegate(LightBox.PosDef x) { return x.ID == id; });
                    if(postemp!=null)
                    {
                        PosDef.Cmp[0] = postemp.Cmp[0];
                        PosDef.Cmp[1] = postemp.Cmp[1];
                        PosDef.Cmp[2] = postemp.Cmp[2];
                        PosDef.Cmp[3] = postemp.Cmp[3];
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        bool SetRowData(int RowID, LightBox.PosDef PosDef)
        {
            if (RowID < 0 || dgv.Rows == null || RowID >= dgv.Rows.Count) return false;
            try
            {
                dgv.Rows[RowID].Cells["Num"].Value = PosDef.ID;
                dgv.Rows[RowID].Cells["Disc"].Value  = PosDef.Name;
                dgv.Rows[RowID].Cells["X1"].Value = Math.Abs(PosDef.X1) < 10000 ? PosDef.X1.ToString("F3") : "";
                dgv.Rows[RowID].Cells["X2"].Value = Math.Abs(PosDef.X2) < 10000 ? PosDef.X2.ToString("F3") : "";
                dgv.Rows[RowID].Cells["Y1"].Value = Math.Abs(PosDef.Y1) < 10000 ? PosDef.Y1.ToString("F3") : "";
                dgv.Rows[RowID].Cells["Z1"].Value = Math.Abs(PosDef.Z1) < 10000 ? PosDef.Z1.ToString("F3") : "";
                dgv.Rows[RowID].Cells["Z2"].Value = Math.Abs(PosDef.Z2) < 10000 ? PosDef.Z2.ToString("F3") : "";
                dgv.Rows[RowID].Cells["Channel"].Value = Math.Abs(PosDef.Channel) < 100000 ? PosDef.Channel.ToString() : "";
                dgv.Rows[RowID].Cells["Delay"].Value = Math.Abs(PosDef.Delay) < 100000 ? PosDef.Delay.ToString() : "0";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        private void FillTableWithPosDef(LightBox.PosDef PosDef, int row = -2)
        {
            if (lightbox == null || PosDef == null || PosDef.Name.Length < 1) return;
            //if empty or add mode then add
            //if (dgv.Rows.Count == 0|| row == -2) row = dgv.Rows.Add();
            if (dgv.Rows.Count == 0 || row < 0 || row >= dgv.Rows.Count) row = dgv.Rows.Add();
            //the last row
            else if (row < 0) row = dgv.Rows.Count - 1;

            SetRowData(row, PosDef);
        }
        public void UpdateShow()
        {
            if (lightbox == null || lightbox.ListPos.Count == 0)
            {
                dgv.Rows.Clear();
                return;
            }

            if (dgv.Rows.Count != lightbox.ListPos.Count)
                dgv.Rows.Clear();
            for (int r = 0; r < lightbox.ListPos.Count; r++)
            {
                FillTableWithPosDef(lightbox.ListPos.ElementAt(r), r);
            }
            //补偿
            if(dgv.CurrentRow != null && dgv.CurrentRow.Cells[0]!=null && dgv.CurrentRow.Cells[0].Value!=null)
            {
                int num = (int)dgv.CurrentRow.Cells["Num"].Value;
                LightBox.PosDef PosDef = lightbox.ListPos.Find(delegate (LightBox.PosDef x) { return x.ID == num;});
                if(PosDef != null)
                {
                    dgvcmp.Rows.Clear();
                    for (int n = 0; n < PosDef.Cmp.Length; n++)
                    {
                        int RowID = dgvcmp.Rows.Add();
                        dgvcmp.Columns[0].HeaderText = string.Format("位置[{0}]的补偿",PosDef.ID);
                        dgvcmp.Rows[RowID].Cells["cmp_ws"].Value = string.Format("工站{0}",n);
                        dgvcmp.Rows[RowID].Cells["cmp_x1"].Value = Math.Abs(PosDef.Cmp[n].X1) < 10000 ? PosDef.Cmp[n].X1.ToString("F3") : "";
                        dgvcmp.Rows[RowID].Cells["cmp_x2"].Value = Math.Abs(PosDef.Cmp[n].X2) < 10000 ? PosDef.Cmp[n].X2.ToString("F3") : "";
                        dgvcmp.Rows[RowID].Cells["cmp_z1"].Value = Math.Abs(PosDef.Cmp[n].Z1) < 10000 ? PosDef.Cmp[n].Z1.ToString("F3") : "";
                        dgvcmp.Rows[RowID].Cells["cmp_z2"].Value = Math.Abs(PosDef.Cmp[n].Z2) < 10000 ? PosDef.Cmp[n].Z2.ToString("F3") : "";
                    }
                }
            }

            dgv.Update();
        }

        private void btn_del_Click(object sender, EventArgs e)
        {
            Del();
        }

        private void btn_add_Click(object sender, EventArgs e)
        {
            Add();
        }

        private void btn_load_Click(object sender, EventArgs e)
        {
            if (lightbox == null) return;
            EM_RES res = lightbox.LoadCfg();
            if (res != EM_RES.OK)
            {
                MessageBox.Show("加载失败!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            UpdateShow();
            MessageBox.Show("加载成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            if (lightbox == null) return;
            List<LightBox.PosDef> listtemp = new List<LightBox.PosDef>();
            listtemp.Clear();
            foreach (DataGridViewRow row in dgv.Rows)
            {
                LightBox.PosDef PosDef = new LightBox.PosDef();
                if (GetRowData(row.Index, ref PosDef))
                {
                    listtemp.Add(PosDef);
                }
            }
            lightbox.ListPos = listtemp;
            EM_RES res = lightbox.SaveCfg();
            if (res != EM_RES.OK)
            {
                MessageBox.Show("保存失败!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            res = lightbox.LoadCfg();
            if (res != EM_RES.OK)
            {
                MessageBox.Show("保存后加载失败!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            UpdateShow();
            MessageBox.Show("保存成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (lightbox == null || e.RowIndex < 0 || e.RowIndex > lightbox.ListPos.Count) return;
            //定位
            if (e.ColumnIndex ==10)
            {
                try
                {
                    LightBox.PosDef PosDef = new LightBox.PosDef();
                    if (!GetRowData(e.RowIndex, ref PosDef)) return;

                    //确保转台在位置
                    //确保对应测试工站在测试位置
                    WS ws = null;
                    //switch (lightbox.disc)
                    //{
                    //    case "左光箱":
                    //        ws = WSFEED.GetWSOnPos(WSFEED.EM_STA.POS1);
                    //        break;
                    //    case "OTP光箱":
                    //        ws = WSFEED.GetWSOnPos(WSFEED.EM_STA.POS2);
                    //        break;
                    //    case "右光箱":
                    //        ws = WSFEED.GetWSOnPos(WSFEED.EM_STA.POS3);
                    //        break;
                    //}
                    //if (ws == null)
                    //{
                    //    MessageBox.Show("转台位置未知，禁止光箱定位！", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    //    return;
                    //}
                    //if (!ws.isInTestPos)
                    //{
                    //    MessageBox.Show(string.Format("{0} 不在测试未知，禁止光箱定位！", ws.disc), "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    //    return;
                    //}

                    //补偿
                    int ws_idx = -1;
                    if (PosDef.GetCmp(ws.num) != null && DialogResult.Yes == MessageBox.Show("当前位置有补偿，是否加上补偿定位?", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning))
                    {
                        ws_idx = ws.num;
                    }
                    else if (DialogResult.Yes != MessageBox.Show("确定要定位?", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning))
                    {
                        return;
                    }
                    EM_RES res = lightbox.MoveTo(ref VAR.gsys_set.bquit, PosDef, ws_idx);
                    if (res == EM_RES.OK)
                        MessageBox.Show("定位成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        MessageBox.Show("定位失败!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            //学习位置
            else if (e.ColumnIndex == 9)
            {
                try
                {
                    LightBox.PosDef PosDef = new LightBox.PosDef();
                    if (!GetRowData(e.RowIndex, ref PosDef)) return;
                    PosDef.X1 = lightbox.ax_x1 != null ? lightbox.ax_x1.fenc_pos:double.MaxValue;
                    PosDef.X2 = lightbox.ax_x2 != null ? lightbox.ax_x2.fenc_pos:double.MaxValue;
                    PosDef.Y1 = lightbox.ax_y1 != null ? lightbox.ax_y1.fenc_pos : double.MaxValue;
                    PosDef.Z1 = lightbox.ax_z1 != null ? lightbox.ax_z1.fenc_pos:double.MaxValue;
                    PosDef.Z2 = lightbox.ax_z2 != null ? lightbox.ax_z2.fenc_pos:double.MaxValue;
                    SetRowData(e.RowIndex, PosDef);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btn_stop_Click(object sender, EventArgs e)
        {
            if(lightbox!=null)
                lightbox.Stop();
        }

        private void btn_cmp_Click(object sender, EventArgs e)
        {
            tp_dgv.RowStyles[1] = new RowStyle(SizeType.Absolute, tableLayoutPanel1.Width / 2);
        }

        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            UpdateShow();
        }

        private void dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if ((e.RowIndex & 1) == 1) e.CellStyle.BackColor = Color.WhiteSmoke;
        }

        private void dgvcmp_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if ((e.RowIndex & 1) == 1) e.CellStyle.BackColor = Color.WhiteSmoke;
        }

        private void LightBoxDef_Load(object sender, EventArgs e)
        {
            this.dgv.ShowCellToolTips = true;
        }

        private void dgv_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != 0 || e.RowIndex < 0 || dgv.Rows.Count <= 0) return;
            string str = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == null ? "" : dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
            int val= str.Length > 0 ? Convert.ToInt32(str) : -10000;
            if(val>=0)
            dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText ="0x"+ Convert.ToString(val,16);
        }
    }
}
