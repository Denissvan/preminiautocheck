using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using MotionCtrl;
using System.Reflection;

namespace UI
{
    public partial class FrSys : Form
    {
        public bool bupdate
        {
            get { return tmr_update.Enabled; }
            set
            {
                tmr_update.Enabled = value;
                if(value==false)ioTable.ShowCfg(-1);
            }
        }

        public FrSys()
        {
            InitializeComponent();
            if (FrMain.frsuser != null)
                PmsfrSys(FrMain.frsuser.user1.cur_user.pms);
            else
                PmsfrSys(User.PERMISSION.Operator);
        }

        public void bEn(bool en=true)
        {
            axisConfig.Enabled = en;
            btn_update_card.Enabled = en;
            btn_load_axis_cfg.Enabled = en;
            btn_save_axis_cfg.Enabled = en;
            rbtn_upcode.Enabled = en;
            rbtn_dwcode.Enabled = en;
            rbtn_fly_pallet_vs.Enabled = en;
            rbtn_unfly_pallet_vs.Enabled = en;
            rbtn_pre_turnon.Enabled = en;
            rbtn_next_turnon.Enabled = en;
            //运行模式
            rbtn_twomd.Enabled = en;
            rbtn_md1.Enabled = en;
            rbtn_md2.Enabled = en;
            rbtn_twowork.Enabled = en;
            //运行方式
            rbtn_run_normal.Enabled = en;
            rbtn_run_updw.Enabled = en;
            rbtn_run_empty.Enabled = en;
            //门禁

            chk_tm.Enabled = false;
            chk_updown.Enabled = false;
            rbtn_backerrcontinueen.Enabled = en;
            rbtn_backerrcontinuedis.Enabled = en;
            //视觉回检
            //Chk_OpenVs.Enabled = en;
            //nud_vschk_xyofs.Enabled=en;
            //nud_vschk_rofs.Enabled=en;
            btn_ptset_save.Enabled = en;
            //料盘放料检测
            rbtn_OpenVsTray.Enabled = en;
            rbtn_CloseVsTray.Enabled = en;
            //上下料移动保护(针对转盘)
            rbtn_safeoff.Enabled = en;
            rbtn_safeon.Enabled = en;
            //料盘二维码设置
            rbtn_traybaren.Enabled = en;
            rbtn_traybardis.Enabled = en;
            //GRR流程
            chk_grr.Enabled = en;
            nud_grrtestcnt.Enabled = en;
            nud_grrtudlcnt.Enabled = en;
            //相机存储图片
            chk_upcam1.Enabled = en;
            chk_dwcam1.Enabled = en;
            chk_upcam2.Enabled = en;
            chk_dwcam2.Enabled = en;
            //取放料角度开放设置
            rbtn_open_degree.Enabled = en;
            rbtn_close_degree.Enabled = en;
            nud_offset_degree.Enabled = en;
            //高透模偏移检测
            chk_GTMCheck.Enabled = en;
            nud_GTMOfs.Enabled = en;
            //二维码回检设备
            rbtn_OpenBarCamBack.Enabled = en;
            rbtn_CloseBarCamBack.Enabled = en;
            ////模组带起
            Chk_ModPasteUp.Enabled = en;
            nud_PlaceDly.Enabled = en;
            nud_MovUpDly.Enabled = en;
            //同工位连续同NG提示
            chk_SameNGTip.Enabled = en;
            nud_SameNGTipCnt.Enabled = en;
            //同工位连续同NG提示
            chk_SameRowNGTip.Enabled = en;
            nud_SameRowNGTipCnt.Enabled = en;
            //夹具扫码
            btnStartJigSan1.Enabled = en;
            btnStartJigSan2.Enabled = en;
            rabt_jigscan_ON.Enabled = en;
            rabt_jigscan_OFF.Enabled = en;
            nud_JigSendTime.Enabled = en;
            groupBoxPreMiniAutoCheck.Enabled = en;
        }

        public void PmsfrSys(User.PERMISSION pms)
        {
            npointCail1.PmsEn(pms);
            affineCail_Dw1ToUp1.PmsEn(pms);
            affineCail_Dw2ToUp2.PmsEn(pms);
            rotAndFly1.PmsEn(pms);
            CogRecordDisplay_sys.PmsEn((pms > User.PERMISSION.Engineer) ? true : false);
            if (pms < User.PERMISSION.SuperAdmin)
            {
                bEn(false);
            }

            else
            {
                bEn(true);
            }
               

        }

        private void FrSys_Load(object sender, EventArgs e)
        {
           
            //加载卡列表CardTable
            FieldInfo[] pArray = typeof(MT).GetFields();
            foreach (FieldInfo p in pArray)
            {
                if (p.FieldType.Name == "CARD")
                    CardTable.AddCard(((CARD)p.GetValue(typeof(MT))));

                if (p.FieldType.Name == "GPIO")
                    ioTable.AddIO(((GPIO)p.GetValue(typeof(MT))));

                if (p.FieldType.Name == "Cylinder")
                    cylinderTable.AddCylinder(((Cylinder)p.GetValue(typeof(MT))));
            }
            ioTable.ShowCfg(0);

            //轴列表
            ctb_ax_sel_SelectedIndexChanged(ctb_ax_sel, null);
            CogRecordDisplay_sys.list_cam.Clear();
            CogRecordDisplay_sys.AddCam(COM.ListCam);
            npointCail1.AddUDLoad(COM.List_UDLoad);
            affineCail_Dw1ToUp1.Init(ref COM.UDLoad1);
            affineCail_Dw2ToUp2.Init(ref COM.UDLoad2);
            rotAndFly1.AddUDLoad(COM.List_UDLoad);
            taskNpoint1.AddCam(COM.ListCam);
            ShowData();
           // rotAndFly1.AddXt(COM.ListXT);
        }

        private void btn_TestMaterialPosWarn_Click(object sender, EventArgs e)
        {
            MT.ST_WARN warn = new MT.ST_WARN();
            warning fr_warn = new warning();

            warn.ok_txt = VAR.IsChinese ? "拍照确认" : "Take a photo";
            warn.abort_txt = VAR.IsChinese ? "继续运行" : "Keep running";
            warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
            warn.ws = null;
            warn.title = VAR.IsChinese ? "提示:物料放偏" : "Tip: Material is biased";
            warn.msg = VAR.IsChinese ? "当前工站有物料放偏!" : "There is material deviation in the current station!";
            warn.lb_msg = VAR.IsChinese
                ? "离线测试弹窗:\r\n  右侧橙色按钮[拍照确认] = OK = 重新拍照\r\n  中间按钮[继续运行] = Abort = 继续运行\r\n  左侧按钮[停止运行] = Cancel = 停止运行\r\n\r\n请直接按 Enter，应返回 OK。"
                : "Offline test dialog:\r\n  Right orange button [Take a photo] = OK = retake photo\r\n  Middle button [Keep running] = Abort = keep running\r\n  Left button [Stop running] = Cancel = stop running\r\n\r\nPress Enter directly. The result should be OK.";

            DialogResult logres = MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.MaterialPosErr);
            MessageBox.Show(string.Format("弹窗返回值: {0}\r\nOK=拍照确认, Abort=继续运行, Cancel=停止运行", logres), "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tmr_update_Tick(object sender, EventArgs e)
        {
            int t;
            tmr_update.Enabled = false;
            switch (ctb_sys.SelectedIndex)
            {
                case 0:
                    t = Environment.TickCount;
                    CardTable.UpdateShow();
                    t = Environment.TickCount - t;
                    lb_card_update_ms.Text = string.Format("{0}ms", t);
                    break;
                case 1:
                    t = Environment.TickCount;
                    axisTable.UpdateShow();
                    t = Environment.TickCount - t;
                    lb_ax_update_ms.Text = string.Format("{0}ms", t);
                    break;
                case 2:
                    if (ioTable.Visible) ioTable.UpdateShow();
                    if (cylinderTable.Visible) cylinderTable.UpdateShow();
                    lb_io_update_ms.Text = string.Format("{0}ms", ioTable.UpdateCt);
                    break;
            }
            //CardTable.UpdateShow();   
            tmr_update.Interval = 500;
            tmr_update.Enabled = true;
        }

        private void btn_load_axis_cfg_Click(object sender, EventArgs e)
        {
            axisConfig.LoadFrFile();
            MessageBox.Show("轴列表参数加载完成!");
        }

        private void btn_save_axis_cfg_Click(object sender, EventArgs e)
        {
            if (true == axisConfig.SaveToFile())
            {
                MessageBox.Show("轴列表参数保存完成!");
            }
        }

        private void ctb_ax_sel_SelectedIndexChanged(object sender, EventArgs e)
        {            
            axisTable.ClearAxis();
            axisConfig.ClearAxis();
            axisTable.UpdateShow();
            axisConfig.UpdateShow();
            switch (((CTabControl)sender).SelectedIndex)
            {                
                case 0:
                    axisTable.AddAxis(MT.Axlist_UDL1);
                    axisConfig.AddAxis(MT.Axlist_UDL1_ExpLC);
                    break;
                case 1:
                    axisTable.AddAxis(MT.Axlist_UDL2);
                    axisConfig.AddAxis(MT.Axlist_UDL2_ExpLC);
                    break;
                case 2:
                    axisTable.AddAxis(MT.Axlist_UDL_LC);
                    axisConfig.AddAxis(MT.Axlist_UDL_LC);
                    break;                
                case 3: 
                        if (!COM.wsfeed2.IsDown)
                        {
                            MessageBox.Show("左测试模块有夹具处于打开状态，禁止操作请手动关闭夹具");
                            return ;
                        }
                   
                    axisTable.AddAxis(MT.AxList_WSFEED2);
                    axisConfig.AddAxis(MT.AxList_WSFEED2);
                    break;
                case 4:
                    if (!COM.wsfeed1.IsDown)
                    {
                        MessageBox.Show("右测试模块有夹具处于打开状态，禁止操作请手动关闭夹具");
                        return;
                    }
                    axisTable.AddAxis(MT.AxList_WSFEED1);
                    axisConfig.AddAxis(MT.AxList_WSFEED1);
                    break;
                case 5:
                    axisTable.AddAxis(MT.Axlist_UDL1_ExpLC);
                    axisTable.AddAxis(MT.Axlist_UDL2_ExpLC);
                    axisTable.AddAxis(MT.Axlist_UDL_LC);
                    axisTable.AddAxis(MT.AxList_WSFEED);
                    axisConfig.AddAxis(MT.Axlist_UDL1_ExpLC);
                    axisConfig.AddAxis(MT.Axlist_UDL2_ExpLC);
                    axisConfig.AddAxis(MT.Axlist_UDL_LC);
                    axisConfig.AddAxis(MT.AxList_WSFEED);
                    break;
            }
            bupdate = true;
        }

        private void ctb_sys_SelectedIndexChanged(object sender, EventArgs e)
        {
            bupdate = true;
            CogRecordDisplay_sys.list_cam.Clear();
            CogRecordDisplay_sys.AddCam(COM.ListCam);
            if (((CTabControl)sender).SelectedIndex !=2)
                ioTable.ShowCfg(-1);
            if ((((CTabControl)sender).SelectedTab.Name) == "tp_sysparm") ShowData();
 
        }

        private void btn_update_card_Click(object sender, EventArgs e)
        {
            string str = string.Empty;
            foreach (CARD card in MT.CardList)
            {
                if (card.isReady)
                {
                    if (EM_RES.OK == card.DownLoadFile()) str = str + String.Format("{0}/id{1},更新配置成功！\r\n", card.disc, card.id);
                    else str = str + String.Format("{0}/id{1},更新配置失败！\r\n", card.disc, card.id);
                }
                else
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, String.Format("{0}/id{1},未初始化！", card.disc, card.id));
                    str = str + String.Format("{0}/id{1},未初始化！\r\n", card.disc, card.id);
                }
            }
            MessageBox.Show(str);
        }

        private void btn_stop_Click(object sender, EventArgs e)
        {
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, String.Format("stop"));
            VAR.gsys_set.bquit = true;
            MT.AllAxStop();            
        }

        private void ctb_io_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(((CTabControl)sender).SelectedTab.Name)
            {


                case "wsfeed2":
                    ioTable.ClearIO();
                    ioTable.AddIO(MT.List_GPIO_OUTWSFEED2);
                    ioTable.Top = 60;
                    ioTable.Left = 30;
                    ioTable.Width = ctb_io_type.Width;
                    ioTable.Height = 780;
                    break;
                case "wsfeed1":
                    ioTable.ClearIO();
                    ioTable.AddIO(MT.List_GPIO_OUTWSFEED1);
                    ioTable.Top = 60;
                    ioTable.Left = 30;
                    ioTable.Width = ctb_io_type.Width;
                    ioTable.Height = 780;
                    break;
                case "tp_in":
                    cylinderTable.Visible = false;
                    ioTable.Visible = true;
                    ioTable.Top = 60;
                    ioTable.Left = 30;
                    ioTable.Width = ctb_io_type.Width;
                    ioTable.Height = 780;
                    ioTable.ShowCfg(1);
                    break;
                case "tp_out":
                    cylinderTable.Visible = false;
                    ioTable.Visible = true;
                    ioTable.Top = 60;
                    ioTable.Left = 30;
                    ioTable.Width = ctb_io_type.Width;
                    ioTable.Height = 780;
                    ioTable.ShowCfg(0);
                    break;
                case "tp_cy":
                    ioTable.Visible = false;
                    cylinderTable.Visible = true;
                    cylinderTable.Top = 60;
                    cylinderTable.Left = 30;
                    cylinderTable.Width = ctb_io_type.Width;
                    cylinderTable.Height = 780;
                    break;
            }
            
        }

         public void ShowData()
        {
            rad_G4C.Checked = PT_SET.bG4C;
            rad_unlight.Checked = PT_SET.unlight;
            rad_light_xsjpro.Checked = PT_SET.bXSjNew;
            rad_light_xsj.Checked = !PT_SET.bG4C && !PT_SET.unlight&& !PT_SET.bXSjNew;
            
            //二维码扫码方式
            rbtn_dwcode.Checked = false;
            rbtn_upcode.Checked = false;
            rbtn_NoCap.Checked = false;
            if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.UP_SCAN) rbtn_upcode.Checked = true;
            else if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN) rbtn_dwcode.Checked = true;
            else rbtn_NoCap.Checked = true;
            //高透模偏移检测
            chk_GTMCheck.Checked = PT_SET.bGTMCheck;
            nud_GTMOfs.Value = (decimal)PT_SET.GTMOfs;

            chk_longxt.Checked = PT_SET.blongXT;

            //运行模式
            rbtn_md1.Checked = false;
            rbtn_md2.Checked = false;
            rbtn_twomd.Checked = false;
            rbtn_twowork.Checked = false;
            if (PT_SET.UpDownRunMode==(int)PT_SET.RUN_MD.BOTH_WORK) rbtn_twomd.Checked = true;
            else if(PT_SET.UpDownRunMode==(int)PT_SET.RUN_MD.MD1_WORK) rbtn_md1.Checked = true;
            else if (PT_SET.UpDownRunMode == (int)PT_SET.RUN_MD.MD2_WORK) rbtn_md2.Checked = true;
            else if (PT_SET.UpDownRunMode == (int)PT_SET.RUN_MD.ALONE_WORK) rbtn_twowork.Checked = true;
            //运行方式
            rbtn_run_normal.Checked = false;
            rbtn_run_updw.Checked = false;
            rbtn_run_empty.Checked = false;
            if (PT_SET.RunPattern == (int)PT_SET.RUN_PATTERN.RUN_NORMAL)rbtn_run_normal.Checked = true;
            else if (PT_SET.RunPattern == (int)PT_SET.RUN_PATTERN.RUN_UPDW) rbtn_run_updw.Checked = true;
            else if (PT_SET.RunPattern == (int)PT_SET.RUN_PATTERN.RUN_EMPTY) rbtn_run_empty.Checked = true;

            //开图顺序设置
            rbtn_next_turnon.Checked = false;
            rbtn_pre_turnon.Checked = false;
            if (PT_SET.turnon) rbtn_pre_turnon.Checked = true;
            else rbtn_next_turnon.Checked = true;
            nud_OpenDly.Value = PT_SET.OpenDly;

            //料盘二维码拍照设置
            rbtn_traybaren.Checked = false;
            rbtn_traybardis.Checked = false;
            if (PT_SET.TrayBarcodeEn) rbtn_traybaren.Checked = true;
            else rbtn_traybardis.Checked = true;
            //OK品下料检测
            rbtn_OkCheckEn.Checked = false;
            rbtn_OkCheckDis.Checked = false;
            if (PT_SET.bOkCheck) rbtn_OkCheckEn.Checked = true;
            else rbtn_OkCheckDis.Checked = true;
            //开关取放料角度设置
            rbtn_open_degree.Checked = false;
            rbtn_close_degree.Checked = false;
            if (PT_SET.isopen_degree) rbtn_open_degree.Checked = true;
            else rbtn_close_degree.Checked = true;
            nud_offset_degree.Value = PT_SET.degree;

            rbtn_open_okdegree.Checked = false;
            rbtn_close_okdegree.Checked = false;
            if (PT_SET.isopen_okdegree) rbtn_open_okdegree.Checked = true;
            else rbtn_close_okdegree.Checked = true;
            nud_offset_okdegree.Value = PT_SET.okdegree;
            //夹具扫码gy0123
            btnStartJigSan1.Checked = COM.wsfeed1.bjigSan;
            btnStartJigSan2.Checked = COM.wsfeed2.bjigSan;
            rabt_jigscan_ON.Checked = PT_SET.bJigSan;
            rabt_jigscan_OFF.Checked = !PT_SET.bJigSan;
            //二维码回检设备
            rbtn_OpenBarCamBack.Checked = false;
            rbtn_CloseBarCamBack.Checked = false;
            if (PT_SET.bBarcodeCamBackEn) rbtn_OpenBarCamBack.Checked = true;
            else rbtn_CloseBarCamBack.Checked = true;
            //视觉回检
            Chk_OpenVs.Checked = PT_SET.bEnVsFB;
            nud_vschk_xyofs.Value = (decimal)PT_SET.Vs_XYofs;
            nud_vschk_rofs.Value = (decimal)PT_SET.Vs_Rofs;

             //回检失败继续运行设置
            rbtn_backerrcontinueen.Checked = false;
            rbtn_backerrcontinuedis.Checked = false;

            if (PT_SET.bBackerrcontinue) rbtn_backerrcontinueen.Checked = true;
            else rbtn_backerrcontinuedis.Checked=true;


            //料盘位置检测
            rbtn_OpenVsTray.Checked = false;
            rbtn_CloseVsTray.Checked = false;
            // 料盘取料                          
            if (PT_SET.bteleconvertergohome) btnbtelgohome.Checked = true;
            else
            {
                btnbtelungohome.Checked = true;
            }
            
            if (PT_SET.btlightgohome) btnlightgohome.Checked = true;
            else
            {
                btnlightungohome.Checked = true;
            }

            if (PT_SET.bput_pallet_vs) rbtn_put_pallet_vs.Checked = true;
            if (PT_SET.Fly_pallet_vs) rbtn_fly_pallet_vs.Checked = true;
            else
            {
                rbtn_unfly_pallet_vs.Checked = true;
            }
            //if (PT_SET.placecps_pallet_vs) rbtn_placecps_pallet_vs.Checked = true;
            
            if (PT_SET.bEnVsTray) rbtn_OpenVsTray.Checked = true;
            else rbtn_CloseVsTray.Checked = true;

            if (PT_SET.Openwithbox) rbtn_Openwithbox.Checked = true;
            else rbtn_Closewithbox.Checked = true;

            //同工位连续同NG提示
            chk_SameNGTip.Checked = PT_SET.bSameNGTip;
            nud_SameNGTipCnt.Value = (decimal)PT_SET.SameNGTipCnt;
            //同工位连续同NG提示
            chk_SameRowNGTip.Checked = PT_SET.bSameRowNGTip;
            nud_SameRowNGTipCnt.Value = (decimal)PT_SET.SameRowNGTipCnt;
            //良率管控
            rbtn_NgWarningEn.Checked = false;
            rbtn_NgWarningDis.Checked = false;
            if (PT_SET.bNgWarn) rbtn_NgWarningEn.Checked = true;
            else rbtn_NgWarningDis.Checked = true;
            nud_okRate.Value = (decimal)PT_SET.OkRate;

            //上下料移动保护(针对转盘)
            rbtn_safeoff.Checked = false;
            rbtn_safeon.Checked = false;
            if (PT_SET.bUdMovSafe) rbtn_safeon.Checked = true;
            else rbtn_safeoff.Checked = true;
             
             //门禁

            chk_tm.Checked = PT_SET.bEnTMDr;
            chk_updown.Checked = PT_SET.bEnUpDownDr;

            //GRR流程

            chk_grr.Checked = PT_SET.bGrrFlow;
            nud_grrtestcnt.Value = PT_SET.GRRTestCnt;
            nud_grrtudlcnt.Value = PT_SET.GRRUdlCnt;
             nud_grrwsid.Value= PT_SET.GRRWSID;
            //相机存储图片
            chk_upcam1.Checked = COM.CamUp1.bSaveImage;
            chk_dwcam1.Checked = COM.CamDw1.bSaveImage;
            chk_upcam2.Checked = COM.CamUp2.bSaveImage;
            chk_dwcam2.Checked = COM.CamDw2.bSaveImage;

            //回检管控
            nud_LeftArea.Value = (decimal)PT_SET.LeftArea;
            nud_RightArea.Value = (decimal)PT_SET.RightArea;
            nud_UpArea.Value = (decimal)PT_SET.UpArea;
            nud_DownArea.Value = (decimal)PT_SET.DownArea;
            nud_Areaofset.Value = (decimal)PT_SET.Area;

            nud_LeftArea2.Value = (decimal)PT_SET.LeftArea2;
            nud_RightArea2.Value = (decimal)PT_SET.RightArea2;
            nud_UpArea2.Value = (decimal)PT_SET.UpArea2;
            nud_DownArea2.Value = (decimal)PT_SET.DownArea2;
            nud_Areaofset2.Value = (decimal)PT_SET.Area2;
            cb_ConnectorCheck.Checked = PT_SET.bConnectorCheck;
            nud_testsafey.Value = (decimal)PT_SET.testsafey;

            //模组带起
            Chk_ModPasteUp.Checked = PT_SET.bModPasteUp;
            nud_PlaceDly.Value = PT_SET.PlaceDly;
            nud_MovUpDly.Value = PT_SET.MovUpDly;

            if (PT_SET.IsMesLocal) rbtn_MesLocal.Checked = true;
            else rbtn_MesRemote.Checked = true;
            //相机参数设置
            rbtn_camcfgsetdis.Checked = false;
            rbtn_camcfgseten.Checked = false;
            if (PT_SET.BCamcfgset) rbtn_camcfgseten.Checked = true;
            else rbtn_camcfgsetdis.Checked = true;
            nud_tray_exposure1.Value = (decimal)PT_SET.TrayExposure[0];
            nud_tray_exposure2.Value = (decimal)PT_SET.TrayExposure[1];
            nud_tray_brightness1.Value = (decimal)PT_SET.TrayBrightness[0];
            nud_tray_brightness2.Value = (decimal)PT_SET.TrayBrightness[1];
            nud_tray_contrast1.Value = (decimal)PT_SET.TrayContrast[0];
            nud_tray_contrast2.Value = (decimal)PT_SET.TrayContrast[1];

            nud_ws_exposure1.Value = (decimal)PT_SET.WsExposure[0];
            nud_ws_exposure2.Value = (decimal)PT_SET.WsExposure[1];

            nud_ws_brightness1.Value = (decimal)PT_SET.WsBrightness[0];
            nud_ws_brightness2.Value = (decimal)PT_SET.WsBrightness[1];

            nud_ws_contrast1.Value = (decimal)PT_SET.WsContrast[0];
            nud_ws_contrast2.Value = (decimal)PT_SET.WsContrast[1];
            nud_jig_exposure1.Value = (decimal)PT_SET.JigExposure[0];
            nud_jig_exposure2.Value = (decimal)PT_SET.JigExposure[1];
            nud_jig_brightness1.Value = (decimal)PT_SET.JigBrightness[0];
            nud_jig_brightness2.Value = (decimal)PT_SET.JigBrightness[1];
            nud_jig_contrast1.Value = (decimal)PT_SET.JigContrast[0];
            nud_jig_contrast2.Value = (decimal)PT_SET.JigContrast[1];
            cbcity.SelectedIndex = PT_SET.citynum;
            ckupload.Checked = PT_SET.upload;

            rbtUpDn1OneXtOff.Checked = false;
            rbtUpDn1OneXtOn.Checked = false;
            rbtUpDn2OneXtOff.Checked = false;
            rbtUpDn2OneXtOn.Checked = false;
            if (PT_SET.bUpDn1XtOnlyOne) rbtUpDn1OneXtOn.Checked = true; else rbtUpDn1OneXtOff.Checked = true;
            if (PT_SET.bUpDn2XtOnlyOne) rbtUpDn2OneXtOn.Checked = true; else rbtUpDn2OneXtOff.Checked = true;

            chkPreMiniWs1AutoCheck.Checked = PT_SET.PreMiniAutoCheckWs1En;
            chkPreMiniWs2AutoCheck.Checked = PT_SET.PreMiniAutoCheckWs2En;
            chkPreMiniWs1DistanceCheck.Checked = PT_SET.PreMiniWs1DistanceCheckEn;
            chkPreMiniWs1LuxCctCheck.Checked = PT_SET.PreMiniWs1LuxCctCheckEn;
            chkPreMiniWs2DistanceCheck.Checked = PT_SET.PreMiniWs2DistanceCheckEn;
            chkPreMiniWs2LuxCctCheck.Checked = PT_SET.PreMiniWs2LuxCctCheckEn;
            cmbPreMiniWs1DistanceCom.Text = PT_SET.PreMiniWs1AutoCheckCom;
            cmbPreMiniWs2DistanceCom.Text = PT_SET.PreMiniWs2AutoCheckCom;
            cmbPreMiniAutoCheckStartSta.Text = PT_SET.PreMiniAutoCheckStartSta.ToString();
            txtPreMiniWs1LightCheckZ1.Text = PT_SET.PreMiniWs1LightCheckZ1.ToString("F3");
            txtPreMiniWs1LightCheckZ2.Text = PT_SET.PreMiniWs1LightCheckZ2.ToString("F3");
            txtPreMiniWs1LightCheckZ3.Text = PT_SET.PreMiniWs1LightCheckZ3.ToString("F3");
            txtPreMiniWs2LightCheckZ1.Text = PT_SET.PreMiniWs2LightCheckZ1.ToString("F3");
            txtPreMiniWs2LightCheckZ2.Text = PT_SET.PreMiniWs2LightCheckZ2.ToString("F3");
            txtPreMiniWs2LightCheckZ3.Text = PT_SET.PreMiniWs2LightCheckZ3.ToString("F3");
            DateTime autoCheckTime1;
            DateTime autoCheckTime2;
            if (!DateTime.TryParse(PT_SET.CheckTimeMorning1, out autoCheckTime1)) autoCheckTime1 = DateTime.Today.AddHours(8);
            if (!DateTime.TryParse(PT_SET.CheckTimeEvening2, out autoCheckTime2)) autoCheckTime2 = DateTime.Today.AddHours(20);
            dtpPreMiniAutoCheckTime1.Value = autoCheckTime1;
            dtpPreMiniAutoCheckTime2.Value = autoCheckTime2;
            rbtnPreMiniHourly.Checked = PT_SET.EnvironmentCheckScheduleMode == 1;
            rbtnPreMiniFixedTime.Checked = !rbtnPreMiniHourly.Checked;
            nudPreMiniAutoCheckTimer.Value = Math.Max(1, Math.Min(24, (decimal)PT_SET.CheckTimer));
            UpdatePreMiniAutoCheckScheduleControls();

        }

        private void PreMiniAutoCheckScheduleMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePreMiniAutoCheckScheduleControls();
        }

        private void UpdatePreMiniAutoCheckScheduleControls()
        {
            bool fixedTime = rbtnPreMiniFixedTime.Checked;
            dtpPreMiniAutoCheckTime1.Enabled = fixedTime;
            dtpPreMiniAutoCheckTime2.Enabled = fixedTime;
            labelPreMiniAutoCheckTime1.Enabled = fixedTime;
            labelPreMiniAutoCheckTime2.Enabled = fixedTime;
            nudPreMiniAutoCheckTimer.Enabled = !fixedTime;
            labelPreMiniAutoCheckTimer.Enabled = !fixedTime;
            PT_SET.EnvironmentCheckScheduleMode = rbtnPreMiniHourly.Checked ? 1 : 0;
        }

        private bool ValidatePreMiniAutoCheckComs()
        {
            string ws1Com = cmbPreMiniWs1DistanceCom.Text.Trim();
            string ws2Com = cmbPreMiniWs2DistanceCom.Text.Trim();
            bool ws1CheckEnabled = chkPreMiniWs1DistanceCheck.Checked || chkPreMiniWs1LuxCctCheck.Checked;
            bool ws2CheckEnabled = chkPreMiniWs2DistanceCheck.Checked || chkPreMiniWs2LuxCctCheck.Checked;
            if (chkPreMiniWs1AutoCheck.Checked && ws1CheckEnabled && string.IsNullOrEmpty(ws1Com))
            {
                MessageBox.Show("右工站已启用自动点检，请设置综合点检COM。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (chkPreMiniWs2AutoCheck.Checked && ws2CheckEnabled && string.IsNullOrEmpty(ws2Com))
            {
                MessageBox.Show("左工站已启用自动点检，请设置综合点检COM。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (chkPreMiniWs1AutoCheck.Checked && ws1CheckEnabled && chkPreMiniWs2AutoCheck.Checked && ws2CheckEnabled &&
                string.Equals(ws1Com, ws2Com, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("左右工站不能使用同一个综合点检COM。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

         public void GetData()
         {
            //夹具扫码gy0123
            COM.wsfeed1.bjigSan = btnStartJigSan1.Checked;
            COM.wsfeed2.bjigSan = btnStartJigSan2.Checked;
            PT_SET.bJigSan = rabt_jigscan_ON.Checked;
            PT_SET.JigCntSend = (int)nud_JigSendTime.Value;

            //光源选择
            PT_SET.bG4C = rad_G4C.Checked;
            PT_SET.unlight = rad_unlight.Checked;
            PT_SET.bXSjNew = rad_light_xsjpro.Checked;
            //二维码扫码方式
            if (rbtn_upcode.Checked) PT_SET.BarcodeMode = (int)PT_SET.BAR_SCAN.UP_SCAN;
            else if (rbtn_dwcode.Checked) PT_SET.BarcodeMode = (int)PT_SET.BAR_SCAN.DW_SCAN;
            else PT_SET.BarcodeMode = (int)PT_SET.BAR_SCAN.NO_SCAN;

            //运行模式
            if (rbtn_twomd.Checked) PT_SET.UpDownRunMode = (int)PT_SET.RUN_MD.BOTH_WORK;
             else if (rbtn_md1.Checked) PT_SET.UpDownRunMode = (int)PT_SET.RUN_MD.MD1_WORK;
             else if (rbtn_md2.Checked) PT_SET.UpDownRunMode = (int)PT_SET.RUN_MD.MD2_WORK;
             else if (rbtn_twowork.Checked) PT_SET.UpDownRunMode = (int)PT_SET.RUN_MD.ALONE_WORK; 

             //运行方式
            if (rbtn_run_normal.Checked) PT_SET.RunPattern = (int)PT_SET.RUN_PATTERN.RUN_NORMAL;
             else if (rbtn_run_updw.Checked) PT_SET.RunPattern = (int)PT_SET.RUN_PATTERN.RUN_UPDW;
             else if (rbtn_run_empty.Checked) PT_SET.RunPattern = (int)PT_SET.RUN_PATTERN.RUN_EMPTY;
            ////视觉回检
            //PT_SET.bEnVsFB = Chk_OpenVs.Checked;
            PT_SET.Vs_XYofs = (double)nud_vschk_xyofs.Value;
            PT_SET.Vs_Rofs = (double)nud_vschk_rofs.Value;
            //料盘二维码拍照设置
            if (rbtn_traybaren.Checked) PT_SET.TrayBarcodeEn = true;
            else if (rbtn_traybardis.Checked) PT_SET.TrayBarcodeEn = false;
            //回检失败继续运行设置
            if (rbtn_backerrcontinueen.Checked) PT_SET.bBackerrcontinue = true;
            else if (rbtn_backerrcontinuedis.Checked) PT_SET.bBackerrcontinue = false;
            //开图顺序设置
            if (rbtn_pre_turnon.Checked) PT_SET.turnon = true;
            else if (rbtn_next_turnon.Checked) PT_SET.overturnon = true;
            PT_SET.OpenDly = (int)nud_OpenDly.Value;
            //取放料开放角度设置
            if (rbtn_open_degree.Checked) PT_SET.isopen_degree = true;
            else if (rbtn_close_degree.Checked) PT_SET.isopen_degree = false;
            PT_SET.degree = (int)nud_offset_degree.Value;

            if (rbtn_open_okdegree.Checked) PT_SET.isopen_okdegree = true;
            else if (rbtn_close_okdegree.Checked) PT_SET.isopen_okdegree = false;
            PT_SET.okdegree = (int)nud_offset_okdegree.Value;
            //料盘取料拍照设置                          
            if (rbtn_put_pallet_vs.Checked) PT_SET.bput_pallet_vs = true;
             else if (rbtn_put_pallet_un_vs.Checked) PT_SET.bput_pallet_vs = false;
             if (rbtn_fly_pallet_vs.Checked) PT_SET.Fly_pallet_vs = true;
             else if (rbtn_unfly_pallet_vs.Checked) PT_SET.Fly_pallet_vs = false;
            //if (rbtn_placecps_pallet_vs.Checked) PT_SET.placecps_pallet_vs = true;
            //else if (rbtn_unplacecps_pallet_vs.Checked) PT_SET.placecps_pallet_vs = false;
            //增距镜用完后是否回零设置
           
            if (btnbtelgohome.Checked) PT_SET.bteleconvertergohome = true;
            else if (btnbtelungohome.Checked) PT_SET.bteleconvertergohome = false;

            if (btnlightgohome.Checked) PT_SET.btlightgohome = true;
            else if (btnlightungohome.Checked) PT_SET.btlightgohome = false;

            //二维码回检设置
            if (rbtn_OpenBarCamBack.Checked) PT_SET.bBarcodeCamBackEn = true;
            else if (rbtn_CloseBarCamBack.Checked) PT_SET.bBarcodeCamBackEn = false;

            //if (rbtn_PickOpenVs.Checked) PT_SET.bput_pallet_vs = true;
            //else if (rbtn_UnPickOpenVs.Checked) PT_SET.bput_pallet_vs = false;
            //Mes模式
            if (rbtn_MesLocal.Checked) PT_SET.IsMesLocal = true;
            else if (rbtn_MesRemote.Checked) PT_SET.IsMesLocal = false;
            //良率管控
            if (rbtn_NgWarningEn.Checked) PT_SET.bNgWarn = true;
            else if (rbtn_NgWarningDis.Checked) PT_SET.bNgWarn = false;
            PT_SET.OkRate = (double)nud_okRate.Value;

            //回检管控
            PT_SET.Area = (double)nud_Areaofset.Value;
            PT_SET.LeftArea = (double)nud_LeftArea.Value;
            PT_SET.RightArea = (double)nud_RightArea.Value;
            PT_SET.UpArea = (double)nud_UpArea.Value;
            PT_SET.DownArea = (double)nud_DownArea.Value;
            PT_SET.Area2 = (double)nud_Areaofset2.Value;
            PT_SET.LeftArea2 = (double)nud_LeftArea2.Value;
            PT_SET.RightArea2 = (double)nud_RightArea2.Value;
            PT_SET.UpArea2 = (double)nud_UpArea2.Value;
            PT_SET.DownArea2 = (double)nud_DownArea2.Value;
            PT_SET.bConnectorCheck = cb_ConnectorCheck.Checked;
             PT_SET.testsafey  = (double)nud_testsafey.Value;

            //料盘位置检测
            if (rbtn_OpenVsTray.Checked) PT_SET.bEnVsTray = true;
             else if (rbtn_CloseVsTray.Checked) PT_SET.bEnVsTray = false;

            if (rbtn_Openwithbox.Checked) PT_SET.Openwithbox = true;
            else if ( rbtn_Closewithbox.Checked) PT_SET.Openwithbox = false;

            //OK品下料检测设置
            if (rbtn_OkCheckEn.Checked) PT_SET.bOkCheck = true;
            else if (rbtn_OkCheckDis.Checked) PT_SET.bOkCheck = false;

            //Y1轴开启设置
            //if (rbtn_Y1en.Checked) PT_SET.Y1En = true;
            //else if (rbtn_Y1dis.Checked) PT_SET.Y1En = false;
            //上下料移动保护(针对转盘)
            if (rbtn_safeon.Checked) PT_SET.bUdMovSafe = true;
             else if (rbtn_safeoff.Checked) PT_SET.bUdMovSafe = false;
            //门禁
            if (!chk_tm.Checked) chk_tm.Checked = true;
            if (!chk_updown.Checked) chk_updown.Checked = true;
            //PT_SET.bEnUpDownDr = chk_updown.Checked;
            //PT_SET.bEnTMDr = chk_tm.Checked;
            //高透模偏移检测
            PT_SET.bGTMCheck = chk_GTMCheck.Checked;
            PT_SET.GTMOfs = (double)nud_GTMOfs.Value;
            PT_SET.blongXT=chk_longxt.Checked ;


            //同工位连续同NG提示
            PT_SET.bSameNGTip = chk_SameNGTip.Checked;
            PT_SET.SameNGTipCnt = (int)nud_SameNGTipCnt.Value;
            //同工位连续同NG提示
            PT_SET.bSameRowNGTip = chk_SameRowNGTip.Checked;
            PT_SET.SameRowNGTipCnt = (int)nud_SameRowNGTipCnt.Value;

            //GRR流程
            PT_SET.bGrrFlow = chk_grr.Checked;
            PT_SET.GRRTestCnt = (int)nud_grrtestcnt.Value;
            PT_SET.GRRUdlCnt = (int)nud_grrtudlcnt.Value;
            PT_SET.GRRWSID = (int)nud_grrwsid.Value;
            // 相机存储图片
            COM.CamUp1.bSaveImage= chk_upcam1.Checked;
               COM.CamDw1.bSaveImage= chk_dwcam1.Checked;
               COM.CamUp2.bSaveImage= chk_upcam2.Checked;
               COM.CamDw2.bSaveImage= chk_dwcam2.Checked;
            ////模组带起
            PT_SET.bModPasteUp = Chk_ModPasteUp.Checked;
            PT_SET.PlaceDly = (int)nud_PlaceDly.Value;
            PT_SET.MovUpDly = (int)nud_MovUpDly.Value;
            //相机参数设置

            if (rbtn_camcfgseten.Checked) PT_SET.BCamcfgset = true;
            else if (rbtn_camcfgsetdis.Checked) PT_SET.BCamcfgset = false;
            PT_SET.TrayExposure[0] = (double)nud_tray_exposure1.Value;
            PT_SET.TrayExposure[1] = (double)nud_tray_exposure2.Value;
            PT_SET.TrayBrightness[0] = (double)nud_tray_brightness1.Value;
            PT_SET.TrayBrightness[1] = (double)nud_tray_brightness2.Value;
            PT_SET.TrayContrast[0] = (double)nud_tray_contrast1.Value;
            PT_SET.TrayContrast[1] = (double)nud_tray_contrast2.Value;

            PT_SET.JigExposure[0] = (double)nud_jig_exposure1.Value;
            PT_SET.JigExposure[1] = (double)nud_jig_exposure2.Value;
            PT_SET.JigBrightness[0] = (double)nud_jig_brightness1.Value;
            PT_SET.JigBrightness[1] = (double)nud_jig_brightness2.Value;
            PT_SET.JigContrast[0] = (double)nud_jig_contrast1.Value;
            PT_SET.JigContrast[1] = (double)nud_jig_contrast2.Value;
            PT_SET.WsExposure[0] = (double)nud_ws_exposure1.Value;
            PT_SET.WsExposure[1] = (double)nud_ws_exposure2.Value;

            PT_SET.WsBrightness[0] = (double)nud_ws_brightness1.Value;
            PT_SET.WsBrightness[1] = (double)nud_ws_brightness2.Value;

            PT_SET.WsContrast[0] = (double)nud_ws_contrast1.Value;
            PT_SET.WsContrast[1] = (double)nud_ws_contrast2.Value;
            PT_SET.upload= ckupload.Checked;
            PT_SET.citynum = cbcity.SelectedIndex;

            ////吸头屏蔽
            PT_SET.bUpDn1XtOnlyOne = rbtUpDn1OneXtOn.Checked;
            PT_SET.bUpDn2XtOnlyOne = rbtUpDn2OneXtOn.Checked;

            PT_SET.PreMiniAutoCheckWs1En = chkPreMiniWs1AutoCheck.Checked;
            PT_SET.PreMiniAutoCheckWs2En = chkPreMiniWs2AutoCheck.Checked;
            PT_SET.PreMiniWs1DistanceCheckEn = chkPreMiniWs1DistanceCheck.Checked;
            PT_SET.PreMiniWs1LuxCctCheckEn = chkPreMiniWs1LuxCctCheck.Checked;
            PT_SET.PreMiniWs2DistanceCheckEn = chkPreMiniWs2DistanceCheck.Checked;
            PT_SET.PreMiniWs2LuxCctCheckEn = chkPreMiniWs2LuxCctCheck.Checked;
            PT_SET.PreMiniWs1AutoCheckCom = cmbPreMiniWs1DistanceCom.Text.Trim();
            PT_SET.PreMiniWs2AutoCheckCom = cmbPreMiniWs2DistanceCom.Text.Trim();
            int preMiniStartSta;
            if (int.TryParse(cmbPreMiniAutoCheckStartSta.Text, out preMiniStartSta)) PT_SET.PreMiniAutoCheckStartSta = preMiniStartSta;
            double preMiniZ;
            if (double.TryParse(txtPreMiniWs1LightCheckZ1.Text, out preMiniZ)) PT_SET.PreMiniWs1LightCheckZ1 = preMiniZ;
            if (double.TryParse(txtPreMiniWs1LightCheckZ2.Text, out preMiniZ)) PT_SET.PreMiniWs1LightCheckZ2 = preMiniZ;
            if (double.TryParse(txtPreMiniWs1LightCheckZ3.Text, out preMiniZ)) PT_SET.PreMiniWs1LightCheckZ3 = preMiniZ;
            if (double.TryParse(txtPreMiniWs2LightCheckZ1.Text, out preMiniZ)) PT_SET.PreMiniWs2LightCheckZ1 = preMiniZ;
            if (double.TryParse(txtPreMiniWs2LightCheckZ2.Text, out preMiniZ)) PT_SET.PreMiniWs2LightCheckZ2 = preMiniZ;
            if (double.TryParse(txtPreMiniWs2LightCheckZ3.Text, out preMiniZ)) PT_SET.PreMiniWs2LightCheckZ3 = preMiniZ;
            PT_SET.CheckTimeMorning1 = dtpPreMiniAutoCheckTime1.Value.ToString("HH:mm:ss");
            PT_SET.CheckTimeEvening2 = dtpPreMiniAutoCheckTime2.Value.ToString("HH:mm:ss");
            PT_SET.CheckTimer = (double)nudPreMiniAutoCheckTimer.Value;
            PT_SET.EnvironmentCheckScheduleMode = rbtnPreMiniHourly.Checked ? 1 : 0;
        }

        private TextBox GetPreMiniLightCheckZTextBox(string tag)
        {
            switch (tag)
            {
                case "R1": return txtPreMiniWs1LightCheckZ1;
                case "R2": return txtPreMiniWs1LightCheckZ2;
                case "R3": return txtPreMiniWs1LightCheckZ3;
                case "L1": return txtPreMiniWs2LightCheckZ1;
                case "L2": return txtPreMiniWs2LightCheckZ2;
                case "L3": return txtPreMiniWs2LightCheckZ3;
                default: return null;
            }
        }

        private AXIS GetPreMiniLightCheckZAxis(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            if (tag.StartsWith("R")) return COM.wsfeed1 != null ? COM.wsfeed1.ax_z_fr : null;
            if (tag.StartsWith("L")) return COM.wsfeed2 != null ? COM.wsfeed2.ax_z_fr : null;
            return null;
        }

        private void btnPreMiniLightCheckZLearn_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            string tag = btn == null ? string.Empty : Convert.ToString(btn.Tag);
            TextBox txt = GetPreMiniLightCheckZTextBox(tag);
            AXIS axis = GetPreMiniLightCheckZAxis(tag);
            if (txt == null || axis == null)
            {
                MessageBox.Show("点检Z轴配置异常", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            txt.Text = axis.fenc_pos.ToString("F3");
        }

        private void btnPreMiniLightCheckZMove_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            string tag = btn == null ? string.Empty : Convert.ToString(btn.Tag);
            TextBox txt = GetPreMiniLightCheckZTextBox(tag);
            AXIS axis = GetPreMiniLightCheckZAxis(tag);
            if (txt == null || axis == null)
            {
                MessageBox.Show("点检Z轴配置异常", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double pos;
            if (!double.TryParse(txt.Text, out pos))
            {
                MessageBox.Show("点检Z位置格式错误", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool bquit = false;
            MT.SetAllAxToManualSpd();
            EM_RES res = MT.Move(ref bquit, ref axis, pos);
            if (res != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}定位到点检Z位置失败:{1:F3}", axis.str_disc, pos));
                MessageBox.Show("点检Z定位失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ManualReadPreMiniDistance(string stationName, string com)
        {
            if (string.IsNullOrWhiteSpace(com))
            {
                MessageBox.Show(string.Format("{0}距离测量COM未设置", stationName), "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量距离按钮点击,Station:{0},COM:{1}", stationName, com));
                double distance = PreMiniAutoChecker.ManualReadDistance(com);
                MessageBox.Show(string.Format("{0}距离测量完成\r\nCOM:{1}\r\nDistance:{2:F3}", stationName, com, distance), "pre-mini手动点检", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}手动测量距离失败,COM:{1},Err:{2}", stationName, com, ex.Message));
                MessageBox.Show(string.Format("{0}距离测量失败\r\nCOM:{1}\r\n{2}", stationName, com, ex.Message), "pre-mini手动点检", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private List<PreMiniAutoChecker.LightChannelReading> ManualReadPreMiniLightChannels(string stationName, string com)
        {
            if (string.IsNullOrWhiteSpace(com))
            {
                MessageBox.Show(string.Format("{0}光源色温测量COM未设置", stationName), "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量三通道光源色温按钮点击,Station:{0},COM:{1}", stationName, com));
            return PreMiniAutoChecker.ManualReadLightChannels(com);
        }

        private void ManualReadPreMiniLux(string stationName, string com)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                List<PreMiniAutoChecker.LightChannelReading> values = ManualReadPreMiniLightChannels(stationName, com);
                if (values == null) return;
                MessageBox.Show(string.Format("{0}照度测量完成\r\nCOM:{1}\r\nCH1 Lux:{2:F3}\r\nCH2 Lux:{3:F3}\r\nCH3 Lux:{4:F3}", stationName, com, values[0].Lux, values[1].Lux, values[2].Lux), "pre-mini手动点检", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}手动测量照度失败,COM:{1},Err:{2}", stationName, com, ex.Message));
                MessageBox.Show(string.Format("{0}照度测量失败\r\nCOM:{1}\r\n{2}", stationName, com, ex.Message), "pre-mini手动点检", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void ManualReadPreMiniCct(string stationName, string com)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                List<PreMiniAutoChecker.LightChannelReading> values = ManualReadPreMiniLightChannels(stationName, com);
                if (values == null) return;
                MessageBox.Show(string.Format("{0}色温测量完成\r\nCOM:{1}\r\nCH1 CCT:{2:F3}\r\nCH2 CCT:{3:F3}\r\nCH3 CCT:{4:F3}", stationName, com, values[0].Cct, values[1].Cct, values[2].Cct), "pre-mini手动点检", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}手动测量色温失败,COM:{1},Err:{2}", stationName, com, ex.Message));
                MessageBox.Show(string.Format("{0}色温测量失败\r\nCOM:{1}\r\n{2}", stationName, com, ex.Message), "pre-mini手动点检", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void btnPreMiniWs1ReadDistance_Click(object sender, EventArgs e)
        {
            ManualReadPreMiniDistance("右工站", cmbPreMiniWs1DistanceCom.Text);
        }

        private void btnPreMiniWs1ReadLuxCct_Click(object sender, EventArgs e)
        {
            ManualReadPreMiniLux("右工站", cmbPreMiniWs1DistanceCom.Text);
        }

        private void btnPreMiniWs1ReadCct_Click(object sender, EventArgs e)
        {
            ManualReadPreMiniCct("右工站", cmbPreMiniWs1DistanceCom.Text);
        }

        private void btnPreMiniWs2ReadDistance_Click(object sender, EventArgs e)
        {
            ManualReadPreMiniDistance("左工站", cmbPreMiniWs2DistanceCom.Text);
        }

        private void btnPreMiniWs2ReadLuxCct_Click(object sender, EventArgs e)
        {
            ManualReadPreMiniLux("左工站", cmbPreMiniWs2DistanceCom.Text);
        }

        private void btnPreMiniWs2ReadCct_Click(object sender, EventArgs e)
        {
            ManualReadPreMiniCct("左工站", cmbPreMiniWs2DistanceCom.Text);
        }

        public bool ReloadUpcamTask(string tskname)
        {
            bool res = true;
            foreach (Cam upcam in COM.ListUpCam)
            {
                Cam.VisionTask tsk = upcam.List_vs_task.Find(s => s.TaskName.Contains(tskname));
                if (tsk != null)
                    res = tsk.Load();
                Cam.VisionTask copytsk = upcam.List_vs_copytask.Find(s => s.TaskName.Contains(tskname));
                if (copytsk != null) upcam.List_vs_copytask.Remove(copytsk);
            }
            return res;
        }

        private void btn_ptset_save_Click(object sender, EventArgs e)
         {
            bool VsUpdate = false;
            bool WsUpdate = false;
            bool res = true;
            int bitOpenMode_temp = 1;
            try
            {
                //判断是否要重新加载任务
                btn_ptset_save.Enabled = false;
                if ((rbtn_upcode.Checked && PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN) ||
                    (rbtn_dwcode.Checked && PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.UP_SCAN) ||
                    (rbtn_NoCap.Checked && PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.NO_SCAN)) VsUpdate = true;
                if (PT_SET.bGTMCheck != chk_GTMCheck.Checked) VsUpdate = true;
                if (Math.Abs(PT_SET.GTMOfs - (double)nud_GTMOfs.Value) > 0.02) VsUpdate = true;
                if (VsUpdate)
                {
                    res = ReloadUpcamTask(CONST.ModUpFw);
                    if (!res)
                    {
                        MessageBox.Show(VAR.IsChinese ? string.Format("保存失败,{0}加载失败!", CONST.ModUpFw) : string.Format("Save failed, {0} failed to load!\r\n保存失败,{0}加载失败!", CONST.ModUpFw), VAR.IsChinese ? "警告" : "Warning");
                        return;
                    }
                }
                VsUpdate = false;
                if (PT_SET.bBarcodeCamBackEn != rbtn_OpenBarCamBack.Checked) VsUpdate = true;
                if (VsUpdate)
                {
                    res = ReloadUpcamTask(CONST.WsModUpFw);
                    if (!res)
                    {
                        MessageBox.Show(VAR.IsChinese ? string.Format("保存失败,{0}加载失败!", CONST.WsModUpFw) : string.Format("Save failed, {0} failed to load!\r\n保存失败,{0}加载失败!", CONST.WsModUpFw), VAR.IsChinese ? "警告" : "Warning");
                        return;
                    }
                }
                VsUpdate = false;
                //if (rbtn_OpenWsVsAddCheck.Checked != PT_SET.bWsVsAddCheckEn) VsUpdate = true;
                //if (VsUpdate)
                //{
                //    res = ReloadUpcamTask(CONST.WsUpFw);
                //    if (!res)
                //    {
                //        MessageBox.Show(VAR.IsChinese ? string.Format("保存失败,{0}加载失败!", CONST.WsUpFw) : string.Format("Save failed, {0} failed to load!\r\n保存失败,{0}加载失败!", CONST.WsUpFw), VAR.IsChinese ? "警告" : "Warning");
                //        return;
                //    }
                //}

                //if (rbtn_single.Checked) bitOpenMode_temp = 1;
                //else if (rbtn_double.Checked) bitOpenMode_temp = 2;
                //else if (rbtn_Lag.Checked) bitOpenMode_temp = 3;
                //else if (rbtn_Sml.Checked) bitOpenMode_temp = 4;
                //if (PT_SET.LbEn != rbtn_lben.Checked || PT_SET.issmall != rbtn_small.Checked ||
                //    PT_SET.bitOpenMode != bitOpenMode_temp) WsUpdate = true;



                if (!ValidatePreMiniAutoCheckComs()) return;
                GetData();
                PT_SET.SavePtCfg(VAR.gsys_set.cur_product_name);
                PT_SET.LoadPtCfg(VAR.gsys_set.cur_product_name);
                foreach (Cam cam in COM.ListCam)
                {
                    cam.LoadOrSaveImgCfg();
                    cam.LoadOrSaveImgCfg(false);
                }

                foreach (XT xt in COM.ListXT)
                {
                    xt.LoadOrSaveCapOfsCfg();
                    xt.LoadOrSaveCapOfsCfg(false);
                }
                if (WsUpdate)
                {

                    foreach (WSFEED ws in COM.list_wsfeed)
                    {
                        foreach (WSFEED.MdDat md in ws.list_md)
                        {
                            md.benable = true;
                        }
                        ws.SaveCfg(VAR.gsys_set.cur_product_name);
                        ws.LoadCfg(VAR.gsys_set.cur_product_name);
                        ws.SaveCfgws(VAR.gsys_set.cur_product_name);
                        ws.LoadCfgws(VAR.gsys_set.cur_product_name);
                    }
                }
                ShowData();
                MessageBox.Show(VAR.IsChinese ? "保存成功!" : "Saved successfully!\r\n保存成功!", VAR.IsChinese ? "提示" : "Prompt");
            }
            finally
            {
                btn_ptset_save.Enabled = true;
            }
        }

         private void rbtn_put_pallet_vs_CheckedChanged(object sender, EventArgs e)
         {

         }

        private void rbtn_MesRemote_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void rbtn_camcfgsetdis_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

                bool VsUpdate = false;
                bool WsUpdate = false;
                bool res = true;
                int bitOpenMode_temp = 1;
                try
                {

                    if (!ValidatePreMiniAutoCheckComs()) return;
                    GetData();
                    PT_SET.SavePtCfg(VAR.gsys_set.cur_product_name);
                    PT_SET.LoadPtCfg(VAR.gsys_set.cur_product_name);
                    foreach (Cam cam in COM.ListCam)
                    {
                        cam.LoadOrSaveImgCfg();
                        cam.LoadOrSaveImgCfg(false);
                    }

                    foreach (XT xt in COM.ListXT)
                    {
                        xt.LoadOrSaveCapOfsCfg();
                        xt.LoadOrSaveCapOfsCfg(false);
                    }
                    if (WsUpdate)
                    {

                        foreach (WSFEED ws in COM.list_wsfeed)
                        {
                            foreach (WSFEED.MdDat md in ws.list_md)
                            {
                                md.benable = true;
                            }
                            ws.SaveCfg(VAR.gsys_set.cur_product_name);
                            ws.LoadCfg(VAR.gsys_set.cur_product_name);
                            ws.SaveCfgws(VAR.gsys_set.cur_product_name);
                            ws.LoadCfgws(VAR.gsys_set.cur_product_name);
                        }
                    }
                    ShowData();
                    MessageBox.Show(VAR.IsChinese ? "保存成功!" : "Saved successfully!\r\n保存成功!", VAR.IsChinese ? "提示" : "Prompt");
                }
                finally
                {
                    btn_ptset_save.Enabled = true;
                }
            }
        }
   
}
