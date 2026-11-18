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
using System.IO;
using System.IO.Ports;
using System.Windows.Documents;
using System.Windows.Markup.Primitives;
using static UI.Product.Tray;
using Win32Lib;
using DevReport;
using System.Diagnostics;
using MesUIWpf.Models;
using MesUIWpf;

namespace UI
{
    public partial class FrRun : Form
    {
        private double lastngcnt;
        System.Timers.Timer timer;
        private static readonly object _Locker = new object();
        public bool bupdate
        {
            get
            {
                return timer_500ms.Enabled;
            }
            //初次延迟5s再显示
            set
            {
                if (timer_500ms.Interval != 500) timer_500ms.Interval = 5000;
                timer_500ms.Enabled = value;

                //if(value)timer.Start(); 
                //else timer.Stop();
            }
        }

        public void PmsfrRun(User.PERMISSION pms)
        {
            cogDisplayer_run.PmsEn((pms > User.PERMISSION.Engineer) ? true : false);
            cb_product_list.Enabled = (pms >= User.PERMISSION.Engineer) ? true : false;
            //  ckb_Check.Enabled = (pms >= User.PERMISSION.Engineer) ? true : false;
            //ckb_not_open.Enabled= (pms >= User.PERMISSION.Engineer) ? true : false;
        }
        //#region
        //class ParameterHelper
        //{
        //    public int[] X_Cor
        //    {
        //        get;
        //        set;
        //    }
        //    public int[] Y_Cor
        //    {
        //        get;
        //        set;
        //    }
        //    public int[] U_Cor
        //    {
        //        get;
        //        set;
        //    }
        //    public int[] V_Cor
        //    {
        //        get;
        //        set;
        //    }
        //    //仿射变换模型六参数
        //    public double m1
        //    {
        //        get
        //        {
        //            return ((U_Cor[1] - U_Cor[0]) - m2 * (Y_Cor[1] - Y_Cor[0])) / (X_Cor[1] - X_Cor[0]);
        //        }
        //    }
        //    public double m2
        //    {
        //        get
        //        {
        //            return ((U_Cor[1] - U_Cor[0]) * (X_Cor[2] - X_Cor[0]) - (U_Cor[2] - U_Cor[0]) * (X_Cor[1] - X_Cor[0])) /
        //                ((Y_Cor[1] - Y_Cor[0]) * (X_Cor[2] - X_Cor[0]) - (Y_Cor[2] - Y_Cor[0]) * (X_Cor[1] - X_Cor[0]));
        //        }
        //    }
        //    public double m3
        //    {
        //        get
        //        {
        //            return ((V_Cor[1] - V_Cor[0]) - m4 * (Y_Cor[1] - Y_Cor[0])) / (X_Cor[1] - X_Cor[0]);
        //        }
        //    }
        //    public double m4
        //    {
        //        get
        //        {
        //            return ((V_Cor[1] - V_Cor[0]) * (X_Cor[2] - X_Cor[0]) - (V_Cor[2] - V_Cor[0]) * (X_Cor[1] - X_Cor[0])) /
        //                ((Y_Cor[1] - Y_Cor[0]) * (X_Cor[2] - X_Cor[0]) - (Y_Cor[2] - Y_Cor[0]) * (X_Cor[1] - X_Cor[0]));
        //        }
        //    }
        //    public double tx
        //    {
        //        get
        //        {
        //            return U_Cor[0] - m1 * X_Cor[0] - m2 * Y_Cor[0];
        //        }
        //    }
        //    public double ty
        //    {
        //        get
        //        {
        //            return V_Cor[0] - m3 * X_Cor[0] - m4 * Y_Cor[0];
        //        }
        //    }
        //}
        //#endregion

        public FrRun()
        {
            InitializeComponent();
            if (FrMain.frsuser != null)
                PmsfrRun(FrMain.frsuser.user1.cur_user.pms);
            else
                PmsfrRun(User.PERMISSION.Operator);
            //PmsfrRun(User.PERMISSION.Operator);
            //timer = new System.Timers.Timer(1000);
            //timer.AutoReset = true;
            //timer.Elapsed += (obj, args) =>
            //{
            //    timer.Stop();
            //    try
            //    {
            //        UpdateDisplay();
            //        //Thread.Sleep(10000);
            //    }
            //    catch(Exception ex)
            //    {
            //        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, ex.Message);
            //    }

            //    timer.AutoReset = false;
            //    timer.Interval = 500;
            //    timer.Start();
            //};
        }

        private void ApplyTrayBoxUiMapping()
        {
            if (RuntimeMachineMode.IsTrayBoxSwapped)
            {
                tableLayoutPanel4.SetCellPosition(traybox_ok, new TableLayoutPanelCellPosition(0, 2));
                tableLayoutPanel4.SetCellPosition(traybox_ng, new TableLayoutPanelCellPosition(0, 1));
                tableLayoutPanel4.SetCellPosition(tray_ok, new TableLayoutPanelCellPosition(1, 2));
                tableLayoutPanel4.SetCellPosition(tray_ng, new TableLayoutPanelCellPosition(1, 1));
                btn_new_traybox_ok.Location = new Point(6, 443);
                btn_new_traybox_ng.Location = new Point(6, 382);
            }
            else
            {
                tableLayoutPanel4.SetCellPosition(traybox_ok, new TableLayoutPanelCellPosition(0, 1));
                tableLayoutPanel4.SetCellPosition(traybox_ng, new TableLayoutPanelCellPosition(0, 2));
                tableLayoutPanel4.SetCellPosition(tray_ok, new TableLayoutPanelCellPosition(1, 1));
                tableLayoutPanel4.SetCellPosition(tray_ng, new TableLayoutPanelCellPosition(1, 2));
                btn_new_traybox_ok.Location = new Point(6, 382);
                btn_new_traybox_ng.Location = new Point(6, 443);
            }
        }

        private void cb_product_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            //加载产品
            try
            {
                VAR.gsys_set.cur_product_name = ((ComboBox)sender).Text;
                EM_RES ret = COM.product.LoadDat(VAR.gsys_set.cur_product_name);
                if (ret != EM_RES.OK) VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "产品数据加载失败!");
                else VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "产品数据加载成功!");

                foreach (WSFEED wsfeed in COM.list_wsfeed)
                {
                    wsfeed.LoadCfg(VAR.gsys_set.cur_product_name);
                    wsfeed.LoadCfgws(VAR.gsys_set.cur_product_name);
                }
                COM.XtInit(VAR.gsys_set.cur_product_name);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            COM.CamInit();
            cogDisplayer_run.list_cam.Clear();
            cogDisplayer_run.AddCam(COM.ListCam);
        }

        private void FrRun_Load(object sender, EventArgs e)
        {
            COM.product.LoadProductList(cb_product_list, VAR.gsys_set.cur_product_name);
            this.cb_product_list.SelectedIndexChanged += new System.EventHandler(this.cb_product_list_SelectedIndexChanged);
            Msg.secsManager.Send(new BaseInfo() { Id = 2, Value = cb_product_list.Text });
            Msg.secsManager.Send(new BaseInfo() { Id = 2 }, TypeId: 2);
            COM.traybox_fd.SetSta(TrayBox.EM_STA.UNTEST);
            COM.traybox_ok.SetSta(TrayBox.EM_STA.FULL);
            COM.traybox_ng.SetSta(TrayBox.EM_STA.FULL);
            COM.traybox_fd.NewBox(Product.EM_CM_RES.UNTEST);
            COM.traybox_ok.NewBox(Product.EM_CM_RES.EMPTY);
            COM.traybox_ng.NewBox(Product.EM_CM_RES.EMPTY);
            ws1.wsfeed = COM.wsfeed1;
            ws2.wsfeed = COM.wsfeed2;
            traybox_fd.box = COM.traybox_fd;
            traybox_ok.box = COM.traybox_ok;
            traybox_ng.box = COM.traybox_ng;
            ApplyTrayBoxUiMapping();
            tray_fd.TrayName = "待测料盘";
            tray_ok.TrayName = "OK料盘";
            tray_ng.TrayName = "NG料盘";
            tray_fd.TrayColor = Color.DarkOrange;
            tray_ok.TrayColor = Color.Lime;
            tray_ng.TrayColor = Color.Red;
            VAR.gsys_set.beep_tmr = 3000;
            VAR.sys_inf.Init(lb_war_inf, MT.GPIO_OUT_ALM_RED, MT.GPIO_OUT_ALM_GREEN, MT.GPIO_OUT_ALM_YELLOW, MT.GPIO_OUT_ALM_BEEPER, VAR.gsys_set.beep_tmr);//lb_war_inf
            VAR.msg.StartUpdate(dgv_msg);
            COM.vs_msg.StartUpdate(dgv_vs);
            cogDisplayer_run.AddCam(COM.ListCam);
        }

        //等待某个变量发生变化或者等待超过一定时间，就执行下面的操作
        //await Task.Run(() =>
        //{
        //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "正在等待MES上位指令!");
        //    SpinWait.SpinUntil(() => false, 10000);
        //});
        public async void btn_run_Click(object sender, EventArgs e)
        {
            try
            {
                UpDownLoad.DelateImage();
                EM_RES res = MT.SetAllAxToWorkSpd();
                res = MT.SetAllAxToWorkSpdPre();
                if (!PT_SET.IsMesLocal)
                {
                    Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.Wait).ToString() });
                    Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "DReport.EmStatus.Wait227");
                    await Task.Run(() =>
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "正在等待MES上位指令!");
                        SpinWait.SpinUntil(() => MT.IsAllowStartUpdate, 10000);
                    });
                    MT.IsAllowStartUpdate = false;
                    //fr?.Close();
                    if (!MT.IsAllowStart)
                    {
                        Dialog(Color.Yellow, "警告", "被MES禁止启动！请联系相关人员。");
                        return;
                    }
                }
                if (VAR.gsys_set.status == EM_SYS_STA.REPAIR)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("提示:当前设备正在维修，无法运行!"));
                    return;
                }
                //检测复位状态
                foreach (CARD card in MT.CardList)
                {
                    if (card.isReady == false)
                    {
                        MessageBox.Show(string.Format("{0}未初始化!", card.disc), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    foreach (AXIS ax in card.AxList)
                    {
                        if (ax.home_status != AXIS.HOME_STA.OK)
                        {
                            MessageBox.Show(string.Format("{0} 状态异常，{1}!\r\n请先复位", ax.disc, ax.home_status), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                //检测运行状态
                if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "---开始键按下---");
                btn_pause.BackColor = SystemColors.ButtonFace;
                VAR.gsys_set.bquit = false;
                WSFEED.bquit = false;
                UpDownLoad.bquit = false;
                WSFEED.bpause = false;
                VAR.Canchange = false;
                BaseAction.run();
            }
            finally
            {
                MT.IsAllowStart = false;
            }
        }


        /// <summary>
        /// 警告窗口
        /// </summary>
        /// <param name="bkColor">背景颜色</param>
        /// <param name="title">警告标题</param>
        /// <param name="info">警告内容</param>
        /// <param name="okText">OK按键名称</param>
        /// <param name="cancelText">Cancel按键名称</param>
        /// <returns>返回按键结果</returns>
        public static DialogResult Dialog(Color bkColor, string title, string info = "", string okText = "确定", string cancelText = "", string AbortText = "")
        {
            lock (_Locker)
            {
                //警告标题
                Label lb_Title = new Label
                {
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = title
                };

                FlowLayoutPanel flp_Panel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RightToLeft = RightToLeft.Yes,
                    Margin = new Padding(0)
                };

                Button btn_Ok = new Button
                {
                    Name = "btnOk",
                    Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                    Dock = DockStyle.Bottom,
                    Width = 240,
                    Height = 80,
                    Margin = new Padding(10),
                    Text = okText.Length == 0 ? "确定" : okText,
                    BackColor = SystemColors.ButtonFace,
                    Font = new Font(flp_Panel.Font.FontFamily, 36)
                };
                btn_Ok.Click += (sender, e) =>
                {
                    var f = ((Button)sender).FindForm();
                    if (f == null) return;
                    f.DialogResult = DialogResult.OK;
                    f.Close();
                };
                flp_Panel.Controls.Add(btn_Ok);

                if (cancelText.Length > 0)
                {
                    Button btn_Cancel = new Button
                    {
                        Name = "btnCancel",
                        Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                        Dock = DockStyle.Bottom,
                        Width = 240,
                        Height = 80,
                        Text = cancelText,
                        BackColor = SystemColors.ButtonFace,
                        Margin = new Padding(10),
                        Font = new Font(flp_Panel.Font.FontFamily, 36)
                    };
                    btn_Cancel.Click += (sender, e) =>
                    {
                        var f = ((Button)sender).FindForm();
                        if (f == null) return;
                        f.DialogResult = DialogResult.Cancel;
                        f.Close();
                    };
                    flp_Panel.Controls.Add(btn_Cancel);
                }

                if (AbortText.Length > 0)
                {
                    Button btn_Abort = new Button
                    {
                        Name = "btnAbort",
                        Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                        Dock = DockStyle.Bottom,
                        Width = 240,
                        Height = 80,
                        Text = AbortText,
                        Margin = new Padding(10),
                        BackColor = SystemColors.ButtonFace,
                        Font = new Font(flp_Panel.Font.FontFamily, 36)
                    };
                    btn_Abort.Click += (sender, e) =>
                    {
                        var f = ((Button)sender).FindForm();
                        if (f == null) return;
                        f.DialogResult = DialogResult.Abort;
                        f.Close();
                    };
                    flp_Panel.Controls.Add(btn_Abort);
                }

                TableLayoutPanel tlp_Panel = new TableLayoutPanel();
                tlp_Panel.Font = new Font(flp_Panel.Font.FontFamily, 36);
                tlp_Panel.Dock = DockStyle.Fill;
                tlp_Panel.ColumnCount = 1;
                tlp_Panel.RowCount = 1 + 1 + (info.Length > 0 ? 1 : 0);
                tlp_Panel.BorderStyle = BorderStyle.None;
                //tlp_Panel.AutoSize = false;
                int rowIdx = 0;

                //警告标题
                tlp_Panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
                tlp_Panel.Controls.Add(lb_Title);
                tlp_Panel.SetRow(lb_Title, rowIdx++);
                tlp_Panel.SetColumn(lb_Title, 0);

                //警告内容
                if (info.Length > 0)
                {
                    Label lb_Info = new Label
                    {
                        AutoSize = true,
                        Anchor = AnchorStyles.Left,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.TopLeft,
                        Text = info,
                        Font = new Font(flp_Panel.Font.FontFamily, 20)
                    };
                    tlp_Panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                    tlp_Panel.Controls.Add(lb_Info);
                    tlp_Panel.SetRow(lb_Info, rowIdx++);
                    tlp_Panel.SetColumn(lb_Info, 0);
                }

                //按键
                tlp_Panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
                tlp_Panel.Controls.Add(flp_Panel);
                tlp_Panel.SetRow(flp_Panel, rowIdx);
                tlp_Panel.SetColumn(flp_Panel, 0);

                Form fr = new Form
                {
                    Name = "LogSearch",
                    FormBorderStyle = FormBorderStyle.FixedSingle,
                    Width = 800,
                    Height = 600,
                    Text = @"",
                    ShowIcon = false,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    TopMost = true,
                    StartPosition = FormStartPosition.CenterScreen,
                    BackColor = bkColor


                };
                fr.Activate();
                fr.Controls.Add(tlp_Panel);
                fr.ShowDialog();
                return fr.DialogResult;
            }
        }
        int i = 0;
        public void btn_stop_Click(object sender, EventArgs e)
        {
            UpDownLoad.DelateImage();
            WSFEED.bquit = true;
            UpDownLoad.bquit = true;
            MT.OnlyLightOn(MT.GPIO_OUT_ALM_YELLOW);
            MT.GPIO_OUT_ALM_BEEPER.SetOff();
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "---停止键按下---");
            //   timer_key.Enabled = false;
            // VAR.gsys_set.bquit = true;
            if (VAR.gsys_set.status != EM_SYS_STA.RUN)
            {

                //VAR.gsys_set.bquit = true;
                if (VAR.gsys_set.status != EM_SYS_STA.RESET)
                {

                    UpDownLoad.UD1Stop();
                    UpDownLoad.UD2Stop();
                    UpDownLoad.LCStop();
                }
                else
                {
                    MT.AllAxStop();
                }
            }
            VAR.gsys_set.status = EM_SYS_STA.STANDBY;
            //}
            //VAR.gsys_set.status = EM_SYS_STA.STANDBY;
            //VAR.sys_inf.Set(EM_ALM_STA.NOR_BLUE, "就绪", 0, true);
        }

        delegate void UpdateCallback();
        void UpdateDisplay()
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            int n = 0;
            while (!IsHandleCreated)
            {
                //解决窗体关闭时出现“访问已释放句柄“的异常
                if (Disposing || IsDisposed)
                    return;
                Application.DoEvents();
                Thread.Sleep(1);
                if (n++ > 100) return;
            }
            if (InvokeRequired)//如果调用控件的线程和创建创建控件的线程不是同一个则为True
            {

                UpdateCallback d = new UpdateCallback(UpdateDisplay);
                BeginInvoke(d, new object[] { });
            }
            else
            {
                //VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "START");
                traybox_fd.UpdateShow();
                traybox_ok.UpdateShow();
                traybox_ng.UpdateShow();
                //VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "START1");
                tray_fd.tray_dat = traybox_fd.box.tray_cur;
                tray_ok.tray_dat = traybox_ok.box.tray_cur;
                tray_ng.tray_dat = traybox_ng.box.tray_cur;


                tray_fd.UpdateShow();
                tray_ok.UpdateShow();
                tray_ng.UpdateShow();
                //VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "START2");
                ws1.UpdateShow();
                ws2.UpdateShow();

                //DownLoadMd.UpdateShow();
                //UpLoadMd.UpdateShow();
                //Turnplate.UpdateShow();

                //VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "START3");
                //lb_leftbox.Text = COM.LeftLightBox.StrOfPos;
                //LightBox.PosDef posdef = COM.LeftLightBox.CurPosDef;
                //lb_lightbox_left_posref.Text = posdef != null ? posdef.Name : "未定义位置";

                //lb_rightbox.Text = COM.RightLightBox.StrOfPos;
                //posdef = COM.RightLightBox.CurPosDef;
                //lb_lightbox_right_posref.Text = posdef != null ? posdef.Name : "未定义位置";

                //VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "END");
            }
        }
        //public Task show;
        private bool colorchange = false;
        bool _bShowDialog = false;
        int NgReteTimes = 0;//良率报警累计
        bool jigSendState = false;
        bool IsCanClear = true;

        private void timer_500ms_Tick(object sender, EventArgs e)
        {
            //    BaseAction.bnotfeed = ckb_not_open.Checked;
            var CurHour = DateTime.Now.Hour;
            if(CurHour==9)
            {
                if(!jigSendState)
                {
                    Msg.secsManager.Send(new BaseInfo() { Id = 9, Value = PT_SET.bJigSan?"1":"0" });
                    jigSendState = true;
                }
                else
                {
                    jigSendState = false;
                }
            }

            UpdateDisplay();
            UpLoad1.UpdateShow(COM.UDLoad1);
            upLoad2.UpdateShow(COM.UDLoad2);
            timer_500ms.Interval = 500;
            if (ckb_Check.Checked)
            {
                VAR.gsys_set.mode = EM_SYS_MODE.CHK;
                if (0 < nud_TestCnt.Value && nud_TestCnt.Value < 6) VAR.ChkCnt = (int)nud_TestCnt.Value;
                else VAR.ChkCnt = 1;

            }
            else
            {
                VAR.gsys_set.mode = EM_SYS_MODE.NORMAL;
            }

            if (VAR.ClearMt)
            {
                btn_ClearMt.BackColor = Color.Cyan;
                btn_ClearMt.Text = VAR.IsChinese ? "-清料中-" : "-Clearing-";
            }
            else
            {
                btn_ClearMt.BackColor = SystemColors.ButtonFace;
                btn_ClearMt.Text = VAR.IsChinese ? "清料" : "Clear";
            }

            if (!ckb_wait_upload.Checked) UpDownLoad.bWaitforUpDownload = false;
            if (UpDownLoad.bWaitforUpDownload)
            {
                if (!colorchange) btn_upload_ok.BackColor = SystemColors.ButtonFace;
                else btn_upload_ok.BackColor = Color.Orange;
                colorchange = !colorchange;
                btn_upload_ok.Text = VAR.IsChinese ? "等待上料..." : "Wait for UpDwLoad";
            }
            else
            {
                colorchange = false;
                btn_upload_ok.BackColor = SystemColors.ButtonFace;
                btn_upload_ok.Text = VAR.IsChinese ? "上料完成" : "UpDwLoad finished";
            }

            if (PT_SET.RunPattern == (int)PT_SET.RUN_PATTERN.RUN_NORMAL && PT_SET.bGrrFlow) lb_mod.Text = VAR.IsChinese ? "<正常GRR测试>" : "Normal GRR";
            else if (PT_SET.RunPattern != (int)PT_SET.RUN_PATTERN.RUN_NORMAL && PT_SET.bGrrFlow) lb_mod.Text = VAR.IsChinese ? "<空跑GRR测试>" : "Empty GRR";
            else if (PT_SET.RunPattern == (int)PT_SET.RUN_PATTERN.RUN_NORMAL && VAR.gsys_set.mode == EM_SYS_MODE.CHK) lb_mod.Text = VAR.IsChinese ? "<正常点检模式>" : "Normal Chk";
            else if (PT_SET.RunPattern != (int)PT_SET.RUN_PATTERN.RUN_NORMAL && VAR.gsys_set.mode == EM_SYS_MODE.CHK) lb_mod.Text = VAR.IsChinese ? "<空跑点检模式>" : "UpDwLoad Chk";
            else if (PT_SET.RunPattern == (int)PT_SET.RUN_PATTERN.RUN_NORMAL) lb_mod.Text = VAR.IsChinese ? "<正常生产模式>" : "Normal";
            else if (PT_SET.RunPattern == (int)PT_SET.RUN_PATTERN.RUN_UPDW) lb_mod.Text = VAR.IsChinese ? "<有料空跑模式>" : "UpDwLoad";
            else if (PT_SET.RunPattern == (int)PT_SET.RUN_PATTERN.RUN_EMPTY) lb_mod.Text = VAR.IsChinese ? "<无料空跑模式>" : "Empty UpDwLoad";
            lb_tt_n.Text = WSFEED.n.ToString();
            lb_tt_ct.Text = WSFEED.ct.ToString("000.0s");
            if (PT_SET.GRRWSID == 1) lb_cur_grrtestcnt.Text = COM.wsfeed1.GrrTestCnt.ToString();
            if (PT_SET.GRRWSID == 1) lb_cur_grrudlcnt.Text = COM.wsfeed1.GrrUDLcnt.ToString();
            if (PT_SET.GRRWSID == 2) lb_cur_grrtestcnt.Text = COM.wsfeed2.GrrTestCnt.ToString();
            if (PT_SET.GRRWSID == 2) lb_cur_grrudlcnt.Text = COM.wsfeed2.GrrUDLcnt.ToString();
            lb_set_grrtestcnt.Text = PT_SET.GRRTestCnt.ToString();
            lb_set_grrudlcnt.Text = PT_SET.GRRUdlCnt.ToString();
            tb_LasterrTime.Text = COUNT_DATA.errtemp.ToString();
            if (VAR.gsys_set.status == EM_SYS_STA.RUN)
            {
                rbtn_NormalProduct.Enabled = false;
                rbtn_RetestProduct.Enabled = false;
                lb_tt_tmr1.Text = PT_SET.testimefeed1.ToString("000.0s");
                lb_tt_tmr2.Text = PT_SET.testimefeed2.ToString("000.0s");
                ////COUNT_DATA.runtime++;
            }

            else
            {
                rbtn_NormalProduct.Enabled = true;
                rbtn_RetestProduct.Enabled = true;
                lb_tt_tmr1.Text = "000.0s";

            }

            TimeSpan ts1 = new TimeSpan(COUNT_DATA.beforerrtemp.Ticks);
            TimeSpan ts2 = new TimeSpan(COUNT_DATA.errtemp.Ticks);
            TimeSpan ts3 = ts1.Subtract(ts2).Duration();
            tb_err_time.Text = string.Format("{0:000}H{1:00}M", ts3.Hours, ts3.Minutes);

            double min = COUNT_DATA.runtime;
            min = min / 60;
            tb_run_time.Text = string.Format("{0:000}H{1:00}M", Math.Floor(min / 60), min % 60);
            min = COUNT_DATA.waittime;
            min = min / 60;
            tb_stop_time.Text = string.Format("{0:000}H{1:00}M", Math.Floor(min / 60), min % 60);
            min = COUNT_DATA.waitwltime;
            min = min / 60;
            tb_waitwl_time.Text = string.Format("{0:000}H{1:00}M", Math.Floor(min / 60), min % 60);
            min = COUNT_DATA.runtime + COUNT_DATA.waittime;
            if (min == 0) min = 1;
            min = 1 - COUNT_DATA.waittime / min;
            min = min * 100;
            tb_Activation.Text = min.ToString("f2") + "%";

            tb_LastClearTime.Text = COUNT_DATA.dt.ToString();
            tb_LastClearuser.Text = COUNT_DATA.clearuser.ToString();
            double all_cnt = COUNT_DATA.allcnt[0] + COUNT_DATA.allcnt[1];
            double ok_cnt = COUNT_DATA.okcnt[0] + COUNT_DATA.okcnt[1];
            double ng_cnt = COUNT_DATA.ngcnt[0] + COUNT_DATA.ngcnt[1];
            double hour = COUNT_DATA.runtime + COUNT_DATA.waittime;
            double uph = all_cnt / (hour == 0 ? 0.001 : hour) * 3600;


            if (COUNT_DATA.OkTwoTestCnt > 50000 || COUNT_DATA.NgTwoTestCnt > 50000)
            {
                COUNT_DATA.Clear();
            }
            var curHour = DateTime.Now;
            if (curHour.Hour == 8 && IsCanClear)//|| curHour.Hour == 20
            {
                IsCanClear = false;
                COUNT_DATA.Clear();
            }

            if (curHour.Hour == 9)
            {
                IsCanClear = true;
            }

            double ok_cntNormal = ok_cnt;
            double ng_cntNormal = ng_cnt;
            double uphNormal = (ok_cntNormal + ng_cntNormal) / (hour == 0 ? 0.001 : hour) * 3600;
            double all_cntNornmal = ok_cntNormal + ng_cntNormal;


            if (tb_all_cnt.Text != all_cnt.ToString("f0") || tb_ok_cnt.Text != ok_cnt.ToString("f0") ||
                tb_uph.Text != uph.ToString("f0") || lastngcnt != ng_cnt)
            {
                //Msg.secsManager.Send((new BaseInfo() { Id = 3, Value = all_cnt.ToString("f0") }));
                //Msg.secsManager.Send((new BaseInfo() { Id = 4, Value = ok_cnt.ToString("f0") }));
                //Msg.secsManager.Send((new BaseInfo() { Id = 5, Value = ng_cnt.ToString("f0") }));
                //Msg.secsManager.Send((new BaseInfo() { Id = 6, Value = uph.ToString("f0") }));
                //Msg.secsManager.Send(new BaseInfo() { Id = 2 }, TypeId: 2);

                Msg.secsManager.Send((new BaseInfo() { Id = 3, Value = all_cntNornmal.ToString("f0") }));
                Msg.secsManager.Send((new BaseInfo() { Id = 4, Value = ok_cntNormal.ToString("f0") }));
                Msg.secsManager.Send((new BaseInfo() { Id = 5, Value = ng_cntNormal.ToString("f0") }));
                Msg.secsManager.Send((new BaseInfo() { Id = 6, Value = uphNormal.ToString("f0") }));
                if (ok_cntNormal >= 0)
                    Msg.secsManager.Send((new BaseInfo() { Id = 10, Value = ok_cntNormal.ToString("f0") }));
                if (ng_cntNormal >= 0)

                {
                    Msg.secsManager.Send((new BaseInfo() { Id = 11, Value = ng_cntNormal.ToString("f0") }));
                    Msg.secsManager.Send((new BaseInfo() { Id = 12, Value = uphNormal.ToString("f0") }));
                }
                Msg.secsManager.Send(new BaseInfo() { Id = 2 }, TypeId: 2);

            }

            lastngcnt = COUNT_DATA.ngcnt[0] + COUNT_DATA.ngcnt[1];

            tb_uph.Text = uph.ToString("f0");
            tb_all_cnt.Text = all_cnt.ToString("f0");
            tb_ok_cnt.Text = ok_cnt.ToString("f0");
            double ok_rate = ok_cnt / (ok_cnt + ng_cnt != 0 ? ok_cnt + ng_cnt : 1) * 100;
            tb_ok_rate.Text = ok_rate.ToString("f2") + "%";
            double openimage_rate = 0;
            if (ok_cnt + ng_cnt != 0)
                openimage_rate = (1 - COUNT_DATA.openimagerate / (ok_cnt + ng_cnt)) * 100;
            else openimage_rate = 0;
            tb_OpenImageRate.Text = openimage_rate.ToString("f2") + "%";
            double suction_rate = 0;
            double err_rate = 0;
            suction_rate = COUNT_DATA.SuctionErrcnt;
            err_rate = COUNT_DATA.Errcnt;
            suction_rate = suction_rate / (all_cnt != 0 ? all_cnt : 1) * 100;
            err_rate = err_rate / (all_cnt != 0 ? all_cnt : 1) * 100;
            tb_SuctionErrRate.Text = suction_rate.ToString("f2") + "%";
            tb_ErrRate.Text = err_rate.ToString("f2") + "%";

            if (PT_SET.bNgWarn && !_bShowDialog && ok_rate <= PT_SET.OkRate && (ok_cnt + ng_cnt) > 150)//数量大于150才统计不良
            {
                if (NgReteTimes == 0)
                {
                    NgReteTimes++;
                    _bShowDialog = true;
                    double ng_image_rate = COUNT_DATA.cnt_ng_image / (ok_cnt + ng_cnt != 0 ? ok_cnt + ng_cnt : 1);
                    double ng_iic_rate = COUNT_DATA.cnt_ng_iic / (ok_cnt + ng_cnt != 0 ? ok_cnt + ng_cnt : 1);
                    double ng_exposure_rate = COUNT_DATA.cnt_ng_exposure / (ok_cnt + ng_cnt != 0 ? ok_cnt + ng_cnt : 1);
                    double ng_miss_pix_rate = COUNT_DATA.cnt_ng_miss_pix / (ok_cnt + ng_cnt != 0 ? ok_cnt + ng_cnt : 1);
                    double ng_OS_rate = COUNT_DATA.cnt_ng_OS / (ok_cnt + ng_cnt != 0 ? ok_cnt + ng_cnt : 1);
                    double ng_other_rate = COUNT_DATA.cnt_ng_other / (ok_cnt + ng_cnt != 0 ? ok_cnt + ng_cnt : 1);
                    string ng_msg = string.Format("\r\n 开图NG率:{0} \r\n IIC读写NG率:{1} \r\n 曝光NG率:{2} \r\n 图像丢帧率:{3}\r\n OS异常率:{4}\r\n 其他NG率: {5}",
                   ng_image_rate.ToString("P"), ng_iic_rate.ToString("P"), ng_exposure_rate.ToString("P"),
                   ng_miss_pix_rate.ToString("P"), ng_OS_rate.ToString("P"), ng_other_rate.ToString("P"));

                    DialogResult dr = Dialog(Color.Yellow, "警告", "当前良率低，请注意!可清零！" + ng_msg, "确定", "清除");
                    if (dr == DialogResult.Cancel)
                    {
                        DialogResult drq = Dialog(Color.Yellow, "警告", "清除后临时生产数据归零。", "确定", "取消");
                        if (dr == DialogResult.OK)
                        {
                            DRpt.Report_Opration(1000, 0, "归位按键按下!");
                            COUNT_DATA.Clear();
                        }
                    }
                    _bShowDialog = false;
                }
                NgReteTimes++;
                if (NgReteTimes > 10)
                    NgReteTimes = 0;
            }
        }





        private void btn_upload_ok_Click(object sender, EventArgs e)
        {
            UpDownLoad.ud1needwait = false;
            UpDownLoad.ud1ws3needwait = false;
            UpDownLoad.ud2ws2needwait = false;
            int num = 10000, num_1 = 10000, num_2 = 10000, num_3 = 10000, num_4 = 10000;
            //SQLData.TesterrDataAdd(COM.wsfeed1.id, 11, "夹具放料", string.Format("{0}需要进行二次下压", COM.wsfeed1.disc));
            // BaseAction.GetRunMode();
            //List<WSFEED.MdDat> WsPickPos = new List<WSFEED.MdDat>();
            //COM.UDLoad2.GetWsPosTeam(ref WsPickPos, COM.wsfeed1, Product.EM_CM_RES.OK, COM.UDLoad2.FindMod);
            //COM.UDLoad1.ChkNextProc(ref num, ref num_3, COM.wsfeed1, COM.UDLoad1.id);
        }

        private void btn_new_traybox_fd_Click(object sender, EventArgs e)
        {
            if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
            EM_RES res = COM.traybox_fd.NewBox();
            if (res == EM_RES.OK) COM.traybox_fd.IsReady = true;
            COM.traybox_fd.tray_idx = 0;
            if (DialogResult.No == MessageBox.Show("供料导轨上有料盘?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                COM.traybox_fd.tray_cur = null;
            }
            else
            {
                COM.traybox_fd.tray_cur = COM.traybox_fd.list_tray[0];
            }

        }

        private void btn_new_traybox_ok_Click(object sender, EventArgs e)
        {
            if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "更换OK料盒: " + COM.DescribeTrayBoxMapping(COM.traybox_ok));
            EM_RES res = COM.traybox_ok.NewBox();
            if (res == EM_RES.OK) COM.traybox_ok.IsReady = true;
            COM.traybox_ok.tray_idx = 0;
            if (DialogResult.No == MessageBox.Show("OK导轨上有料盘?\r\n" + COM.DescribeTrayBoxMapping(COM.traybox_ok), "", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                COM.traybox_ok.tray_cur = null;
            }
            else
            {
                COM.traybox_ok.tray_cur = COM.traybox_ok.list_tray[0];
            }
        }

        private void btn_new_traybox_ng_Click(object sender, EventArgs e)
        {
            if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "更换NG料盒: " + COM.DescribeTrayBoxMapping(COM.traybox_ng));
            EM_RES res = COM.traybox_ng.NewBox();
            if (res == EM_RES.OK) COM.traybox_ng.IsReady = true;
            COM.traybox_ng.tray_idx = 0;
            if (DialogResult.No == MessageBox.Show("NG导轨上有料盘?\r\n" + COM.DescribeTrayBoxMapping(COM.traybox_ng), "", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                COM.traybox_ng.tray_cur = null;
            }
            else
            {
                COM.traybox_ng.tray_cur = COM.traybox_ng.list_tray[0];
            }
        }

        private void timer_key_Tick(object sender, EventArgs e)
        {
        }

        private void btn_home_Click(object sender, EventArgs e)
        {

            if (VAR.gsys_set.status != EM_SYS_STA.RUN)
            {
                MT.SetAllAxToManualSpd();
                VAR.gsys_set.bquit = false;
                EM_RES res = MT.Move(ref VAR.gsys_set.bquit, ref COM.UDLoad1.ax_z, 0, ref COM.UDLoad2.ax_z, 0);
                if (res == EM_RES.OK)
                {
                    res = MT.ZupMove(ref VAR.gsys_set.bquit, ref COM.UDLoad1.ax_y, 0, ref COM.UDLoad2.ax_y, 0, ref COM.UDLoad1.ax_x, 0, ref COM.UDLoad2.ax_x, 0);
                    if (res == EM_RES.OK)
                    {
                        //   MT.Move(ref VAR.gsys_set.bquit, ref COM.UDLoad1.ax_x, 0, ref COM.UDLoad1.ax_y, 0, ref COM.UDLoad2.ax_x, 0, ref COM.UDLoad2.ax_y, 0);
                        //  MT.Move(ref VAR.gsys_set.bquit, ref COM.UDLoad1.ax_x, 0, ref COM.UDLoad2.ax_x, 0);
                    }
                }
            }
        }

        private void btn_ClearMt_Click(object sender, EventArgs e)
        {

        }

        private void btn_pause_Click(object sender, EventArgs e)
        {
            WSFEED.bquit = true;
            UpDownLoad.bquit = true;
            MT.GPIO_OUT_ALM_BEEPER.SetOff();
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "---停止键按下---");
            //   timer_key.Enabled = false;
            // VAR.gsys_set.bquit = true;
            if (VAR.gsys_set.status != EM_SYS_STA.RUN)
            {

                //VAR.gsys_set.bquit = true;
                if (VAR.gsys_set.status != EM_SYS_STA.RESET)
                {

                    UpDownLoad.UD1Stop();
                    UpDownLoad.UD2Stop();
                    UpDownLoad.LCStop();
                }
                else
                {
                    MT.AllAxStop();
                }
            }
        }

        private void btn_SetZero_Click(object sender, EventArgs e)
        {
            COUNT_DATA.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UpDownLoad.wsfeed2cangotest = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UpDownLoad.wsfeed1cangotest = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            COM.wsfeed1.FeedStatus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            COM.wsfeed2.FeedStatus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (DialogResult.No == MessageBox.Show("是否清料？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question)) return;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("清料"));
            VAR.ClearMt = true;
            UpDownLoad.wsfeedclear = COM.wsfeed1;
            if (VAR.gsys_set.status == EM_SYS_STA.REPAIR)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("提示:当前设备正在维修，无法运行!"));
                return;
            }
            //检测复位状态
            foreach (CARD card in MT.CardList)
            {
                if (card.isReady == false)
                {
                    MessageBox.Show(string.Format("{0}未初始化!", card.disc), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                foreach (AXIS ax in card.AxList)
                {
                    if (ax.home_status != AXIS.HOME_STA.OK)
                    {
                        MessageBox.Show(string.Format("{0} 状态异常，{1}!\r\n请先复位", ax.disc, ax.home_status), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            VAR.gsys_set.bquit = false;
            UpDownLoad.bquit = false;
            //UpDownLoad.RunClearTask();
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "清料线程启动");
            VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, "清料", 0, true);
            MT.OnlyKeyLOn(MT.GPIO_OUT_KL_START);
            MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // int num = 10000, num_1 = 10000, num_3 = 10000;
            // // UpDownLoad.ud1needwait = true;         
            //COM.UDLoad1.SafeProtect(ref WSFEED.bquit);
            //BaseAction.GetRunMode();
            //COM.UDLoad1.ChkNextProc(ref num, ref num_3, COM.wsfeed1, 0);
            List<PosInf> ls_pos = COM.traybox_fd.tray_cur.GetPosListOrder(Product.EM_CM_RES.UNTEST);
            UpDownLoad.ud2needwait = false;
            UpDownLoad.ud1needwait = false;
            UpDownLoad.ud1ws3needwait = false;
            UpDownLoad.ud2ws2needwait = false;
        }

        private void updatecheck_Click(object sender, EventArgs e)
        {
            if (FrMain.frsuser == null || FrMain.frsuser.user1.cur_user.name == null)
            {
                MessageBox.Show("未登陆，不允许清零");
            }
            else
            {
                DialogResult result = MessageBox.Show("按是选择点检模式①:1、3工位，按否选择点检模式②:1-4工位，按取消则取消屏蔽并上传点检完毕数据", "点检上传", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes)
                {

                    foreach (WSFEED ws in COM.list_wsfeed)
                    {
                        foreach (WSFEED.MdDat md in ws.list_md)
                        {
                            if (md.Num == 1 || md.Num == 3)
                                md.benable = true;
                            else md.benable = false;
                        }
                        ws.SaveCfg(VAR.gsys_set.cur_product_name);
                        ws.LoadCfg(VAR.gsys_set.cur_product_name);
                    }
                    DRpt.Report_Opration(2000, 0, string.Format("开始点检,点检人:{0}", FrMain.frsuser.user1.cur_user.name));
                }
                if (result == DialogResult.No)
                {

                    foreach (WSFEED ws in COM.list_wsfeed)
                    {
                        foreach (WSFEED.MdDat md in ws.list_md)
                        {
                            if (md.Num <= 4 && md.Num >= 1)
                                md.benable = true;
                            else md.benable = false;
                        }
                        ws.SaveCfg(VAR.gsys_set.cur_product_name);
                        ws.LoadCfg(VAR.gsys_set.cur_product_name);
                    }
                    DRpt.Report_Opration(2000, 0, string.Format("开始点检,点检人:{0}", FrMain.frsuser.user1.cur_user.name));
                }
                if (result == DialogResult.Cancel)
                {

                    foreach (WSFEED ws in COM.list_wsfeed)
                    {
                        foreach (WSFEED.MdDat md in ws.list_md)
                        {
                            md.benable = true;
                        }
                        ws.SaveCfg(VAR.gsys_set.cur_product_name);
                        ws.LoadCfg(VAR.gsys_set.cur_product_name);
                    }
                    DRpt.Report_Opration(2000, 0, string.Format("点检完毕,点检人:{0}", FrMain.frsuser.user1.cur_user.name));
                }
            }

        }

        private void btn_SetZero_Click_1(object sender, EventArgs e)
        {
            if (FrMain.frsuser == null || FrMain.frsuser.user1.cur_user.name == null)
            {
                MessageBox.Show("未登陆，不允许清零");
            }
            else
            {
                COUNT_DATA.clearuser = tb_LastClearuser.Text = FrMain.frsuser.user1.cur_user.name.ToString();
                DRpt.Report_Opration(1000, 0, "归位按键按下!");
                COUNT_DATA.Clear();
            }

        }

        private void btn_ClearMt_Click_1(object sender, EventArgs e)
        {
            if (VAR.gsys_set.status == EM_SYS_STA.RUN)
            {
                WSFEED.bquit = true;
                UpDownLoad.bquit = true;
                MT.GPIO_OUT_ALM_BEEPER.SetOff();
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "---清料时正在运行，停止键默认按下---");
                //   timer_key.Enabled = false;
                // VAR.gsys_set.bquit = true;
                if (VAR.gsys_set.status != EM_SYS_STA.RUN)
                {

                    //VAR.gsys_set.bquit = true;
                    if (VAR.gsys_set.status != EM_SYS_STA.RESET)
                    {

                        UpDownLoad.UD1Stop();
                        UpDownLoad.UD2Stop();
                        UpDownLoad.LCStop();
                    }
                    else
                    {
                        MT.AllAxStop();
                    }
                }
                Thread.Sleep(1000);
            }

            if (DialogResult.No == MessageBox.Show(VAR.IsChinese ? "是否清料？" : "Are you sure to clear?\r\n是否清料？", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                VAR.ClearMt = false;
                return;
            }
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("清料键按下") : "Clear   (清料键按下)");
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("清料") : "Clear   (清料)");
            VAR.ClearMt = true;
            COM.wsfeed1.isclearfinsh = false;
            COM.wsfeed2.isclearfinsh = false;
            if (VAR.gsys_set.status == EM_SYS_STA.REPAIR)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("提示:当前设备正在维修，无法运行!"));
                return;
            }
            //检测复位状态
            foreach (CARD card in MT.CardList)
            {
                if (card.isReady == false)
                {
                    MessageBox.Show(string.Format("{0}未初始化!", card.disc), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                foreach (AXIS ax in card.AxList)
                {
                    if (ax.home_status != AXIS.HOME_STA.OK)
                    {
                        MessageBox.Show(string.Format("{0} 状态异常，{1}!\r\n请先复位", ax.disc, ax.home_status), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            //检测运行状态
            if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "---清料键按下---");
            btn_pause.BackColor = SystemColors.ButtonFace;
            VAR.gsys_set.bquit = false;
            WSFEED.bquit = false;
            UpDownLoad.bquit = false;
            WSFEED.bpause = false;
            foreach (WSFEED ws in COM.list_wsfeed)
            {
                ws.FeedStatus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD;
            }
            COM.UDLoad1.Proc = UpDownLoad.EM_STA.CHECK;
            COM.UDLoad2.Proc = UpDownLoad.EM_STA.CHECK;
            VAR.ClearMtcnt = 0;
            VAR.Canchange = true;
            BaseAction.run();

        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            //EM_RES res = MT.SetAllAxToWorkSpd();
            //while (true)
            //{
            //    MT.Move(ref VAR.gsys_set.bquit, ref COM.traybox_ng.ax_x, 1);
            //    Thread.Sleep(200);
            //    MT.Move(ref VAR.gsys_set.bquit, ref COM.traybox_ng.ax_x, 300);
            //}

        }

        private void button5_Click_2(object sender, EventArgs e)
        {
            WSFEED.bquit = true;
            UpDownLoad.bquit = true;
            MT.GPIO_OUT_ALM_BEEPER.SetOff();
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "---清料完状态清空键按下---");
            //   timer_key.Enabled = false;
            // VAR.gsys_set.bquit = true;
            if (VAR.gsys_set.status != EM_SYS_STA.RUN)
            {

                //VAR.gsys_set.bquit = true;
                if (VAR.gsys_set.status != EM_SYS_STA.RESET)
                {

                    UpDownLoad.UD1Stop();
                    UpDownLoad.UD2Stop();
                    UpDownLoad.LCStop();
                }
                else
                {
                    MT.AllAxStop();
                }
            }
            foreach (WSFEED ws in COM.list_wsfeed)
            {
                ws.FeedStatus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD;
            }
        }

        private void rbtn_NormalProduct_CheckedChanged(object sender, EventArgs e)
        {
            if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
            if (rbtn_NormalProduct.Checked)
            {
                VAR.Isnormal = true;
                VAR.bSameNGTip_Temp = true;
                //PT_SET.UpDownRunMode = (int)PT_SET.RUN_MD.BOTH_WORK;
                DRpt.Report_Opration(1000, 0, "正常品模式按键按下!");
            }
            else
            {
                VAR.Isnormal = false;
                //PT_SET.UpDownRunMode = (int)PT_SET.RUN_MD.ALONE_WORK;
                DRpt.Report_Opration(1000, 0, "复测品模式按键按下!");
                //if (DialogResult.No == MessageBox.Show(VAR.IsChinese ? "是否暂时取消同工位连续同NG报警?" : "Are you sure to temporarily cancel the continuous same-NG alarm at the same station?\r\n是否暂时取消同工位连续同NG报警?", VAR.IsChinese ? "提示" : "Prompt", MessageBoxButtons.YesNo, MessageBoxIcon.Question)) return;
                // VAR.bSameNGTip_Temp = false;
            }
        }

        private void button5_Click_3(object sender, EventArgs e)
        {
            UpDownLoad.bWaitforUpDownload = false;
            //COM.UDLoad1.upcam.SaveOriginImage(FrMain.frrun.cogDisplayer_run.cogRecordDisplay.Image, string.Format("{0}\\image\\{1}\\BACK", VAR.gsys_set.GetCurProductPath, COM.UDLoad1.upcam.mName), string.Format("{0}{1}.jpg",
            //                       DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.ToString("HHmmss_fff")));
        }

        Stopwatch stopwatch = new Stopwatch();
        bool show = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (VAR.gsys_set.status == EM_SYS_STA.STANDBY || COM.traybox_fd.ChgML || COM.traybox_ng.ChgML || COM.traybox_ok.ChgML)
            {
                show = false;
                stopwatch.Start();
            }
            else
            {
                stopwatch.Stop();
                show = true;
                var secondst = stopwatch.ElapsedMilliseconds / 1000;

                //if (secondst<=180 && secondst>5 && !PT_SET.Isshowmsgpre && !PT_SET.Isshowmsg)
                //{
                //    // 获取当前时间  
                //    DateTime now = DateTime.Now;

                //    if (secondst<=60)
                //    {
                //        double minute = (secondst / 60);
                //      // 创建一个表示1分钟的时间间隔  
                //      TimeSpan threeMinutes = TimeSpan.FromMinutes(minute);
                //        // 从当前时间中减去1分钟  
                //        DateTime threeMinutesAgo = now.Subtract(threeMinutes);
                //        PT_SET.WaitMode = 25;
                //        VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, string.Format("（微停机）待机上传开始事件，原因为{0}", PT_SET.WaitMode));
                //        Msg.secsManager.Send(new BaseInfo() { Id = 250, Value = (DateTime.Now - threeMinutesAgo).TotalSeconds.ToString("F2") });
                //    }
                //    else
                //    {
                //        double minute = (secondst / 60);
                //        // 创建一个表示3分钟的时间间隔  
                //        TimeSpan threeMinutes = TimeSpan.FromMinutes(minute);
                //        // 从当前时间中减去3分钟  
                //        DateTime threeMinutesAgo = now.Subtract(threeMinutes);
                //        PT_SET.WaitMode = 26;
                //        VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, string.Format("（微停机）待机上传开始事件，原因为{0}", PT_SET.WaitMode));
                //        Msg.secsManager.Send(new BaseInfo() { Id = 250, Value = (DateTime.Now - threeMinutesAgo).TotalSeconds.ToString("F2") });
                //    }
                //    Msg.secsManager.Send(new BaseInfo() { Id = 241, Value = PT_SET.WaitMode.ToString() });
                //    Msg.secsManager.Send(new BaseInfo() { Id = 242, Value = Encoding.UTF8.GetBytes(MesData.CobMesIdleStatusDict[PT_SET.WaitMode]).Aggregate("", (current, next) => current + " " + next).TrimStart() });
                //    Msg.secsManager.Send(new BaseInfo() { Id = 8 }, TypeId: 2);
                //    PT_SET.Isshowmsgpre = true;

                //    #region  待机细分

                //    if (PT_SET.Isshowmsgpre)
                //    {
                //        PT_SET.Isshowmsgpre = false;
                //        PT_SET.WaitMode = PT_SET.WaitMode == 0 ? 26 : PT_SET.WaitMode;
                //        Msg.secsManager.Send(new BaseInfo() { Id = 241, Value = PT_SET.WaitMode.ToString() });
                //        Msg.secsManager.Send(new BaseInfo() { Id = 242, Value = Encoding.UTF8.GetBytes(MesData.CobMesIdleStatusDict[PT_SET.WaitMode]).Aggregate("", (current, next) => current + " " + next).TrimStart() });
                //        Msg.secsManager.Send(new BaseInfo() { Id = 9 }, TypeId: 2);
                //        VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, string.Format("（微停机）待机上传结束事件，原因为{0}", PT_SET.WaitMode));
                //        Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.Run).ToString() });
                //        Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
                //    }

                //    #endregion
                //}

                stopwatch.Reset();
            }

            double seconds = stopwatch.ElapsedMilliseconds / 1000;

            if (seconds > 180)
            {
                stopwatch.Stop();

                if (!show && !PT_SET.Isshowmsg)
                {
                    DRpt.Report_Status(DReport.EmStatus.Wait, DReport.EmHareware.Null, DReport.EmStatus.Wait.GetDescription(VAR.IsChinese));
                    Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.Wait).ToString() });
                    Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "DReport.EmStatus.Wait1261");
                    // 获取当前时间  
                    DateTime now = DateTime.Now;
                    // 创建一个表示3分钟的时间间隔  
                    TimeSpan threeMinutes = TimeSpan.FromMinutes(3);
                    // 从当前时间中减去3分钟  
                    DateTime threeMinutesAgo = now.Subtract(threeMinutes);

                    Msg.secsManager.Send(new BaseInfo() { Id = 240, Value = threeMinutesAgo.ToString("yyyy-MM-dd HH:mm:ss") });
                    show = true;
                    COM.mesWaitWarning.WindowState = System.Windows.WindowState.Maximized; //设置全屏
                    PT_SET.WaitMode = COM.mesWaitWarning.OnShowDialog();

                    show = false;
                    stopwatch.Reset();
                    if (PT_SET.WaitMode == -1) return;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, string.Format("待机上传开始事件，原因为{0}", PT_SET.WaitMode));
                    Msg.secsManager.Send(new BaseInfo() { Id = 250, Value = (DateTime.Now - threeMinutesAgo).TotalSeconds.ToString("F2") });
                    Msg.secsManager.Send(new BaseInfo() { Id = 241, Value = PT_SET.WaitMode.ToString() });
                    Msg.secsManager.Send(new BaseInfo() { Id = 242, Value = Encoding.UTF8.GetBytes(MesData.CobMesIdleStatusDict[PT_SET.WaitMode]).Aggregate("", (current, next) => current + " " + next).TrimStart() });
                    Msg.secsManager.Send(new BaseInfo() { Id = 8 }, TypeId: 2);
                    PT_SET.Isshowmsg = true;
                }
            }
        }
    }
}
