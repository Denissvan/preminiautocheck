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
using System.Threading;

namespace UI.Compment
{
    public partial class AZD : UserControl
    {
        public WSFEED ws = null;
        public bool bon_zk = true;
        public bool bon_power = true;
        public AZD()
        {
            InitializeComponent();
            lb1.Click += new EventHandler(lb1_Click);
            lb2.Click += new EventHandler(lb1_Click);
            lb3.Click += new EventHandler(lb1_Click);
            lb4.Click += new EventHandler(lb1_Click);
        }

        private void lb1_Click(object sender, EventArgs e)
        {
            if (ws == null) return;
            if (ws.list_cyl == null && ws.list_cyl.Count == 0) return;
            if (!ws.isUInFeedPos)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("U轴未在上料位,{0}禁止气缸开合", ws.disc) : string.Format("Updownload did not return to the safe position, {0} forbidden to open and close the cylinder!         (上下料未回安全位置,{0}禁止气缸开合)", ws.disc));
                return;
            }
            //get cylinder
            int idx = -1;
            if (!int.TryParse(((Label)sender).Text, out idx)) return;
            idx -= 1;
            if (idx < 0) return;
            Cylinder cy = null;
            if (idx < ws.list_cyl.Count)
            {
                cy = ws.list_cyl.ElementAt(idx);
            }
            else
            {
                idx -= ws.list_cyl.Count;
                if (idx < ws.list_cyl.Count)
                {
                    cy = ws.list_cyl.ElementAt(idx);
                }
            }
            if (cy == null || cy.io_out1 == null || cy.io_out2 == null) return;

            //check
            EM_RES res = EM_RES.OK;
            if (cy.isON)
            {
                if (!ws.isUInFeedPos)
                {
                    if (DialogResult.OK != MessageBox.Show(VAR.IsChinese ? string.Format("{0}不在水平上料位，打开压盖可能掉料，确定要打开？", ws.disc) : string.Format("{0} is not in the horizontal loading level, the gland may fall when you open it. Are you sure you want to open it?\r\n{0}不在水平上料位，打开压盖可能掉料，确定要打开？", ws.disc), "", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning))
                        return;
                }
                if (!ws.isYInFeedPos)
                {
                    if (DialogResult.OK != MessageBox.Show(VAR.IsChinese ? string.Format("{0}当前Y轴不在上料位置，打开压盖可能碰到光源，确定要打开？", ws.disc) : string.Format("{0} is not in the horizontal loading level, the gland may fall when you open it. Are you sure you want to open it?\r\n{0}不在水平上料位，打开压盖可能掉料，确定要打开？", ws.disc), "", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning))
                        return;
                }
                if (!COM.UDLoad1.ax_x.isORG || !COM.UDLoad2.ax_x.isORG || COM.UDLoad1.ax_y.fenc_pos > COM.UDLoad1.list_xt[1].st_cap_pos.y || COM.UDLoad2.ax_y.fenc_pos > COM.UDLoad2.list_xt[1].st_cap_pos.y)
                {
                    if (DialogResult.OK != MessageBox.Show(VAR.IsChinese ? string.Format("{0}当前上下料不在安全位置，确定要打开？", ws.disc) : string.Format("{0} is not in the horizontal loading level, the gland may fall when you open it. Are you sure you want to open it?\r\n{0}不在水平上料位，打开压盖可能掉料，确定要打开？", ws.disc), "", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning))
                        return;
                }
            }
            //res = cy.Invert(); 
            if (cy.isON)
            {
                cy.SetOn();
            }
            else
            {
                cy.SetOff();
            }
            if (res != EM_RES.OK)
                MessageBox.Show(VAR.IsChinese ? string.Format("{0}操作异常！", cy.io_out1.str_disc) : string.Format("{0} Abnormal operation!\r\n{1}操作异常！", cy.io_out1.str_disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public bool ChkAndUpdatePnl1(List<Cylinder> list_cld, TableLayoutPanel tbp, int indofs = 0)
        {
            if (list_cld == null || tbp == null) return false;

            bool bAllOpen = true;
            int num = 0;
            for (int n = 0; n < list_cld.Count; n++)
            {
                Control[] control = tbp.Controls.Find(string.Format("panel{0}", n + 1 + indofs), true);
                if (control != null && control.Count() > 0)
                {
                    //check out
                    if (list_cld[n].isON)                         //气缸打开，夹具打开
                    {
                        //                         if (n + 1 + indofs > list_cld.Count) ((Panel)control[0]).Dock = DockStyle.Bottom;
                        //                         else ((Panel)control[0]).Dock = DockStyle.Top;

                        ((Panel)control[0]).BorderStyle = BorderStyle.Fixed3D;
                        ((Panel)control[0]).BackColor = Color.Orange;


                        //check sensor
                        //if (list_cld[n].isOFFByChkSen && !list_cld[n].isONByChkSen)
                        if (list_cld[n].isONByChkSen && !list_cld[n].isOFFByChkSen)      //放宽条件
                        {
                            ((Panel)control[0]).BackColor = System.Drawing.SystemColors.InactiveCaption;
                        }

                    }
                    else
                    {
                        //                         if (n + 1 + indofs > list_cld.Count) ((Panel)control[0]).Dock = DockStyle.Top;
                        //                         else ((Panel)control[0]).Dock = DockStyle.Bottom;

                        ((Panel)control[0]).BorderStyle = BorderStyle.FixedSingle;
                        ((Panel)control[0]).BackColor = SystemColors.InactiveCaption;
                        bAllOpen = false;
                        //check sensor
                        if (list_cld[n].isOFFByChkSen && !list_cld[n].isONByChkSen)
                        {
                            ((Panel)control[0]).BackColor = Color.Lime;
                        }
                    }

                    //if (((Panel) control[0]).BackColor == Color.Lime)
                    //{
                    //    if (n + 1 + indofs > list_cld.Count ) ((Panel)control[0]).Dock = DockStyle.Bottom;
                    //    else ((Panel)control[0]).Dock = DockStyle.Top;
                    //}
                    //else
                    //{
                    //    if (n + 1 + indofs > list_cld.Count) ((Panel)control[0]).Dock = DockStyle.Top;
                    //    else ((Panel)control[0]).Dock = DockStyle.Bottom;
                    //}
                }
            }
            if (num == list_cld.Count)
            {
                bAllOpen = true;
            }
            return bAllOpen;
        }
        public bool ChkAndUpdatePnl(List<Cylinder> list_cld, TableLayoutPanel tbp, int indofs = 0)
        {
            if (list_cld == null || tbp == null) return false;

            bool bAllOpen = true;
            int num = 0;
            for (int n = 0; n < list_cld.Count; n++)
            {
                Control[] control = tbp.Controls.Find(string.Format("panel{0}", n + 1 + indofs), true);
                if (control != null && control.Count() > 0)
                {
                    //check out
                    if (list_cld[n].isOFF)                         //气缸打开，夹具打开
                    {
                        //if (n + 1 + indofs > list_cld.Count) ((Panel)control[0]).Dock = DockStyle.Bottom;
                        //else ((Panel)control[0]).Dock = DockStyle.Top;

                        ((Panel)control[0]).BorderStyle = BorderStyle.Fixed3D;
                        ((Panel)control[0]).BackColor = Color.Orange;
                        if (n == 3) panel4.BackColor = Color.Orange;
                        bAllOpen = false;

                        //check sensor
                        //if (list_cld[n].isOFFByChkSen && !list_cld[n].isONByChkSen)
                        if (list_cld[n].isOFFByChkSen && !list_cld[n].isONByChkSen)      //放宽条件
                        {
                            ((Panel)control[0]).BackColor = Color.Lime;
                        }
                    }
                    else
                    {
                        //if (n + 1 + indofs > list_cld.Count) ((Panel)control[0]).Dock = DockStyle.Top;
                        //else ((Panel)control[0]).Dock = DockStyle.Bottom;

                        ((Panel)control[0]).BorderStyle = BorderStyle.FixedSingle;
                        ((Panel)control[0]).BackColor = SystemColors.InactiveCaption;

                        //check sensor
                        if (list_cld[n].isOFFByChkSen && !list_cld[n].isONByChkSen)
                        {
                            //if (n + 1 + indofs > list_cld.Count) ((Panel)control[0]).Dock = DockStyle.Bottom;
                            //else ((Panel)control[0]).Dock = DockStyle.Top;
                            ((Panel)control[0]).BackColor = Color.Lime;
                            if(n==3)panel4.BackColor = Color.Lime;
                        }
                        //else if (list_cld[n].isOFFByChkSen || !list_cld[n].isONByChkSen)            //气缸打开，夹具打开
                        //{
                        //    ((Panel)control[0]).BackColor = Color.Orange;
                        //    //bAllOpen = true;
                        //    num++;
                        //}
                    }

                    //if (((Panel) control[0]).BackColor == Color.Lime)
                    //{
                    //    if (n + 1 + indofs > list_cld.Count ) ((Panel)control[0]).Dock = DockStyle.Bottom;
                    //    else ((Panel)control[0]).Dock = DockStyle.Top;
                    //}
                    //else
                    //{
                    //    if (n + 1 + indofs > list_cld.Count) ((Panel)control[0]).Dock = DockStyle.Top;
                    //    else ((Panel)control[0]).Dock = DockStyle.Bottom;
                    //}
                }
            }
            if (num == list_cld.Count)
            {
                bAllOpen = true;
            }
            return bAllOpen;
        }
        public void UpdateShow()
        {
            if (ws == null) return;
            if (ws.list_cyl == null && ws.list_cyl.Count == 0) return;
            string str_sta = VAR.IsChinese ? "未知" : "Unknown";
            if (ws.ax_u != null)
            {
                if (ws.isUInTestPos)
                    str_sta = VAR.IsChinese ? "测试位◐" : "Test◐";
                else if (ws.isUInFeedPos)
                    str_sta = VAR.IsChinese ? "水平位◓" : "Feed◐";
                else
                    str_sta = VAR.IsChinese ? "异常" : "ERROR";
            }
            bool bAllOpenA = false, bAllOpenB = false, bAllOpen = false;
            bAllOpenA = ChkAndUpdatePnl1(ws.list_cyl, tblA);
            bAllOpenB = ChkAndUpdatePnl1(ws.list_cyl, tblB);
            bAllOpen = bAllOpenA && bAllOpenB;
            if (VAR.IsChinese) btn_open_close_a.Text = bAllOpen ? "合" : "开";
            else btn_open_close_a.Text = bAllOpen ? "Close" : "Open";
            //    if (VAR.IsChinese) btn_zk_on_off.Text = bon_zk ? "真空" : "破真空";
            //    else btn_zk_on_off.Text = bon_zk ? "Vacuum" : "Vacuum Break";
        }

        private void AZD_Resize(object sender, EventArgs e)
        {
            //tblA.Height = tbp_btnA.Height * 2 / 3;
            //tblB.Height = tbp_btnB.Height * 2 / 3;
            //for (int i = 0; i < 4; i++)
            //{
            //    Control[] control = tblA.Controls.Find(string.Format("pnl{0}", i + 1), true);
            //    if (control != null && control.Count() > 0)
            //    {
            //        ((Panel) control[0]).Height = tblA.Height * 2 / 3;
            //    }
            //      control = tblB.Controls.Find(string.Format("pnl{0}", i + 5), true);
            //    if (control != null && control.Count() > 0)
            //    {
            //        ((Panel)control[0]).Height = tblB.Height * 2 / 3;
            //    }
            //}

        }




        private void btn_u_0_a_Click(object sender, EventArgs e)
        {
            if (ws == null) return;
            if (DialogResult.Yes != MessageBox.Show(VAR.IsChinese ? string.Format("{0}确定要旋转到测试位?", ws.disc) : string.Format("Are you sure you want to rotate {0} to the test position?\r\n{0}确定要旋转到测试位?", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.YesNo, MessageBoxIcon.Question)) return;
            VAR.gsys_set.bquit = false;
            EM_RES res = ws.MoveToUTestPos(ref VAR.gsys_set.bquit, true);
            if (res == EM_RES.OK)
            {
                MessageBox.Show(VAR.IsChinese ? string.Format("{0}旋转到测试位完成！", ws.disc) : string.Format("{0} is rotated to the test position！\r\n{0}旋转到测试位完成！", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (res == EM_RES.MOVE_PROTECT)
            {
                MessageBox.Show(VAR.IsChinese ? string.Format("{0}旋转到测试位异常！\r\n气缸未全部压盖到位", ws.disc) : string.Format("The rotation of {0} to the test position is abnormal! \r\n Cylinders are not fully capped\r\n{0}旋转到测试位异常！\r\n气缸未全部压盖到位", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(VAR.IsChinese ? string.Format("{0}旋转到测试位异常，{1}", ws.disc, Utility.GetDescription(res, VAR.IsChinese)) : string.Format("The rotation of {0} to the test position is abnormal!,{1}\r\n{0}旋转到测试位异常，{2}", ws.disc, Utility.GetDescription(res, VAR.IsChinese), Utility.GetDescription(res)), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btn_u_90_a_Click(object sender, EventArgs e)
        {
            if (ws == null) return;
            if (DialogResult.Yes != MessageBox.Show(VAR.IsChinese ? string.Format("{0}确定要旋转到上料位(水平位)?", ws.disc) : string.Format("Are you sure you want to rotate {0} to the loading level (horizontal position)?\r\n{0}确定要旋转到上料位(水平位)?", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.YesNo, MessageBoxIcon.Question)) return;

            VAR.gsys_set.bquit = false;
            EM_RES res = ws.MoveToUFeedPos(ref VAR.gsys_set.bquit, true);
            if (res == EM_RES.OK)
            {
                MessageBox.Show(VAR.IsChinese ? string.Format("{0}旋转到上料位(水平位)完成！", ws.disc) : string.Format("Rotate {0} to the loading level (horizontal position)!\r\n{0}旋转到上料位(水平位)完成！", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (res == EM_RES.MOVE_PROTECT)
            {
                MessageBox.Show(VAR.IsChinese ? string.Format("{0}旋转到上料位(水平位)异常！\r\n安全保护!", ws.disc) : string.Format("The rotation of {0} to loading level (horizontal position) is abnormal\r\n{0}旋转到上料位(水平位)异常！\r\n安全保护!", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(VAR.IsChinese ? string.Format("{0}旋转到上料位(水平位)异常，{1}", ws.disc, Utility.GetDescription(res, VAR.IsChinese)) : string.Format("The rotation of {0} to loading level (horizontal position) abnormal,{1}\r\n{0}旋转到上料位(水平位)异常，{2}", ws.disc, Utility.GetDescription(res, VAR.IsChinese), Utility.GetDescription(res)), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btn_test_pos_Click(object sender, EventArgs e)
        {
            //EM_RES res = EM_RES.OK;
            //VAR.gsys_set.bquit = false;
            //res = ws.SetupForTest(ref VAR.gsys_set.bquit);
            //if (res != EM_RES.OK)
            //    MessageBox.Show(VAR.IsChinese?string.Format("{0}转到测试状态异常！", ws.disc): string.Format("{0} went to the test pos abnormally!\r\n{0}转到测试状态异常！", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btn_feed_pos_Click(object sender, EventArgs e)
        {
            //EM_RES res = EM_RES.OK;
            //VAR.gsys_set.bquit = false;
            //res = ws.SetupForFeed(ref VAR.gsys_set.bquit);
            //if (res != EM_RES.OK)
            //    MessageBox.Show(VAR.IsChinese ? string.Format("{0}转到上下料状态异常！", ws.disc): string.Format("{0} went to the feed pos abnormally!\r\n{0}转到上下料状态异常！", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btn_open_close_b_Click(object sender, EventArgs e)
        {
            bool bon = ((Button)sender).Text == (VAR.IsChinese ? "开" : "Open");

            EM_RES res = EM_RES.OK;
            VAR.gsys_set.bquit = false;

            if (ws.isYInFeedPos && (!COM.UDLoad1.ax_x.isORG || !COM.UDLoad2.ax_x.isORG || COM.UDLoad1.ax_y.fenc_pos > COM.UDLoad1.list_xt[1].st_cap_pos.y || COM.UDLoad2.ax_y.fenc_pos > COM.UDLoad2.list_xt[1].st_cap_pos.y))
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("上下料未回安全位置,{0}禁止气缸开合", ws.disc) : string.Format("Updownload did not return to the safe position, {0} forbidden to open and close the cylinder!         (上下料未回安全位置,{0}禁止气缸开合)", ws.disc));
                return;
            }
            if(!ws.isUInFeedPos)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("U轴未在上料位,{0}禁止气缸开合", ws.disc) : string.Format("Updownload did not return to the safe position, {0} forbidden to open and close the cylinder!         (上下料未回安全位置,{0}禁止气缸开合)", ws.disc));
                return;
            }
            if (!bon)
            {
                res = ws.Cyldown(ref VAR.gsys_set.bquit);

            }
            else
            {
                res = ws.Cylup(ref VAR.gsys_set.bquit);
            }    
            if (res != EM_RES.OK)
                MessageBox.Show(VAR.IsChinese ? string.Format("{0}气缸动作异常！", ws.disc) : string.Format("{0}Front row abnormal\r\n{0}气缸动作异常！", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btn_open_close_a_Click(object sender, EventArgs e)
        {
            bool bon = ((Button)sender).Text == (VAR.IsChinese ? "开" : "Open");

            EM_RES res = EM_RES.OK;
            VAR.gsys_set.bquit = false;

            if (ws.isYInFeedPos && (!COM.UDLoad1.ax_x.isORG || !COM.UDLoad2.ax_x.isORG || COM.UDLoad1.ax_y.fenc_pos > COM.UDLoad1.list_xt[1].st_cap_pos.y || COM.UDLoad2.ax_y.fenc_pos > COM.UDLoad2.list_xt[1].st_cap_pos.y))
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese?string.Format("上下料未回安全位置,{0}禁止气缸开合", ws.disc): string.Format("Updownload did not return to the safe position, {0} forbidden to open and close the cylinder!         (上下料未回安全位置,{0}禁止气缸开合)", ws.disc));
                return;
            }

            if (bon)
                res = ws.Cylup(ref VAR.gsys_set.bquit);
            else
                res = ws.Cyldown(ref VAR.gsys_set.bquit);

            if (res != EM_RES.OK)
                MessageBox.Show(VAR.IsChinese?string.Format("{0}气缸动作异常！", ws.disc): string.Format("{0}Front row abnormal\r\n{0}气缸动作异常！", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btn_ZK_on_off_Click(object sender, EventArgs e)
        {
            bon_zk = ((Button)sender).Text == (VAR.IsChinese ? "真空开" : "Vacuum Open");
            
            EM_RES res = EM_RES.OK;
            VAR.gsys_set.bquit = false;
            if (bon_zk)
            {
                res = ws.gpio_openfr.SetOn();
                ((Button)sender).Text = (VAR.IsChinese ? "真空关" : "Vacuum Close");
                bon_zk = false;
            }
            else
            {
                res = ws.gpio_openfr.SetOff();
                ((Button)sender).Text = (VAR.IsChinese ? "真空开" : "Vacuum Open");
                bon_zk = true;
            }

            if (res != EM_RES.OK)
                MessageBox.Show(VAR.IsChinese?string.Format("{0}真空、破真空异常！", ws.disc): string.Format("Vacuum and vacuum breaking is abnormal!\r\n{0}真空、破真空异常！", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btn_power_on_off_Click(object sender, EventArgs e)
        {
            //bon_power = ((Button)sender).Text == (VAR.IsChinese ? "上电" : "Power Up");

            //EM_RES res = EM_RES.OK;
            //VAR.gsys_set.bquit = false;
            //if (bon_power)
            //{
            //    res = ws.PowerOn(ref VAR.gsys_set.bquit);
            //    bon_power = false;
            //}
            //else
            //{
            //    res = ws.PowerOff(ref VAR.gsys_set.bquit);
            //    bon_power = true;
            //}

            //if (res != EM_RES.OK)
            //    MessageBox.Show(VAR.IsChinese?string.Format("{0}上下电异常！", ws.disc): string.Format("{0}Power on and off abnormally!\r\n{0}上下电异常！", ws.disc), VAR.IsChinese ? "提示" : "prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void lb1_Click_1(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
