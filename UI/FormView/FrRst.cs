using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MotionCtrl;
using DevReport;
using Win32Lib;
using UI.Class;

namespace UI
{
    public partial class FrRst : Form
    {
        bool bflag;
        bool breset = false;

        #region 委托弹框
        delegate DialogResult MessageBoxShow(string text,string caption, MessageBoxButtons buttons,MessageBoxIcon icon);
        DialogResult MessageBoxShowF(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            var dr = MessageBox.Show(text,caption,buttons,icon);
            return dr;
        }
        public DialogResult ShowMessage(string text, string caption="提示", MessageBoxButtons buttons= MessageBoxButtons.OKCancel, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            var dr = new DialogResult();
            try
            {
                var tmp = this.Invoke(new MessageBoxShow(MessageBoxShowF), new object[] { text, caption, buttons, icon });
                if (tmp != null) dr = (DialogResult)tmp;
            }
            catch
            {

            }
            return dr;
        }
        #endregion
        #region 委托操作按键
        delegate void EnableObject(object sender,bool ben = true);
        void EnableObj(object sender, bool ben = true)
        {
            ((dynamic)sender).Enabled = ben;
        }
        public void Enablebtn(object sender, bool ben = true)
        {
            try
            {
                this.Invoke(new EnableObject(EnableObj), new object[] { sender, ben });
            }
            catch
            {

            }
        }
        #endregion

        
        public FrRst()
        {
            InitializeComponent();
            ApplyTrayBoxUiMapping();
        }

        private void ApplyTrayBoxUiMapping()
        {
            if (RuntimeMachineMode.IsTrayBoxSwapped)
            {
                lb_box_ok.Location = new Point(109, 292);
                lb_box_ng.Location = new Point(109, 182);
                btn_traybox_ok_z.Location = new Point(35, 289);
                btn_traybox_ng_z.Location = new Point(35, 179);
                btn_traybox_ok_x.Location = new Point(112, 340);
                btn_traybox_ng_x.Location = new Point(112, 230);
            }
            else
            {
                lb_box_ok.Location = new Point(109, 182);
                lb_box_ng.Location = new Point(109, 292);
                btn_traybox_ok_z.Location = new Point(35, 179);
                btn_traybox_ng_z.Location = new Point(35, 289);
                btn_traybox_ok_x.Location = new Point(112, 230);
                btn_traybox_ng_x.Location = new Point(112, 340);
            }
        }

        public bool bupdate
        {
            get { return timer_update.Enabled; }
            set { timer_update.Enabled = value; }
        }

        private void FrUser_Load(object sender, EventArgs e)
        {

        }
        void SetBtnStatusColor(Button btn, AXIS.HOME_STA sta, bool bflag)
        {
            if (sta == AXIS.HOME_STA.OK) btn.BackColor = Color.DodgerBlue;
            else if (sta == AXIS.HOME_STA.HOMING) btn.BackColor = bflag ? Color.DodgerBlue : SystemColors.ButtonFace;
            else if (sta == AXIS.HOME_STA.ERROR || sta == AXIS.HOME_STA.UNKOWN) btn.BackColor = Color.Orange;
            else btn.BackColor = SystemColors.ButtonFace;
        }        
        private void btn_ul_dl_home_Click(object sender, EventArgs e)
        {
            EM_RES res_chk = EM_RES.OK;
            if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
            if (COM.UDLoad1.status_ud == UpDownLoad.EM_STA.HOME || COM.UDLoad2.status_ud ==UpDownLoad.EM_STA.HOME) return;
            if (DialogResult.OK != MessageBox.Show("确定要复位 【上下料】？", "警告", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)) return;
            MT.DoorAlarmMsg = string.Empty;
            MT.DoorAlarmMsgTemp = string.Empty;
            res_chk = MT.ChkAllSafeSen(0x01);
            if (res_chk != EM_RES.OK) return;
            Task task_home = new Task(() =>
            {
                try
                {
                    MT.OnlyKeyLOn(MT.GPIO_OUT_KL_RESET);
                    Enablebtn(btn_all_home, false);
                    Enablebtn(sender, false);
                    VAR.gsys_set.bquit = false;
                    EM_RES  res=new EM_RES();
                    EM_RES[] array_res=new EM_RES[2];
                    Task[] task_udlhome = new Task[2];
                    foreach (UpDownLoad Udl in COM.List_UDLoad)
                    {
                         task_udlhome[COM.List_UDLoad.IndexOf(Udl)] = new Task(() =>{ array_res[COM.List_UDLoad.IndexOf(Udl)] = Udl.Home(ref VAR.gsys_set.bquit); });
                         task_udlhome[COM.List_UDLoad.IndexOf(Udl)].Start();
                         Udl.status_ud = UpDownLoad.EM_STA.HOME;
                    }

                    task_udlhome[0].Wait();
                    task_udlhome[1].Wait();
                    if (array_res[0] == EM_RES.OK && array_res[1] == EM_RES.OK) res = EM_RES.OK;
                    else res = EM_RES.ERR;
                    for (int i = 0; i < COM.List_UDLoad.Count; i++)
                    {
                        if(array_res[i]==EM_RES.OK) COM.List_UDLoad[i].status_ud= UpDownLoad.EM_STA.READY;
                        else COM.List_UDLoad[i].status_ud = UpDownLoad.EM_STA.UNKNOW;
                    }
                     
                    ShowMessage(string.Format("【{0}】 复位 {1}！", "上下料", res == EM_RES.OK ? "成功" : "失败"), "提示", MessageBoxButtons.OK);
                }
                finally
                {
                    MT.OnlyKeyLOn(MT.GPIO_OUT_KL_STOP);
                    Enablebtn(sender, true);
                    Enablebtn(btn_all_home, true);
                    COM.UDLoad1.ax_y.DisableHcmp(COM.CamDw1.TriIO.num);
                    COM.UDLoad1.ax_y.DisableHcmp(COM.CamUp1.TriIO.num);
                    COM.UDLoad2.ax_y.DisableHcmp(COM.CamDw2.TriIO.num);
                    COM.UDLoad2.ax_y.DisableHcmp(COM.CamUp2.TriIO.num);
                }
            }
            );
            task_home.Start();
        }

        private void btn_close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_stop_Click(object sender, EventArgs e)
        {
            if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
            VAR.gsys_set.bquit = true;
            UpDownLoad.bquit = true;
            UpDownLoad.UD1Stop();
            UpDownLoad.UD2Stop();
            UpDownLoad.LCStop();
            //MT.AllAxStop();
        }

        private void timer_update_Tick(object sender, EventArgs e)
        {
            bflag = !bflag;


            //上下料
            SetBtnStatusColor(btn_udl2_x, MT.AXIS_UDL2_X.home_status, bflag);
            SetBtnStatusColor(btn_udl2_z, MT.AXIS_UDL2_Z.home_status, bflag);
            SetBtnStatusColor(btn_udl2_y, MT.AXIS_UDL2_Y.home_status, bflag);
            SetBtnStatusColor(btn_udl2_u1, MT.AXIS_UDL2_U1.home_status, bflag);
            SetBtnStatusColor(btn_udl2_u2, MT.AXIS_UDL2_U2.home_status, bflag);
            //料仓
            SetBtnStatusColor(btn_udl1_x, MT.AXIS_UDL1_X.home_status, bflag);
            SetBtnStatusColor(btn_udl1_y, MT.AXIS_UDL1_Y.home_status, bflag);
            SetBtnStatusColor(btn_udl1_z, MT.AXIS_UDL1_Z.home_status, bflag);
            SetBtnStatusColor(btn_udl1_u1, MT.AXIS_UDL1_U1.home_status, bflag);
            SetBtnStatusColor(btn_udl1_u2, MT.AXIS_UDL1_U2.home_status, bflag);
            SetBtnStatusColor(btn_traybox_fd_z, COM.traybox_fd.ax_z.home_status, bflag);
            SetBtnStatusColor(btn_traybox_fd_x, COM.traybox_fd.ax_x.home_status, bflag);
            SetBtnStatusColor(btn_traybox_ok_z, COM.traybox_ok.ax_z.home_status, bflag);
            SetBtnStatusColor(btn_traybox_ok_x, COM.traybox_ok.ax_x.home_status, bflag);
            SetBtnStatusColor(btn_traybox_ng_z, COM.traybox_ng.ax_z.home_status, bflag);
            SetBtnStatusColor(btn_traybox_ng_x, COM.traybox_ng.ax_x.home_status, bflag);
            //测试机台

          

            SetBtnStatusColor(btn_wsfeed1_y, COM.wsfeed1.ax_y.home_status, bflag);
            SetBtnStatusColor(btn_wsfeed1_x, COM.wsfeed1.ax_x.home_status, bflag);
            SetBtnStatusColor(btn_wsfeed1_u, COM.wsfeed1.ax_u.home_status, bflag);
            SetBtnStatusColor(btn_wsfeed1_zfr, COM.wsfeed1.ax_z_fr.home_status, bflag);
            SetBtnStatusColor(btn_wsfeed1_zbk, COM.wsfeed1.ax_z_bk.home_status, bflag);
            SetBtnStatusColor(btn_wsfeed2_y, COM.wsfeed2.ax_y.home_status, bflag);
            SetBtnStatusColor(btn_wsfeed2_x, COM.wsfeed2.ax_x.home_status, bflag);
            SetBtnStatusColor(btn_wsfeed2_u, COM.wsfeed2.ax_u.home_status, bflag);
            SetBtnStatusColor(btn_wsfeed2_zfr, COM.wsfeed2.ax_z_fr.home_status, bflag);
            SetBtnStatusColor(btn_wsfeed2_zbk, COM.wsfeed2.ax_z_bk.home_status, bflag);
            lb_udl1.Text = COM.UDLoad1.StrOfPos;
            lb_udl2.Text = COM.UDLoad2.StrOfPos;
            lb_box_ok.Text = COM.traybox_ok.StrOfPos;
            lb_box_ng.Text = COM.traybox_ng.StrOfPos;            
            lb_box_fd.Text = COM.traybox_fd.StrOfPos;                     
            lb_wsfeed_2.Text = COM.wsfeed2.StrOfPos;
            lb_wsfeed_1.Text = COM.wsfeed1.StrOfPos;
        }



        private void btn__all_stop_Click(object sender, EventArgs e)
        {
            COM.Stop();
        }

        private async void btn_all_home_Click(object sender, EventArgs e)
        {
            EM_RES res_chk = EM_RES.OK;
            if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
            if (COM.bhomeing) return;
             COM.bhomeing = true;
 
            if (DialogResult.OK != MessageBox.Show("确定要系统复位，请确认上下料与测试机台的安全性？", "警告", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)) return;
            MT.DoorAlarmMsg = string.Empty;
            MT.DoorAlarmMsgTemp = string.Empty;
            res_chk = MT.ChkAllSafeSen();
            if (res_chk != EM_RES.OK)
            {
                COM.bhomeing = false;
                return;
            }
            
           
            Task task_home = new Task(() =>
            {
                try
                {
                    COM.bhomeing = true;
                    MT.OnlyKeyLOn(MT.GPIO_OUT_KL_RESET);
                    Enablebtn(btn_ul_dl_home, false); 
                    Enablebtn(btn_zt_home, false);                                       
                    Enablebtn(sender, false);
                    EM_RES res = COM.Home();
                    if (res == EM_RES.OK)
                    {
                        VAR.gsys_set.status = EM_SYS_STA.STANDBY;
                        VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, "就绪", 0, true);
                        COM.wsfeed1.SetupForFeed(ref VAR.gsys_set.bquit,false);
                        COM.wsfeed2.SetupForFeed(ref VAR.gsys_set.bquit,false);
                    }

                    ShowMessage(string.Format("【{0}】 复位 {1}", "系统", res == EM_RES.OK ? "成功" : "失败"), "提示", MessageBoxButtons.OK);
                }
                finally
                {

                    MT.OnlyKeyLOn(MT.GPIO_OUT_KL_STOP);                    
                    Enablebtn(btn_ul_dl_home, true);
                    Enablebtn(btn_zt_home, true);
                    Enablebtn(sender, true);
                    COM.bhomeing = false;

                }
            }
            );
            task_home.Start();
            await task_home;
            WSFEED.IsFirst = true;
            COM.bhomeing = false;
            VAR.gsys_set.status = EM_SYS_STA.STANDBY;
            MT.OnlyLightOn(MT.GPIO_OUT_ALM_YELLOW);
            Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.Ready).ToString() });
            Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "DReport.EmStatus.Ready269");

        }

        private async void btn_zt_home_Click(object sender, EventArgs e)
        {
            EM_RES res=EM_RES.OK;
            res = MT.ChkAllSafeSen(0x02);
            if (res != EM_RES.OK) return;
            if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
            foreach (WSFEED wsfeed in COM.list_wsfeed)
            {
                if (!wsfeed.IsDown)
                {
                    MessageBox.Show("测试机台有夹具处于打开状态，禁止回零请手动关闭夹具");
                    return ;
                }
            }
            if (DialogResult.OK != MessageBox.Show("确定要复位 【测试机上料模块及工站】？", "警告", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)) return;

            //确保上料Y/下料Y位置
            if (COM.UDLoad1.ax_x.home_status != AXIS.HOME_STA.OK||COM.UDLoad1.ax_y.home_status!=AXIS.HOME_STA.OK|| !MT.AXIS_UDL1_X.isORG || MT.AXIS_UDL1_Y.fenc_pos > 700)
            {
                MessageBox.Show("上料模块[1]_X轴不在原点或Y轴位置>700,可能有干涉,禁止测试机回零！", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (COM.UDLoad2.ax_x.home_status != AXIS.HOME_STA.OK || COM.UDLoad2.ax_y.home_status != AXIS.HOME_STA.OK || !MT.AXIS_UDL2_X.isORG || MT.AXIS_UDL2_Y.fenc_pos > 700)
            {
                MessageBox.Show("上料模块[2]_X轴不在原点或Y轴位置>700,可能有干涉,禁止测试机回零！", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            VAR.gsys_set.bquit = false;
            WSFEED.bpause = false;
            WSFEED.bquit = false;
            UpDownLoad.bquit = false;

            //两个回原线程
            Task task_zhome = new Task(() =>
            {
                try
                {
                    EM_RES[] array_zres = new EM_RES[2];
                    Task[] task_testzhome = new Task[2];
                    foreach (WSFEED wsfeed in COM.list_wsfeed)
                    {
                        task_testzhome[COM.list_wsfeed.IndexOf(wsfeed)] = new Task(() => { array_zres[COM.list_wsfeed.IndexOf(wsfeed)] = wsfeed.ZHome(ref VAR.gsys_set.bquit); });
                        task_testzhome[COM.list_wsfeed.IndexOf(wsfeed)].Start();
                        wsfeed.status = WSFEED.EM_STA.HOME;
                    }
                    task_testzhome[0].Wait();
                    task_testzhome[1].Wait();
                    if (array_zres[0] == EM_RES.OK && array_zres[1] == EM_RES.OK) res = EM_RES.OK;
                    else res = EM_RES.ERR;
                    for (int i = 0; i < COM.list_wsfeed.Count; i++)
                    {
                        if (array_zres[i] == EM_RES.OK) COM.list_wsfeed[i].status = WSFEED.EM_STA.READY;
                        else COM.list_wsfeed[i].status = WSFEED.EM_STA.UNKNOWN;
                    }
                   // ShowMessage(string.Format("【{0}】 复位 {1}！", "测试机台Z轴", res == EM_RES.OK ? "成功" : "失败"), "提示", MessageBoxButtons.OK);
                }
                finally
                {
                    MT.OnlyKeyLOn(MT.GPIO_OUT_KL_STOP);
                }
            }
            );
            task_zhome.Start();
            await task_zhome;



            //两个回原线程
            Task task_home = new Task(() =>
            {
                try
                {                   
                    EM_RES[] array_res = new EM_RES[2];
                    Task[] task_testhome = new Task[2];
                    foreach (WSFEED wsfeed in COM.list_wsfeed)
                    {
                        task_testhome[COM.list_wsfeed.IndexOf(wsfeed)] = new Task(() => { array_res[COM.list_wsfeed.IndexOf(wsfeed)] = wsfeed.Home(ref VAR.gsys_set.bquit); });
                        task_testhome[COM.list_wsfeed.IndexOf(wsfeed)].Start();
                        wsfeed.status = WSFEED.EM_STA.HOME;
                    }                   
                    task_testhome[0].Wait();
                    task_testhome[1].Wait();
                    if (array_res[0] == EM_RES.OK && array_res[1] == EM_RES.OK) res = EM_RES.OK;
                    else res = EM_RES.ERR;
                    for (int i = 0; i < COM.list_wsfeed.Count; i++)
                    {
                        if (array_res[i] == EM_RES.OK) COM.list_wsfeed[i].status = WSFEED.EM_STA.READY;
                        else COM.list_wsfeed[i].status = WSFEED.EM_STA.UNKNOWN;
                    }                   
                   
                }
                finally
                {
                    MT.OnlyKeyLOn(MT.GPIO_OUT_KL_STOP);
                }
            }
            );
            task_home.Start();
            await task_home;


            //两个回原线程
            Task task_uhome = new Task(() =>
            {
                try
                {
                    EM_RES[] array_ures = new EM_RES[2];
                    Task[] task_testuhome = new Task[2];
                    foreach (WSFEED ws in COM.list_wsfeed)
                    {
                        task_testuhome[COM.list_wsfeed.IndexOf(ws)] = new Task(() => { array_ures[COM.list_wsfeed.IndexOf(ws)] = ws.UHome(ref VAR.gsys_set.bquit); });
                        task_testuhome[COM.list_wsfeed.IndexOf(ws)].Start();
                        ws.status = WSFEED.EM_STA.HOME;
                    }
                    task_testuhome[0].Wait();
                    task_testuhome[1].Wait();
                    if (array_ures[0] == EM_RES.OK && array_ures[1] == EM_RES.OK) res = EM_RES.OK;
                    else res = EM_RES.ERR;
                    for (int i = 0; i < COM.list_wsfeed.Count; i++)
                    {
                        if (array_ures[i] == EM_RES.OK) COM.list_wsfeed[i].status = WSFEED.EM_STA.READY;
                        else COM.list_wsfeed[i].status = WSFEED.EM_STA.UNKNOWN;
                    }
                    ShowMessage(string.Format("【{0}】 复位 {1}！", "测试机台", res == EM_RES.OK ? "成功" : "失败"), "提示", MessageBoxButtons.OK);
                }
                finally
                {
                    MT.OnlyKeyLOn(MT.GPIO_OUT_KL_STOP);
                    Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.Ready).ToString() });
                    Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "DReport.EmStatus.Ready403");
                }
            }
            );
            task_uhome.Start();
            await task_uhome;
        }

        private void btn_zt_stop_Click_1(object sender, EventArgs e)
        {
            if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
            VAR.gsys_set.bquit = true;
          
            foreach (WSFEED wsfeed in COM.list_wsfeed)
            {
                wsfeed.ax_y.bhomequit = true;
                wsfeed.ax_y.Stop();
                wsfeed.ax_z_fr.bhomequit = true;
                wsfeed.ax_z_fr.Stop();
                wsfeed.ax_z_bk.bhomequit = true;
                wsfeed.ax_z_bk.Stop();
                wsfeed.ax_u.bhomequit = true;
                wsfeed.ax_u.Stop();
                wsfeed.ax_x.bhomequit = true;
                wsfeed.ax_x.Stop();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            EM_RES res = EM_RES.OK;
            foreach (WSFEED ws in COM.list_wsfeed)
            {
                if (!ws.isUInFeedPos)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("U轴未在上料位,{0}禁止气缸开合", ws.disc) : string.Format("Updownload did not return to the safe position, {0} forbidden to open and close the cylinder!         (上下料未回安全位置,{0}禁止气缸开合)", ws.disc));
                    return;
                }
               res = ws.Cyldown(ref VAR.gsys_set.bquit);

            }
        }
    }
}
