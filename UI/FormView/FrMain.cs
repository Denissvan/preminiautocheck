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
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using UI.Compment;
using DevReport;
using Win32Lib;
using System.Diagnostics;
using UI.Class;

namespace UI
{
    public partial class FrMain : Form
    {
        public static FrCount frcount = new FrCount();
        public static FrSys frsys = new FrSys();
        public static FrRun frrun = new FrRun();
        public static FrProduct frproduct = new FrProduct();
        public static FrUser frsuser = new FrUser();
        public static FrRst frrst = new FrRst();
        public static FrMain frmain = null;
        public delegate void ShowString(string a);
        public string ReadStr1 = "";
        public string ReadStr2 = "";
        public void ShowTxt(string a)
        {
           // VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, string.Format("收到串口{0} 发来的消息{1}！", VAR.gsys_set.str_w1_com, a));
        }
        public void ShowTxtB(string a)
        {
            //VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, string.Format("收到串口{0} 发来的消息{1}！", (VAR.gsys_set.str_w2_com), a));
        }
        ShowString AA;
        ShowString AB;
        /// <summary>
        /// 接收字符串存储
        /// </summary>
        string ReadStrport1 = "";
        string ReadStrport2 = "";
        private DateTime preMiniAutoCheckLastScheduleCheck;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                //cp.Style |= 0x100000;
                //cp.Style |= 0x200000;
                //cp.Style |= 0x00000040;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        public FrMain()
        {
            InitializeComponent();
            frmain = this;
            timer_sysTimeCnt.Interval = 1000;
            timer_sysTimeCnt.Tick += timer_sysTimeCnt_Tick;
            preMiniAutoCheckLastScheduleCheck = DateTime.Now;
            AA = new ShowString(ShowTxt);
            AB = new ShowString(ShowTxtB);
            Msg.secsManager.Send(new BaseInfo() { Value = Path.GetFullPath("..") + @"\product" }, TypeId: 3);
            Msg.secsManager.ReceiveMsg += msg =>
            {
                if (msg.TypeId == 13)
                {
                    UpDownLoad.GetAllJigData();//发送一次夹具生产数据  

                    //foreach (var ws in COM.list_ws)
                    //{
                    //    foreach (var md in ws.list_md)
                    //    {

                    //        md.cnt_ok = 0; md.cnt_ng_image = 0; md.cnt_ng_OS = 0; md.
                    //              cnt_ng_miss_pix = 0; md.cnt_ng_exposure = 0; md.cnt_ng_iic = 0; md.cnt_ng_other = 0;
                    //    }
                    //    ws.SaveCfg();
                    //}
                }
                else
                if (msg.TypeId == 3)
                {
                    Msg.secsManager.Send(new BaseInfo() { Value = Path.GetFullPath("..") + @"\product" }, TypeId: 3);
                }
                else if (msg.TypeId == 4)
                {

                    //if (msg.Infos.Count() != 2) return;
                    var result = 0;
                    //ret结果说明，0为成功
                    //0 - ok, completed 已完成
                    //1 - invalid command 无效的命令
                    //2 - cannot do now 现在不能做
                    //3 - parameter error 参数错误
                    //4 - initiated for asynchronous completion 以异步启动完成
                    //5 - rejected, already in desired condition 拒绝，已处于所需的状态
                    //6 - invalid object 无效对象

                    if (msg.Infos[0].Value.ToLower() == "start")
                    {
                        frrun.btn_run_Click(null, null);
                        //开始操作
                        result = 4;
                    }
                    else if (msg.Infos[0].Value.ToLower() == "stop")
                    {
                        //停止操作
                        frrun.btn_stop_Click(null, null);
                        result = 4;
                    }
                    else if (msg.Infos[0].Value.ToLower() == "startcheck")
                    {
                        if (Convert.ToInt32(msg.Infos[2].Value) == 0)
                            MT.IsAllowStart = false;
                        else if (Convert.ToInt32(msg.Infos[2].Value) == 1)
                            MT.IsAllowStart = true;

                        MT.IsAllowStartUpdate = true;
                        result = 0;
                    }
                    else if (msg.Infos[0].Value.ToLower() == "stripstartcheck")
                    {
                        //停止操作
                        result = 0;
                        if (msg.Infos[2].Value == "1")
                        {
                            //允许
                            MT.IsAllowPointStart = true;   
                            MT.IsAllowPointStartUpdate = true;
                        }
                        else if (msg.Infos[2].Value == "0")
                        {
                            //不允许，停止加工，重新开始
                            MT.IsAllowPointStart = false;
                            MT.IsAllowPointStartUpdate = true;
                        }
                    }

                    List<BaseInfo> msgs = new List<BaseInfo>();
                    msgs.Add(new BaseInfo() { Id = 1, Value = $"{result}" });
                    msgs.Add(new BaseInfo() { Id = 2, Value = msg.Infos[1].Value });
                    Msg.secsManager.SendRange(msgs, TypeId: 4);
                }
            };
        }

        private void hook_KeyUp(object sender, KeyEventArgs e)
        {
            //if (Control.IsKeyLocked(Keys.NumLock) == true) return;
            if (VAR.gsys_set.status != EM_SYS_STA.STANDBY && VAR.gsys_set.status != EM_SYS_STA.PAUSE) return;

            if (e.KeyData == Keys.Add)
            {
                if (tsc_step_sel.SelectedIndex < (tsc_step_sel.Items.Count - 1)) tsc_step_sel.SelectedIndex++;
                e.Handled = true;
            }
            else if (e.KeyData == Keys.Subtract)
            {
                if (tsc_step_sel.SelectedIndex > 0) tsc_step_sel.SelectedIndex--;
                e.Handled = true;
            }

            if ((e.KeyData == Keys.Left) || (e.KeyData == Keys.Right) || (e.KeyData == Keys.Up) || (e.KeyData == Keys.Down) || (e.KeyData == Keys.PageUp) || (e.KeyData == Keys.PageDown))
            {
                //MT.AXIS_X.JOG_Stop();
                //MT.AXIS_Y.JOG_Stop();
                //MT.AXIS_R.JOG_Stop();
                e.Handled = true;
            }
        }

        private void btn_quit_Click(object sender, EventArgs e)
        {
            MT.OnlyLightOn(null);
            MT.Close();
            Close();
        }
        private Form FindForm(string formName)
        {
            foreach (Form form in Application.OpenForms)//获得所有打开的窗体
            {
                if (form.Name == formName)
                {
                    return form;
                }
            }
            return null;
        }

        public void form_sel(string btn_name, string page_name="", string page_name2 = "")
        {
            //    if (frrun != null) frrun.timer_update.Enabled = false;
            //    if (frsys != null) frsys.timer_update.Enabled = false;

            Font ft = new Font("Microsoft Sans Serif", 18, FontStyle.Bold);
            rbtn_run.Font = ft;
            rbtn_product.Font = ft;
            rbtn_sys.Font = ft;
            rbtn_count.Font = ft;

            rbtn_run.ForeColor = Color.DarkGray;
            rbtn_product.ForeColor = Color.DarkGray;
            rbtn_sys.ForeColor = Color.DarkGray;
           rbtn_count.ForeColor = Color.DarkGray;

            rbtn_run.BackColor = Color.Transparent;
            rbtn_product.BackColor = Color.Transparent;
            rbtn_sys.BackColor = Color.Transparent;
            rbtn_count.BackColor = Color.Transparent;

            Form form = null;
            ft = new Font("Microsoft Sans Serif", 22, FontStyle.Bold);

            //if (frsys == null) frsys = new FrSys();
            if (frsys != null) frsys.bupdate = false;
            //if (frrst == null) frrst = new FrRst();
            if (frrst != null) frrst.bupdate = false;
            //if (frproduct == null) frproduct = new FrProduct();
            if (frproduct != null) frproduct.bupdate = false;
            //if (frrun == null) frrun = new FrRun();
            if (frrun != null) frrun.bupdate = false;

            switch (btn_name)
            {
                
                case "rbtn_run":
                    rbtn_run.Checked = true;
                    rbtn_run.ForeColor = Color.WhiteSmoke;
                    rbtn_run.Font = ft;
                    if (frrun == null) frrun = new FrRun();                
                    form = frrun;
                    frrun.bupdate = true;
                    foreach (Cam cam in COM.ListCam) cam.mCogRecDisplay = frrun.cogDisplayer_run.cogRecordDisplay;
                    break;
                case "rbtn_product":
                    rbtn_product.Checked = true;
                    rbtn_product.ForeColor = Color.WhiteSmoke;
                    rbtn_product.Font = ft;
                    if (frproduct == null) frproduct = new FrProduct();
                    form = frproduct;
                    frproduct.bupdate = true;
                     foreach (Cam cam in COM.ListCam) cam.mCogRecDisplay = frproduct.cogDisplayer_product.cogRecordDisplay;

                    break;
                case "rbtn_count":
                    rbtn_count.Checked = true;
                    rbtn_count.ForeColor = Color.WhiteSmoke;
                    rbtn_count.Font = ft;
                    if (frcount == null) frcount = new FrCount();
                    form = frcount;
                    break;
                case "rbtn_sys":
                    rbtn_sys.Checked = true;
                    rbtn_sys.ForeColor = Color.WhiteSmoke;
                    rbtn_sys.Font = ft;
                    if (frsys == null) frsys = new FrSys();
                    form = frsys;
                    frsys.bupdate = true;
                    foreach (Cam cam in COM.ListCam) cam.mCogRecDisplay = frsys.CogRecordDisplay_sys.cogRecordDisplay;

                    break;
                case "rbtn_user":
                    rbtn_user.Checked = true;
                    rbtn_user.ForeColor = Color.WhiteSmoke;
                    rbtn_user.Font = ft;
                    if (frsuser == null) frsuser = new FrUser();
                    form = frsuser;
                    break;

                case "rbtn_rst":
                    rbtn_rst.Checked = true;
                    rbtn_rst.ForeColor = Color.WhiteSmoke;
                    rbtn_rst.Font = ft;
                    if (frrst == null) frrst = new FrRst();
                    form = frrst;
                    frrst.bupdate = true;
                    break;
            }

            if (form == null) return;
            pnl_sub.Controls.Clear();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            pnl_sub.Controls.Add(form);
            form.Left = 0;
            form.Top = 0;
            form.Width = pnl_sub.Width;
            form.Height = pnl_sub.Height - 8;
            form.Show();
        }

        void LoadForm(object sender, EventArgs e)
        {
            //这里是不同的分辨率设置控件的显示
            this.Tag = 1301 + "," +1024;//1586,1024
            string[] tmp = ((Form)sender).Tag.ToString().Split(',');
            float width = (float)((Form)sender).Width / (float)Convert.ToInt16(tmp[0]);
            float heigth = (float)((Form)sender).Height / (float)Convert.ToInt16(tmp[1]);

            ((Form)sender).Tag = ((Form)sender).Width.ToString() + "," + ((Form)sender).Height;

            foreach (Control control in ((Form)sender).Controls)
            {
                control.Scale(new SizeF(width, heigth));

            }

        }
        //这里是让所有的控件随窗体的变化而变化
        public void AutoScale(Form frm)
        {
            frm.Tag = frm.Width.ToString() + "," + frm.Height.ToString();
            frm.SizeChanged += new EventHandler(Form1_SizeChanged);
        }
        void Form1_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                tmp = ((Form)sender).Tag.ToString().Split(',');
                if ((int)((Form)sender).Width > 200)
                {
                    float width = (float)((Form)sender).Width / (float)Convert.ToInt16(tmp[0]);
                    float heigth = (float)((Form)sender).Height / (float)Convert.ToInt16(tmp[1]);

                    ((Form)sender).Tag = ((Form)sender).Width.ToString() + "," + ((Form)sender).Height;

                    foreach (Control control in ((Form)sender).Controls)
                    {
                        control.Scale(new SizeF(width, heigth));
                    }
                }
            }
            catch
            {

                // log your exception here

            }
        }

        private void FrMain_Load(object sender, EventArgs e)
        {
            LoadForm(sender, e);
            // do some other local here for you application
            AutoScale(this);
            EM_RES ret;
            Msg.secsManager.Start();
            VAR.msg.ShowMsgCfg(1000, (Msg.EM_MSGTYPE)0xffff);
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "系统启动...");
            lbVer.Text = string.Format("Pre-mini   V{0}", Assembly.GetExecutingAssembly().GetName().Version);
           // lbVer.Text = string.Format("Pre-Mini   V{0}.{1}", Assembly.GetExecutingAssembly().GetName().Version.Major, Assembly.GetExecutingAssembly().GetName().Version.Minor);
            Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.StartUp).ToString() });
            Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "DReport.EmStatus.StartUp362");
            //load sys config
            VAR.gsys_set.LoadSysCfg();
            VAR.gsys_set.status = EM_SYS_STA.UNKOWN;
            VAR.gsys_set.bclose = false;

            VAR.sys_inf.Set(EM_ALM_STA.WAR_YELLOW_FLASH, "正在加载", 2, true);

            //加载产品
            try
            {
                if (COM.product == null) COM.product = new Product();
                foreach (WSFEED wsfeed in COM.list_wsfeed)
                {
                    wsfeed.LoadCfg(VAR.gsys_set.cur_product_name);
                    wsfeed.LoadCfgws(VAR.gsys_set.cur_product_name);
                }
                //加载吸头
                COM.XtInit(VAR.gsys_set.cur_product_name);
                //加载仓储
                COM.TrayBoxInit();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            //硬件初始化
            Task TaskHWInit = new Task(() =>
            {
                
                ret = MT.Init(Path.GetFullPath("..") + "\\syscfg\\");
                if (ret != EM_RES.OK) VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "板卡初始化失败!");
                else VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "板卡初始化成功!");
            }
            );
            TaskHWInit.Start();
             ret =COM.CamInit();
             if (ret != EM_RES.OK) VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "相机初始化失败!");
             else VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "相机初始化成功!");
            Task TaskTrayInit = new Task(() =>
                {
                    COM.tray_fd = new Product.Tray(COM.traybox_fd.strCfgPath, 0);
                    COM.tray_ok = new Product.Tray(COM.traybox_ok.strCfgPath, 0);
                    COM.tray_ng = new Product.Tray(COM.traybox_ng.strCfgPath, 0);
                }
            );
            TaskTrayInit.Start();
            bool res1 = false;
            bool res2 = false;
            COM.wsfeed1.testprostaus = WSFEED.EM_PC_STA.READY;
            COM.wsfeed2.testprostaus = WSFEED.EM_PC_STA.READY;
            //上下料模块
            COM.UDLoad1 = new UpDownLoad(0, "上下料1", ref COM.CamDw1, ref COM.CamUp1, ref MT.AXIS_UDL1_X, ref MT.AXIS_UDL1_Y,
            ref MT.AXIS_UDL1_Z, ref MT.AXIS_UDL1_U1, ref MT.AXIS_UDL1_U2, ref COM.ListXT1);

            COM.UDLoad2 = new UpDownLoad(1, "上下料2", ref COM.CamDw2, ref COM.CamUp2, ref MT.AXIS_UDL2_X, ref MT.AXIS_UDL2_Y,
            ref MT.AXIS_UDL2_Z, ref MT.AXIS_UDL2_U1, ref MT.AXIS_UDL2_U2, ref COM.ListXT2);

            COM.List_UDLoad = new List<UpDownLoad>() { COM.UDLoad1, COM.UDLoad2 };

            UpDownLoad.DisAxisX = Math.Abs(COM.xt1.st_pos_samevs[0].x) + Math.Abs(COM.xt1.st_pos_samevs[1].x);
            if (UpDownLoad.DisAxisX < 400)
                UpDownLoad.DisAxisX = 400;
            PT_SET.LoadPtCfg(VAR.gsys_set.cur_product_name);           
            Application.DoEvents();
            Thread.Sleep(10);
            timer_reconnect.Enabled = true;
            if (frrun != null) frrun.bupdate = true;  

            VAR.sys_inf.Set(EM_ALM_STA.WAR_YELLOW_FLASH, "待回零", 10, true);

            if (MT.isReady)
            {

            }
            MT.SetSafeFunc();

            MT.OnlyKeyLOn(MT.GPIO_OUT_KL_STOP);
            //clear data
            Utility.CatchDiskAndDeleteFile(null, Path.GetFullPath("..") + "\\log\\", -30);
            Utility.CatchDiskAndDeleteFile(null, Path.GetFullPath("..") + "\\CsvData\\", -5);
            foreach (Cam cam in COM.ListCam)
            {
                Utility.CatchDiskAndDeleteFile(null, Path.GetFullPath("..") + "\\product\\" + VAR.gsys_set.cur_product_name + "\\Image\\"+cam.mName+"\\", -5,false);
            }
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "设备当前版本:"+lbVer.Text);
            DRpt.Report_Status(DReport.EmStatus.Unkown, DReport.EmHareware.Null, DReport.EmStatus.Unkown.GetDescription(VAR.IsChinese));
            COUNT_DATA.LoadCountCfg(VAR.gsys_set.cur_product_name);
            Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.Unkown).ToString() });
            Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
            //VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "DReport.EmStatus.Unkown453");
            //Path.GetFullPath("..") + "\\syscfg\\syscfg.ini";

            Msg.secsManager.Send(new BaseInfo() { Value = Path.GetFullPath("..") + @"\product" }, TypeId: 3);
            
            if (PT_SET.bXSjNew)
            {
                MT.COM3.Open();
                MT.COM4.Open();
                COM.Grpcsevice.Startsevice();
            }
            if (PT_SET.bG4C)
            {
                MT.COM2.init();
                MT.COM10.init();
                MT.COM11.init();
            }

            DRpt.Report_Status(DReport.EmStatus.StartUp, DReport.EmHareware.Null, DReport.EmStatus.StartUp.GetDescription(VAR.IsChinese));
            Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.StartUp).ToString() });
            Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
            //VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "DReport.EmStatus.StartUp475");
            MT.OnlyLightOn(MT.GPIO_OUT_ALM_YELLOW);
        }
        private void rbtn_run_Click(object sender, EventArgs e)
        {
            
            switch (((RadioButton)sender).Name)
            {
                case "rbtn_run":
                    form_sel(((RadioButton)sender).Name);
                    if (frrun != null) frrun.bupdate = true;
                    break;
                case "rbtn_product":
                    if (frproduct == null) frproduct = new FrProduct();
                    switch (frproduct.ctb_product.SelectedTab!=null?frproduct.ctb_product.SelectedTab.Name:"")
                    {
                        case "tb_tg_cfg":                            
                            form_sel(((RadioButton)sender).Name, frproduct.ctb_product.SelectedTab.Name);
                            break;
                        case "tb_tg_vs":
                            form_sel(((RadioButton)sender).Name, frproduct.ctb_product.SelectedTab.Name);
                            break;
                        case "tb_ofs":
                            form_sel(((RadioButton)sender).Name, frproduct.ctb_product.SelectedTab.Name);
                            break;
                        default:
                            form_sel(((RadioButton)sender).Name, frproduct.ctb_product.SelectedTab.Name);
                            break;
                    }
                    break;
                case "rbtn_sys":
                    if (frsys == null) frsys = new FrSys();
                    form_sel(((RadioButton)sender).Name, frsys.ctb_sys.SelectedTab.Name);
                    break;
                case "rbtn_count":
                    form_sel(((RadioButton)sender).Name);
                    break;
                default:
                    form_sel(((RadioButton)sender).Name);
                    break;
            }

        }


        private EM_SYS_STA SysStatusTemp;
        private void timer_update_Tick(object sender, EventArgs e)
        {
            lb_Date.Text = System.DateTime.Today.ToString("yyyy-MM-dd");
            lb_Time.Text = System.DateTime.Now.ToString("HH:mm:ss");
            if (frsuser!=null)
            {
                if (frsuser.user1.cur_user.bUpdate == true)
                {
                    frsuser.user1.cur_user.bUpdate = false;
                    if(frproduct != null)
                        frproduct.PmsfrProduct(frsuser.user1.cur_user.pms);
                    if (frsys != null)
                        frsys.PmsfrSys(frsuser.user1.cur_user.pms);
                    if (frrun != null)
                        frrun.PmsfrRun(frsuser.user1.cur_user.pms);
                }
            }
            //lb_Date1.Text = System.DateTime.Today.ToString("yyyy-MM-dd");
            //lb_Time1.Text = System.DateTime.Now.ToString("HH:mm:ss");

            if (SysStatusTemp!=VAR.gsys_set.status)
            {
                SysStatusTemp = VAR.gsys_set.status;

                if (VAR.gsys_set.status == EM_SYS_STA.RUN)
                {
                    rbtn_rst.Enabled = false;
                    rbtn_product.Enabled = false;
                  //  rbtn_count.Enabled = false;
                    frrun.btn_stop.BackColor = SystemColors.ButtonFace;
                    frrun.btn_run.BackColor = Color.Lime;
                    rbtn_sys.Enabled = false;
                    rbtn_user.Enabled = false;
                    if (frsuser != null)
                    {
                        frsuser.user1.cur_user.pms = User.PERMISSION.None;
                        frsuser.user1.cur_user.password = "";
                        frsuser.user1.tb_pw.Text = "";
                        frsuser.user1.cur_user.bUpdate = true;
                    }                 
                }
                else
                {
                    frrun.btn_stop.BackColor = Color.OrangeRed;
                    frrun.btn_run.BackColor = SystemColors.ButtonFace;
                    rbtn_rst.Enabled = true;
                  /* if(VAR.Canchange)*/ rbtn_product.Enabled = true;
                    rbtn_count.Enabled = true;
                    rbtn_sys.Enabled = true;
                    rbtn_user.Enabled = true;
                }
            }
            //if (MT.isReady == false)
            //{
            //    timer_key.Enabled = false;
            //    return;
            //}
            //timer_key.Enabled = true;

            //if (MT.GPIO_IN_EMG.isOFF)
            //{
            //    if (VAR.gsys_set.status != CONST.SYS_STATUS_EMG)
            //    {
            //        VAR.gsys_set.status = CONST.SYS_STATUS_EMG;
            //        Action.VAR.sys_inf.Set(CONST.EM_ALM_STA.WAR_RED_FLASH, "EMG");
            //    }
            //}
            //else
            //{
            //    foreach (AXIS ax in MT.AxisListExceptFd)
            //    {
            //        if (!ax.isSVRON)
            //        {
            //            if (VAR.gsys_set.status != CONST.SYS_STATUS_UNKOWN)
            //            {
            //                VAR.gsys_set.status = CONST.SYS_STATUS_UNKOWN;
            //                Action.VAR.sys_inf.Set(CONST.EM_ALM_STA.WAR_RED_FLASH, ax.disc + "未使能");
            //            }
            //        }
            //        else if (ax.isALM)
            //        {
            //            if (VAR.gsys_set.status != CONST.SYS_STATUS_ERR)
            //            {
            //                VAR.gsys_set.status = CONST.SYS_STATUS_ERR;
            //                Action.VAR.sys_inf.Set(CONST.EM_ALM_STA.WAR_RED_FLASH, ax.disc + "ALM", 0);
            //            }
            //        }
            //    }
            //}
        }
        int JisSendTimeCnt = 0;
        int cnt = 0,savecnt=0;
        public DReport.EmStatus SendStauts = DReport.EmStatus.Run;
        private async void timer_key_Tick(object sender, EventArgs e)
        {
            timer_key.Interval = 100;
            int i;
            cnt++;
            if (!COM.mesWaitWarning.IsVisible)
            {
                if (MT.GPIO_IN_KEY_START.isON)
                {
                    i = 0;
                    Thread.Sleep(30);
                    if (MT.GPIO_IN_KEY_START.isON)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("---开始键按下---"));
                        if (VAR.gsys_set.status == EM_SYS_STA.REPAIR)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("提示:当前设备正在维修，无法运行!"));
                            return;
                        }

                        if (!PT_SET.IsMesLocal)
                        {
                            //Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.Wait).ToString() });
                            Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
                            await Task.Run(() =>
                            {
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "正在等待MES上位指令!");
                                SpinWait.SpinUntil(() => MT.IsAllowStartUpdate, 10000);
                            });
                            MT.IsAllowStartUpdate = false;
                            //fr?.Close();
                            if (!MT.IsAllowStart)
                            {
                                FrRun.Dialog(Color.Yellow, "警告", "被MES禁止启动！请联系相关人员。");
                                return;
                            }
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
                        if (VAR.gsys_set.status != EM_SYS_STA.RUN)
                        {
                            VAR.gsys_set.bquit = false;
                            UpDownLoad.bquit = false;
                            WSFEED.bquit = false;
                            WSFEED.bpause = false;
                            BaseAction.run();
                        }
                        else
                        {
                            try
                            {
                                warning fr;
                                fr = (warning)FindForm("warning");
                                if (fr != null)
                                {
                                    fr.btn_ok.BeginInvoke(new Action(() =>
                                    {
                                        fr.btn_ok.PerformClick();
                                    }));
                                }
                            }
                            catch (Exception ex)
                            {
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "fr.btn_ok.PerformClick错误:" + ex.ToString());
                            }



                        }
                        while (MT.GPIO_IN_KEY_START.isON)
                        {
                            if (i++ > 600)
                            {
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("等待开始按键松开大于3秒！"));
                                MessageBox.Show(string.Format("等待开始按键松开大于3秒！"));
                                break;
                            }
                            Thread.Sleep(5);
                            Application.DoEvents();
                        }
                    }
                }

                //stop
                if (MT.GPIO_IN_KEY_STOP.isON)
                {
                    i = 0;
                    Thread.Sleep(30);
                    if (MT.GPIO_IN_KEY_STOP.isON)
                    {
                        // VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("停止按键按下"));
                        //Action.stop();
                        if (frrun != null)
                            frrun.btn_stop.PerformClick();

                        while (MT.GPIO_IN_KEY_STOP.isON)
                        {
                            if (i++ > 600)
                            {
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("等待停止按键松开大于3秒！"));
                                MessageBox.Show(string.Format("等待停止按键松开大于3秒！"));
                                break;
                            }
                            Thread.Sleep(5);
                            Application.DoEvents();
                        }
                    }


                }

                //stop
                if (MT.GPIO_IN_KEY_RESET.isON)
                {
                    i = 0;
                    Thread.Sleep(30);
                    if (MT.GPIO_IN_KEY_RESET.isON)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("---复位键按下---"));
                        //Action.stop();
                        form_sel("rbtn_rst");
                        frrst.btn_all_home.PerformClick();
                    }
                    while (MT.GPIO_IN_KEY_RESET.isON)
                    {
                        if (i++ > 600)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("等待复位按键松开大于3秒！"));
                            MessageBox.Show(string.Format("等待复位按键松开大于3秒！"));
                            break;
                        }
                        Thread.Sleep(5);
                        Application.DoEvents();
                    }
                }
            }
           

            if (VAR.gsys_set.status == EM_SYS_STA.RUN)
            {
                if (cnt >= 10)
                {
                    cnt = 0;
                    COUNT_DATA.runtime++;
                }

            }
            else
            {
                if (cnt >= 10)
                {
                    cnt = 0;
                    savecnt++;
                    COUNT_DATA.waittime++;
                    if (savecnt >= 6)
                    {
                        savecnt = 0;
                        COUNT_DATA.SaveCountCfg(VAR.gsys_set.cur_product_name);
                    }

                }

            }
            //状态发送
            if (VAR.gsys_set.status == EM_SYS_STA.RUN)
            {
                List<int> tempalarmid = Msg.alarmid;
                if (tempalarmid.Count != 0)
                {
                    foreach (int alarmid in tempalarmid)
                    {
                        Msg.secsManager.Send(new BaseInfo() { Id = alarmid, Value = "false" }, 1);
                    }
                    Msg.alarmid.Clear();
                }

                if (SendStauts != DReport.EmStatus.Wait &&
                    (COM.traybox_fd.ChgML || COM.traybox_ng.ChgML || COM.traybox_ok.ChgML))
                {
                    //SendStauts = DReport.EmStatus.Wait;
                    //DRpt.Report_Status(DReport.EmStatus.Wait, DReport.EmHareware.Null, DReport.EmStatus.Wait.GetDescription(VAR.IsChinese));
                    //Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.Wait).ToString() });
                    //Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
                    //VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "DReport.EmStatus.Wait813");
                }
                else if (SendStauts != DReport.EmStatus.Error && VAR.IsErrAlm && !COM.traybox_fd.ChgML && !COM.traybox_ng.ChgML && !COM.traybox_ok.ChgML)
                {

                    SendStauts = DReport.EmStatus.Error;
                    DRpt.Report_Status(DReport.EmStatus.Error, DReport.EmHareware.Null, DReport.EmStatus.Error.GetDescription(VAR.IsChinese));
                    Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.Error).ToString() });
                    Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
                   //VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "DReport.EmStatus.Error820");
                }
                else if (SendStauts != DReport.EmStatus.Run && !VAR.IsErrAlm && !COM.traybox_fd.ChgML && !COM.traybox_ng.ChgML && !COM.traybox_ok.ChgML)
                {
                    SendStauts = DReport.EmStatus.Run;
                    DRpt.Report_Status(DReport.EmStatus.Run, DReport.EmHareware.Null, DReport.EmStatus.Run.GetDescription(VAR.IsChinese));
                    Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.Run).ToString() });
                    Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
                }
                JisSendTimeCnt++;
                if (JisSendTimeCnt > (PT_SET.JigCntSend * (60000 / timer_key.Interval)))//半个小时发一次工站夹具生产数据gy0123
                {
                    JisSendTimeCnt = 0;
                    if (PT_SET.bJigSan)
                        UpDownLoad.GetAllJigData();//发送一次夹具生产数据               
                }


            }

        }

        private void timer_sysTimeCnt_Tick(object sender, EventArgs e)
        {
            if (!PT_SET.PreMiniAutoCheckWs1En && !PT_SET.PreMiniAutoCheckWs2En) return;

            DateTime now = DateTime.Now;
            DateTime previous = preMiniAutoCheckLastScheduleCheck;
            preMiniAutoCheckLastScheduleCheck = now;
            EnvironmentInspectionScheduleMode mode =
                PT_SET.EnvironmentCheckScheduleMode == (int)EnvironmentInspectionScheduleMode.HourlyInterval
                    ? EnvironmentInspectionScheduleMode.HourlyInterval
                    : EnvironmentInspectionScheduleMode.FixedTime;

            TimeSpan fixedTime1 = TimeSpan.Zero;
            TimeSpan fixedTime2 = TimeSpan.Zero;
            if (mode == EnvironmentInspectionScheduleMode.FixedTime &&
                (!TimeSpan.TryParse(PT_SET.CheckTimeMorning1, out fixedTime1) ||
                 !TimeSpan.TryParse(PT_SET.CheckTimeEvening2, out fixedTime2))) return;

            DateTime dueTime;
            if (!EnvironmentInspectionSchedule.HasDueTime(previous, now, mode, fixedTime1,
                fixedTime2, (int)Math.Round(PT_SET.CheckTimer), out dueTime)) return;

            VAR.PreMiniAutoCheckTimeReached = true;
            if (PT_SET.PreMiniAutoCheckWs1En) VAR.PreMiniAutoCheckWs1Pending = true;
            if (PT_SET.PreMiniAutoCheckWs2En) VAR.PreMiniAutoCheckWs2Pending = true;
            string modeText = mode == EnvironmentInspectionScheduleMode.HourlyInterval ? "按小时周期" : "固定时间";
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG,
                string.Format("pre-mini自动点检时间到,模式:{0},计划时间:{1:yyyy-MM-dd HH:mm:ss}", modeText, dueTime));
        }

        private void lb_Date_Click(object sender, EventArgs e)
        {
            if(!ts_manual.Visible&& !ts_manual2.Visible)
            {
                ts_manual.Top = pnl_sub.Top;
                ts_manual.Left = tbl_main.Width - ts_manual.Width;
                ts_manual.Visible = !ts_manual.Visible;
                if (tsc_step_sel.SelectedIndex < 0 || tsc_step_sel.SelectedIndex >= tsc_step_sel.Items.Count) tsc_step_sel.SelectedIndex = 1;
                else tsc_step_sel.SelectedIndex = tsc_step_sel2.SelectedIndex;
            }
            else if (ts_manual.Visible && !ts_manual2.Visible)
            {
                ts_manual.Visible = !ts_manual.Visible;
                ts_manual2.Top = pnl_sub.Top;
                ts_manual2.Left = tbl_main.Width - ts_manual2.Width;
                ts_manual2.Visible = !ts_manual2.Visible;
                tsc_step_sel2.SelectedIndex = tsc_step_sel.SelectedIndex;
            }
            else if (!ts_manual.Visible && ts_manual2.Visible)
            {
                ts_manual2.Visible = !ts_manual2.Visible;
            }
        }
        private void StrToAxis(String pch, ref AXIS axis, ref AXIS.AX_DIR dir)
        {
            if (true == pch.Equals("tsb_1xp"))
            {
                axis = MT.AXIS_UDL1_X;
                dir = AXIS.AX_DIR.P;
            }
            else if (true == pch.Equals("tsb_1xn"))
            {
                axis = MT.AXIS_UDL1_X;
                dir = AXIS.AX_DIR.N;
            }
            else if (true == pch.Equals("tsb_1yp"))
            {
                axis = MT.AXIS_UDL1_Y;
                dir = AXIS.AX_DIR.P;
            }
            else if (true == pch.Equals("tsb_1yn"))
            {
                axis = MT.AXIS_UDL1_Y;
                dir = AXIS.AX_DIR.N;
            }
            else if (true == pch.Equals("tsb_1zp"))
            {
                axis = MT.AXIS_UDL1_Z;
                dir = AXIS.AX_DIR.P;
            }
            else if (true == pch.Equals("tsb_1zn"))
            {
                axis = MT.AXIS_UDL1_Z;
                dir = AXIS.AX_DIR.N;
            }
            else if (true == pch.Equals("tsb_1u1p"))
            {
                axis = MT.AXIS_UDL1_U1;
                dir = AXIS.AX_DIR.P;
            }
            else if (true == pch.Equals("tsb_1u1n"))
            {
                axis = MT.AXIS_UDL1_U1;
                dir = AXIS.AX_DIR.N;
            }
            else if (true == pch.Equals("tsb_1u2p"))
            {
                axis = MT.AXIS_UDL1_U2;
                dir = AXIS.AX_DIR.P;
            }
            else if (true == pch.Equals("tsb_1u2n"))
            {
                axis = MT.AXIS_UDL1_U2;
                dir = AXIS.AX_DIR.N;
            }
            else  if (true == pch.Equals("tsb_2xp"))
            {
                axis = MT.AXIS_UDL2_X;
                dir = AXIS.AX_DIR.P;
            }
            else if (true == pch.Equals("tsb_2xn"))
            {
                axis = MT.AXIS_UDL2_X;
                dir = AXIS.AX_DIR.N;
            }
            else if (true == pch.Equals("tsb_2yp"))
            {
                axis = MT.AXIS_UDL2_Y;
                dir = AXIS.AX_DIR.P;
            }
            else if (true == pch.Equals("tsb_2yn"))
            {
                axis = MT.AXIS_UDL2_Y;
                dir = AXIS.AX_DIR.N;
            }
            else if (true == pch.Equals("tsb_2zp"))
            {
                axis = MT.AXIS_UDL2_Z;
                dir = AXIS.AX_DIR.P;
            }
            else if (true == pch.Equals("tsb_2zn"))
            {
                axis = MT.AXIS_UDL2_Z;
                dir = AXIS.AX_DIR.N;
            }
            else if (true == pch.Equals("tsb_2u1p"))
            {
                axis = MT.AXIS_UDL2_U1;
                dir = AXIS.AX_DIR.P;
            }
            else if (true == pch.Equals("tsb_2u1n"))
            {
                axis = MT.AXIS_UDL2_U1;
                dir = AXIS.AX_DIR.N;
            }
            else if (true == pch.Equals("tsb_2u2p"))
            {
                axis = MT.AXIS_UDL2_U2;
                dir = AXIS.AX_DIR.P;
            }
            else if (true == pch.Equals("tsb_2u2n"))
            {
                axis = MT.AXIS_UDL2_U2;
                dir = AXIS.AX_DIR.N;
            }
            else
            {
                axis = null;
                dir = AXIS.AX_DIR.N;
            }

        }
        double step = 0.1;
        private string[] tmp;
        private void tsb_xn_MouseDown(object sender, MouseEventArgs e)
        {
            ToolStripButton btn = (ToolStripButton)sender;
            AXIS ax = null;
            AXIS.AX_DIR dir = AXIS.AX_DIR.P;
            StrToAxis(btn.Name, ref ax, ref dir);
            if (ax == null) return;
            //z限制1mm步进
            if ((ax.id == MT.AXIS_UDL1_Z.id || ax.id == MT.AXIS_UDL2_Z.id) && step > 1) step = 1;
            ax.JOG_Step(ref VAR.gsys_set.bquit, dir, step);
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, btn.Text + " down," + ax.disc);
            Thread.Sleep(500);
        }

        private void tsb_xn_MouseUp(object sender, MouseEventArgs e)
        {
            ToolStripButton btn = (ToolStripButton)sender;
            AXIS ax = null;
            AXIS.AX_DIR dir = AXIS.AX_DIR.P;
            StrToAxis(btn.Text, ref ax, ref dir);
            if (ax == null) return;
            ax.JOG_Stop();
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, btn.Text + " up," + ax.disc);
        }

        private void tsb_stop_Click(object sender, EventArgs e)
        {
            VAR.gsys_set.bquit = true;
            MT.AllAxStop();
        }

        private void FrMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (VAR.gsys_set.status == EM_SYS_STA.RUN)
            {
                if (MessageBox.Show("运行中，是否要停止?", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    for (int n = 0; n < 100; n++)
                    {
                        VAR.gsys_set.bquit = true;
                        VAR.gsys_set.bpause = false;
                        //VAR.gsys_set.bclose = true;
                        Thread.Sleep(10);
                        Application.DoEvents();
                    }
                    VAR.gsys_set.status = EM_SYS_STA.STANDBY;
                    return;
                }
            }
            if (MessageBox.Show("是确定要关闭软件?", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                //k_hook.KeyDownEvent -= hook_KeyDown;//钩住键按下
                //k_hook.KeyUpEvent -= hook_KeyUp;//钩住键按下
                //k_hook.Stop();//安装键盘钩子
                //Acquistion.AllCameraDisconnect();
                if (PT_SET.bXSjNew)
                {
                    MT.COM3.Close();
                    MT.COM4.Close();
                    COM.Grpcsevice.Stopsevice();
                }
                if (PT_SET.bG4C)
                {
                    MT.COM2.close();
                    MT.COM10.close();
                    MT.COM11.close();
                }
                DRpt.Report_Status(DReport.EmStatus.Quit, DReport.EmHareware.Null, DReport.EmStatus.Quit.GetDescription(VAR.IsChinese));
                Msg.secsManager.Send(new BaseInfo() { Id = 1, Value = Convert.ToInt32(DReport.EmStatus.Quit).ToString() });
                Msg.secsManager.Send(new BaseInfo() { Id = 1 }, 2);
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "DReport.EmStatus.Quit1050");
                Msg.secsManager.Send(new BaseInfo(), 10);
                Task.Delay(10).Wait();
                Msg.secsManager.Stop();
                BaseAction.Close();
                Environment.Exit(Environment.ExitCode);
            }
            else e.Cancel = true;
        }

        private void tsc_step_sel_SelectedIndexChanged(object sender, EventArgs e)
        {
            double[] step_array = { 0.01, 0.1, 1, 10 };
           
            try
            {
                if (ts_manual2.Visible)
                {
                    step = step_array[tsc_step_sel2.SelectedIndex];
                }                
                else
                {
                    step = step_array[tsc_step_sel.SelectedIndex];
                }
            }
            catch
            {
                MessageBox.Show("步距数据出错!");
                step = 0.1;
            }
            //foreach (AXIS ax in MT.AxisListExceptFd) ax.manual_step = step;
        }

        private async void timer_reconnect_Tick(object sender, EventArgs e)
        {
            try
            {
                ((System.Windows.Forms.Timer)sender).Enabled = false;
                Task Reconnect = new Task(() =>
                {
                    MT.ChkAndReConnect();
                }
                );
                Reconnect.Start();
                await Reconnect;
            }
            finally
            {
                ((System.Windows.Forms.Timer)sender).Enabled = true;
            }  
        }

        private void Minimized_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void chineseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (VAR.gsys_set.status == EM_SYS_STA.RUN ) return;

            ToolStripMenuItem menu = sender as ToolStripMenuItem;
            string lang = "";
            if (menu == chineseToolStripMenuItem)
            {
                lang = Enum.GetName(typeof(enumLanguage), 0);
                chineseToolStripMenuItem.Checked = true;
                englishToolStripMenuItem.Checked = false;
                frrun.cogDisplayer_run.toolStripLabel4.Text = "显示模式";
                frrun.cogDisplayer_run.tsb_cam_list.Text = "相机列表";
                frrun.cogDisplayer_run.tsb_block_edit.Text = "流程编辑";
                frrun.cogDisplayer_run.toolStripLabel5.Text = "曝光(ms)";
                frrun.cogDisplayer_run.toolStripLabel2.Text = "亮度(0-1)";
                frrun.cogDisplayer_run.toolStripLabel3.Text = "对比度(0-1)";
                frrun.cogDisplayer_run.tsb_save.Text = "保存参数";
                frrun.cogDisplayer_run.tsb_GoCenter.Text = "图像对中";
                frrun.cogDisplayer_run.tsb_save_screen.Text = "保存图片";
                frrun.cogDisplayer_run.tsb_continue.Text = "连续测量";
                frproduct.cogDisplayer_product.toolStripLabel4.Text = "显示模式";
                frproduct.cogDisplayer_product.tsb_cam_list.Text = "相机列表";
                frproduct.cogDisplayer_product.tsb_block_edit.Text = "流程编辑";
                frproduct.cogDisplayer_product.toolStripLabel5.Text = "曝光(ms)";
                frproduct.cogDisplayer_product.toolStripLabel2.Text = "亮度(0-1)";
                frproduct.cogDisplayer_product.toolStripLabel3.Text = "对比度(0-1)";
                frproduct.cogDisplayer_product.tsb_save.Text = "保存参数";
                frproduct.cogDisplayer_product.tsb_GoCenter.Text = "图像对中";
                frproduct.cogDisplayer_product.tsb_save_screen.Text = "保存图片";
                frproduct.cogDisplayer_product.tsb_continue.Text = "连续测量";
            }
            else if (menu == englishToolStripMenuItem)
            {
                lang = Enum.GetName(typeof(enumLanguage), 1);
                chineseToolStripMenuItem.Checked = false;
                englishToolStripMenuItem.Checked = true;
                frrun.cogDisplayer_run.toolStripLabel4.Text = "Display mode";
                frrun.cogDisplayer_run.tsb_cam_list.Text = "Cam list";
                frrun.cogDisplayer_run.tsb_block_edit.Text = "Edit";
                frrun.cogDisplayer_run.toolStripLabel5.Text = "Exposure(ms)";
                frrun.cogDisplayer_run.toolStripLabel2.Text = "Brightness(0-1)";
                frrun.cogDisplayer_run.toolStripLabel3.Text = "Contrast(0-1)";
                frrun.cogDisplayer_run.tsb_save.Text = "Save cfg";
                frrun.cogDisplayer_run.tsb_GoCenter.Text = "Center";
                frrun.cogDisplayer_run.tsb_save_screen.Text = "Save img";
                frrun.cogDisplayer_run.tsb_continue.Text = "Live";
                frproduct.cogDisplayer_product.toolStripLabel4.Text = "Display mode";
                frproduct.cogDisplayer_product.tsb_cam_list.Text = "Cam list";
                frproduct.cogDisplayer_product.tsb_block_edit.Text = "Edit";
                frproduct.cogDisplayer_product.toolStripLabel5.Text = "Exposure(ms)";
                frproduct.cogDisplayer_product.toolStripLabel2.Text = "Brightness(0-1)";
                frproduct.cogDisplayer_product.toolStripLabel3.Text = "Contrast(0-1)";
                frproduct.cogDisplayer_product.tsb_save.Text = "Save cfg";
                frproduct.cogDisplayer_product.tsb_GoCenter.Text = "Center";
                frproduct.cogDisplayer_product.tsb_save_screen.Text = "Save img";
                frproduct.cogDisplayer_product.tsb_continue.Text = "Live";
            }
            LanguageSwitch(this, lang);
            LanguageSwitch(frrun, lang);
            LanguageSwitch(frcount, lang);
            LanguageSwitch(frproduct, lang);
            LanguageSwitch(frrst, lang);
            LanguageSwitch(frsuser, lang);
            LanguageSwitch(frsys, lang);
        }
        private void LanguageSwitch(Form form, string lang)
        {
            if (!MultiLanguage.LoadLanguage(form, lang))
            {
                return;
            }
            MultiLanguage.SetDefaultLanguage(lang);
            if (lang == Enum.GetName(typeof(enumLanguage), 0))
            {
                chineseToolStripMenuItem.Checked = true;
                englishToolStripMenuItem.Checked = false;
                VAR.IsChinese = true;
                frsys.affineCail_Dw1ToUp1.lbl_affine_place.Text = "吸头1放料位";
                frsys.affineCail_Dw1ToUp1.lbl_affine_upcam.Text = "上相机1拍照位";
                frsys.affineCail_Dw1ToUp1.lbl_affine_downcam.Text = "下相机1拍照位";
                frsys.affineCail_Dw2ToUp2.lbl_affine_place.Text = "吸头3放料位";
                frsys.affineCail_Dw2ToUp2.lbl_affine_upcam.Text = "上相机2拍照位";
                frsys.affineCail_Dw2ToUp2.lbl_affine_downcam.Text = "下相机2拍照位";
                frsys.npointCail1.lbl_unit_cam.Text =
                frsys.npointCail1.cb_UDload.SelectedIndex == 0 ? "上相机1拍照位" : "上相机2拍照位";
                frrun.cogDisplayer_run.toolStripLabel4.Text = "显示模式";
                frrun.cogDisplayer_run.tsb_cam_list.Text = "相机列表";
                frrun.cogDisplayer_run.tsb_block_edit.Text = "流程编辑";
                frrun.cogDisplayer_run.toolStripLabel5.Text = "曝光(ms)";
                frrun.cogDisplayer_run.toolStripLabel2.Text = "亮度(0-1)";
                frrun.cogDisplayer_run.toolStripLabel3.Text = "对比度(0-1)";
                frrun.cogDisplayer_run.tsb_save.Text = "保存参数";
                frrun.cogDisplayer_run.tsb_GoCenter.Text = "图像对中";
                frrun.cogDisplayer_run.tsb_save_screen.Text = "保存图片";
                frrun.cogDisplayer_run.tsb_continue.Text = "连续测量";
                frproduct.cogDisplayer_product.toolStripLabel4.Text = "显示模式";
                frproduct.cogDisplayer_product.tsb_cam_list.Text = "相机列表";
                frproduct.cogDisplayer_product.tsb_block_edit.Text = "流程编辑";
                frproduct.cogDisplayer_product.toolStripLabel5.Text = "曝光(ms)";
                frproduct.cogDisplayer_product.toolStripLabel2.Text = "亮度(0-1)";
                frproduct.cogDisplayer_product.toolStripLabel3.Text = "对比度(0-1)";
                frproduct.cogDisplayer_product.tsb_save.Text = "保存参数";
                frproduct.cogDisplayer_product.tsb_GoCenter.Text = "图像对中";
                frproduct.cogDisplayer_product.tsb_save_screen.Text = "保存图片";
                frproduct.cogDisplayer_product.tsb_continue.Text = "连续测量";


            }
            else if (lang == Enum.GetName(typeof(enumLanguage), 1))
            {
                chineseToolStripMenuItem.Checked = false;
                englishToolStripMenuItem.Checked = true;
                VAR.IsChinese = false;
                frsys.affineCail_Dw1ToUp1.lbl_affine_place.Text = "XT1 Place";
                frsys.affineCail_Dw1ToUp1.lbl_affine_upcam.Text = "UpCam1";
                frsys.affineCail_Dw1ToUp1.lbl_affine_downcam.Text = "DwCam1";
                frsys.affineCail_Dw2ToUp2.lbl_affine_place.Text = "XT3 Place";
                frsys.affineCail_Dw2ToUp2.lbl_affine_upcam.Text = "UpCam2";
                frsys.affineCail_Dw2ToUp2.lbl_affine_downcam.Text = "DwCam2";
                frsys.npointCail1.lbl_unit_cam.Text =
                frsys.npointCail1.cb_UDload.SelectedIndex == 0 ? "UpCam1" : "UpCam2";
                frrun.cogDisplayer_run.toolStripLabel4.Text = "Display mode";
                frrun.cogDisplayer_run.tsb_cam_list.Text = "Cam list";
                frrun.cogDisplayer_run.tsb_block_edit.Text = "Edit";
                frrun.cogDisplayer_run.toolStripLabel5.Text = "Exposure(ms)";
                frrun.cogDisplayer_run.toolStripLabel2.Text = "Brightness(0-1)";
                frrun.cogDisplayer_run.toolStripLabel3.Text = "Contrast(0-1)";
                frrun.cogDisplayer_run.tsb_save.Text = "Save cfg";
                frrun.cogDisplayer_run.tsb_GoCenter.Text = "Center";
                frrun.cogDisplayer_run.tsb_save_screen.Text = "Save img";
                frrun.cogDisplayer_run.tsb_continue.Text = "Live";
                frproduct.cogDisplayer_product.toolStripLabel4.Text = "Display mode";
                frproduct.cogDisplayer_product.tsb_cam_list.Text = "Cam list";
                frproduct.cogDisplayer_product.tsb_block_edit.Text = "Edit";
                frproduct.cogDisplayer_product.toolStripLabel5.Text = "Exposure(ms)";
                frproduct.cogDisplayer_product.toolStripLabel2.Text = "Brightness(0-1)";
                frproduct.cogDisplayer_product.toolStripLabel3.Text = "Contrast(0-1)";
                frproduct.cogDisplayer_product.tsb_save.Text = "Save cfg";
                frproduct.cogDisplayer_product.tsb_GoCenter.Text = "Center";
                frproduct.cogDisplayer_product.tsb_save_screen.Text = "Save img";
                frproduct.cogDisplayer_product.tsb_continue.Text = "Live";
            }

        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (VAR.gsys_set.status == EM_SYS_STA.RUN) return;
            EM_SYS_STA sys_status = VAR.gsys_set.status;
            VAR.gsys_set.status = EM_SYS_STA.REPAIR;
            VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "设备维修!" : "Equ upkeep", 20, true);
            MT.ST_WARN warn = new MT.ST_WARN();
            warning fr_warn = new warning();
            warn.ok_txt = VAR.IsChinese ? "确定" : "OK";
            warn.ws = null;
            warn.title = VAR.IsChinese ? "提示:设备维修!" : "Tip: Equipment maintenance!";
            warn.msg = VAR.IsChinese ? "当前设备在维修状态！" : "The current equipment is in maintenance status!\r\n当前设备在维修状态！";
            warn.lb_msg = VAR.IsChinese ? "提示:当前设备正在维修，请确认设备内没有维修人员后再关闭此对话框!" : "Tip: The current equipment is being repaired, please confirm that there are no maintenance personnel in the equipment before closing this dialog box!\r\n提示:当前设备正在维修，请确认设备内没有维修人员后再关闭此对话框!";
            MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.Null, false);
            VAR.gsys_set.status = sys_status;
        }

        private void timersecs_Tick(object sender, EventArgs e)
        {
            if (Process.GetProcessesByName("SecsApp").Count() == 0)
            {
                var path = @"Release\SecsApp.exe";
                if (File.Exists(path)) Process.Start(path);
            }
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            Msg.secsManager.DealMessage(m);
            base.WndProc(ref m);
        }
        //private void SPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        //{
        //    byte[] ReDatas;
        //    if (SPort1.IsOpen) //严谨性
        //    {
        //        if (e.EventType == SerialData.Eof)//修复索引超出数组范围异常抛出问题
        //            return;
        //        byte[] byteRead = new byte[SPort1.BytesToRead]; //BytesToRead:sp1接收的字符个数
        //        try
        //        {
        //            Byte[] receivedData = new Byte[SPort1.BytesToRead]; //创建接收字节数组
        //            SPort1.Read(receivedData, 0, receivedData.Length);                   
        //            SPort1.DiscardInBuffer(); //清空SerialPort控件的Buffer
        //            string strRcv = null;
        //            for (int i = 0; i < receivedData.Length; i++) //窗体显示
        //            {
        //                Thread.Sleep(30);
        //                strRcv += receivedData[i].ToString("X2"); //16进制显示
        //            }
        //            Utility.WriteStrToCSV(System.DateTime.Today.ToString("yyyy-MM-dd") + "," + System.DateTime.Now.ToString("HH:mm:ss") + "," + "port1"+","+ strRcv);
        //            if (COM.wsfeed1.isInTestPos)
        //            {
        //                COM.wsfeed1.ResultAnalysisPort1(receivedData);
        //            }
        //            else if (COM.wsfeed2.isInTestPos)
        //            {
        //                COM.wsfeed2.ResultAnalysisPort1(receivedData);
        //            }
        //            ReadStrport1 = strRcv;
        //            ReadStr1 = strRcv;                    
        //            this.Invoke(AA, ReadStrport1);
        //            receivedData = null;
        //        }
        //        catch (System.Exception ex)
        //        {
        //            MessageBox.Show(ex.Message, "出错提示");

        //        }                
        //    }
        //    else
        //    {
        //        MessageBox.Show("串口" + VAR.gsys_set.str_w1_com + "未打开，请确认");
        //    }
        //}

        //private void SPort2_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        //{
        //    byte[] ReDatas;
        //    if (SPort2.IsOpen) //严谨性
        //    {
        //        byte[] byteRead = new byte[SPort2.BytesToRead]; //BytesToRead:sp1接收的字符个数
        //        if (e.EventType == SerialData.Eof)
        //            return;
        //        //this.Invoke(serialRecvDelegate);
        //        try
        //        {
        //            Byte[] receivedData = new Byte[SPort2.BytesToRead]; //创建接收字节数组
        //            SPort2.Read(receivedData, 0, receivedData.Length); //读取数据                    
        //            SPort2.DiscardInBuffer(); //清空SerialPort控件的Buffer
        //            string strRcv = null;
        //            for (int i = 0; i < receivedData.Length; i++) //窗体显示
        //            {
        //                Thread.Sleep(30);
        //                strRcv += receivedData[i].ToString("X2"); //16进制显示

        //            }
        //            Utility.WriteStrToCSV(System.DateTime.Today.ToString("yyyy-MM-dd") + "," + System.DateTime.Now.ToString("HH:mm:ss") + "," + "port2" + "," + strRcv);
        //            if (COM.wsfeed1.isInTestPos)
        //            {
        //                COM.wsfeed1.ResultAnalysisPort2(receivedData);
        //            }
        //            else if (COM.wsfeed2.isInTestPos)
        //            {
        //                COM.wsfeed2.ResultAnalysisPort2(receivedData);
        //            }
        //            ReadStrport2 = strRcv;
        //            ReadStr2 = strRcv;
        //            this.Invoke(AB, ReadStrport2);

        //            receivedData = null;
        //        }
        //        catch (System.Exception ex)
        //        {
        //            MessageBox.Show(ex.Message, "出错提示");

        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("串口" + VAR.gsys_set.str_w2_com + "未打开，请确认");
        //    }
        //}
    }
}
