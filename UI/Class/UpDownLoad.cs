using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.Implementation.Internal;
using Microsoft.SqlServer.Server;
using MotionCtrl;
using DevReport;
using System.Diagnostics;
using Win32Lib;
using Win32Lib;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using System.IO;
using static UI.Product.Tray;
using ControlzEx.Standard;

namespace UI
{
    public class UpDownLoad
    {
        #region 状态
        public enum EM_STA
        {
            [Description("未知")]
            UNKNOW,
            [Description("忙")]
            BUSY,
            [Description("等待")]
            WAIT,
            [Description("回零中")]
            HOME,
            [Description("就绪")]
            READY,
            [Description("GRR测试")]
            GRRTEST,
            [Description("取放检测")]
            CHECK,
            [Description("工站取料")]
            PICKWS,
            [Description("料盘放料")]
            PLACETRAY,
            [Description("飞拍失败放料")]
            FLYERRPLACE,
            [Description("料盘取料")]
            PICKTRAY,
            [Description("工站放料")]
            PLACEWS,
            [Description("上下料结束")]
            UPDOWNLOADEND,
            [Description("仓储进出料")]
            CALLTRAY,
            [Description("GRR拍照")]
            GRR_CAM,
            [Description("错误")]
            ERR,
            [Description("退出")]
            QUIT
        }

        //寻找工站模组方式
        public enum EM_FIN_MOD
        {
            ALL,    //全工站搜索
            LOW,    //低8位工站搜索
            HIGH,    //高8位工站搜索
            NONE    //不搜索

        }

        //飞拍流程配置
        public enum EM_FLY_CFG
        {
            MOD_DW,   //下相机拍模组底部
            MOD_UP,   //上相机拍模组表面
            WS_UP,    //上相机拍工站表面
            WS_MOD_UP //上相机拍工站上物料
        }
        #endregion

        #region 委托CheckBox
        public delegate void ReadChkHandler();
        public static void SetCalResult()
        {
            if (FrMain.frrun.ckb_wait_upload.InvokeRequired)
            {
                ReadChkHandler set = new ReadChkHandler(SetCalResult);//委托的方法参数应和SetCalResult一致
                FrMain.frrun.ckb_wait_upload.Invoke(set, new object[] { }); //此方法第二参数用于传入方法,代替形参result
            }
            else
            {
                bWaitforUpDownload = FrMain.frrun.ckb_wait_upload.Checked;
            }
        }


        #endregion

        #region  参数
        //上下料id
        public int id;
        //上下料描述符
        public string disc;
        //第一次开机标志
        public bool IsFirst = true;
        public bool modchange = true;//工站序号变化
        //错误信息
        public string ErrMsg = string.Empty;
        //当前放料最后工站拍照坐标
        public ST_XY FeedBackPos = new ST_XY();
        //当前放料工站
        public List<WSFEED.MdDat> FeedBackWs = new List<WSFEED.MdDat>();
        //当前批次尚未完成二次回检的模组。停止时保留，恢复后只回检这些模组。
        private readonly FeedbackBatch feedbackBatch = new FeedbackBatch();
        //当前放料工站位置记录
        public bool bUpdateFBPos_OKNG = false;
        //当前放料工站位置记录
        public bool bUpdateFBPos_UT = false;
        public EM_FIN_MOD FindMod = EM_FIN_MOD.ALL;
        public static bool wsfeed1cangotest = false;
        public static bool wsfeed2cangotest = false;
        public static bool wsfeed1openchart = false;//工位1开图允许
        public static bool wsfeed2openchart = false; //工位2开图允许

        private bool BeginFeedbackBatch(List<WSFEED.MdDat> modules)
        {
            if (!feedbackBatch.TryBegin(modules.Select(module => module.Num))) return false;

            FeedBackWs.Clear();
            foreach (WSFEED.MdDat module in modules)
            {
                FeedBackWs.Add(module.Clone());
            }
            return true;
        }

        private List<WSFEED.MdDat> GetPendingFeedbackModules()
        {
            return FeedBackWs.Where(module => feedbackBatch.IsPending(module.Num)).ToList();
        }

        private void CompleteFeedbackModule(int moduleNumber)
        {
            feedbackBatch.Complete(moduleNumber);
            FeedBackWs.RemoveAll(module => module.Num == moduleNumber);
        }

        private void CancelFeedbackModule(int moduleNumber)
        {
            feedbackBatch.Cancel(moduleNumber);
            FeedBackWs.RemoveAll(module => module.Num == moduleNumber);
        }

        private EM_RES RecoverPendingFeedbackBeforeNextAction(ref bool bquit, WSFEED ws)
        {
            if (!feedbackBatch.HasPending) return EM_RES.OK;

            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR,
                string.Format("{0}检测到停止前未完成的工站回检，优先回检后再继续取放料", disc));
            ST_XYZA endpos = new ST_XYZA();
            EM_RES res = FlyCamToLastPos(ref bquit, ws, ref endpos);
            if (res != EM_RES.OK && res != EM_RES.NEXT && res != EM_RES.GIVEWAY) return res;
            return feedbackBatch.HasPending ? EM_RES.RETRY : EM_RES.OK;
        }

        private EM_RES VerifyUpperBarcodeBackcheck(ref bool bquit, WSFEED ws, ST_XYN position,
            string expectedBarcode, string actualBarcode, ref bool barcodeErr, string flowName)
        {
            BarcodeBackcheckResult result = BarcodeBackcheckPolicy.Classify(expectedBarcode, actualBarcode);
            if (result == BarcodeBackcheckResult.Match) return EM_RES.OK;

        RECAP:
            EM_RES res = MT.ZupMove(ref bquit, ref ax_x, position.x, ref ax_y, position.y);
            if (res != EM_RES.OK) return res;
            res = upcam.FindTaskTriAndWait(CONST.WsModUpFw, Demo);
            if (res != EM_RES.OK && res != EM_RES.CAM_ERR) return res;

            string capturedBarcode = upcam.curTask.ResData.BarCode;
            result = BarcodeBackcheckPolicy.Classify(expectedBarcode, capturedBarcode);
            if (result == BarcodeBackcheckResult.Empty)
            {
                COUNT_DATA.ErrTemp = System.DateTime.Now;
                VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "没检到二维码!" : "No Barcode", 20, true);
                MT.ST_WARN st_warn = new MT.ST_WARN();
                warning fr_warn = new warning();
                st_warn.ws = null;
                st_warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                st_warn.title = VAR.IsChinese ? "提示:没检到二维码" : "Tip: No barcode detected";
                st_warn.msg = VAR.IsChinese ? string.Format("上相机{0}上模组二维码无法识别.", ws.disc) :
                    string.Format("Upcam {0} module barcode cannot be identified\\r\\n上相机{0}上模组二维码无法识别.", ws.disc);
                st_warn.lb_msg = VAR.IsChinese ? "提示:" + st_warn.msg + "请确认!\\r\\n  1.按'继续运行'键则放弃当前检查继续运行!\\r\\n  " +
                    "2.如需确认问题请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!\\r\\n 3.如按重新拍照键则重新获取当前位置图像!" :
                    "Tip:" + st_warn.msg + "\\r\\nPlease confirm!\\r\\n  1. Press the 'Continue Running' key to abandon the current check and continue running! !\\r\\n  " +
                    "2. If you need to confirm the problem, please press the stop running button to exit the operation, and then press the run button after the top left corner of the interface is ready!\\r\\n 3. If you press the 'Take a photo' button, the current position image will be obtained again!" + "\\r\\n提示:" + st_warn.msg + "请确认!\\r\\n  1.按'继续运行'键则放弃当前检查继续运行!\\r\\n  " +
                    "2.如需确认问题请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!\\r\\n 3.如按重新拍照键则重新获取当前位置图像!";

                if (PT_SET.bBackerrcontinue)
                {
                    st_warn.ok_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                    st_warn.abort_txt = VAR.IsChinese ? "重新拍照" : "Take a photo";
                    SQLData.TesterrDataAdd(99, 99, "料盘放料", string.Format("上下料{0}未检测到二维码", id));
                    COUNT_DATA.Errcnt++;
                    DialogResult choice = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.BarcodeAbnormal);
                    if (choice == DialogResult.Cancel) return EM_RES.ERR;

                    VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                    MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                    goto RECAP;
                }

                st_warn.ok_txt = VAR.IsChinese ? "重新拍照" : "Take a photo";
                DialogResult retryChoice = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.BarcodeAbnormal);
                if (retryChoice == DialogResult.Cancel) return EM_RES.ERR;
                if (retryChoice == DialogResult.OK)
                {
                    VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                    goto RECAP;
                }
                return EM_RES.ERR;
            }

            if (result == BarcodeBackcheckResult.Mismatch)
            {
                WSFEED.MdDat md = ws.list_md[position.n];
                md.res = 111;
                ws.breschanged = true;
                barcodeErr = true;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR,
                    string.Format("上相机{0}二维码回检失败,工站:{1},位置:{2},记录:{3},实拍:{4}",
                        flowName, ws.disc, md.Num, md.bardcode ?? "<空>", capturedBarcode ?? "<空>"));
            }
            return EM_RES.OK;
        }
        public static bool wsfeed2isworking = false;
        public static WSFEED wsfeedclear = null;
        public static bool ud2needwait = false;
        public static bool ud1needwait = false;
        public static bool ud2ws2needwait = false;
        public static bool ud1ws3needwait = false;
        /// <summary>
        /// 下一次工站拍照位置
        /// </summary>
        public List<WSFEED.MdDat> CurPlaceMod = new List<WSFEED.MdDat>();
        //上料等待
        public static bool bWaitforUpDownload = false;
        //X1与X2的行程
        public static double DisAxisX = 0;
        public static double DisAxisXSafe = 390;//定义的安全防护距离
        public string barcodestr = "";//料盘二维码
        public double time_cnt = 0;

        /// <summary>
        /// 上下料模块的状态
        /// </summary>
        public EM_STA status_ud = EM_STA.UNKNOW;
        public List<Product.Tray.PosInf> placeposfd = new List<Product.Tray.PosInf>();
        public List<Product.Tray.PosInf> placepos = new List<Product.Tray.PosInf>();
        public List<Product.Tray.PosInf> placeposng = new List<Product.Tray.PosInf>();
        //  public EM_FIN_MOD FindMod = EM_FIN_MOD.ALL;

        public bool bfinal = false;

        public bool bflycam = false;

        public EM_STA Proc = EM_STA.CHECK;
        public EM_STA GrrProc = EM_STA.PICKWS;
        public int ErrTryPickCnt = 0;

        public bool Demo = false;

        public double TrayGoPos = 0;

        public static ST_XYA Vs_TrayOK = new ST_XYA(), Vs_TrayNg = new ST_XYA();
        List<Cam.VisionTask> modupcam_task = new List<Cam.VisionTask>();
        //lock
        private static readonly object pickws = new object();
        private static readonly object placews = new object();
        private static readonly object picktray = new object();
        private static readonly object placetray = new object();
        private static readonly object fdtray = new object();
        #endregion

        #region 硬件
        /// <summary>
        ///上下料对应下相机
        /// </summary>
        public Cam dwcam = null;
        /// <summary>
        ///上下料对应上相机
        /// </summary>
        public Cam upcam = null;
        /// <summary>
        /// 上料X轴
        /// </summary>
        public AXIS ax_x = null;
        /// <summary>
        /// 上料Y轴
        /// </summary>
        public AXIS ax_y = null;
        /// <summary>
        /// 上料Z轴
        /// </summary>
        public AXIS ax_z = null;
        /// <summary>
        /// 上料U轴
        /// </summary>
        public AXIS ax_u1 = null;
        /// <summary>
        /// 上料U轴
        /// </summary>
        public AXIS ax_u2 = null;
        /// <summary>
        ///上下料对应的吸头
        /// </summary>
        public List<XT> list_xt = new List<XT>();
        /// <summary>
        ///上下料对应的待测仓储
        /// </summary>
        public TrayBox traybox_fd = COM.traybox_fd;
        /// <summary>
        ///上下料对应的OK仓储
        /// </summary>
        public TrayBox traybox_ok = COM.traybox_ok;
        /// <summary>
        ///上下料对应的NG仓储
        /// </summary>
        public TrayBox traybox_ng = COM.traybox_ng;
        #endregion

        #region 初始化
        public UpDownLoad()
        { }

        public UpDownLoad(int id, string disc, ref Cam dwcam, ref Cam upcam, ref AXIS ax_x, ref AXIS ax_y,
                           ref AXIS ax_z, ref AXIS ax_u1, ref AXIS ax_u2, ref List<XT> list_xt)
        {
            this.id = id;
            this.disc = disc;
            this.upcam = upcam;
            this.dwcam = dwcam;
            this.ax_x = ax_x;
            this.ax_y = ax_y;
            this.ax_z = ax_z;
            this.ax_u1 = ax_u1;
            this.ax_u2 = ax_u2;
            this.list_xt = list_xt;
            foreach (XT xt in list_xt)
            {
                xt.Parent = this;
                if (this.ax_y.m_id != xt.ax_y.m_id)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}的Y轴与{1}Y轴不匹配!", this.disc, xt.disc));
                }
            }
        }
        #endregion

        #region 坐标获取函数
        double x = 0, y = 0, z = 0, u1 = 0, u2 = 0;
        bool btsk = false;
        public ST_XYZA CurPos
        {
            get
            {
                ST_XYZA pos = new ST_XYZA();
                pos.x = ax_x != null ? ax_x.fenc_pos : 0;
                pos.y = ax_y != null ? ax_y.fenc_pos : 0;
                pos.z = ax_z != null ? ax_z.fenc_pos : 0;
                pos.a = ax_u1 != null ? ax_u1.fcmd_pos : 0;
                return pos;
            }
        }
        public string StrOfPos
        {
            get
            {
                Task tsk = new Task(() =>
                {
                    if (btsk) return;
                    btsk = true;
                    x = ax_x != null ? ax_x.fcmd_pos : 0;
                    y = ax_y != null ? ax_y.fcmd_pos : 0;
                    z = ax_z != null ? ax_z.fcmd_pos : 0;
                    u1 = ax_u1 != null ? ax_u1.fcmd_pos : 0;
                    u2 = ax_u2 != null ? ax_u2.fcmd_pos : 0;
                    btsk = false;
                });
                tsk.Start();
                return string.Format("X: {0:000.000}\nY: {1:000.000}\nZ: {2:000.000}\nU1: {3:000.000}\nU2: {4:000.000}", x, y, z, u1, u2);
                // else return string.Format("X: {0:000.000}  U1: {3:000.000}\nY: {1:000.000}  U2: {4:000.000}\nZ: {2:000.000}", x, y, z, u1, u2);
            }
        }
        #endregion

        #region 定位函数
        //Z抬起
        public EM_RES Zup(ref bool bquit)
        {
            EM_RES res = EM_RES.OK;
            res = MT.Move(ref bquit, ref ax_z, 0);
            return res;
        }
        //快速回初始位置
        public EM_RES GoZero(ref bool bquit, WSFEED wsfeed,bool IsZero = true)
        {
            if (id == 0 && wsfeed.id == 1)
            {
                UpDownLoad.ud2needwait = false;
                UpDownLoad.ud2ws2needwait = false;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}马上归位，上下料2等待提前释放-----", disc));
            }
           
            EM_RES res = EM_RES.OK;
            if(PT_SET.blongXT)
            {
                res = MT.ZupMove(ref bquit, ref ax_y, IsZero == true ? 0 : list_xt[1].st_cap_pos.y - 50, ref ax_u1, 0, ref ax_u2, 0);
                if (res != EM_RES.OK) return res;
                res = MT.ZupMove(ref bquit, ref ax_x, 0);
                if (res != EM_RES.OK) return res;
            }
            else
            {
                res = MT.ZupMove(ref bquit, ref ax_x, 0, ref ax_y, IsZero == true ? 0 : list_xt[1].st_cap_pos.y - 50, ref ax_u1, 0, ref ax_u2, 0);
                if (res != EM_RES.OK) return res;
            }
            if (id == 1 && wsfeed.id == 1)
            {
                UpDownLoad.ud1needwait = false;
                UpDownLoad.ud1ws3needwait = false;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}马上归位，上下料1等待提前释放-----", disc));
            }
            return EM_RES.OK;
        }
        #endregion

        #region 回零
        public EM_RES Home(ref bool bquit)
        {
            EM_RES res = EM_RES.OK;

            if (bquit) return EM_RES.QUIT;
            //吸头上没有物料清空
            foreach (XT xt in list_xt)
            {
                if (!xt.cy_zk.isONByChkSen)
                    xt.cy_zk.SetOff();
                xt.gpio_pzk.SetOff();
            }
            //Z轴复位
            res = MT.AxisHome(ref bquit, ax_z);
            if (res != EM_RES.OK) return res;
            //Y轴复位
            res = MT.AxisHome(ref bquit, ax_y);
            if (res != EM_RES.OK) return res;

            //复位XA  模块1同时把料仓复位
            if (id == 0)
            {
                res = MT.AxisHome(ref bquit, ax_x, ax_u1, ax_u2, traybox_fd.ax_x, traybox_ok.ax_x, traybox_ng.ax_x);
                if (res != EM_RES.OK) return res;
                res = MT.AxisHome(ref bquit, traybox_fd.ax_z, traybox_ok.ax_z, traybox_ng.ax_z);
            }
            else
            {
                res = MT.AxisHome(ref bquit, ax_x, ax_u1, ax_u2);
            }
            if (res != EM_RES.OK) return res;

            foreach (XT xt in list_xt)
            {
                if (xt.cy_zk.isONByChkSen && IsFirst)
                    xt.cy_zk.SetOff();
            }
            IsFirst = false;
            return EM_RES.OK;
        }


        #endregion

        #region  停止
        public void Stop()
        {
            ax_x.bhomequit = true;
            ax_x.Stop();

            ax_y.bhomequit = true;
            ax_y.Stop();

            ax_z.bhomequit = true;
            ax_z.Stop();

            ax_u1.bhomequit = true;
            ax_u1.Stop();

            ax_u2.bhomequit = true;
            ax_u2.Stop();

            traybox_fd.ax_x.bhomequit = true;
            traybox_fd.ax_x.Stop();

            traybox_fd.ax_z.bhomequit = true;
            traybox_fd.ax_z.Stop();

            traybox_ok.ax_x.bhomequit = true;
            traybox_ok.ax_x.Stop();

            traybox_ok.ax_z.bhomequit = true;
            traybox_ok.ax_z.Stop();

            traybox_ok.ax_x.bhomequit = true;
            traybox_ok.ax_x.Stop();

            traybox_ng.ax_x.bhomequit = true;
            traybox_ng.ax_x.Stop();
        }

        public static void UD1Stop()
        {
            foreach (AXIS ax in MT.Axlist_UDL1_ExpLC)
            {
                ax.bhomequit = true;
                ax.Stop();
            }
        }
        public static void UD2Stop()
        {
            foreach (AXIS ax in MT.Axlist_UDL2_ExpLC)
            {
                ax.bhomequit = true;
                ax.Stop();
            }
        }

        public static void LCStop()
        {
            foreach (AXIS ax in MT.Axlist_UDL_LC)
            {
                ax.bhomequit = true;
                ax.Stop();
            }
        }
        #endregion

        #region 相关函数

        /// <summary>
        /// 确认下一个流程
        /// </summary>
        /// <param name="MdNum"></param>
        /// <param name="ws"></param>
        /// <param name="ud_id"></param>
        /// <returns></returns>
        public EM_STA ChkNextProc(ref int MdNum, ref int MdNum_1, WSFEED ws, int ud_id)
        {
            EM_STA proc_temp = EM_STA.CHECK;
            int numempty = 10000, numtestok = 10000;
            List<WSFEED.MdDat> pos_empty = new List<WSFEED.MdDat>();
            List<WSFEED.MdDat> pos_testok = new List<WSFEED.MdDat>();
            GetWsPosTeam(ref pos_empty, ws, Product.EM_CM_RES.EMPTY, COM.List_UDLoad[ud_id].FindMod);
            GetWsPosTeam(ref pos_testok, ws, Product.EM_CM_RES.OK, COM.List_UDLoad[ud_id].FindMod);

            if (pos_empty.Count > 0)
                numempty = (pos_empty[0].Num - 1) % 4;
            if (pos_testok.Count > 0)
                numtestok = (pos_testok[0].Num - 1) % 4;

            if (!VAR.ClearMt)
            {
                if (pos_testok.Count > 0 && pos_empty.Count > 0)
                {
                    MdNum = numtestok > numempty ? numempty : numtestok;
                    MdNum_1 = numtestok > numempty ? pos_empty[0].Num : pos_testok[0].Num;
                    proc_temp = numtestok > numempty ? EM_STA.PICKTRAY : EM_STA.PICKWS;
                }
                else if (pos_testok.Count > 0)
                {
                    MdNum = numtestok;
                    MdNum_1 = pos_testok[0].Num;
                    proc_temp = EM_STA.PICKWS;
                }
                else if (pos_empty.Count > 0)
                {
                    MdNum = numempty;
                    MdNum_1 = pos_empty[0].Num;
                    proc_temp = EM_STA.PICKTRAY;
                }
                else
                {
                    MdNum = 10000;
                    MdNum_1 = 10000;
                    proc_temp = EM_STA.UPDOWNLOADEND;
                }
            }
            else
            {
                int xtzkcnt = 0;
                int mdzkcnt = 0;
                foreach (XT xt in list_xt)
                {
                    if (xt.cy_zk.isON && xt.cy_zk.isONByChkSen) xtzkcnt++;                   
                }
                //foreach (WSFEED.MdDat md in ws.list_md)
                //{
                //    if (md.benable) mdzkcnt++;

                //}
                if (pos_testok.Count > 0&& xtzkcnt==0)
                {
                    MdNum = numtestok;
                    MdNum_1 = pos_testok[0].Num;
                    proc_temp = EM_STA.PICKWS;
                }
                else if (xtzkcnt > 0) proc_temp = EM_STA.PLACEWS;                
                else
                {
                    MdNum = 10000;
                    MdNum_1 = 10000;
                    proc_temp = EM_STA.UPDOWNLOADEND;
                    //isfirst = true;
                }
            }
            return proc_temp;
        }

        public EM_STA ChkGrrNextProc(WSFEED ws, int ud_id)
        {
            EM_STA proc_temp = EM_STA.CHECK;
            List<WSFEED.MdDat> pos_empty = new List<WSFEED.MdDat>();
            List<WSFEED.MdDat> pos_testok = new List<WSFEED.MdDat>();
            GetWsOrderPosTeam(ref pos_empty, ws, Product.EM_CM_RES.EMPTY, COM.List_UDLoad[ud_id].FindMod);
            GetWsOrderPosTeam(ref pos_testok, ws, Product.EM_CM_RES.OK, COM.List_UDLoad[ud_id].FindMod);
            if (pos_empty.Count > 0 || pos_testok.Count > 0)
                proc_temp = EM_STA.GRRTEST;
            else
                proc_temp = EM_STA.UPDOWNLOADEND;
            return proc_temp;
        }
        /// <summary>
        /// 吸头上的物料放回待测料盘
        /// </summary>
        /// <param name="bquit"></param>
        /// <param name="xt"></param>
        /// <returns></returns>
        /// <summary>
        /// 根据顺序查找工站的模组（逐个搜索）
        /// </summary>
        /// <param name="PlaceMod">返回需要放置模组或者取模组的信息列表list</param>
        /// <param name="wsfeed">工站</param>
        /// <param name="res">查找类型</param>
        public EM_RES XtMovPickPos(ref bool bquit, XT xt)
        {
            EM_RES res = EM_RES.OK;
            double pos_tray_x = 0;
            double ax_x_pos = ax_x.fenc_pos;
            if (Math.Abs(ax_x.fenc_pos) < 50)
            {
                if (id == 0) ax_x_pos = 50;
                else if (id == 1) ax_x_pos = -50;
            }
            pos_tray_x = -xt.xt_pos_pick_mod.x + ax_x_pos + xt.xt_pos_pick_tray_x;
            if (pos_tray_x < 0)
            {
                xt.xt_pos_pick_mod.x = ax_x_pos - pos_tray_x;
                pos_tray_x = 0;
            }
            else
            {
                xt.xt_pos_pick_mod.x = ax_x_pos;
            }
            //move tray_x
            res = MT.ZupMove(ref bquit, ref ax_x, xt.xt_pos_pick_mod.x, ref ax_y, xt.xt_pos_pick_mod.y, ref traybox_fd.ax_x, pos_tray_x>0? pos_tray_x:0);
            return EM_RES.OK;
        }

        /// <summary>
        /// 根据顺序查找工站的模组(按个搜索)搜索一个
        /// </summary>
        /// <param name="PlaceMod">返回结果</param>
        /// <param name="ws">工站</param>
        /// <param name="res">查找类型</param>
        /// <param name="mod">搜索方式</param>
        public void GetWsPosTeam(ref List<WSFEED.MdDat> PlaceMod, WSFEED ws, Product.EM_CM_RES res = Product.EM_CM_RES.EMPTY, EM_FIN_MOD mod = EM_FIN_MOD.ALL)
        {
            modchange = false;
      PlaceMod.Clear();
            if (mod == EM_FIN_MOD.NONE) return;
            int n = 0;
            for (int i = 0; i < ws.list_md.Count; i++)
            {
                if (i % 2 == 0) n = i - i / 2;
                else n = i + (ws.list_md.Count - 1 - i) / 2;
                if ((mod == EM_FIN_MOD.LOW && (n % 4 >= 2)) || (mod == EM_FIN_MOD.HIGH && (n % 4 < 2))) continue;
                if (mod == EM_FIN_MOD.HIGH && (n == 2 || n == 6))
                {
                    WSFEED.MdDat WsMod1 = ws.list_md[n];
                    WSFEED.MdDat WsMod2 = ws.list_md[n+1];
                    if ((WsMod2.res != (int)res && res < Product.EM_CM_RES.OK) || (WsMod2.res < (int)res && res == Product.EM_CM_RES.OK)) modchange = false;
                    else if ((WsMod2.benable))
                    {
                        modchange = true;
                    }
                    if(!VAR.ClearMt&&(n==2||n == 6) && WsMod1.benable&& (!(ws.list_md[3].res==(int) Product.EM_CM_RES.UNTEST|| ws.list_md[3].res == (int)Product.EM_CM_RES.EMPTY || ws.list_md[7].res ==(int) Product.EM_CM_RES.UNTEST|| ws.list_md[7].res == (int)Product.EM_CM_RES.EMPTY))) modchange = true;
                    if (!ws.list_md[3].benable|| !ws.list_md[7].benable) modchange = false;

                }
               
                if (mod == EM_FIN_MOD.HIGH && (n == 2 || n == 6) && modchange) n++;
                if (n < ws.list_md.Count)
                {
                    WSFEED.MdDat WsMod = ws.list_md[n];
                    if (!WsMod.benable) continue;
                    //if (VAR.gsys_set.isChkMode && VAR.ChkPC != WsMod.PC_ID) continue;
                    if ((WsMod.res != (int)res && res < Product.EM_CM_RES.OK) || (WsMod.res < (int)res && res == Product.EM_CM_RES.OK)) continue;
                    if (PlaceMod.Count > 0 && PlaceMod[0].Num % 4 != WsMod.Num % 4) break;
                    if (mod == EM_FIN_MOD.ALL)
                    {
                        if (id == 0 && ws.id == 1 && (WsMod.Num == 1|| WsMod.Num == 2|| WsMod.Num == 5|| WsMod.Num == 6)) continue;
                        if (id == 1 && ws.id == 0 && (WsMod.Num == 3 || WsMod.Num == 4 || WsMod.Num == 7 || WsMod.Num == 8)) continue;
                    }
                    PlaceMod.Add(WsMod);
                    bool bOnlySelectOne = id == 0 ? PT_SET.bUpDn1XtOnlyOne : PT_SET.bUpDn2XtOnlyOne;
                    if (bOnlySelectOne) break;
                    if (mod == EM_FIN_MOD.HIGH && n == 7) modchange = false;                   
                    if (mod == EM_FIN_MOD.HIGH && n == 6&& ws.list_md[n+1].benable) modchange = true;
                    if (!VAR.ClearMt&&mod == EM_FIN_MOD.HIGH && !ws.list_md[7].benable&& !(ws.list_md[3].res == (int)Product.EM_CM_RES.UNTEST|| ws.list_md[3].res == (int)Product.EM_CM_RES.EMPTY)) break;
                    if (WsMod.Num > 4) break;
                }
            }
        }


        public void GetWsOrderPosTeam(ref List<WSFEED.MdDat> PlaceMod, WSFEED ws, Product.EM_CM_RES res = Product.EM_CM_RES.EMPTY, EM_FIN_MOD mod = EM_FIN_MOD.ALL)
        {
            PlaceMod.Clear();
            if (mod == EM_FIN_MOD.NONE) return;
            int n = 0;
            for (int i = 0; i < ws.list_md.Count; i++)
            {
                n = i;
                if ((mod == EM_FIN_MOD.LOW && (n % 4 >= 2)) || (mod == EM_FIN_MOD.HIGH && (n % 4 < 2))) continue;
                if (n < ws.list_md.Count)
                {
                    WSFEED.MdDat WsMod = ws.list_md[n];
                    if (!WsMod.benable) continue;
                    // if (VAR.gsys_set.isChkMode && VAR.ChkPC != WsMod.PC_ID) continue;
                    if ((WsMod.res != (int)res && res < Product.EM_CM_RES.OK) || (WsMod.res < (int)res && res == Product.EM_CM_RES.OK)) continue;
                    PlaceMod.Add(WsMod);
                    break;
                }
            }
        }

        /// <summary>
        /// 飞拍配置
        /// </summary>
        /// <param name="flyCfg">模式</param>
        /// <param name="frPos">配置的点位</param>
        /// <param name="cnt">配置个数</param>
        /// <param name="Ofs_On">补偿</param>
        /// <param name="Dir">运动方向  true:Low->High    false:High->low</param>
        public void FlyCfg(EM_FLY_CFG flyCfg, List<ST_XYN> frPos, int cnt, bool Dir = true, bool Ofs_On = true, bool IfClear = true)
        {
            if (cnt <= 0) return;
            List<double> TriPos = new List<double>();
            List<string> ListTaskName = new List<string>();
            List<double> xt_ofs = new List<double>();
            List<double> ws_ofs = new List<double>();
            List<double> mod_ofs = new List<double>();
            List<double> ofs = new List<double>();
            List<ST_XYN> ST_TriPos = new List<ST_XYN>();
            string taskname = string.Empty;
            int chkcnt = 0;
            int xt_num = 0;
            double xtofs = 0;
            if (Ofs_On)
            {
                xt_ofs.Add(list_xt[0].xt_cap_ofs);
                xt_ofs.Add(list_xt[1].xt_cap_ofs);
                if (cnt > 1)
                {
                    if (Dir)
                    {
                        ws_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        ws_ofs.Add(list_xt[0].ws_cap_far_ofs);
                        mod_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        mod_ofs.Add(0.00);

                    }
                    else
                    {
                        ws_ofs.Add(list_xt[0].ws_cap_far_ofs);
                        ws_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        mod_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        mod_ofs.Add(0.00);

                    }
                }
                else
                {
                    ws_ofs.Add(list_xt[0].ws_cap_far_ofs);
                    mod_ofs.Add(0.00);
                }

            }

            if (cnt > 1)
            {
                frPos.Sort((a, b) =>
                {
                    if ((a.y > b.y && Dir) || (a.y < b.y && !Dir)) return 1;
                    else return -1;
                });
            }

            switch (flyCfg)
            {
                case EM_FLY_CFG.MOD_DW:
                    if (Demo && !PT_SET.xt1firsten) chkcnt = cnt;
                    else chkcnt = list_xt.Count;
                    //if (!VAR.ClearMt || cnt > 1) chkcnt = cnt;
                    //else chkcnt = list_xt.Count;
                    for (int i = 0; i < chkcnt; i++)
                    {
                        if (Dir)
                        {
                            xt_num = (i + 1) % 2;
                            if (Ofs_On)
                                xtofs = Math.Abs(xt_ofs[xt_num]);
                        }
                        else
                        {
                            xt_num = i;
                            if (Ofs_On)
                                xtofs = -Math.Abs(xt_ofs[xt_num]);
                        }

                        if ((list_xt[xt_num].cy_zk.isONByChkSen && list_xt[xt_num].XtMd != null && !Demo) || (Demo && list_xt[xt_num].XtMd != null))
                        {
                            if (Ofs_On) TriPos.Add(list_xt[xt_num].st_cap_pos.y - xtofs);
                            else TriPos.Add(list_xt[xt_num].st_cap_pos.y);
                            ListTaskName.Add(CONST.ModDwFw[xt_num]);
                            ST_TriPos.Add(new ST_XYN(0.00, list_xt[xt_num].st_cap_pos.y, xt_num));
                        }

                    }
                    dwcam.ListTaskCfg(ListTaskName, ST_TriPos);
                    ax_y.SetHcmp(TriPos.ToArray(), dwcam.TriIO.num);
                    break;
                case EM_FLY_CFG.WS_UP:
                    taskname = CONST.WsUpFw;
                    ofs.AddRange(ws_ofs);
                    break;
                case EM_FLY_CFG.WS_MOD_UP:
                    taskname = CONST.WsModUpFw;
                    ofs.AddRange(ws_ofs);
                    break;
                case EM_FLY_CFG.MOD_UP:
                    taskname = CONST.ModUpFw;
                    ofs.AddRange(mod_ofs);
                    break;
            }

            if (taskname != string.Empty)
            {
                for (int i = 0; i < cnt; i++)
                {
                    if (Ofs_On)
                    {
                        if (Dir) TriPos.Add(frPos[i].y - Math.Abs(ofs[i]));
                        else TriPos.Add(frPos[i].y + Math.Abs(ofs[i]));
                    }
                    else
                    {
                        TriPos.Add(frPos[i].y);
                    }
                    ListTaskName.Add(taskname);
                }
                upcam.ListTaskCfg(ListTaskName, frPos, IfClear);
                ax_y.SetHcmp(TriPos.ToArray(), upcam.TriIO.num, 4, IfClear);
            }
        }
        public void FlyCfgaction(EM_FLY_CFG flyCfg, List<ST_XYN> frPos, int cnt, bool Dir = true, bool Ofs_On = true, bool IfClear = true)
        {
            if (cnt <= 0) return;
            List<double> TriPos = new List<double>();
            List<string> ListTaskName = new List<string>();
            List<double> xt_ofs = new List<double>();
            List<double> ws_ofs = new List<double>();
            List<double> mod_ofs = new List<double>();
            List<double> ofs = new List<double>();
            List<ST_XYN> ST_TriPos = new List<ST_XYN>();
            string taskname = string.Empty;
            int chkcnt = 0;
            int xt_num = 0;
            double xtofs = 0;
            if (Ofs_On)
            {
                xt_ofs.Add(list_xt[0].xt_cap_ofs);
                xt_ofs.Add(list_xt[1].xt_cap_ofs);
                if (cnt > 1)
                {
                    if (Dir)
                    {
                        ws_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        ws_ofs.Add(list_xt[0].ws_cap_far_ofs);
                        mod_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        mod_ofs.Add(0.00);

                    }
                    else
                    {
                        ws_ofs.Add(list_xt[0].ws_cap_far_ofs);
                        ws_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        mod_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        mod_ofs.Add(0.00);

                    }
                }
                else
                {
                    ws_ofs.Add(list_xt[0].ws_cap_far_ofs);
                    mod_ofs.Add(0.00);
                }

            }

            if (cnt > 1)
            {
                frPos.Sort((a, b) =>
                {
                    if ((a.y > b.y && Dir) || (a.y < b.y && !Dir)) return 1;
                    else return -1;
                });
            }

            switch (flyCfg)
            {
                case EM_FLY_CFG.MOD_DW:
                    if (Demo && !PT_SET.xt1firsten) chkcnt = cnt;
                    else chkcnt = list_xt.Count;
                    //if (!VAR.ClearMt || cnt > 1) chkcnt = cnt;
                    //else chkcnt = list_xt.Count;
                    for (int i = 0; i < chkcnt; i++)
                    {
                        if (Dir)
                        {
                            xt_num = (i + 1) % 2;
                            if (Ofs_On)
                                xtofs = Math.Abs(xt_ofs[xt_num]);
                        }
                        else
                        {
                            xt_num = i;
                            if (Ofs_On)
                                xtofs = -Math.Abs(xt_ofs[xt_num]);
                        }

                        if ((list_xt[xt_num].cy_zk.isONByChkSen  && !Demo) || (Demo ))
                        {
                            if (Ofs_On) TriPos.Add(list_xt[xt_num].st_cap_pos.y - xtofs);
                            else TriPos.Add(list_xt[xt_num].st_cap_pos.y);
                            ListTaskName.Add(CONST.ModDwFw[xt_num]);
                            ST_TriPos.Add(new ST_XYN(0.00, list_xt[xt_num].st_cap_pos.y, xt_num));
                        }

                    }
                    dwcam.ListTaskCfg(ListTaskName, ST_TriPos);
                    ax_y.SetHcmp(TriPos.ToArray(), dwcam.TriIO.num);
                    break;
                case EM_FLY_CFG.WS_UP:
                    taskname = CONST.WsUpFw;
                    ofs.AddRange(ws_ofs);
                    break;
                case EM_FLY_CFG.WS_MOD_UP:
                    taskname = CONST.WsModUpFw;
                    ofs.AddRange(ws_ofs);
                    break;
                case EM_FLY_CFG.MOD_UP:
                    taskname = CONST.ModUpFw;
                    ofs.AddRange(mod_ofs);
                    break;
            }

            if (taskname != string.Empty)
            {
                for (int i = 0; i < cnt; i++)
                {
                    if (Ofs_On)
                    {
                        if (Dir) TriPos.Add(frPos[i].y - Math.Abs(ofs[i]));
                        else TriPos.Add(frPos[i].y + Math.Abs(ofs[i]));
                    }
                    else
                    {
                        TriPos.Add(frPos[i].y);
                    }
                    ListTaskName.Add(taskname);
                }
                upcam.ListTaskCfg(ListTaskName, frPos, IfClear);
                ax_y.SetHcmp(TriPos.ToArray(), upcam.TriIO.num, 4, IfClear);
            }
        }
        public void FlyCfg2(EM_FLY_CFG flyCfg, List<ST_XYN> frPos, int cnt, bool Dir = true, bool Ofs_On = true, bool IfClear = true)
        {
            if (cnt <= 0) return;
            List<double> TriPos = new List<double>();
            List<string> ListTaskName = new List<string>();
            List<double> xt_ofs = new List<double>();
            List<double> ws_ofs = new List<double>();
            List<double> mod_ofs = new List<double>();
            List<double> ofs = new List<double>();
            List<ST_XYN> ST_TriPos = new List<ST_XYN>();
            string taskname = string.Empty;
            int chkcnt = 0;
            if (Ofs_On)
            {
                xt_ofs.Add(list_xt[0].xt_cap_ofs);
                xt_ofs.Add(list_xt[1].xt_cap_ofs);
                if (cnt > 1)
                {
                    if (Dir)
                    {
                        ws_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        ws_ofs.Add(list_xt[0].ws_cap_far_ofs);
                        mod_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        mod_ofs.Add(0.00);

                    }
                    else
                    {
                        ws_ofs.Add(list_xt[0].ws_cap_far_ofs);
                        ws_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        mod_ofs.Add(list_xt[0].ws_cap_near_ofs);
                        mod_ofs.Add(0.00);

                    }
                }
                else
                {
                    ws_ofs.Add(list_xt[0].ws_cap_far_ofs);
                    mod_ofs.Add(0.00);
                }

            }

            if (cnt > 1)
            {
                frPos.Sort((a, b) =>
                {
                    if ((a.y > b.y && Dir) || (a.y < b.y && !Dir)) return 1;
                    else return -1;
                });
            }

            switch (flyCfg)
            {
                case EM_FLY_CFG.MOD_DW:
                    if (Demo) chkcnt = cnt;
                    else chkcnt = list_xt.Count;
                    //for (int i = 0; i < chkcnt; i++)
                    //{
                    //    if ((list_xt[(i + 1) % 2].cy_zk.isONByChkSen && !Demo) || (Demo && list_xt[(i + 1) % 2].XtMd != null))
                    //    {
                    //        if (Ofs_On) TriPos.Add(list_xt[(i + 1) % 2].st_cap_pos.y - Math.Abs(xt_ofs[(i + 1) % 2]));
                    //        else TriPos.Add(list_xt[(i + 1) % 2].st_cap_pos.y);
                    //        ListTaskName.Add(CONST.ModDwFw[(i + 1) % 2]);
                    //        ST_TriPos.Add(new ST_XYN(0.00, list_xt[(i + 1) % 2].st_cap_pos.y, (i + 1) % 2));
                    //    }

                    //}
                    for (int i = 1; i >= chkcnt - 2; i--)
                    {
                        if ((list_xt[(i + 1) % 2].cy_zk.isONByChkSen && !Demo) || (Demo && list_xt[(i + 1) % 2].XtMd != null))
                        {
                            if (Ofs_On) TriPos.Add(list_xt[(i + 1) % 2].st_cap_pos.y - Math.Abs(xt_ofs[(i + 1) % 2]));
                            else TriPos.Add(list_xt[(i + 1) % 2].st_cap_pos.y);
                            ListTaskName.Add(CONST.ModDwFw[(i + 1) % 2]);
                            ST_TriPos.Add(new ST_XYN(0.00, list_xt[(i + 1) % 2].st_cap_pos.y, (i + 1) % 2));
                        }

                    }
                    dwcam.ListTaskCfg(ListTaskName, ST_TriPos);
                    ax_y.SetHcmp(TriPos.ToArray(), dwcam.TriIO.num);
                    break;
                case EM_FLY_CFG.WS_UP:
                    taskname = CONST.WsUpFw;
                    ofs.AddRange(ws_ofs);
                    break;
                case EM_FLY_CFG.WS_MOD_UP:
                    taskname = CONST.WsModUpFw;
                    ofs.AddRange(ws_ofs);
                    break;
                case EM_FLY_CFG.MOD_UP:
                    taskname = CONST.ModUpFw;
                    ofs.AddRange(mod_ofs);
                    break;
            }

            if (taskname != string.Empty)
            {
                for (int i = 0; i < cnt; i++)
                {
                    if (Ofs_On)
                    {
                        if (Dir) TriPos.Add(frPos[i].y - Math.Abs(ofs[i]));
                        else TriPos.Add(frPos[i].y + Math.Abs(ofs[i]));
                    }
                    else
                    {
                        TriPos.Add(frPos[i].y);
                    }
                    ListTaskName.Add(taskname);
                }
                upcam.ListTaskCfg(ListTaskName, frPos, IfClear);
                ax_y.SetHcmp(TriPos.ToArray(), upcam.TriIO.num, 4, IfClear);
            }
        }
        /// <summary>
        /// 等待飞拍视觉结果
        /// </summary>
        public EM_RES WaitCamRes(Cam cam, int cnt)
        {
            int n;
            int count;
            if (cnt < 1) return EM_RES.OK;
            for (n = 0; n < 200; n++)
            {
                count = 0;
                foreach (Cam.VisionTask task in cam.List_vs_task_cur)
                {
                    if (task.ResData.bUpdate) count++;
                }

                if (count == cam.List_vs_task_cur.Count)
                {
                    if (Demo)
                    {
                        foreach (Cam.VisionTask task in cam.List_vs_task_cur)
                        {
                            task.ResData.bOK = true;
                            task.ResData.PosMM = new ST_XYA(0, 0, 0);
                            task.ResData.BarCode = "abc";
                        }
                    }
                    break;
                }
                //time out
                Thread.Sleep(10);
            }

            if (n >= 200)
            {
                foreach (Cam.VisionTask task in cam.List_vs_task_cur)
                {
                    task.ResData.bOK = false;
                    if (!task.ResData.bUpdate)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}{1}数据更新超时!", cam.mName, task.TaskName) : string.Format("{0}{1} data update timeout!          ({0}{1}数据更新超时!)", cam.mName, task.TaskName), DReport.EmErrCode.Timeout, (int)DReport.EmHareware.Cam);

                    }
                }

                return EM_RES.TIMEOUT;
            }

            return EM_RES.OK;
        }

        /// <summary>
        /// 飞拍结果确认
        /// </summary>
        /// <param name="cam">检查相机</param>
        /// <param name="cnt">结果个数</param>
        /// <returns></returns>
        public EM_RES ChkCamRes(Cam cam, int cnt, bool chksame = true)
        {
            if (cnt <= 0 || Demo) return EM_RES.OK;
            if (cam.List_vs_task_cur.Count == cnt)
            {
                if (cam.List_vs_task_cur.Count == 2 && chksame)
                {
                    ST_XYA camres = cam.List_vs_task_cur[0].ResData.PosMM - cam.List_vs_task_cur[1].ResData.PosMM;
                    if ((Math.Abs(camres.x) < 0.002) && (Math.Abs(camres.y) < 0.002 && (Math.Abs(camres.a) < 0.002)) && cam.List_vs_task_cur[0].ResData.bOK && cam.List_vs_task_cur[1].ResData.bOK)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}_{1}飞拍两结果数据一样", disc, cam.disc) : string.Format("{0}_{1} The data of the two shots are the same     ({2}_{3}飞拍两结果数据一样)", disc, cam.englishdisc, disc, cam.disc), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.UpDownLoad + id, ERR_ALM.EmErrItem.CaptureAbnomal);
                        return EM_RES.ERR;
                    }
                }
            }
            else
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}_{1}飞拍缺少结果", disc, cam.disc) : string.Format("{0}_{1} flyshot result is null      ({2}_{3}飞拍缺少结果)", disc, cam.englishdisc, disc, cam.disc), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.UpDownLoad + id, ERR_ALM.EmErrItem.CaptureAbnomal);
                return EM_RES.CAM_LACKRES;
            }
            return EM_RES.OK;
        }

        /// <summary>
        /// 寻找OK与NG料盘放料的第一个位置
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public EM_RES FindOkOrNgTrayPos(ref ST_XYZA pos, TrayBox traybox, WSFEED ws)
        {
            List<WSFEED.MdDat> EmptyPos = new List<WSFEED.MdDat>();
            int i = 0;
            //寻找第一个OK料
            foreach (XT xt in list_xt)
            {
                if ((!xt.cy_zk.isONByChkSen && !Demo) || xt.XtMd == null)
                {
                    i++;
                    xt.cy_zk.SetOff();
                    continue;
                }
                if (xt.XtMd.res != (int)Product.EM_CM_RES.OK && traybox.disc == traybox_ok.disc) continue;
                if (xt.XtMd.res <= (int)Product.EM_CM_RES.OK && traybox.disc == traybox_ng.disc) continue;
                int ngcode = traybox.disc == traybox_ok.disc ? -10000 : xt.XtMd.res;
                List<Product.Tray.PosInf> Listpos_empty;
                List<Product.Tray.PosInf> Listpos;

                if (ws.id == 0 && id == 0)//当上下料1仅对工站1上料时
                {
                    Listpos_empty = traybox.tray_cur.GetPosList(Product.EM_CM_RES.EMPTY);
                    Listpos = traybox.tray_cur.GetPosList(Product.EM_CM_RES.EMPTY, ngcode);
                }
                else
                {
                    Listpos_empty = traybox.tray_cur.GetPosListOrder(Product.EM_CM_RES.EMPTY);
                    Listpos = traybox.tray_cur.GetPosListOrder(Product.EM_CM_RES.EMPTY, ngcode);
                }               
                    //如果X方向：轴在测试上料夹具1或者4以外的位置，那就不变化
                if (ax_x.fenc_pos < (ws.list_md[0].st_place_pos[id].x + 5) && ax_x.fenc_pos > (ws.list_md[3].st_place_pos[id].x - 5))
                {
                    //  pos.x = ax_x.fenc_pos;//原先不带避让
                    double temppos = 0;
                    if (FindMod == EM_FIN_MOD.HIGH && Math.Abs(ax_x.fenc_pos) > 40.01) temppos = ws.list_md[3].st_place_pos[id].x - 40;//避让40mm，模块1靠右，模块2靠左
                    else if (FindMod == EM_FIN_MOD.LOW && Math.Abs(ax_x.fenc_pos) > 40.01) temppos = ws.list_md[0].st_place_pos[id].x + 40;
                    else temppos = ax_x.fenc_pos;
                    pos.x = temppos;//X:104.547 异常情况
                }
                else
                    pos.x = FindMod == EM_FIN_MOD.HIGH ? ws.list_md[3].st_place_pos[id].x - 40 : FindMod == EM_FIN_MOD.LOW ? ws.list_md[0].st_place_pos[id].x + 40 : ws.list_md[0].st_place_pos[id].x;//避让40mm，模块1靠右，模块2靠左
                pos.z = 0;

                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}OK或者NG第一次计算: pos.x:{1}", disc, pos.x));
                if (!traybox.IsReady || traybox.tray_cur == null || Listpos.Count == 0)
                {
                    pos.y = COM.tray_ok.tl[id].y + xt.st_rol_cap.y - list_xt[0].st_rol_cap.y;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}OK或者NG第一次计算: pos.y:{1}", disc, pos.y));
                    pos.a = -10000;
                }
                else
                {
                    if (Listpos_empty.Count == traybox.tray_cur.list_pos.Count && PT_SET.bEnVsTray)
                    {
                        pos.y = traybox.tray_cur.TrayVsPos[id].y;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}OK或者NG第一次计算: pos.y:{1}", disc, pos.y));
                        pos.a = -traybox.tray_cur.TrayVsPos[id].x + pos.x + traybox.tray_cur.TrayVsPos[id].z;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}OK或者NG第一次计算: pos.a:{1}", disc, pos.a));
                    }
                    else
                    {
                        pos.y = Listpos[0].Pos[id].y + xt.st_rol_cap.y - list_xt[0].st_rol_cap.y;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}OK或者NG第一次计算: pos.y:{1}", disc, pos.y));
                        pos.a = -Listpos[0].Pos[id].x + pos.x + Listpos[0].Pos[id].a;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}OK或者NG第一次计算: pos.a:{1}", disc, pos.a));
                    }
                    if (pos.a < 0)
                    {
                        pos.x = pos.x - pos.a;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}OK或者NG第二次计算: pos.x:{1}", disc, pos.x));
                        pos.a = 0;
                    }
                }
                if(FindMod == EM_FIN_MOD.HIGH && ws.id==0&& pos.x >= ws.list_md[3].st_place_pos[id].x)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}此时pos.x:{1}已经大于安全距离，需要进行调整", disc, pos.x));
                    pos.x =ws.list_md[3].st_place_pos[id].x - 40;
                    if(pos.x<0)
                    {
                        pos.x = 1;
                    }
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}调整后: pos.x:{1}", disc, pos.x));
                }
                return EM_RES.OK;
            }
            if (i == 2) return EM_RES.ABORT;
            return EM_RES.NEXT;
        }

        /// <summary>
        /// 寻找料盘放料另一模块的下一个的X1位置
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public EM_RES FindNextTrayPosX1(ref double pos_x1, TrayBox traybox, WSFEED ws, UpDownLoad ud)
        {
            List<WSFEED.MdDat> EmptyPos = new List<WSFEED.MdDat>();
            ST_XYZA pos = new ST_XYZA();
            Product.EM_CM_RES traymod = Product.EM_CM_RES.EMPTY;
            //寻找第一个OK料
            traymod = traybox.disc == traybox_fd.disc ? Product.EM_CM_RES.UNTEST : Product.EM_CM_RES.EMPTY;
            List<Product.Tray.PosInf> Listpos;

            if (ws.id == 0 && id == 0)//当上下料1仅对工站1上料时
            {

                Listpos = traybox.tray_cur.GetPosList(traymod);
            }
            else
            {
                Listpos = traybox.tray_cur.GetPosListOrder(traymod);
            }
            

            // if (Listpos.Count > 0 && (ud.TrayGoPos < (ws.list_md[0].st_place_pos[ud.id].x + 5) && ud.TrayGoPos > (ws.list_md[3].st_place_pos[ud.id].x - 5)))
            if (Listpos.Count > 0 && (ud.TrayGoPos < (ws.list_md[0].st_place_pos[ud.id].x + 45) && ud.TrayGoPos > (ws.list_md[3].st_place_pos[ud.id].x - 45)))
            {
                pos.x = ud.TrayGoPos;
                if (traybox.disc == COM.traybox_ng.disc)
                {
                    int cn = traybox.tray_cur.list_pos.Count / 2;
                    pos.a = -traybox.tray_cur.list_pos[cn].Pos[ud.id].x + pos.x + traybox.tray_cur.list_pos[cn].Pos[ud.id].a;
                }
                else pos.a = -Listpos[0].Pos[ud.id].x + pos.x + Listpos[0].Pos[ud.id].a;
                if (pos.a < 0)
                {
                    pos.x = pos.x - pos.a;
                    pos.a = 0;
                }
                pos_x1 = pos.a;
            }
            else
            {
                pos_x1 = -10000;
            }
            return EM_RES.OK;

        }

        /// <summary>
        /// 寻找待测料盘上料的最后一个位置
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public EM_RES FindFdTrayPos(ref ST_XYZA pos, ref List<ST_XYN> TrayTriPos, WSFEED ws, bool Dir = true)
        {

            List<WSFEED.MdDat> UntestPos = new List<WSFEED.MdDat>();
            List<Product.Tray.PosInf> Listpos;
            if (ws.id == 0 && id == 0)//当上下料1仅对工站1上料时
            {
                Listpos = traybox_fd.tray_cur.GetPosList(Product.EM_CM_RES.UNTEST);
            }
            else
            {
                Listpos = traybox_fd.tray_cur.GetPosListOrder(Product.EM_CM_RES.UNTEST);
            }
           

            if (Listpos.Count == 0 || traybox_fd.tray_cur == null || !traybox_fd.IsReady) return EM_RES.END;
            GetWsPosTeam(ref UntestPos, ws, Product.EM_CM_RES.EMPTY, FindMod);
            if (UntestPos.Count == 0)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("{0}放料完成或找不到可以放料的位置!", ws.disc) : string.Format("{0}place completed or can't find place to place     ({0}放料完成或找不到可以放料的位置!)", ws.disc));
                return EM_RES.ABORT;
            }
            TrayTriPos.Clear();
            double pos_compen = (Dir == true) ? 10 : -10;
            if (ax_x.fenc_pos < (ws.list_md[0].st_place_pos[id].x + 10) && ax_x.fenc_pos > (ws.list_md[3].st_place_pos[id].x - 10))
            {
                // pos.x = ax_x.fenc_pos;
                double temppos = 0;
                if (FindMod == EM_FIN_MOD.HIGH && Math.Abs(ax_x.fenc_pos) > 40.01) temppos = ws.list_md[3].st_place_pos[id].x - 40;//避让40mm，模块1靠右，模块2靠左
                else if (FindMod == EM_FIN_MOD.LOW && Math.Abs(ax_x.fenc_pos) > 40.01) temppos = ws.list_md[0].st_place_pos[id].x + 40;
                else temppos = ax_x.fenc_pos;
                pos.x = temppos;
            }
            else
            {
                if (UntestPos.Count != 0)
                {
                    // pos.x = UntestPos[0].st_place_pos[id].x;
                    double temppos = 0;
                    //if (FindMod == EM_FIN_MOD.HIGH) temppos = UntestPos[0].st_place_pos[id].x - 40;//避让40mm，模块1靠右，模块2靠左
                    //else if (FindMod == EM_FIN_MOD.LOW) temppos = UntestPos[0].st_place_pos[id].x + 40;
                    if (FindMod == EM_FIN_MOD.HIGH) temppos = ws.list_md[3].st_place_pos[id].x - 40;//避让40mm，模块1靠右，模块2靠左
                    else if (FindMod == EM_FIN_MOD.LOW) temppos = ws.list_md[0].st_place_pos[id].x + 40;
                    else temppos = UntestPos[0].st_place_pos[id].x;
                    pos.x = temppos;

                }
                // else pos.x = FindMod == EM_FIN_MOD.HIGH ? ws.list_md[3].st_place_pos[id].x : ws.list_md[0].st_place_pos[id].x;
                else pos.x = FindMod == EM_FIN_MOD.HIGH ? ws.list_md[3].st_place_pos[id].x - 40 : FindMod == EM_FIN_MOD.LOW ? ws.list_md[0].st_place_pos[id].x + 40 : ws.list_md[0].st_place_pos[id].x;//避让40mm，模块1靠右，模块2靠左
            }

            if (UntestPos.Count > 1 && Listpos.Count > 1 && Listpos[0].Row == Listpos[1].Row)
            {

                TrayTriPos.Add(new ST_XYN(pos.x, Listpos[0].Pos[id].y, Listpos[0].idx));
                TrayTriPos.Add(new ST_XYN(pos.x, Listpos[1].Pos[id].y, Listpos[1].idx));
                TrayTriPos.Sort((a, b) =>
                {
                    if ((a.y > b.y && Dir) || (a.y < b.y && !Dir)) return 1;
                    else return -1;
                });
                pos.y = TrayTriPos[1].y + pos_compen;
            }
            else
            {

                TrayTriPos.Add(new ST_XYN(pos.x, Listpos[0].Pos[id].y, Listpos[0].idx));
                pos.y = Listpos[0].Pos[id].y + pos_compen;
            }

            pos.a = -Listpos[0].Pos[id].x + pos.x + Listpos[0].Pos[id].a;

            if (pos.a < 0)
            {
                pos.x = pos.x - pos.a;
                pos.a = 0;
            }
            return EM_RES.OK;
        }

        /// <summary>
        /// 飞拍移动到料盘
        /// </summary>
        /// <returns></returns>
        public EM_RES FLyToTray(ref bool bquit, ST_XYZA endpos, List<ST_XYN> WsTriPos, List<ST_XYN> TrayTriPos, TrayBox traybox = null, bool Dir = false)
        {

            EM_RES res;
            double pos_mov_x = list_xt[0].st_cap_pos.y;
            AXIS ax_x_tray = null;
            int xt_cnt = 0;
            if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN && PT_SET.bBarcodeCamBackEn && ax_y.fenc_pos > list_xt[0].st_cap_pos.y)
            {
                foreach (XT xt in list_xt)
                {
                    if ((xt.cy_zk.isONByChkSen && xt.XtMd != null && xt.XtMd.res >= 0 && !Demo) || (Demo && xt.XtMd != null)) xt_cnt++;
                }

            }
            if (WsTriPos.Count == 0 && TrayTriPos.Count == 0 && (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.UP_SCAN || (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN && PT_SET.bBarcodeCamBackEn && xt_cnt == 0)))
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}加载触发点并定位返回NEXT: X:{1} Y:{2}", disc, endpos.x, endpos.y));
                return EM_RES.NEXT;
            }
            //加载流程
            try
            {
                //if (traybox != null && endpos.a != -10000) ax_x_tray = traybox.ax_x;
                if (traybox != null) ax_x_tray = traybox.ax_x;
                //加载工站拍照点
                FlyCfg(EM_FLY_CFG.WS_MOD_UP, WsTriPos, WsTriPos.Count, Dir);
                //加载吸头拍照点

                if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN && PT_SET.bBarcodeCamBackEn)
                {

                    FlyCfg(EM_FLY_CFG.MOD_DW, new List<ST_XYN>(), xt_cnt, Dir);
                }

                //加载料盘拍照点
                FlyCfg(EM_FLY_CFG.MOD_UP, TrayTriPos, TrayTriPos.Count, Dir, true, WsTriPos.Count > 0 ? false : true);
                //料盘进仓储位置确认
                //if (endpos.a > traybox_fd.fd_safe_x)
                //{
                //    res = traybox_fd.PosTrayMovIn(ref bquit);
                endpos.a=endpos.a > 1 ? endpos.a :1;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("{0}定位到飞拍结束位置X:{1:f3},Y:{2:f3},X1:{3:f3},方向{4}", disc, endpos.x, endpos.y, endpos.a, Dir) : string.Format("{0}move to flyshot end posX :{2:f3},Y:{3:f3},X1:{4:f3},DIR:{5}       ({1}定位到飞拍结束位置X:{2:f3},Y:{3:f3},X1:{4:f3},方向{5})", disc, disc, endpos.x, endpos.y, endpos.a, Dir));

                if (ax_x_tray != null)
                {
                    int n = 0;
                    while (traybox.runin || !traybox.ax_x.isStop)
                    {
                        if (n++ > 500) break;
                        if (bquit) return EM_RES.QUIT;
                        Thread.Sleep(10);
                    }
                    traybox.MoveToLastPos[id] = true;
                    if (n > 500)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}定位超时5S", ax_x_tray.disc) : string.Format("{0}move timeout(5s)       ({0}定位超时5S)", ax_x_tray.disc), DReport.EmErrCode.Timeout, (int)DReport.EmHareware.UpDownLoad + id, ERR_ALM.EmErrItem.TimeOut);
                        return EM_RES.ERR;
                    }
                    if (traybox.disc == COM.traybox_fd.disc)
                    {
                        int posmaxofs = WsTriPos.Count > 0 ? 10 : 2;
                        if ((Math.Abs(ax_x_tray.fenc_pos - endpos.a) > posmaxofs) && !traybox.runin && traybox.ax_x.isStop)
                        {
                            //检查Z轴高度
                            if (Math.Abs(ax_z.fenc_pos) > 3)
                            {
                                res = MT.Move(ref bquit, ref ax_z, 0);
                                if (res != EM_RES.OK) return res;
                            }

                            res = MT.Move(ref bquit, ref ax_x_tray, endpos.a>2? endpos.a:2);
                            if (res != EM_RES.OK) return res;
                        }
                    }
                }

                //检查Z轴高度
                if (Math.Abs(ax_z.fenc_pos) > 3)
                {
                    res = MT.Move(ref bquit, ref ax_z, 0);
                    if (res != EM_RES.OK) return res;
                }

                //定位X X1与Y轴
                if (WsTriPos.Count > 0)
                {
                    res = ax_y.MoveTo(ref bquit, endpos.y);
                    if (res != EM_RES.OK) return res;
                    int t = 0;
                    while (true)
                    {
                        if (ax_y.fenc_pos < pos_mov_x)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}移动到料盘定位开始，位置X:{1}", disc, endpos.x));
                            res = MT.Move(ref bquit, ref ax_x, endpos.x, ref ax_x_tray, endpos.a>2? endpos.a:2, ref ax_u1, PT_SET.isopen_degree ? -PT_SET.degree : 0, ref ax_u2, PT_SET.isopen_degree ? -PT_SET.degree : 0, 3000);                           
                            if (res != EM_RES.OK) return res;
                            if (bquit) return EM_RES.QUIT;
                            if (id == 0 && !bquit)
                            {
                                UpDownLoad.ud2needwait = false;                             
                                UpDownLoad.ud2ws2needwait = false;
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}移动到料盘结束，上下料2等待释放-----", disc));
                            }
                            if (id == 1 && !bquit)
                            {
                                UpDownLoad.ud1needwait = false;
                                UpDownLoad.ud1ws3needwait = false;
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}移动到料盘结束，上下料1等待释放-----", disc));
                            }
                            break;
                        }
                        if (bquit) return EM_RES.QUIT;
                        Thread.Sleep(5);
                        if (t++ > 1000)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}飞拍定位超时!", ax_y.disc) : string.Format("{0}  flyshot move timeout!    ({0}飞拍定位超时!)", ax_y.disc), DReport.EmErrCode.Timeout, (int)DReport.EmHareware.UpDownLoad + id, ERR_ALM.EmErrItem.TimeOut);
                            return EM_RES.TIMEOUT;
                        }

                    }
                    res = ax_y.WaitForMoveDone(ref bquit, endpos.y, 10000);
                    if (res != EM_RES.OK) return res;
                }
                else
                {
                    ////U回零
                    //res = MT.Move(ref bquit, ref ax_u1, 0, ref ax_u2, 0, 1000);
                    //if (res != EM_RES.OK) return res;
                    //XY_X1定位
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("{0}放料{1}--> X:{2} Y:{3} X1:{4}", disc, traybox.disc, endpos.x, endpos.y, endpos.a) : string.Format("{0}Place mod{1}-->X:{4} Y:{5} X1:{6}         ({2}放料{3}--> X:{4} Y:{5} X1:{6})", disc, traybox.name, disc, traybox.disc, endpos.x, endpos.y, endpos.a));
                    AXIS ax_x_tray_temp = traybox.runin == true ? null : ax_x_tray;
                    res = MT.Move(ref bquit, ref ax_x, endpos.x, ref ax_y, endpos.y, ref ax_x_tray_temp, endpos.a>2? endpos.a:2,ref ax_u1, PT_SET.isopen_degree ? -PT_SET.degree : 0, ref ax_u2, PT_SET.isopen_degree ? -PT_SET.degree : 0);
                    if (res != EM_RES.OK) return res;
                }

            }
            finally
            {

                traybox.runin = false;
                ax_y.Stop();
                ax_y.DisableHcmp(upcam.TriIO.num);
                if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN && PT_SET.bBarcodeCamBackEn)
                ax_y.DisableHcmp(dwcam.TriIO.num);
                if (id == 0&&!bquit)
                {
                    UpDownLoad.ud2needwait = false;
                    UpDownLoad.ud2ws2needwait = false;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}移动到料盘结束，上下料2等待释放-----", disc));
                }
                if (id == 1 && !bquit)
                {
                    UpDownLoad.ud1needwait = false;
                    UpDownLoad.ud1ws3needwait = false;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}移动到料盘结束，上下料1等待释放-----", disc));
                }          
            }
            return EM_RES.OK;
        }

        /// <summary>
        /// 飞拍移动到料盘
        /// </summary>
        /// <param name="bquit"></param>
        /// <param name="traybox"></param>
        /// <returns></returns>
        public EM_RES FlyCamToTray(ref bool bquit, WSFEED ws, TrayBox traybox, ref ST_XYZA endpos, bool dir = true)
        {
            if (bquit) return EM_RES.QUIT;

            List<ST_XYN> WsTriPos = new List<ST_XYN>();
            List<ST_XYN> TrayTriPos = new List<ST_XYN>();
            List<Cam.VisionTask> List_vs_task_temp = new List<Cam.VisionTask>();
            List<Cam.VisionTask> List_vs_task_DwTemp = new List<Cam.VisionTask>();

            int xt_cnt = 0;
            //ST_XYZA endpos=new ST_XYZA();
            EM_RES res = EM_RES.OK;
            //配置工站回拍
            List<WSFEED.MdDat> pendingFeedbackModules = GetPendingFeedbackModules();
            if (pendingFeedbackModules.Count > 0)// && ax_y.fenc_pos > list_xt[0].st_cap_pos.y)
            {
                foreach (WSFEED.MdDat md in pendingFeedbackModules)
                {
                    WsTriPos.Add(new ST_XYN(FeedBackPos.x, md.st_place_pos[id].y, md.Num - 1));
                }

                if (Math.Abs(FeedBackPos.x - ax_x.fenc_pos) > 2 || Math.Abs(FeedBackPos.y - ax_y.fenc_pos) > 5)
                {
                    //if (FeedBackPos.y - ax_y.fenc_pos < 150)
                    //{
                    //    res = ax_y.SetToWorkSpd(0.4);
                    //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}速度降为原来的40%:", ax_y.disc));
                    //}
                    if (FindMod == EM_FIN_MOD.HIGH && id == 0)
                    {
                        UpDownLoad.ud2needwait = true;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}进行回检定位，上下料2等待开启-----", disc, ws.disc));
                    }
                    if (FindMod == EM_FIN_MOD.LOW && id == 1)
                    {
                        UpDownLoad.ud1needwait = true;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}进行回检定位，上下料1等待开启-----", disc, ws.disc));
                    }
                    #region 取放料保护
                    //Thread.Sleep(80);
                    res = SafeProtect(ref bquit);
                    if (res != EM_RES.OK)
                    {
                        return res;
                    }
                    #endregion
                    res = MT.ZupMove(ref bquit, ref ax_x, FeedBackPos.x, ref ax_y, FeedBackPos.y,ref ax_u1, PT_SET.isopen_degree ? -PT_SET.degree : 0, ref ax_u2, PT_SET.isopen_degree ? -PT_SET.degree : 0);
                    if (res != EM_RES.OK) return res;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}料盘放料定位到回拍完成: X:{1} Y:{2} ", disc, FeedBackPos.x, FeedBackPos.y));
                }

                dir = false;
            }

            //配置飞拍结束位置
            if (traybox.disc == traybox_fd.disc) res = FindFdTrayPos(ref endpos, ref TrayTriPos, ws, dir);
            else res = FindOkOrNgTrayPos(ref endpos, traybox, ws);
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}配置飞拍结束位置完成: X:{1} Y:{2} ，返回值：{3}", disc, endpos.x, endpos.y,res.ToString()));
            if (res != EM_RES.ABORT && res != EM_RES.OK) return res;
            else if (res == EM_RES.ABORT) return EM_RES.OK;

            if (ax_y.fenc_pos > endpos.y && dir && PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN && PT_SET.bBarcodeCamBackEn && traybox.disc != traybox_fd.disc) dir = false;
            //加载触发点并定位
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}加载触发点并定位: X:{1} Y:{2}", disc, endpos.x, endpos.y));
            res = FLyToTray(ref bquit, endpos, WsTriPos, TrayTriPos, traybox, dir);
            if (res != EM_RES.OK) return res;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}加载触发点并定位完成: X:{1} Y:{2}", disc, endpos.x, endpos.y));
            //等待结果
            bool barcode_err = false;
            WaitCamRes(upcam, WsTriPos.Count + TrayTriPos.Count);
            if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN && PT_SET.bBarcodeCamBackEn)
            {
                foreach (XT xt in list_xt)
                {
                    if ((xt.cy_zk.isONByChkSen && xt.XtMd != null && xt.XtMd.res >= 0 && !Demo) || (Demo && xt.XtMd != null))
                    {
                        xt_cnt++;
                        //List_vs_task_DwTemp.Add(dwcam.List_vs_task_cur[list_xt.IndexOf(xt)]);
                    }
                }
                if (xt_cnt > 0)
                {
                    WaitCamRes(dwcam, xt_cnt);
                    //复制任务
                    for (int i = 0; xt_cnt > 0 && i < xt_cnt; i++)
                    {
                        List_vs_task_DwTemp.Add(dwcam.List_vs_task_cur[i]);
                    }

                    if (!Demo)
                    {
                        //检查模组二维码             
                        foreach (Cam.VisionTask tsk in List_vs_task_DwTemp)
                        {
                            if (list_xt[tsk.TriPos.n].XtMd.res == 0 &&
                                (tsk.ResData.BarCode == null || tsk.ResData.BarCode.Length < 1 ||
                                 tsk.ResData.BarCode != list_xt[tsk.TriPos.n].XtMd.bardcode))
                            {
                                barcode_err = true;
                                RECAP:
                                res = MT.ZupMove(ref bquit, ref ax_y, tsk.TriPos.y);
                                if (res != EM_RES.OK) return res;
                                res = dwcam.FindTaskTriAndWait(CONST.ModDwFw[tsk.TriPos.n], Demo);
                                if (res != EM_RES.OK)
                                {
                                    if (res != EM_RES.CAM_ERR) return res;
                                }

                                if (res == EM_RES.OK &&
                                    (dwcam.curTask.ResData.BarCode == null || dwcam.curTask.ResData.BarCode.Length < 1))
                                {
                                    if (PT_SET.bBackerrcontinue)
                                    {
                                        COUNT_DATA.ErrTemp = System.DateTime.Now;
                                        VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "没检到二维码!" : "No Barcode", 20, true);
                                        MT.ST_WARN st_warn1 = new MT.ST_WARN();
                                        warning fr_warn1 = new warning();
                                        st_warn1.ok_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                                        st_warn1.abort_txt = VAR.IsChinese ? "重新拍照" : "Take a photo";
                                        st_warn1.ws = null;
                                        st_warn1.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                                        st_warn1.title = VAR.IsChinese ? "提示:没检到二维码" : "Tip: No barcode detected";
                                        st_warn1.msg = VAR.IsChinese ? string.Format("下相机{0}上模组二维码无法识别.", list_xt[tsk.TriPos.n].disc) : string.Format("Dwcam {0} module barcode cannot be identified\r\n下相机{0}上模组二维码无法识别.", list_xt[tsk.TriPos.n].disc);
                                        st_warn1.lb_msg = VAR.IsChinese ? "提示:" + st_warn1.msg + "请确认!\r\n  1.按'继续运行'键则放弃当前检查继续运行!\r\n  " +
                                                          "2.如需确认问题请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按重新拍照键则重新获取当前位置图像!" : "Tip:" + st_warn1.msg + "\r\nPlease confirm!\r\n  1. Press the 'Continue Running' key to abandon the current check and continue running! !\r\n  " +
                                                                                                                             "2. If you need to confirm the problem, please press the stop running button to exit the operation, and then press the run button after the top left corner of the interface is ready!\r\n 3. If you press the 'Take a photo' button, the current position image will be obtained again!" + "\r\n提示:" + st_warn1.msg + "请确认!\r\n  1.按'继续运行'键则放弃当前检查继续运行!\r\n  " +
                                                                                                                             "2.如需确认问题请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按重新拍照键则重新获取当前位置图像!";
                                        SQLData.TesterrDataAdd(99, 99, "料盘放料", string.Format("上下料{0}未检测到二维码", id));
                                        COUNT_DATA.Errcnt++;
                                        DialogResult logres1 = MT.Display_frwarn(fr_warn1, st_warn1, ERR_ALM.EmErrItem.BarcodeAbnormal);
                                        
                                        if (DialogResult.Cancel == logres1) return EM_RES.ERR;

                                        else if (DialogResult.Abort == logres1)
                                        {
                                            VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                            MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                            goto RECAP;
                                        }
                                        else
                                        {
                                            VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                            MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                            goto RECAP;
                                        }
                                    }
                                    else
                                    {
                                        COUNT_DATA.ErrTemp = System.DateTime.Now;
                                        VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "没检到二维码!" : "No Barcode", 20, true);
                                        MT.ST_WARN st_warn1 = new MT.ST_WARN();
                                        warning fr_warn1 = new warning();
                                        //st_warn1.ok_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                                        st_warn1.ok_txt = VAR.IsChinese ? "重新拍照" : "Take a photo";
                                        st_warn1.ws = null;
                                        st_warn1.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                                        st_warn1.title = VAR.IsChinese ? "提示:没检到二维码" : "Tip: No barcode detected";
                                        st_warn1.msg = VAR.IsChinese ? string.Format("下相机{0}上模组二维码无法识别.", list_xt[tsk.TriPos.n].disc) : string.Format("Dwcam {0} module barcode cannot be identified\r\n下相机{0}上模组二维码无法识别.", list_xt[tsk.TriPos.n].disc);
                                        st_warn1.lb_msg = VAR.IsChinese ? "提示:" + st_warn1.msg + "请确认!\r\n  1.按'继续运行'键则放弃当前检查继续运行!\r\n  " +
                                                          "2.如需确认问题请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按重新拍照键则重新获取当前位置图像!" : "Tip:" + st_warn1.msg + "\r\nPlease confirm!\r\n  1. Press the 'Continue Running' key to abandon the current check and continue running! !\r\n  " +
                                                                                                                             "2. If you need to confirm the problem, please press the stop running button to exit the operation, and then press the run button after the top left corner of the interface is ready!\r\n 3. If you press the 'Take a photo' button, the current position image will be obtained again!" + "\r\n提示:" + st_warn1.msg + "请确认!\r\n  1.按'继续运行'键则放弃当前检查继续运行!\r\n  " +
                                                                                                                             "2.如需确认问题请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按重新拍照键则重新获取当前位置图像!";
                                        DialogResult logres1 = MT.Display_frwarn(fr_warn1, st_warn1, ERR_ALM.EmErrItem.BarcodeAbnormal);
                                        if (DialogResult.Cancel == logres1) return EM_RES.ERR;

                                        if (DialogResult.OK == logres1)
                                        {
                                            VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                            goto RECAP;
                                        }
                                    }
                                }

                                if (dwcam.curTask.ResData.BarCode != null && dwcam.curTask.ResData.BarCode.Length > 1 &&
                                    dwcam.curTask.ResData.BarCode != list_xt[tsk.TriPos.n].XtMd.bardcode && !Demo)
                                {
                                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG,
                                        VAR.IsChinese ? string.Format("下相机->{0}回检二维码不一致,实拍Barcode:{1},记录Barcode:{2},记录二维码结果0改为111",
                                            list_xt[tsk.TriPos.n].disc, tsk.ResData.BarCode,
                                            list_xt[tsk.TriPos.n].XtMd.bardcode) : string.Format("DwCam{0} check back barcode is inconsistent,new barcode:{1},old barcode:{2},recording the barcode results as 111.           (下相机->{0}回检二维码不一致,实拍Barcode:{1},记录Barcode:{2},记录二维码结果0改为111)",
                                            list_xt[tsk.TriPos.n].disc, tsk.ResData.BarCode,
                                            list_xt[tsk.TriPos.n].XtMd.bardcode))
                                    ;
                                    list_xt[tsk.TriPos.n].XtMd.res = 111;
                                }
                            }
                        }
                    }
                }
            }

            //检查结果 
            res = ChkCamRes(upcam, WsTriPos.Count + TrayTriPos.Count);
            if (res != EM_RES.OK) return res;



            //回拍结果判断
            modupcam_task.Clear();
            bool mtofs = false;
            bool wsBarcodeErr = false;
            if (!Demo)
            {
                //复制任务
                for (int i = 0; WsTriPos.Count > 0 && i < WsTriPos.Count; i++)
                {
                    List_vs_task_temp.Add(upcam.List_vs_task_cur[i]);
                }

                //寻找上相机拍模组数据
                if (TrayTriPos.Count > 0)
                {
                    foreach (Cam.VisionTask task in upcam.List_vs_task_cur)
                    {
                        if (task.TaskName.Contains(CONST.ModUpFw))
                            modupcam_task.Add(task);
                    }

                    if (modupcam_task.Count > 1)
                    {
                        Cam.VisionTask task1 = modupcam_task.Find(s => s.TaskName.Equals("ModUp_Shp"));
                        Cam.VisionTask task2 = modupcam_task.Find(s => s.TaskName.Equals("ModUp_Shp1"));
                        if (task1 != null && task2 != null && task1.ResData.TimeStamp > task2.ResData.TimeStamp)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} photograph (ModUp_Shp) TimeStamp err!ModUp_Shp:{2},ModUp_Shp1:{3}           ({1}拍模组ModUp_Shp时间戳有误!ModUp_Shp:{2},ModUp_Shp1:{3})", disc, disc, task1.ResData.TimeStamp, task2.ResData.TimeStamp), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.UpDownLoad + id);
                            task1.ResData.bOK = false;
                            task2.ResData.bOK = false;
                        }
                    }
                }

                for (int i = 0; WsTriPos.Count > 0 && i < WsTriPos.Count; i++)
                {
                    Cam.VisionTask task = List_vs_task_temp[i];
                    //二维码回检
                    if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.UP_SCAN && PT_SET.bBarcodeCamBackEn)
                    {
                        WSFEED.MdDat md = ws.list_md[WsTriPos[i].n];
                        res = VerifyUpperBarcodeBackcheck(ref bquit, ws, WsTriPos[i], md.bardcode,
                            task.ResData.BarCode, ref wsBarcodeErr, "普通飞拍");
                        if (res != EM_RES.OK) return res;
                    }
                    //放偏确认
                    // if (!task.ResData.bOK || Math.Abs(task.ResData.PosMM.x) > PT_SET.Vs_XYofs || Math.Abs(task.ResData.PosMM.y) > PT_SET.Vs_XYofs || Math.Abs(task.ResData.PosMM.a) > PT_SET.Vs_Rofs)
                    string[] date = new string[3];
                    double left = double.MaxValue;
                    double right = double.MaxValue;
                    double up = double.MaxValue; ;
                    double down = double.MaxValue;
                    if (task.ResData.bOK)
                    {
                        try
                        {
                            date = task.ResData.Message.Split(',');
                            var ret = AreaCal(date, out left, out right, out up, out down);
                            if (ret != EM_RES.OK) return ret;

                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }

                    //放偏确认
                    //if (!task.ResData.bOK || Math.Abs(task.ResData.PosMM.x) > PT_SET.Vs_XYofs || Math.Abs(task.ResData.PosMM.y) > PT_SET.Vs_XYofs || Math.Abs(task.ResData.PosMM.a) > PT_SET.Vs_Rofs)
                    if (!task.ResData.bOK
                        || Math.Abs(task.ResData.PosMM.x) > PT_SET.Vs_XYofs
                        || Math.Abs(task.ResData.PosMM.y) > PT_SET.Vs_XYofs
                        || Math.Abs(task.ResData.PosMM.a) > PT_SET.Vs_Rofs
                        || (Math.Abs(left - PT_SET.LeftArea) > PT_SET.Area && id == 0)
                        || (Math.Abs(right - PT_SET.RightArea) > PT_SET.Area && id == 0)
                        || (Math.Abs(up - PT_SET.UpArea) > PT_SET.Area && PT_SET.bConnectorCheck && id == 0)
                        || (Math.Abs(down - PT_SET.DownArea) > PT_SET.Area && PT_SET.bConnectorCheck && id == 0)

                        || (Math.Abs(left - PT_SET.LeftArea2) > PT_SET.Area2 && id == 1)
                        || (Math.Abs(right - PT_SET.RightArea2) > PT_SET.Area2 && id == 1)
                        || (Math.Abs(up - PT_SET.UpArea2) > PT_SET.Area2 && PT_SET.bConnectorCheck && id == 1)
                        || (Math.Abs(down - PT_SET.DownArea2) > PT_SET.Area2 && PT_SET.bConnectorCheck && id == 1)
                        )
                        {                        
                        upcam.SaveOriginImage(task.Image, string.Format("{0}\\image\\{1}\\BACK", VAR.gsys_set.GetCurProductPath, upcam.mName), string.Format("{0}{1}.jpg",
                                    DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.ToString("HHmmss_fff")));
                        //upcam.SaveOriginImage(FrMain.frrun.cogDisplayer_run.cogRecordDisplay.Image, string.Format("{0}\\image\\{1}\\BACK", VAR.gsys_set.GetCurProductPath, upcam.mName), string.Format("{0}{1}.jpg",
                        //            DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.ToString("HHmmss_fff")));
                        COUNT_DATA.SuctionErrcnt++;
                        // SQLData.TesterrDataAdd(ws.id, ws.list_md[WsTriPos[i].n].Num, "工站放料", string.Format("{0}物料放偏", ws.disc));
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}物料放偏坐标{1}，对工位{2}重新确认",disc, task.ResData.PosMM.ToString(), WsTriPos[i].n + 1) : string.Format("Crooked material pos{0},WS{1} reconfirms           (物料放偏坐标{0}，工位{1}重新确认)", task.ResData.PosMM.ToString(), WsTriPos[i].n + 1));
                        RECAM:
                        #region 取放料保护
                        if (id == 1) ud1needwait = true;
                        if (id == 0) ud1needwait = true; ud2needwait = true; 
                        res = SafeProtect(ref bquit);
                        if (res != EM_RES.OK)
                        {
                            return res;
                        }
                        #endregion
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}重新拍照定位坐标X: {1}，坐标Y: {2}", disc,WsTriPos[i].x.ToString(), WsTriPos[i].y.ToString()) : string.Format("Crooked material pos{0},WS{1} reconfirms", WsTriPos[i].x.ToString(), WsTriPos[i].y.ToString()));
                        res = MT.ZupMove(ref bquit, ref ax_x, WsTriPos[i].x, ref ax_y, WsTriPos[i].y);
                        if (res != EM_RES.OK) return res;
                        res = upcam.FindTaskTriAndWait(CONST.WsModUpFw, Demo);
                        if (res != EM_RES.OK)
                        {
                            if (res != EM_RES.CAM_ERR) return res;
                        }
                        else
                        {
                            date = upcam.curTask.ResData.Message.Split(',');
                            try
                            {
                                if (date.Length < 2)
                                {
                                    MT.ST_WARN warn = new MT.ST_WARN();
                                    warning fr_warn = new warning();
                                    warn.ok_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                                    warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                                    warn.ws = null;
                                    warn.title = VAR.IsChinese ? "提示:数据异常" : "Tip: Data File  Err";
                                    warn.msg = "当前放偏检测没有防压伤功能!";
                                    warn.lb_msg = "提示:请确认视觉文件和模板，请确认!\r\n  1.如果忽视,请按继续运行键!\r\n  " +
                                                                  "2.请停止运行，更新最新视觉文件和模板后，再按运行键!";
                                    DialogResult logres = MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.MaterialPosErr);
                                    if (DialogResult.Cancel == logres)
                                    {
                                        return EM_RES.ERR;
                                    }
                                    else

                                        left = 0;
                                    right = 0;
                                    up = 0;
                                    down = 0;
                                }
                                else
                                {
                                    left = Convert.ToDouble(date[0]);
                                    right = Convert.ToDouble(date[1]);
                                    up = Convert.ToDouble(date[2]);
                                    down = Convert.ToDouble(date[3]);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                        if (!upcam.curTask.ResData.bOK
                            || Math.Abs(upcam.curTask.ResData.PosMM.x) > PT_SET.Vs_XYofs
                            || Math.Abs(upcam.curTask.ResData.PosMM.y) > PT_SET.Vs_XYofs
                            || Math.Abs(upcam.curTask.ResData.PosMM.a) > PT_SET.Vs_Rofs
                            || (Math.Abs(left - PT_SET.LeftArea) > PT_SET.Area && id == 0)
                        || (Math.Abs(right - PT_SET.RightArea) > PT_SET.Area && id == 0)
                        || (Math.Abs(up - PT_SET.UpArea) > PT_SET.Area && PT_SET.bConnectorCheck && id == 0)
                        || (Math.Abs(down - PT_SET.DownArea) > PT_SET.Area && PT_SET.bConnectorCheck && id == 0)

                        || (Math.Abs(left - PT_SET.LeftArea2) > PT_SET.Area2 && id == 1)
                        || (Math.Abs(right - PT_SET.RightArea2) > PT_SET.Area2 && id == 1)
                        || (Math.Abs(up - PT_SET.UpArea2) > PT_SET.Area2 && PT_SET.bConnectorCheck && id == 1)
                        || (Math.Abs(down - PT_SET.DownArea2) > PT_SET.Area2 && PT_SET.bConnectorCheck && id == 1)
                        )
                            // if (!upcam.curTask.ResData.bOK || Math.Abs(upcam.curTask.ResData.PosMM.x) > PT_SET.Vs_XYofs || Math.Abs(upcam.curTask.ResData.PosMM.y) > PT_SET.Vs_XYofs || Math.Abs(upcam.curTask.ResData.PosMM.a) > PT_SET.Vs_Rofs)
                            mtofs = true;
                        if (mtofs && PT_SET.bEnVsFB)
                        {
                            if (PT_SET.bBackerrcontinue)
                            {
                                COUNT_DATA.ErrTemp = System.DateTime.Now;
                                mtofs = false;
                                VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "物料放偏!" : "Place deflected", 20, true);
                                MT.ST_WARN warn = new MT.ST_WARN();
                                warning fr_warn = new warning();
                                warn.ok_txt = VAR.IsChinese ? "拍照确认" : "Take a photo";
                                warn.abort_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                                warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                                warn.ws = null;
                                warn.title = VAR.IsChinese ? "提示:物料放偏" : "Tip: Material is biased";
                                warn.msg = VAR.IsChinese ? "当前工站有物料放偏!" : "There is material deviation in the current station!\r\n当前工站有物料放偏!";
                                warn.lb_msg = VAR.IsChinese ? "提示:当前工站有物料放偏，请确认!\r\n  1.如需重新拍照确认,请按拍照确认键!\r\n  " +
                                              "2.如果确认没有物料放偏,请按继续运行键!\r\n  3.如有物料放偏请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!" : "Tip:There is material deviation in the current station, please confirm!\r\n  1.If you need to retake a photo, please press the 'Take a photo' button!\r\n  " +
                                                                                       "2.If there is no material deviation, please press the 'Keep running' button!\r\n  3.If there is material deviation, please press the stop running button to exit the operation, and then press the run button after the ready is displayed in the upper left corner of the interface!\r\n提示:当前工站有物料放偏，请确认!\r\n  1.如需重新拍照确认,请按拍照确认键!\r\n  " +
                                                                                       "2.如果确认没有物料放偏,请按继续运行键!\r\n  3.如有物料放偏请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!";
                                SQLData.TesterrDataAdd(ws.id, ws.list_md[WsTriPos[i].n].Num, "工站放料", string.Format("{0}物料放偏", ws.disc));
                                COUNT_DATA.Errcnt++;
                                DialogResult logres = MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.MaterialPosErr);
                                if (DialogResult.Cancel == logres)
                                {
                                    return EM_RES.ERR;
                                }
                                else if (DialogResult.OK == logres)
                                {
                                    VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                    MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                    goto RECAM;
                                }
                                else
                                {
                                    VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                    MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                    //goto RECAM;
                                }
                            }
                            else
                            {
                                COUNT_DATA.ErrTemp = System.DateTime.Now;
                                mtofs = false;
                                SQLData.TesterrDataAdd(ws.id, ws.list_md[WsTriPos[i].n].Num, "工站放料", string.Format("{0}物料放偏", ws.disc));
                                COUNT_DATA.Errcnt++;
                                VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "物料放偏!" : "Place deflected", 20, true);
                                MT.ST_WARN warn = new MT.ST_WARN();
                                warning fr_warn = new warning();
                                //warn.ok_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                                warn.ok_txt = VAR.IsChinese ? "拍照确认" : "Take a photo";
                                warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                                warn.ws = null;
                                warn.title = VAR.IsChinese ? "提示:物料放偏" : "Tip: Material is biased";
                                warn.msg = VAR.IsChinese ? "当前工站有物料放偏!" : "There is material deviation in the current station!\r\n当前工站有物料放偏!";
                                warn.lb_msg = VAR.IsChinese ? "提示:当前工站有物料放偏，请确认!\r\n  1.如果没有物料放偏,请按继续运行键!\r\n  " +
                                              "2.如有物料放偏请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!" : "Tip:There is material deviation in the current station, please confirm!\r\n  1.If there is no material deviation, please press the 'Keep running' button!\r\n  " +
                                                                                       "2.If there is material deviation, please press the stop running button to exit the operation, and then press the run button after the ready is displayed in the upper left corner of the interface!\r\n提示:当前工站有物料放偏，请确认!\r\n  1.如果没有物料放偏,请按继续运行键!\r\n  " +
                                                                                       "2.如有物料放偏请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!";
                                DialogResult logres = MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.MaterialPosErr);
                                if (DialogResult.Cancel == logres)
                                {
                                    return EM_RES.ERR;
                                }
                                else if (DialogResult.OK == logres)
                                {
                                    VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                    goto RECAM;
                                }
                                else
                                {
                                    VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                    //goto RECAM;
                                }
                            }
                        }
                    }

                    CompleteFeedbackModule(WsTriPos[i].n + 1);
                }
            }
            else
            {
                foreach (ST_XYN position in WsTriPos)
                {
                    CompleteFeedbackModule(position.n + 1);
                }
            }

            //清空相机数据
            if (!upcam.FlushOk && !upcam.FlushUpdate)
            {
                Task tak = new Task(() =>
                {
                    upcam.FlushUpdate = true;
                    upcam.mAcqFifo.Flush();
                    upcam.FlushOk = true;
                    upcam.FlushUpdate = false;
                });
                tak.Start();
            }

            if (!dwcam.FlushOk && !dwcam.FlushUpdate)
            {
                Task takdw = new Task(() =>
                {
                    dwcam.FlushUpdate = true;
                    dwcam.mAcqFifo.Flush();
                    dwcam.FlushOk = true;
                    dwcam.FlushUpdate = false;
                });
                takdw.Start();
            }
            if (wsBarcodeErr) return EM_RES.RETRY;
            if (barcode_err && traybox.disc == traybox_ok.disc) return EM_RES.ABORT;
            return EM_RES.OK;
        }
        //最后一个位置回检
        public EM_RES FLyToLastPos(ref bool bquit, ST_XYZA endpos, List<ST_XYN> WsTriPos, bool Dir = false)
        {

            EM_RES res;
            double pos_mov_x = list_xt[0].st_cap_pos.y;
            int xt_cnt = 0;
            if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN && PT_SET.bBarcodeCamBackEn && ax_y.fenc_pos > list_xt[0].st_cap_pos.y)
            {
                foreach (XT xt in list_xt)
                {
                    if ((xt.cy_zk.isONByChkSen && xt.XtMd != null && xt.XtMd.res >= 0 && !Demo) || (Demo && xt.XtMd != null)) xt_cnt++;
                }

            }
            if (WsTriPos.Count == 0 && (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.UP_SCAN || (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN && PT_SET.bBarcodeCamBackEn && xt_cnt == 0))) return EM_RES.NEXT;

            //加载流程
            try
            {
                //加载工站拍照点
                FlyCfg(EM_FLY_CFG.WS_MOD_UP, WsTriPos, WsTriPos.Count, Dir);
                endpos.a = endpos.a > 1 ? endpos.a : 1;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("{0}定位到飞拍结束位置X:{1:f3},Y:{2:f3},X1:{3:f3},方向{4}", disc, endpos.x, endpos.y, endpos.a, Dir) : string.Format("{0}move to flyshot end posX :{2:f3},Y:{3:f3},X1:{4:f3},DIR:{5}            ({1}定位到飞拍结束位置X:{2:f3},Y:{3:f3},X1:{4:f3},方向{5})", disc, disc, endpos.x, endpos.y, endpos.a, Dir));

                //定位X X1与Y轴
                if (WsTriPos.Count > 0)
                {
                    res = ax_y.MoveTo(ref bquit, endpos.y);
                    if (res != EM_RES.OK) return res;
                    int t = 0;
                    while (true)
                    {
                        if (ax_y.fenc_pos < pos_mov_x)
                        {
                            res = MT.Move(ref bquit, ref ax_x, endpos.x, ref ax_u1, 0, ref ax_u2, 0, 3000);
                            if (res != EM_RES.OK) return res;
                            break;
                        }
                        if (bquit) return EM_RES.QUIT;
                        Thread.Sleep(5);
                        if (t++ > 1000)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}飞拍定位超时!", ax_y.disc) : string.Format("{0} flyshot move timeout!     ({0}飞拍定位超时!)", ax_y.disc), DReport.EmErrCode.Timeout, (int)DReport.EmHareware.UpDownLoad + id, ERR_ALM.EmErrItem.TimeOut);
                            return EM_RES.TIMEOUT;
                        }

                    }
                    res = ax_y.WaitForMoveDone(ref bquit, endpos.y, 10000);
                    if (res != EM_RES.OK) return res;
                }
            }
            finally
            {
                ax_y.Stop();
                ax_y.DisableHcmp(upcam.TriIO.num);
                //if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN && PT_SET.bBarcodeCamBackEn)
                //    ax_y.DisableHcmp(dwcam.TriIO.num);
            }
            return EM_RES.OK;
        }
        public EM_RES FlyCamToLastPos(ref bool bquit, WSFEED ws, ref ST_XYZA endpos, bool dir = true)
        {
            if (bquit) return EM_RES.QUIT;

            List<ST_XYN> WsTriPos = new List<ST_XYN>();
            // List<ST_XYN> TrayTriPos = new List<ST_XYN>();
            List<Cam.VisionTask> List_vs_task_temp = new List<Cam.VisionTask>();
            //List<Cam.VisionTask> List_vs_task_DwTemp = new List<Cam.VisionTask>();

            //ST_XYZA endpos=new ST_XYZA();
            EM_RES res = EM_RES.OK;
            //配置工站回拍
            List<WSFEED.MdDat> pendingFeedbackModules = GetPendingFeedbackModules();
            if (pendingFeedbackModules.Count > 0)// && ax_y.fenc_pos > list_xt[0].st_cap_pos.y)
            {
                foreach (WSFEED.MdDat md in pendingFeedbackModules)
                {
                    WsTriPos.Add(new ST_XYN(FeedBackPos.x, md.st_place_pos[id].y, md.Num - 1));
                }

                if (Math.Abs(FeedBackPos.x - ax_x.fenc_pos) > 2 || Math.Abs(FeedBackPos.y - ax_y.fenc_pos) > 5)
                {
                    if (FindMod == EM_FIN_MOD.HIGH && id == 0)
                    {
                        UpDownLoad.ud2needwait = true;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}进行最后放料位置回检，上下料2等待开启-----", disc, ws.disc));
                    }
                    if (FindMod == EM_FIN_MOD.LOW && id == 1)
                    {
                        UpDownLoad.ud1needwait = true;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}进行最后放料位置回检，上下料1等待开启-----", disc, ws.disc));
                    }

                    #region 取放料保护

                    res = SafeProtect(ref bquit);
                    if (res != EM_RES.OK)
                    {
                        return res;
                    }
                    #endregion
                    if (PT_SET.blongXT)
                    {
                        res = MT.Move(ref bquit, ref ax_x, FeedBackPos.x);
                        if (res != EM_RES.OK) return res;
                        res = MT.Move(ref bquit, ref ax_y, FeedBackPos.y);
                        if (res != EM_RES.OK) return res;
                    }
                    else
                    {
                        res = MT.Move(ref bquit, ref ax_x, FeedBackPos.x, ref ax_y, FeedBackPos.y);
                        if (res != EM_RES.OK) return res;
                    }                    
                    
                }

                dir = false;
            }
            //配置飞拍结束位置
            endpos.x = FeedBackPos.x;
            endpos.y = list_xt[1].st_cap_pos.y;
            endpos.z = 0;
            endpos.a = 1;

            // if (ax_y.fenc_pos > endpos.y && dir && PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN && PT_SET.bBarcodeCamBackEn) dir = false;
            //加载触发点并定位
            res = FLyToLastPos(ref bquit, endpos, WsTriPos, dir);
            if (res != EM_RES.OK) return res;

            //等待结果
            // bool barcode_err = false;
            WaitCamRes(upcam, WsTriPos.Count);

            //检查结果 
            res = ChkCamRes(upcam, WsTriPos.Count);
            if (res != EM_RES.OK) return res;



            //回拍结果判断
            //modupcam_task.Clear();
            bool mtofs = false;
            bool wsBarcodeErr = false;
            if (!Demo)
            {
                //复制任务
                for (int i = 0; WsTriPos.Count > 0 && i < WsTriPos.Count; i++)
                {
                    List_vs_task_temp.Add(upcam.List_vs_task_cur[i]);
                }

                ////寻找上相机拍模组数据
                //if (TrayTriPos.Count > 0)
                //{
                //    foreach (Cam.VisionTask task in upcam.List_vs_task_cur)
                //    {
                //        if (task.TaskName.Contains(CONST.ModUpFw))
                //            modupcam_task.Add(task);
                //    }

                //    if (modupcam_task.Count > 1)
                //    {
                //        Cam.VisionTask task1 = modupcam_task.Find(s => s.TaskName.Equals("ModUp_Shp"));
                //        Cam.VisionTask task2 = modupcam_task.Find(s => s.TaskName.Equals("ModUp_Shp1"));
                //        if (task1 != null && task2 != null && task1.ResData.TimeStamp > task2.ResData.TimeStamp)
                //        {
                //            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}拍模组ModUp_Shp时间戳有误!ModUp_Shp:{1},ModUp_Shp1:{2}", disc, task1.ResData.TimeStamp, task2.ResData.TimeStamp));
                //            task1.ResData.bOK = false;
                //            task2.ResData.bOK = false;
                //        }
                //    }
                //}

                for (int i = 0; WsTriPos.Count > 0 && i < WsTriPos.Count; i++)
                {
                    Cam.VisionTask task = List_vs_task_temp[i];
                    //二维码回检
                    if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.UP_SCAN && PT_SET.bBarcodeCamBackEn)
                    {
                        WSFEED.MdDat md = ws.list_md[WsTriPos[i].n];
                        res = VerifyUpperBarcodeBackcheck(ref bquit, ws, WsTriPos[i], md.bardcode,
                            task.ResData.BarCode, ref wsBarcodeErr, "最后位置");
                        if (res != EM_RES.OK) return res;
                    }
                    //放偏确认
                    // if (!task.ResData.bOK || Math.Abs(task.ResData.PosMM.x) > PT_SET.Vs_XYofs || Math.Abs(task.ResData.PosMM.y) > PT_SET.Vs_XYofs || Math.Abs(task.ResData.PosMM.a) > PT_SET.Vs_Rofs)
                    string[] date = new string[3];
                    double left = double.MaxValue;
                    double right = double.MaxValue;
                    double up = double.MaxValue; ;
                    double down = double.MaxValue;
                    if (task.ResData.bOK)
                    {
                        try
                        {
                            date = task.ResData.Message.Split(',');
                            var ret = AreaCal(date, out left, out right, out up, out down);
                            if (ret != EM_RES.OK) return ret;
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                    if (!task.ResData.bOK
                        || Math.Abs(task.ResData.PosMM.x) > PT_SET.Vs_XYofs
                        || Math.Abs(task.ResData.PosMM.y) > PT_SET.Vs_XYofs
                        || Math.Abs(task.ResData.PosMM.a) > PT_SET.Vs_Rofs
                        || (Math.Abs(left - PT_SET.LeftArea) > PT_SET.Area && id == 0)
                        || (Math.Abs(right - PT_SET.RightArea) > PT_SET.Area && id == 0)
                        || (Math.Abs(up - PT_SET.UpArea) > PT_SET.Area && PT_SET.bConnectorCheck && id == 0)
                        || (Math.Abs(down - PT_SET.DownArea) > PT_SET.Area && PT_SET.bConnectorCheck && id == 0)

                        || (Math.Abs(left - PT_SET.LeftArea2) > PT_SET.Area2 && id == 1)
                        || (Math.Abs(right - PT_SET.RightArea2) > PT_SET.Area2 && id == 1)
                        || (Math.Abs(up - PT_SET.UpArea2) > PT_SET.Area2 && PT_SET.bConnectorCheck && id == 1)
                        || (Math.Abs(down - PT_SET.DownArea2) > PT_SET.Area2 && PT_SET.bConnectorCheck && id == 1)

                     )
                    {
                        COUNT_DATA.SuctionErrcnt++;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("物料放偏坐标{0}，工位{1}重新确认", task.ResData.PosMM.ToString(), WsTriPos[i].n + 1) : string.Format("Crooked material pos{0},WS{1} reconfirms           (物料放偏坐标{0}，工位{1}重新确认)", task.ResData.PosMM.ToString(), WsTriPos[i].n + 1));
                        RECAM:
                        #region 取放料保护
                        if (id == 1) ud1needwait = true;
                        if (id == 0) ud2needwait = true;
                        res = SafeProtect(ref bquit);
                        if (res != EM_RES.OK)
                        {
                            return res;
                        }
                        #endregion
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("重新拍照定位坐标X: {0}，坐标Y: {1}", WsTriPos[i].x.ToString(), WsTriPos[i].y.ToString()) : string.Format("Crooked material pos{0},WS{1} reconfirms", WsTriPos[i].x.ToString(), WsTriPos[i].y.ToString()));
                        if (PT_SET.blongXT)
                        {
                            res = MT.ZupMove(ref bquit, ref ax_x, WsTriPos[i].x);
                            if (res != EM_RES.OK) return res;
                            res = MT.ZupMove(ref bquit, ref ax_y, WsTriPos[i].y);
                            if (res != EM_RES.OK) return res;
                        }
                        else
                        {
                            res = MT.ZupMove(ref bquit, ref ax_x, WsTriPos[i].x, ref ax_y, WsTriPos[i].y);
                            if (res != EM_RES.OK) return res;
                        }
                          
                        res = upcam.FindTaskTriAndWait(CONST.WsModUpFw, Demo);
                        if (res != EM_RES.OK)
                        {
                            if (res != EM_RES.CAM_ERR) return res;
                        }
                        else
                        {
                            try
                            {
                                date = upcam.curTask.ResData.Message.Split(',');
                                var ret = AreaCal(date, out left, out right, out up, out down);
                                if (ret != EM_RES.OK) return ret;
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }

                        //if (!upcam.curTask.ResData.bOK || Math.Abs(upcam.curTask.ResData.PosMM.x) > PT_SET.Vs_XYofs || Math.Abs(upcam.curTask.ResData.PosMM.y) > PT_SET.Vs_XYofs || Math.Abs(upcam.curTask.ResData.PosMM.a) > PT_SET.Vs_Rofs)
                        if (!upcam.curTask.ResData.bOK
                                || Math.Abs(upcam.curTask.ResData.PosMM.x) > PT_SET.Vs_XYofs
                                || Math.Abs(upcam.curTask.ResData.PosMM.y) > PT_SET.Vs_XYofs
                                || Math.Abs(upcam.curTask.ResData.PosMM.a) > PT_SET.Vs_Rofs
                                || (Math.Abs(left - PT_SET.LeftArea) > PT_SET.Area && id == 0)
                            || (Math.Abs(right - PT_SET.RightArea) > PT_SET.Area && id == 0)
                            || (Math.Abs(up - PT_SET.UpArea) > PT_SET.Area && PT_SET.bConnectorCheck && id == 0)
                            || (Math.Abs(down - PT_SET.DownArea) > PT_SET.Area && PT_SET.bConnectorCheck && id == 0)

                            || (Math.Abs(left - PT_SET.LeftArea2) > PT_SET.Area2 && id == 1)
                            || (Math.Abs(right - PT_SET.RightArea2) > PT_SET.Area2 && id == 1)
                            || (Math.Abs(up - PT_SET.UpArea2) > PT_SET.Area2 && PT_SET.bConnectorCheck && id == 1)
                            || (Math.Abs(down - PT_SET.DownArea2) > PT_SET.Area2 && PT_SET.bConnectorCheck && id == 1)
                            )
                            mtofs = true;
                        if (mtofs && PT_SET.bEnVsFB)
                        {
                            mtofs = false;
                            VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "物料放偏!" : "Place deflected", 80, true);
                            MT.ST_WARN warn = new MT.ST_WARN();
                            warning fr_warn = new warning();
                            if (PT_SET.bBackerrcontinue)
                            {
                                warn.ok_txt = VAR.IsChinese ? "拍照确认" : "Take a photo";
                                warn.abort_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                                warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                                warn.ws = null;
                                warn.title = VAR.IsChinese ? "提示:物料放偏" : "Tip: Material is biased";
                                warn.msg = VAR.IsChinese ? "当前工站有物料放偏!" : "There is material deviation in the current station!\r\n当前工站有物料放偏!";
                                warn.lb_msg = VAR.IsChinese ? "提示:当前工站有物料放偏，请确认!\r\n  1.如需重新拍照确认,请按拍照确认键!\r\n  " +
                                                              "2.如果确认没有物料放偏,请按继续运行键!\r\n  3.如有物料放偏请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!" : "Tip:There is material deviation in the current station, please confirm! \r\n 1. If you need to retake a photo, please press the 'Take a photo' button! \r\n 2. If there is no material deviation, please press the continue running key! \r\n 3. If the material is biased, please press the stop running button to exit the operation, and then press the run button after the upper left corner of the interface is ready!\r\n提示:当前工站有物料放偏，请确认!\r\n  1.如需重新拍照确认,请按拍照确认键!\r\n  " +
                                                                                                        "2.如果确认没有物料放偏,请按继续运行键!\r\n  3.如有物料放偏请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!";
                                DialogResult logres = MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.MaterialPosErr);
                                if (DialogResult.Cancel == logres)
                                {
                                    return EM_RES.ERR;
                                }
                                else if (DialogResult.OK == logres)
                                {
                                    VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                    goto RECAM;
                                }
                                else
                                {
                                    VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                }
                            }
                            else
                            {
                                //warn.ok_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                                warn.ok_txt = VAR.IsChinese ? "拍照确认" : "Take a photo";
                                warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                                warn.ws = null;
                                warn.title = VAR.IsChinese ? "提示:物料放偏" : "Tip: Material is biased";
                                warn.msg = VAR.IsChinese ? "当前工站有物料放偏!" : "There is material deviation in the current station!\r\n当前工站有物料放偏!";
                                warn.lb_msg = VAR.IsChinese ? "提示:当前工站有物料放偏，请确认!\r\n  1.如果没有物料放偏,请按继续运行键!\r\n  " +
                                                              "2.如有物料放偏请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!" : "Tip:There is material deviation in the current station, please confirm! \r\n 1. If there is no material deviation, please press the continue running key! \r\n 2. If the material is biased, please press the stop running button to exit the operation, and then press the run button after the upper left corner of the interface is ready!\r\n提示:当前工站有物料放偏，请确认!\r\n  1.如果没有物料放偏,请按继续运行键!\r\n  " +
                                                                                                        "2.如有物料放偏请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!";
                                DialogResult logres = MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.MaterialPosErr);
                                if (DialogResult.Cancel == logres)
                                {
                                    return EM_RES.ERR;
                                }
                                else if (DialogResult.OK == logres)
                                {
                                    VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                    goto RECAM;
                                }

                            }

                        }
                    }

                    CompleteFeedbackModule(WsTriPos[i].n + 1);
                }
            }
            else
            {
                foreach (ST_XYN position in WsTriPos)
                {
                    CompleteFeedbackModule(position.n + 1);
                }
            }

            //清空相机数据
            if (!upcam.FlushOk && !upcam.FlushUpdate)
            {
                Task tak = new Task(() =>
                {
                    upcam.FlushUpdate = true;
                    upcam.mAcqFifo.Flush();
                    upcam.FlushOk = true;
                    upcam.FlushUpdate = false;
                });
                tak.Start();
            }

            if (wsBarcodeErr) return EM_RES.RETRY;
            //if (barcode_err) return EM_RES.ABORT;
            return EM_RES.OK;
        }
        private EM_RES AreaCal(string[] data, out double left, out double right, out double up, out double down)
        {
            left = double.MaxValue;
            right = double.MaxValue;
            up = double.MaxValue; ;
            down = double.MaxValue;
            if (data.Length <= 2)
            {
                MT.ST_WARN warn = new MT.ST_WARN();
                warning fr_warn = new warning();
                warn.ok_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                warn.ws = null;
                warn.title = VAR.IsChinese ? "提示:数据异常" : "Tip: Data File  Err";
                warn.msg = "当前放偏检测没有防压伤功能!";
                warn.lb_msg = "提示:请确认视觉文件和模板，请确认!\r\n  1.如果忽视,请按继续运行键!\r\n  " +
                                              "2.请停止运行，更新最新视觉文件和模板后，再按运行键!";
                DialogResult logres = MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.MaterialPosErr);
                if (DialogResult.Cancel == logres)
                {
                    return EM_RES.ERR;
                }
                else

                    left = 0;
                right = 0;
                up = 0;
                down = 0;
            }
            else
            {
                left = Convert.ToDouble(data[0]);
                right = Convert.ToDouble(data[1]);
                up = Convert.ToDouble(data[2]);
                down = Convert.ToDouble(data[3]);
            }
            return EM_RES.OK;
        }
        //相机参数设置
        public void Camcfgset(bool istray, bool bJig = false)
        {
            Cam curCam;
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                    curCam = COM.CamUp1;
                else
                    curCam = COM.CamUp2;
                if (curCam == null) return;
                try
                {
                    Cam.ST_CAP_CFG cap_cfg = curCam.curCapCfg;
                    double exposure = istray ? PT_SET.TrayExposure[i] : PT_SET.WsExposure[i];
                    if (bJig) exposure = PT_SET.JigExposure[i];
                    if (curCam != null && exposure > 0 && exposure < 1000)
                    {
                        cap_cfg.Exposure = exposure;
                    }
                    else MessageBox.Show(VAR.IsChinese ? "曝光参数数据异常!\r\n请检查数据格式及大小范围!" : "The exposure parameter data is abnormal! \r\nPlease check the data format and size range!");
                    double Brightness = istray ? PT_SET.TrayBrightness[i] : PT_SET.WsBrightness[i];
                    if (bJig) Brightness = PT_SET.JigBrightness[i];
                    if (curCam != null && Brightness >= 0 && Brightness < 1)
                    {
                        cap_cfg.Brightness = Brightness;
                    }
                    else MessageBox.Show(VAR.IsChinese ? "亮度参数数据异常!\r\n请检查数据格式及大小范围!" : "The brightness parameter data is abnormal! \r\nPlease check the data format and size range!");

                    double Contrast = istray ? PT_SET.TrayContrast[i] : PT_SET.WsContrast[i];
                    if (bJig) Contrast = PT_SET.JigContrast[i];
                    if (curCam != null && Contrast >= 0 && Contrast < 1)
                    {
                        cap_cfg.Contrast = Contrast;
                    }
                    else MessageBox.Show(VAR.IsChinese ? "对比参数数据异常!\r\n请检查数据格式及大小范围!" : "The comparison parameter data is abnormal! \r\nPlease check the data format and size range!");
                    //save
                    if (curCam.isLive) curCam.liveCapCfg = cap_cfg;
                    else curCam.curCapCfg = cap_cfg;
                    curCam.SaveCfg();
                    //MessageBox.Show(VAR.IsChinese ? "参数保存完成！" : "Parameters are saved!\r\n参数保存完成！");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(VAR.IsChinese ? "参数数据异常!\r\n请检查数据格式及大小范围!" : "Parameter data is abnormal!\r\nPlease check the data format and size range!\r\n参数数据异常!\r\n请检查数据格式及大小范围!", ex.Message);
                }
            }

        }
        #endregion

        #region 安全防护函数
        /// <summary>
        /// 安全防护
        /// </summary>
        /// <param name="targetpos"> 上下料模块2的X方向目标位置</param>
        /// <returns></returns>
        public EM_RES SafeProtect(ref bool bquit)
        {
            int n = 0;
            bool refresh = true;
            Stopwatch sw = new Stopwatch();
            Thread.Sleep(80);
            if(ud1needwait&& ud2needwait&&( FindMod != EM_FIN_MOD.ALL|| FindMod != EM_FIN_MOD.NONE))
            {
                EM_RES res = EM_RES.OK;                
                if(COM.UDLoad1.ax_x.fenc_pos>50)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}两模块互相等待，模块1退让40-----", disc));
                    res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, COM.UDLoad1.ax_x.fenc_pos - 40);
                    if (res != EM_RES.OK) return res;
                }
                else
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}两模块互相等待，模块1X轴归位-----", disc));
                    res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, 0);
                    if (res != EM_RES.OK) return res;
                }                
                //ud2needwait = false;
                ud2needwait = false;
            }
            sw.Start();       
            while (((id == 0 &&ud1needwait&& FindMod == EM_FIN_MOD.HIGH) ||(id == 1&& ud2needwait && FindMod == EM_FIN_MOD.LOW)) && !bquit)
            {
                if (refresh)
                {                
                    refresh = false;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}等待另一上下料模块中-----", disc));
                }
                if (n++ > 600) break;
                if (ud1needwait && ud2needwait && (FindMod != EM_FIN_MOD.ALL || FindMod != EM_FIN_MOD.NONE))
                {
                    EM_RES res = EM_RES.OK;
                    if (COM.UDLoad1.ax_x.fenc_pos > 50)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}两模块互相等待，模块1退让40-----", disc));
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, COM.UDLoad1.ax_x.fenc_pos - 40);
                        if (res != EM_RES.OK) return res;
                    }
                    else
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}两模块互相等待，模块1X轴归位-----", disc));
                        if (PT_SET.blongXT)
                        {
                            res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_y, list_xt[1].st_cap_pos.y - 100);
                            if (res != EM_RES.OK) return res;
                            res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, 0);
                            if (res != EM_RES.OK) return res;
                        }
                        else
                        {
                            res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, 0, ref COM.UDLoad1.ax_y, list_xt[1].st_cap_pos.y - 100);
                            if (res != EM_RES.OK) return res;
                        }
                    }
                    ud2needwait = false;
                }
                Thread.Sleep(10);
                Application.DoEvents();
            }
            sw.Stop();
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("ud1needwait状态为{0}，ud2needwait状态为{1},FindMod为{2},id为{3}", ud1needwait,ud2needwait, FindMod,id));
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}等待另一模块，耗时{1:F}s-----", disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds/1000,3))));
            if(bquit)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}系统检测到退出，取消等待", disc));
                return EM_RES.QUIT;
            }
            if (n > 600)
            {
                EM_RES res = EM_RES.OK;
                if (((id == 0 && ud1needwait && FindMod == EM_FIN_MOD.HIGH)) && !bquit)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}等待超时，模块1 X轴归位-----", disc));                  
                    if (PT_SET.blongXT)
                    {
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_y, list_xt[1].st_cap_pos.y - 100);
                        if (res != EM_RES.OK) return res;
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, 0);
                        if (res != EM_RES.OK) return res;
                    }
                    else
                    {
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, 0, ref COM.UDLoad1.ax_y, list_xt[1].st_cap_pos.y - 100);
                        if (res != EM_RES.OK) return res;
                    }
                    
                   
                }
                if (((id == 1 && ud2needwait && FindMod == EM_FIN_MOD.LOW)) && !bquit)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}等待超时，模块2 X轴归位-----", disc));
                    if (PT_SET.blongXT)
                    {
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad2.ax_y, list_xt[1].st_cap_pos.y - 100);
                        if (res != EM_RES.OK) return res;
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad2.ax_x, 0);
                        if (res != EM_RES.OK) return res;
                    }
                    else
                    {
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad2.ax_x, 0, ref COM.UDLoad2.ax_y, list_xt[1].st_cap_pos.y - 100);
                        if (res != EM_RES.OK) return res;
                    }
                   
                }
                return EM_RES.GIVEWAY;

                //VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}等待另一上下料模块完成动作超时", disc));
                //VAR.sys_inf.Set(EM_ALM_STA.WAR_YELLOW_FLASH, VAR.IsChinese ? "等待超时!" : "Draw ERR", 30, true);
                //MT.ST_WARN st_warn = new MT.ST_WARN();
                //warning fr_warn = new warning();
                //st_warn.ok_txt = VAR.IsChinese ? "确定" : "ok";
                //st_warn.abort_txt = VAR.IsChinese ? "确定" : "abort";
                //st_warn.ws = null;
                //st_warn.cancle_txt = VAR.IsChinese ? "取消" : "cancle";
                //st_warn.title = VAR.IsChinese ? "提示:等待另一上下料模块完成动作超时!" : "Tip: Abnormal suction!";
                //st_warn.msg = string.Format("{0}等待另一上下料模块完成动作超时,请确认后按下启动键!", disc);
                //st_warn.lb_msg = "提示:" + st_warn.msg;
                //DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);

                //if (DialogResult.Cancel == logres) return EM_RES.ERR;
                ////VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                //if (DialogResult.OK == logres)
                //{
                //    return EM_RES.QUIT;
                //}
                //else if (DialogResult.Abort == logres)
                //{
                //    return EM_RES.QUIT;
                //}
                //else if (DialogResult.Retry == logres)
                //{
                //    return EM_RES.QUIT;
                //}
            }

            return EM_RES.OK;
        }
        public EM_RES SafeProtectws23(ref bool bquit)
        {
            int n = 0;
            bool refresh = true;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (((id == 0 && ud1ws3needwait && FindMod == EM_FIN_MOD.HIGH) || (id == 1 && ud2ws2needwait && FindMod == EM_FIN_MOD.LOW)) && !bquit)
            {
                if (refresh)
                {
                    refresh = false;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}等待另一上下料模块中(工站23）-----", disc));
                }
                if (n++ > 600) break;
                if (ud1ws3needwait && ud2ws2needwait && (FindMod != EM_FIN_MOD.ALL || FindMod != EM_FIN_MOD.NONE))
                {
                    EM_RES res = EM_RES.OK;
                    if (COM.UDLoad1.ax_x.fenc_pos > 50)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}两模块互相等待，模块1退让40-----", disc));
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, COM.UDLoad1.ax_x.fenc_pos - 40);
                        if (res != EM_RES.OK) return res;
                    }
                    else
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}两模块互相等待，模块1X轴归位-----", disc));
                        if (PT_SET.blongXT)
                        {
                            res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_y, list_xt[1].st_cap_pos.y - 100);
                            if (res != EM_RES.OK) return res;
                            res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, 0);
                            if (res != EM_RES.OK) return res;
                        }
                        else
                        {
                            res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, 0, ref COM.UDLoad1.ax_y, list_xt[1].st_cap_pos.y - 100);
                            if (res != EM_RES.OK) return res;
                        }
                    }
                    ud2ws2needwait = false;
                }
                Thread.Sleep(10);
                Application.DoEvents();
            }
            sw.Stop();
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}等待另一模块(工站23），耗时{1:F}s-----", disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds / 1000, 3))));
            if (bquit)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}系统检测到退出，取消等待(工站23）", disc));
                return EM_RES.QUIT;
            }
            if (n > 600)
            {
                EM_RES res = EM_RES.OK;
                if (((id == 0 && ud1ws3needwait && FindMod == EM_FIN_MOD.HIGH)) && !bquit)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}等待超时，模块1 X轴归位-----", disc));
                    if (PT_SET.blongXT)
                    {
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_y, list_xt[1].st_cap_pos.y - 100);
                        if (res != EM_RES.OK) return res;
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, 0);
                        if (res != EM_RES.OK) return res;
                    }
                    else
                    {
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, 0, ref COM.UDLoad1.ax_y, list_xt[1].st_cap_pos.y - 100);
                        if (res != EM_RES.OK) return res;
                    }
                }
                if (((id == 1 && ud2ws2needwait && FindMod == EM_FIN_MOD.LOW)) && !bquit)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}等待超时，模块2 X轴归位-----", disc));
                    if (PT_SET.blongXT)
                    {
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad2.ax_y, list_xt[1].st_cap_pos.y - 100);
                        if (res != EM_RES.OK) return res;
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad2.ax_x, 0);
                        if (res != EM_RES.OK) return res;
                    }
                    else
                    {
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad2.ax_x, 0, ref COM.UDLoad2.ax_y, list_xt[1].st_cap_pos.y - 100);
                        if (res != EM_RES.OK) return res;
                    }
                }
                return EM_RES.GIVEWAY;
                //VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}等待另一上下料模块完成动作超时(工站23）", disc));
                //VAR.sys_inf.Set(EM_ALM_STA.WAR_YELLOW_FLASH, VAR.IsChinese ? "等待超时!" : "Draw ERR", 30, true);
                //MT.ST_WARN st_warn = new MT.ST_WARN();
                //warning fr_warn = new warning();
                //st_warn.ok_txt = VAR.IsChinese ? "确定" : "ok";
                //st_warn.abort_txt = VAR.IsChinese ? "确定" : "abort";
                //st_warn.ws = null;
                //st_warn.cancle_txt = VAR.IsChinese ? "取消" : "cancle";
                //st_warn.title = VAR.IsChinese ? "提示:等待另一上下料模块完成动作超时(工站23）!" : "Tip: Abnormal suction!";
                //st_warn.msg = string.Format("{0}等待另一上下料模块完成动作超时,请确认后按下启动键!", disc);
                //st_warn.lb_msg = "提示:" + st_warn.msg;
                //DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);

                //if (DialogResult.Cancel == logres) return EM_RES.ERR;
                ////VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                //if (DialogResult.OK == logres)
                //{
                //    return EM_RES.QUIT;
                //}
                //else if (DialogResult.Abort == logres)
                //{
                //    return EM_RES.QUIT;
                //}
                //else if (DialogResult.Retry == logres)
                //{
                //    return EM_RES.QUIT;
                //}
            }
            return EM_RES.OK;
        }
        public EM_RES SafeProtectMT(ref bool bquit,int id, double targe_pos = double.MaxValue)
        {
            int n = 0;
            bool refresh = true;
            UpDownLoad Other_ud;
            if (id == 0) Other_ud = COM.UDLoad2;
            else Other_ud = COM.UDLoad1;
            Stopwatch sw = new Stopwatch();
            if (ud1needwait && ud2needwait && (FindMod != EM_FIN_MOD.ALL || FindMod != EM_FIN_MOD.NONE))
            {
                EM_RES res = EM_RES.OK;

                if (COM.UDLoad1.ax_x.fenc_pos > 50)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}两模块互相等待，模块1退让40-----", disc));
                    res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, COM.UDLoad1.ax_x.fenc_pos - 40);
                    if (res != EM_RES.OK) return res;
                }
                else
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}两模块互相等待，模块1X轴归位-----", disc));
                    res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, 0);
                    if (res != EM_RES.OK) return res;
                }
                ud2needwait = false;
            }
            sw.Start();
            while (((id == 0 && ud1needwait && FindMod == EM_FIN_MOD.HIGH) || (id == 1 && ud2needwait && FindMod == EM_FIN_MOD.LOW)) && !bquit)
            {
                if (refresh)
                {
                    refresh = false;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}等待另一上下料模块中-----", disc));
                }
                if (Math.Abs(targe_pos) + Math.Abs(Other_ud.ax_x.fenc_pos) + 89 > UpDownLoad.DisAxisX && COM.UDLoad1.ax_x.home_status == AXIS.HOME_STA.OK && COM.UDLoad2.ax_x.home_status == AXIS.HOME_STA.OK)
                {
                }
                else
                {
                    break;
                }
                if (ud1needwait && ud2needwait && (FindMod != EM_FIN_MOD.ALL || FindMod != EM_FIN_MOD.NONE))
                {
                    EM_RES res = EM_RES.OK;

                    if (COM.UDLoad1.ax_x.fenc_pos > 50)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}两模块互相等待，模块1退让40-----", disc));
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, COM.UDLoad1.ax_x.fenc_pos - 40);
                        if (res != EM_RES.OK) return res;
                    }
                    else
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}两模块互相等待，模块1X轴归位-----", disc));
                        res = MT.ZupMove(ref bquit, ref COM.UDLoad1.ax_x, 0);
                        if (res != EM_RES.OK) return res;
                    }
                    ud2needwait = false;
                }
                if (n++ > 2000) break;
                Thread.Sleep(10);
                Application.DoEvents();
            }
            sw.Stop();
       
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}等待另一模块，耗时{1:F}s-----", disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds / 1000, 3))));
            if (bquit)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}系统检测到退出，取消等待", disc));
                return EM_RES.QUIT;
            }
            if (n > 2000)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}等待另一上下料模块完成动作超时", disc));
                VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "等待超时!" : "Draw ERR", 30, true);
                MT.ST_WARN st_warn = new MT.ST_WARN();
                warning fr_warn = new warning();
                st_warn.ok_txt = VAR.IsChinese ? "确定" : "ok";
                st_warn.abort_txt = VAR.IsChinese ? "确定" : "abort";
                st_warn.ws = null;
                st_warn.cancle_txt = VAR.IsChinese ? "取消" : "cancle";
                st_warn.title = VAR.IsChinese ? "提示:等待另一上下料模块完成动作超时!" : "Tip: Abnormal suction!";
                st_warn.msg = string.Format("{0}等待另一上下料模块完成动作超时,请确认后按下启动键!", disc);
                st_warn.lb_msg = "提示:" + st_warn.msg;
                DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);

                if (DialogResult.Cancel == logres) return EM_RES.ERR;
                //VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                if (DialogResult.OK == logres)
                {
                    return EM_RES.QUIT;
                }
                else if (DialogResult.Abort == logres)
                {
                    return EM_RES.QUIT;
                }
                else if (DialogResult.Retry == logres)
                {
                    return EM_RES.QUIT;
                }
            }

            return EM_RES.OK;
        }
        #endregion

        #region 放模组于料盘
        //预判下一个位置
        public EM_RES MoveNextTrayX1Pos(ref bool bquit, TrayBox traybox, WSFEED wsfeed,UpDownLoad ud)
        {
            EM_RES res = EM_RES.OK;
            try
            {
                traybox.runin = true;
                double pos_x1=-10000;
                if(traybox.tray_cur!=null)
                FindNextTrayPosX1(ref pos_x1, traybox, wsfeed, ud);
                traybox.runUpdate = true;
                if (traybox_ng.runUpdate && traybox_ok.runUpdate)
                {
                    traybox_ng.runUpdate = false;
                    traybox_ok.runUpdate = false;
                }
                if(traybox_fd.runUpdate)
                {
                    traybox.runUpdate = false;
                }

                if(pos_x1>-10 && (traybox.disc !=traybox_ng.disc) && (!traybox.MoveToLastPos[(id+1)%2]))
                {
                    if (traybox.ax_x.isStop && traybox.tray_cur!=null&&traybox.IsReady)
                    {
                        res = MT.Move(ref bquit, ref traybox.ax_x, pos_x1>2? pos_x1 : 2);
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}提前定位", traybox.ax_x.disc));
                    }
                    
                }
                return res;
            }
            finally
            {
                traybox.MoveToLastPos[(id + 1) % 2] = false;
                traybox.runin = false;
            }                             
        }

        public EM_RES FlyToTrayMd(ref bool bquit, ref ST_XY pos_flystop, bool Ofs_On = true)
        {
            try
            {
                EM_RES res;
                double fly_safepos = list_xt[0].st_cap_pos.y + 160;
                List<ST_XYN> TriPos = new List<ST_XYN>();

                foreach (XT xt in list_xt)
                {
                    TriPos.Add(new ST_XYN(xt.st_cap_pos.x, xt.st_cap_pos.y, list_xt.Count));
                }
                pos_flystop.x = list_xt[0].st_cap_pos.x;
                pos_flystop.y = list_xt[0].st_cap_pos.y - 100;
                //配置下拍照
               // FlyCfg(EM_FLY_CFG.MOD_DW, TriPos, 2, true, Ofs_On);
                FlyCfg2(EM_FLY_CFG.MOD_DW, new List<ST_XYN>(), list_xt.Count, true, Ofs_On);
                //定位飞拍
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}Y轴飞拍前其它轴定位!", disc));

                if (ax_y.fenc_pos > fly_safepos)
                {
                    AXIS axisy = null;
                    if (ax_y.fenc_pos > fly_safepos) axisy = ax_y;
                    res = MT.ZupMove(ref bquit, ref axisy, fly_safepos, ref ax_u1, 0, ref ax_u2, 0);
                    if (res != EM_RES.OK) return res;
                }
                if (Math.Abs(ax_u2.fcmd_pos) > 8 || Math.Abs(ax_u1.fcmd_pos) > 8)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}定位到飞拍放料前转正角度U1:{1} U2:{2}", disc, ax_u1.fcmd_pos, ax_u2.fcmd_pos));
                    res = MT.ZupMove(ref bquit, ref ax_u1, 0, ref ax_u2, 0);
                    if (res != EM_RES.OK) return res;
                }
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}定位到料盘飞拍放料起飞", disc));
                //这里只跑Y轴，不跑X轴
                res = MT.ZupMove(ref bquit, ref ax_y, pos_flystop.y, ref ax_u1, 0, ref ax_u2, 0);
                if (res != EM_RES.OK) return res;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}定位到飞拍放料结束位置Y:{1:f3}", disc, pos_flystop.y));
                return EM_RES.OK;
            }
            finally
            {
                ax_y.DisableHcmp(dwcam.TriIO.num);
            }

        }
        public  EM_RES TrayPlaceMd(ref bool bquit, TrayBox traybox,WSFEED wsfeed)
        {
            if (!traybox.IsReady) return EM_RES.ABORT;
            if (traybox.tray_cur == null) return EM_RES.PARA_OUTOFRANG;
            if (wsfeed.id==0&&id==0)
            {
                placepos = traybox.tray_cur.GetPosList(Product.EM_CM_RES.EMPTY);
            }
            else
            {
                placepos = traybox.tray_cur.GetPosListOrder(Product.EM_CM_RES.EMPTY);
            }
            
            //if (placepos.Count <= 86)//86
            //{
            //    protectforoktray = true;
            //    protectforngtray = true;
            //}
            if (placepos.Count == 0) return EM_RES.END;
            if (bquit) return EM_RES.QUIT;
            try
            {               
                foreach (XT xt in list_xt)
                {
                    if ((!xt.cy_zk.isONByChkSen && !Demo) || xt.XtMd == null)
                    {
                        xt.cy_zk.SetOff();
                        xt.XtMd = null;
                        continue;
                    }
                    if (traybox.disc == traybox_ok.disc && xt.XtMd.res != 0) continue;//OK料盘且测试结果为OK的才能放
                    if (traybox.disc == traybox_ng.disc && xt.XtMd.res < 1) continue;//NG料盘且测试结果为NG的才能放
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}料盘放料开始，模组结果{1}", disc, xt.XtMd.res));
                    EM_RES res = xt.PlaceMd(ref bquit, id,traybox, wsfeed, Demo);
                    if (res != EM_RES.OK) return res;
                    if (placepos.Count == 1) return EM_RES.END;

                }
            }
            finally
            {
                
            }
            return EM_RES.OK;
        }
        /// <summary>
        /// 放模组
        /// </summary>
        /// <param name="bquit"></param>
        /// <returns></returns>
        public EM_RES PlaceMdToTray(ref bool bquit, WSFEED ws)
        {
            EM_RES res;
            int cnt = 0;
            //放料前旋转 - 90°，不管是否有料；
            //if (PT_SET.isopen_degree)
            //{
            //    res = MT.ZupMove(ref bquit, ref ax_u1, -PT_SET.degree, ref ax_u2, -PT_SET.degree);
            //    if (res != EM_RES.OK) return res;
            //}
            while (!bquit)
            {
                //仓储进出料
                res = TrayPlaceMd(ref bquit, traybox_ng, ws);
                if (res == EM_RES.PARA_OUTOFRANG || res == EM_RES.END)
                {
                    if (traybox_ng.IsReady)
                    {
                        traybox_ng.IsReady = false;
                        Task tskng = new Task(() =>
                        {
                            traybox_ng.Tray_Action(Demo);
                        });
                        tskng.Start();
                    }

                }
                else if (res != EM_RES.OK && res != EM_RES.ABORT)
                {
                    return res;
                }


                res = TrayPlaceMd(ref bquit, traybox_ok, ws);
                if (res == EM_RES.PARA_OUTOFRANG || res == EM_RES.END)
                {
                    if (traybox_ok.IsReady)
                    {
                        traybox_ok.IsReady = false;
                        Task tskok = new Task(() =>
                        {
                            traybox_ok.Tray_Action(Demo);
                        });
                        tskok.Start();
                    }
                }
                else if (res != EM_RES.OK && res != EM_RES.ABORT)
                {
                    return res;
                }

                //确认物料完成
                cnt = 0;
                foreach (XT xt in list_xt)
                {
                    if (!xt.cy_zk.isONByChkSen && xt.XtMd == null) cnt++;
                }

                if (cnt == list_xt.Count) break;
                Thread.Sleep(50);
            }
            return EM_RES.OK;
        }
        #endregion

        #region 取工站模组函数
        public bool HaveWsPickMd(List<WSFEED.MdDat> WsFeedPickPos)
        {
            if (WsFeedPickPos == null || WsFeedPickPos.Count==0) return false;
            foreach (WSFEED.MdDat md in WsFeedPickPos)
            {               
                if (md.benable && md.res > -1)
                    return true;
            }
            return false;
        }
        public EM_RES PickWsMod(ref bool bquit, ref WSFEED wsfeed)
        {
            EM_RES res = EM_RES.OK;
            int xtid = 0;
            Product.MdDat pXtMd = new Product.MdDat();
            ST_XYZA pickpos = new ST_XYZA();
            List<WSFEED.MdDat> WsPickPos = new List<WSFEED.MdDat>();
            ST_XY xtvs = new ST_XY();
            bool tryagain = false;
            bool updatebarcode = false;
            int PickNum = 0;
            string[] Errmsg = new string[3];
            bool bAlm = false;
            // if (WsPickPos.Count > 0) WsPickPos.Clear();
            GetWsPosTeam(ref WsPickPos, wsfeed, Product.EM_CM_RES.OK, FindMod);
            foreach (var ws in WsPickPos)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, $"{FindMod}模式下{wsfeed.id}当前放料{ws.Num}");
            }
            while (!bquit)
            {
                bool HaveMd = HaveWsPickMd(WsPickPos);
                if (!HaveMd)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? "HaveWsPickMd没有找到测试完成的模组!" : "HaveWsPickMd,no mod was found to complete the test!         (HaveWsPickMd没有找到测试完成的模组!)");
                    return EM_RES.END;
                }
                if (tryagain)
                {
                    tryagain = false;
                    if (PickNum < (WsPickPos[0].Num % 4) % 2)
                        return EM_RES.OK;
                }

                if (updatebarcode == false && PT_SET.bOkCheck)
                {
                    updatebarcode = true;
                    foreach (WSFEED.MdDat md in WsPickPos)
                    {
                        if (md.res != 0) continue;
                        RECAP:
                        if (PT_SET.blongXT)
                        {
                            res = MT.ZupMove(ref bquit, ref ax_x, md.st_place_pos[id].x);
                            if (res != EM_RES.OK) return res;

                            res = MT.ZupMove(ref bquit, ref ax_y, md.st_place_pos[id].y);
                            if (res != EM_RES.OK) return res;
                        }
                        else
                        {
                            res = MT.ZupMove(ref bquit, ref ax_x, md.st_place_pos[id].x, ref ax_y, md.st_place_pos[id].y);
                            //ax_y.SetToWorkSpd();
                            if (res != EM_RES.OK) return res;
                        }
                           
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "Pick_Move_End_Cap->md:" + md.Num.ToString());
                        res = upcam.FindTaskTriAndWait(CONST.WsModUpFw, Demo);
                        bAlm = false;
                        if (!upcam.curTask.ResData.bUpdate || !upcam.curTask.ResData.bOK || Math.Abs(upcam.curTask.ResData.PosMM.x) > PT_SET.Vs_XYofs
                             || Math.Abs(upcam.curTask.ResData.PosMM.y) > PT_SET.Vs_XYofs || Math.Abs(upcam.curTask.ResData.PosMM.a) > PT_SET.Vs_Rofs)
                        {
                            Errmsg[0] = VAR.IsChinese ? "模组被带偏" : "Open deflected";
                            Errmsg[1] = VAR.IsChinese ? string.Format("{0}_工位{1}上模组被带偏.", wsfeed.disc, md.Num) : string.Format("{0}_ws{1}product was deflected by fixture.", wsfeed.disc, md.Num);
                            bAlm = true;
                        }
                        else if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.UP_SCAN && (res == EM_RES.OK && PT_SET.bBarcodeCamBackEn && (upcam.curTask.ResData.BarCode == null || upcam.curTask.ResData.BarCode.Length < 1)) && !Demo)
                        {
                            Errmsg[0] = VAR.IsChinese ? "没检到二维码!" : "No Barcode";
                            Errmsg[1] = VAR.IsChinese
                                ? string.Format("{0}_工位{1}上模组二维码无法识别.", wsfeed.disc, md.Num)
                                : string.Format("{0}_ws{1}can't recognize barcode.", wsfeed.disc, md.Num);
                            bAlm = true;
                        }
                        if (bAlm)
                        {
                            //COUNT_DATA.ErrTemp = System.DateTime.Now;
                            List<int> NgSameResNum = new List<int>();
                            bAlm = false;
                            VAR.sys_inf.Set(EM_ALM_STA.WAR_YELLOW_FLASH, Errmsg[0], 20, true);
                            MT.ST_WARN st_warn1 = new MT.ST_WARN();
                            warning fr_warn1 = new warning();
                            st_warn1.ok_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                            st_warn1.abort_txt = VAR.IsChinese ? "重新拍照" : "Take a photo";
                            st_warn1.ws = null;
                            NgSameResNum.Add(md.Num);
                            st_warn1.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                            st_warn1.msg = Errmsg[1];
                            st_warn1.title = VAR.IsChinese ? "提示:" + Errmsg[0] : "Tip:" + Errmsg[0];
                            st_warn1.lb_msg = VAR.IsChinese ? "提示:" + st_warn1.msg + "请确认!\r\n  1.按'继续运行'键则放弃当前检查继续运行!\r\n  " +
                                             "2.如需确认问题请按'停止运行'键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按'重新拍照'键则重新获取当前位置图像!" : "Tip:" + st_warn1.msg + "\r\nPlease confirm!\r\n  1.Press the 'Keep running' button to abandon the current check and continue running!\r\n  " +
                                                                                                                    "2.If you need to confirm the problem, press the 'Stop running' button to exit the operation. After the ready is displayed in the upper left corner of the interface, press the Run button.!\r\n 3.If you press the 'Take a photo' button, you will get the current position image again!" + "\r\n提示:" + st_warn1.msg + "请确认!\r\n  1.按'继续运行'键则放弃当前检查继续运行!\r\n  " +
                                                                                                                    "2.如需确认问题请按'停止运行'键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按'重新拍照'键则重新获取当前位置图像!";
                            DialogResult logres1 = MT.Display_frwarn(fr_warn1, st_warn1, ERR_ALM.EmErrItem.UpDownLoadAbnormal);
                            SQLData.TesterrDataAdd(wsfeed.id, md.Num, "工站取料", string.Format("{0}模组被带偏", wsfeed.disc));
                            COUNT_DATA.Errcnt++;
                            if (DialogResult.Cancel == logres1) return EM_RES.ERR;
                            VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                            if (DialogResult.Abort == logres1)
                            {
                                MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                goto RECAP; 
                            }
                        }
                        if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.UP_SCAN && PT_SET.bBarcodeCamBackEn && !Demo &&
                            (string.IsNullOrWhiteSpace(md.bardcode) || string.IsNullOrWhiteSpace(upcam.curTask.ResData.BarCode) || upcam.curTask.ResData.BarCode != md.bardcode))
                        {
                            md.res = 111;
                            wsfeed.breschanged = true;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("上相机取料前二维码回检失败,工站:{0},位置:{1},记录:{2},实拍:{3}", wsfeed.disc, md.Num, md.bardcode ?? "<空>", upcam.curTask.ResData.BarCode ?? "<空>"));
                            return EM_RES.RETRY;
                        }

                    }

                    if (!upcam.FlushOk && !upcam.FlushUpdate)
                    {
                        Task tak = new Task(() =>
                        {
                            upcam.FlushUpdate = true;
                            upcam.mAcqFifo.Flush();
                            upcam.FlushOk = true;
                            upcam.FlushUpdate = false;
                        });
                        tak.Start();
                    }
                }

                //取料
                foreach (WSFEED.MdDat md in WsPickPos)
                {
                    bool OnlyOneXt = id == 0 ? PT_SET.bUpDn1XtOnlyOne : PT_SET.bUpDn2XtOnlyOne;
                    if ((id==0&&(md.Num==3|| md.Num == 7))|| (id == 1 && (md.Num == 2|| md.Num == 6)))
                    {
                        res = SafeProtectws23(ref bquit);
                        if (res != EM_RES.OK)
                        {
                            return res;
                        }
                    }
                    if (!OnlyOneXt) xtid = md.Num / 5;
                    else if (PT_SET.xt1firsten) xtid = 0;
                    else xtid = 1;
                    xtvs = list_xt[xtid].st_vs_pos_xtshp.ToXY();
                    pickpos = md.st_pickpos[id].ToXYZA() + list_xt[xtid].st_rol_cap.ToXYZA() - list_xt[0].st_rol_cap.ToXYZA() + xtvs.ToXYZA();
                    pickpos.z = xtid == 0 ? pickpos.z : -pickpos.z;
                    pickpos.a = 0;
                    PICK:
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("{0}工站取料: X:{1} Y:{2}", disc, pickpos.x, pickpos.y) : string.Format("{0}WS pickmod: X:{2} Y:{3}            ({1}工站取料: X:{2} Y:{3})", disc, disc, pickpos.x, pickpos.y));
                    for (int i = 0; i < 2; i++)
                    {
                        if (FindMod == EM_FIN_MOD.HIGH && id == 0 && (md.Num == 3 || md.Num == 7))
                        {
                            UpDownLoad.ud2needwait = true;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}中的3、7工位进行工站取料，上下料2等待开启-----", disc, wsfeed.disc));
                        }
                        if (FindMod == EM_FIN_MOD.HIGH && id == 0 && (md.Num ==4 || md.Num == 8))
                        {
                            UpDownLoad.ud2ws2needwait = true;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}中的4、8工位进行工站取料，上下料2对工位2等待开启-----", disc, wsfeed.disc));
                        }
                        if (FindMod == EM_FIN_MOD.LOW && id == 1 && (md.Num == 2 || md.Num == 6))
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}中的2、6工位进行工站取料，上下料1等待开启-----", disc, wsfeed.disc));
                            UpDownLoad.ud1needwait = true;
                        }
                        if (FindMod == EM_FIN_MOD.LOW && id == 1 && (md.Num == 1 || md.Num == 5))
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}中的1、5工位进行工站取料，上下料1对工位3等待开启-----", disc, wsfeed.disc));
                            UpDownLoad.ud1ws3needwait = true;
                        }
                        res = list_xt[xtid].Pick(ref bquit, pickpos, true, Demo, true);
                        if (res != EM_RES.PICK_ERR || !PT_SET.bWsPickAgain) break;
                        else
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("{0}工站重取物料: X:{1} Y:{2}", disc, pickpos.x, pickpos.y) : string.Format("{0} WS retry to pick mod: X:{2} Y:{3}           ({1}工站重取物料: X:{2} Y:{3})", disc, disc, pickpos.x, pickpos.y));
                    }
                    if (res == EM_RES.OK)
                    {
                        pXtMd.res = md.res;
                        pXtMd.bardcode = md.bardcode;
                        pXtMd.Num = md.Num;
                        pXtMd.WS_ID = md.WS_ID;
                        pXtMd.PC_ID = md.PC_ID;
                        list_xt[xtid].XtMd = pXtMd.Clone();
                        md.res = -2;
                        md.Ishavemd = false;
                        md.bardcode = null;
                    }
                    else if (res == EM_RES.PICK_ERR)
                    {
                        COUNT_DATA.ErrTemp = System.DateTime.Now;
                        VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "吸料异常!" : "Draw ERR", 20, true);
                        MT.ST_WARN st_warn = new MT.ST_WARN();
                        warning fr_warn = new warning();
                        st_warn.ok_txt = VAR.IsChinese ? "放弃吸取" : "Give up";
                        st_warn.abort_txt = VAR.IsChinese ? "重新吸取" : "Try again";
                        st_warn.ws = wsfeed;
                        st_warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                        st_warn.title = VAR.IsChinese ? "提示:吸料异常!" : "Tip: Abnormal suction!";
                        st_warn.msg = VAR.IsChinese ? string.Format("{0}下料失败,{1}无法吸起当前工站第{2}模组!", disc, list_xt[xtid].disc, md.Num) : string.Format("{0} download failed,{2} can't suck up module {3} of the current station \r\n{1}下料失败,{2}无法吸起当前工站第{3}模组!", disc, disc, list_xt[xtid].disc, md.Num);
                        st_warn.lb_msg = VAR.IsChinese ? "提示:" + st_warn.msg + "请确认!\r\n  1.如果工站上没有物料,请按放弃吸取键继续吸下一个物料!\r\n  " +
                                         "2.如有物料请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按重新吸取键则吸嘴再次吸取!" : "Tip:" + st_warn.msg + "\r\nPlease confirm!\r\n  1.If there is no material on the station, please press the 'Give up' button to continue sucking the next material!\r\n  " +
                                                                                                      "2.If there is any material, press the 'Stop running' button to exit the operation, and then press the run button after the ready display on the upper left corner of the interface!\r\n 3.If the 'Try again' button is pressed, the suction nozzle sucks again!" + "\r\n提示:" + st_warn.msg + "请确认!\r\n  1.如果工站上没有物料,请按放弃吸取键继续吸下一个物料!\r\n  " +
                                                                                                      "2.如有物料请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按重新吸取键则吸嘴再次吸取!";
                        SQLData.TesterrDataAdd(wsfeed.id, md.Num, "工站取料", string.Format("{0}取料失败", wsfeed.disc));
                        COUNT_DATA.Errcnt++;
                        DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);
                        ////增加门禁检测
                        //while (PT_SET.bEnUpDownDr)
                        //{
                        //   res = MT.ChkAllSafeSen(0x01);
                        //    if (res != EM_RES.OK)
                        //    {
                        //        MT.DoorAlmTip();
                        //    }
                        //    else break;
                        //    Thread.Sleep(100);
                        //}

                        if (DialogResult.Cancel == logres) return EM_RES.ERR;
                        VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                        if (DialogResult.OK == logres)
                        {
                            md.res = -2;
                            md.Ishavemd = false;
                            md.bardcode = null;
                            MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                        }
                        else if (DialogResult.Abort == logres)
                        {
                            MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                            goto PICK;
                           
                        }
                        else if (DialogResult.Retry == logres)
                        {
                            tryagain = true;
                            PickNum = (WsPickPos[0].Num % 4) % 2;
                            MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                            break;
                        }

                    }
                    else
                    {
                        if (res != EM_RES.ABORT) return res;
                    }
                    wsfeed.breschanged = true;
                }
                if (tryagain)
                {
                    if (WsPickPos.Count > 0) WsPickPos.Clear();
                    GetWsPosTeam(ref WsPickPos, wsfeed, Product.EM_CM_RES.OK, FindMod);
                    break;
                }
                else
                {
                    if (FindMod == EM_FIN_MOD.ALL || (FindMod != EM_FIN_MOD.ALL && TrayGoPos == 0))
                    {
                        double temppos = 0;
                        if (FindMod == EM_FIN_MOD.HIGH && Math.Abs(ax_x.fenc_pos) > 40.01) temppos = ax_x.fenc_pos - 40;//避让40mm，模块1靠右，模块2靠左
                        else if (FindMod == EM_FIN_MOD.LOW && Math.Abs(ax_x.fenc_pos) > 40.01) temppos = ax_x.fenc_pos + 40;
                        else temppos = ax_x.fenc_pos;
                        TrayGoPos = temppos;
                    }
                       
                    break;
                }
            }
            return EM_RES.OK;
        }
        #endregion

        #region 飞拍失败放回料盘
        public EM_RES DwCamErrPalceBack(ref bool bquit)
        {
            int xtnum;
            EM_RES res = EM_RES.OK;
            foreach (Cam.VisionTask task in dwcam.List_vs_task_cur)
            {
               // xtnum = (dwcam.List_vs_task_cur.IndexOf(task) + 1) % 2;
                xtnum = task.TriPos.n;
                if (!task.ResData.bOK && list_xt[xtnum].cy_zk.isONByChkSen && task.ResData.bUpdate)
                {
                    try
                    {

                        int n = 0;                      
                        while (traybox_fd.runin || !traybox_fd.ax_x.isStop)
                        {
                            if (n++ > 2000) break;                         
                            if (bquit) return EM_RES.QUIT;
                            Thread.Sleep(10);
                            Application.DoEvents();
                        }
                        traybox_fd.MoveToLastPos[id] = true;
                        if (n > 2000)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("等待{0}定位超时5S", traybox_fd.ax_x.disc));
                            return EM_RES.ERR;
                        }

                        res = XtMovPickPos(ref bquit, list_xt[xtnum]);
                        if (res != EM_RES.OK) break;

                        PosInf curpos = null;
                        if (traybox_fd.tray_cur != null)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("放回产品的序号为:{0}", list_xt[xtnum].XtMd.idx));
                            curpos = traybox_fd.tray_cur.list_pos.ElementAt(list_xt[xtnum].XtMd.idx);
                            if (curpos !=null)
                            {
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("飞拍失败放回，查找料盘序号:{0}", curpos.idx));
                            }                            
                        }
                        if (traybox_fd.tray_cur == null || curpos == null || (curpos.md != null && curpos.md.res != -2))
                        {
                            if (curpos.md !=null)
                            {
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("飞拍失败放回，当前位置的结果为{0}", curpos.md.res));
                            }
                           
                       ReChk:
                            MT.ST_WARN warn = new MT.ST_WARN();
                            warning fr_warn = new warning();//增加语言
                            warn.ok_txt = MultiLanguage.TxtSelct("继续", "GoOn", "Đi tiếp");
                            warn.cancle_txt = MultiLanguage.TxtSelct("停止", "Stop", "Ngừng lại");
                            warn.title = MultiLanguage.TxtSelct("提示:飞拍失败放回异常", "Tip: Fly Err Back Place Tray Error", "Mẹo: Fly Err Back Place Tray Error");
                            VAR.sys_inf.Set(EM_ALM_STA.WAR_YELLOW_FLASH, VAR.IsChinese ? "飞拍失败放回异常" : "Place Tray Error", 10, true);
                            warn.msg = MultiLanguage.TxtSelct("飞拍失败放回异常!", "Place Tray Error", "Lỗi đặt khay");
                            warn.lb_msg = MultiLanguage.TxtSelct(
                                $"{disc}当前飞拍失败放回料盘找不到位置，请手动取走吸头 {list_xt[xtnum].disc} 上物料后按继续运行。否者按停止运行则停机",

                                $"{disc} The current flyer fails to put it back on the tray and can't find the position. Please manually remove the material on the suction head" +
                                $"{list_xt[xtnum].disc} and press Continue to run. Otherwise, press stop to stop",

                                $"{disc} Tờ rơi hiện tại không thể đặt nó trở lại khay và không thể tìm thấy vị trí. Vui lòng loại bỏ thủ công vật liệu trên đầu hút " +
                                $"{list_xt[xtnum].disc} và nhấn Tiếp tục để chạy. Nếu không, hãy nhấn dừng để dừng lại");
                            DialogResult logres = MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.MaterialPosErr);
                            if (logres == DialogResult.OK)
                            {

                                if (list_xt[xtnum].cy_zk.isONByChkSen)
                                    goto ReChk;
                                else
                                {
                                    list_xt[xtnum].XtMd = null;
                                    VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "Run", 0, true);
                                    continue;
                                }
                            }
                            else return EM_RES.ERR;
                        }

                        //res = MT.ZupMove(ref bquit, ref ax_x, list_xt[xtnum].xt_pos_pick_mod.x, ref ax_y, list_xt[xtnum].xt_pos_pick_mod.y);
                        //if (res != EM_RES.OK) return res;
                        res = list_xt[xtnum].PickOrPlace(ref bquit, list_xt[xtnum].xt_pos_pick_mod, false, Demo);
                        if (res == EM_RES.OK)
                        {
                            CancelFeedbackModule(list_xt[xtnum].XtMd.Num);
                            list_xt[xtnum].XtMd = null;
                        }
                        else  if (res != EM_RES.OK) break ;
                    }
                    finally
                    {
                        traybox_fd.runin = false;
                    }
                }
            }

            return res;
        }
        #endregion

        #region 放模组于工站函数
        /// <summary>
        /// 移动到工站飞拍
        /// </summary>
        public EM_RES FlyToWs(ref bool bquit, ref ST_XY pos_flystop, List<WSFEED.MdDat> WsTriPos, bool Ofs_On = true)
        {
            try
            {
                EM_RES res;
                double fly_safepos = list_xt[1].st_cap_pos.y - 180;
                List<ST_XYN> TriPos = new List<ST_XYN>();

                //配置下拍照
                FlyCfg(EM_FLY_CFG.MOD_DW, new List<ST_XYN>(), WsTriPos.Count, true, Ofs_On);

                //记录本批次待二次回检模组；旧批次未完成时禁止继续放料。
                if (!BeginFeedbackBatch(WsTriPos)) return EM_RES.RETRY;
                foreach (WSFEED.MdDat md in WsTriPos)
                {
                    TriPos.Add(new ST_XYN(WsTriPos[WsTriPos.Count - 1].st_place_pos[id].x, md.st_place_pos[id].y, md.Num - 1));
                }
                if (WsTriPos.Count > 0)
                {
                    pos_flystop.x = WsTriPos[WsTriPos.Count - 1].st_place_pos[id].x;
                    pos_flystop.y = WsTriPos[WsTriPos.Count - 1].st_place_pos[id].y + 50;
                }
                FlyCfg(EM_FLY_CFG.WS_UP, TriPos, WsTriPos.Count, true, Ofs_On);

                //定位飞拍
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("{0}Y轴飞拍前其它轴定位!", disc) : string.Format("other axes move before {0}Y axis flyshot!          ({1}Y轴飞拍前其它轴定位!)", disc, disc));
                if (Math.Abs(pos_flystop.x - ax_x.fenc_pos) > 50 || ax_y.fenc_pos > fly_safepos)
                {
                    AXIS axisy = null;
                    if (ax_y.fenc_pos > fly_safepos) axisy = ax_y;
                    #region 取放料保护
                    Thread.Sleep(80);
                    res = SafeProtect(ref bquit);
                    if (res != EM_RES.OK)
                    {
                        return res;
                    }
                    #endregion
                    res = MT.ZupMove(ref bquit, ref ax_x, pos_flystop.x, ref axisy, fly_safepos, ref ax_u1, 0, ref ax_u2, 0);
                    if (res != EM_RES.OK) return res;
                }
                if (Math.Abs(ax_u2.fcmd_pos) > 8 || Math.Abs(ax_u1.fcmd_pos) > 8)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("{0}定位到飞拍放料前转正角度U1:{1} U2:{2}", disc, ax_u1.fcmd_pos, ax_u2.fcmd_pos) : string.Format("{0} XT U axis home before flyshot U1:{2} U2:{3}           ({1}定位到飞拍放料前转正角度U1:{2} U2:{3})", disc, disc, ax_u1.fcmd_pos, ax_u2.fcmd_pos));
                    res = MT.ZupMove(ref bquit, ref ax_u1, 0, ref ax_u2, 0);
                    if (res != EM_RES.OK) return res;
                }
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("{0}定位到飞拍放料起飞", disc) : string.Format("{0}  move to flyshot end pos         ({1}定位到飞拍放料起飞)", disc, disc));
                res = MT.ZupMove(ref bquit, ref ax_x, pos_flystop.x, ref ax_y, pos_flystop.y, ref ax_u1, 0, ref ax_u2, 0);
                if (res != EM_RES.OK) return res;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("{0}定位到飞拍放料结束位置X:{1:f3},Y:{2:f3}", disc, pos_flystop.x, pos_flystop.y) : string.Format("{0}  move to flyshot end pos X:{1:f3},Y:{2:f3}           ({1}定位到飞拍放料结束位置X:{1:f3},Y:{2:f3})", disc, pos_flystop.x, pos_flystop.y));
                return EM_RES.OK;
            }
            finally
            {
                ax_y.DisableHcmp(upcam.TriIO.num);
                ax_y.DisableHcmp(dwcam.TriIO.num);
            }

        }
        public EM_RES FlyToWsaction(ref bool bquit, ref ST_XY pos_flystop, List<WSFEED.MdDat> WsTriPos, bool Ofs_On = true)
        {
            try
            {
                EM_RES res;
                double fly_safepos = list_xt[1].st_cap_pos.y - 180;
                List<ST_XYN> TriPos = new List<ST_XYN>();

                //配置下拍照
                FlyCfgaction(EM_FLY_CFG.MOD_DW, new List<ST_XYN>(), WsTriPos.Count, true, Ofs_On);

                //记录本批次待二次回检模组；旧批次未完成时禁止继续放料。
                if (!BeginFeedbackBatch(WsTriPos)) return EM_RES.RETRY;
                foreach (WSFEED.MdDat md in WsTriPos)
                {
                    TriPos.Add(new ST_XYN(WsTriPos[WsTriPos.Count - 1].st_place_pos[id].x, md.st_place_pos[id].y, md.Num - 1));
                }
                if (WsTriPos.Count > 0)
                {
                    pos_flystop.x = WsTriPos[WsTriPos.Count - 1].st_place_pos[id].x;
                    pos_flystop.y = WsTriPos[WsTriPos.Count - 1].st_place_pos[id].y + 50;
                }
                FlyCfg(EM_FLY_CFG.WS_UP, TriPos, WsTriPos.Count, true, Ofs_On);

                //定位飞拍
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("{0}Y轴飞拍前其它轴定位!", disc) : string.Format("other axes move before {0}Y axis flyshot!          ({1}Y轴飞拍前其它轴定位!)", disc, disc));
                if (Math.Abs(pos_flystop.x - ax_x.fenc_pos) > 50 || ax_y.fenc_pos > fly_safepos)
                {
                    AXIS axisy = null;
                    if (ax_y.fenc_pos > fly_safepos) axisy = ax_y;
                    #region 取放料保护

                    res = SafeProtect(ref bquit);
                    if (res != EM_RES.OK)
                    {
                        return res;
                    }
                    #endregion
                    res = MT.ZupMove(ref bquit, ref ax_x, pos_flystop.x, ref axisy, fly_safepos, ref ax_u1, 0, ref ax_u2, 0);
                    if (res != EM_RES.OK) return res;
                }
                if (Math.Abs(ax_u2.fcmd_pos) > 8 || Math.Abs(ax_u1.fcmd_pos) > 8)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("{0}定位到飞拍放料前转正角度U1:{1} U2:{2}", disc, ax_u1.fcmd_pos, ax_u2.fcmd_pos) : string.Format("{0} XT U axis home before flyshot U1:{2} U2:{3}           ({1}定位到飞拍放料前转正角度U1:{2} U2:{3})", disc, disc, ax_u1.fcmd_pos, ax_u2.fcmd_pos));
                    res = MT.ZupMove(ref bquit, ref ax_u1, 0, ref ax_u2, 0);
                    if (res != EM_RES.OK) return res;
                }
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("{0}定位到飞拍放料起飞", disc) : string.Format("{0}  move to flyshot end pos         ({1}定位到飞拍放料起飞)", disc, disc));
                res = MT.ZupMove(ref bquit, ref ax_x, pos_flystop.x, ref ax_y, pos_flystop.y, ref ax_u1, 0, ref ax_u2, 0);
                if (res != EM_RES.OK) return res;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("{0}定位到飞拍放料结束位置X:{1:f3},Y:{2:f3}", disc, pos_flystop.x, pos_flystop.y) : string.Format("{0}  move to flyshot end pos X:{1:f3},Y:{2:f3}           ({1}定位到飞拍放料结束位置X:{1:f3},Y:{2:f3})", disc, pos_flystop.x, pos_flystop.y));
                return EM_RES.OK;
            }
            finally
            {
                ax_y.DisableHcmp(upcam.TriIO.num);
                ax_y.DisableHcmp(dwcam.TriIO.num);
            }

        }

        /// <summary>
        /// 放料于工站
        /// </summary>
        /// <param name="bquit"></param>
        /// <param name="WsTriPos">工站</param>
        public EM_RES FlyPlaceWsMod(ref bool bquit, List<WSFEED.MdDat> WsTriPos, WSFEED ws)
        {
            EM_RES res = EM_RES.OK;
            ST_XY Pos_Upcam = new ST_XY();
            int xtzk = 0;
            if (!Demo)
            {
                foreach (XT xt in list_xt)
                {
                    if (xt.cy_zk.isON && xt.cy_zk.isONByChkSen) xtzk++;
                  //  if (xt.cy_zk.isON && xt.cy_zk.isOFFByChkSen) xt.cy_zk.SetOff();
                }
                if (VAR.ClearMt)
                {

                    if (xtzk == 1 && WsTriPos.Count > 1)
                        WsTriPos.RemoveAt(1);
                }
                else
                {
                    if (WsTriPos.Count > xtzk)
                        return EM_RES.RETRY;
                }
            }
            //进行马达二维码扫描
            //bool dwcam_recap_err = false;
            for (int n = 0; n < 2; n++)
            {
                //定位到放料位
                if (FindMod == EM_FIN_MOD.HIGH && id == 0 && (WsTriPos[0].Num == 3 || WsTriPos[0].Num == 7))
                {
                    ud2needwait = true;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}中的3、7工位进行工站放料，上下料2等待开启-----", disc, ws.disc));
                }
                if (FindMod == EM_FIN_MOD.LOW && id == 1 && (WsTriPos[0].Num == 2 || WsTriPos[0].Num == 6))
                {
                    ud1needwait = true;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}中的2、6工位进行工站放料，上下料1等待开启-----", disc, ws.disc));
                }
                res = FlyToWs(ref bquit, ref FeedBackPos, WsTriPos);
                if (res != EM_RES.OK) return res;
                //等待视觉回传结果 
                WaitCamRes(dwcam, WsTriPos.Count);
                WaitCamRes(upcam, WsTriPos.Count);
                //检查视觉回传结果 

                res = ChkCamRes(dwcam, WsTriPos.Count);
                if (res != EM_RES.OK)
                {
                    if (n == 0) continue;
                    else return res;
                }
                res = ChkCamRes(upcam, WsTriPos.Count, false);
                if (res != EM_RES.OK)
                {
                    if (n == 0) continue;
                    else return res;
                }
                break;
            }
            //确认有没有模组拍照料失败
            int xtnum;
            bool dwcam_recap_err = false;
            foreach (Cam.VisionTask task in dwcam.List_vs_task_cur)
            {
                xtnum = (dwcam.List_vs_task_cur.IndexOf(task) + 1) % 2;
                if (!task.ResData.bOK)
                {
                    res = MT.ZupMove(ref bquit, ref ax_y, list_xt[xtnum].st_cap_pos.y, ref list_xt[xtnum].ax_u, 0);
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}飞拍失败，定位到拍照位置Y:{1}mm->重拍", list_xt[xtnum].disc, list_xt[xtnum].ax_y.fenc_pos) : string.Format("{0} flyshot failed,move to Y:{1}mm->retry photograph               ({0}飞拍失败，定位到拍照位置Y:{1}mm->重拍)", list_xt[xtnum].disc, list_xt[xtnum].ax_y.fenc_pos), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.UpDownLoad + id);
                    if (res != EM_RES.OK) return res;
                    Thread.Sleep(50);
                    res = task.TriAndWaitResult(ref bquit, 1000, 1, Demo);
                    if (res != EM_RES.OK)
                    {
                        SQLData.TesterrDataAdd(ws.id, WsTriPos[0].Num, "工站放料", string.Format("{0}拍照失败", ws.disc));
                        COUNT_DATA.Errcnt++;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}重拍失败！", list_xt[xtnum].disc) : string.Format("{0} photograph again failed!        ({0}重拍失败！)", list_xt[xtnum].disc), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.UpDownLoad + id);
                        dwcam_recap_err = true;

                    }
                }
            }
            if (dwcam_recap_err) return EM_RES.CAM_ERR_PLACE_BACK;
            foreach (Cam.VisionTask task in upcam.List_vs_task_cur)
            {
                xtnum = upcam.List_vs_task_cur.IndexOf(task);
                if (!task.ResData.bOK)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("工站位置{0}飞拍失败，定位到拍照位置重拍!", WsTriPos[xtnum].Num) : string.Format("WS num{0} flyshot failed,move to photograph pos to take a picture agian!              (工站位置{0}飞拍失败，定位到拍照位置重拍!)", WsTriPos[xtnum].Num), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.UpDownLoad + id);
                    res = MT.ZupMove(ref bquit, ref ax_x, FeedBackPos.x, ref ax_y, WsTriPos[xtnum].st_place_pos[id].y);
                    if (res != EM_RES.OK) return res;
                    Thread.Sleep(50);
                    res = task.TriAndWaitResult(ref bquit, 2000, 1, Demo);
                    if (res != EM_RES.OK)
                    {
                        SQLData.TesterrDataAdd(ws.id, WsTriPos[0].Num, "工站放料", string.Format("{0}拍照失败", ws.disc));
                        COUNT_DATA.ErrTemp = System.DateTime.Now;
                        VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "拍照错误!" : "Cam ERR", 20, true);
                        MT.ST_WARN warn = new MT.ST_WARN();
                        warning fr_warn = new warning();
                        warn.ok_txt = VAR.IsChinese ? "确定" : "OK";
                        warn.ws = null;
                        warn.title = VAR.IsChinese ? "提示:拍照错误!" : "Tip: Failed to take a picture!";
                        warn.msg = VAR.IsChinese ? string.Format("工站位置{0}重拍失败,请检查对应的工站位置是否有异物!", WsTriPos[xtnum].Num) : string.Format("Workstation location {0} failed to retake, please check the corresponding station location for foreign objects!\r\n工站位置{0}重拍失败,请检查对应的工站位置是否有异物!", WsTriPos[xtnum].Num);
                        warn.lb_msg = VAR.IsChinese ? string.Format("提示:工站位置{0}重拍失败,请检查对应的工站位置是否有异物或物料，按确定键停止,确认无异物后再按运行键继续!", WsTriPos[xtnum].Num) : string.Format("Tips:Workstation location {0} failed to retake. Please check whether there is any foreign object or material in the corresponding station location. Press the OK key to stop. After confirming that there are no foreign objects, press the Run key to continue!\r\n提示:工站位置{0}重拍失败,请检查对应的工站位置是否有异物或物料，按确定键停止,确认无异物后再按运行键继续!", WsTriPos[xtnum].Num);
                        MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.CaptureAbnomal);
                        return EM_RES.ERR;
                    }
                }
            }
            //清空相机数据
            if (!upcam.FlushOk || !dwcam.FlushOk)
            {
                Task tak = new Task(() =>
                {
                    if (!upcam.FlushOk && !upcam.FlushUpdate)
                    {
                        upcam.FlushUpdate = true;
                        upcam.mAcqFifo.Flush();
                        upcam.FlushOk = true;
                        upcam.FlushUpdate = false;
                    }
                    if (!dwcam.FlushOk && !dwcam.FlushUpdate)
                    {
                        dwcam.FlushUpdate = true;
                        dwcam.mAcqFifo.Flush();
                        dwcam.FlushOk = true;
                        dwcam.FlushUpdate = false;
                    }
                });
                tak.Start();
            }
            #region 先放吸头2后放吸头1      
            foreach (Cam.VisionTask dw_task in dwcam.List_vs_task_cur)
            {
                // xtnum = (dwcam.List_vs_task_cur.IndexOf(dw_task)+1)%2;
                xtnum = dw_task.TriPos.n;
                if (dw_task.ResData.bOK && dw_task.ResData.bUpdate)
                {
                    for (int j = upcam.List_vs_task_cur.Count - 1; j >= 0; j--)
                    {
                        Cam.VisionTask up_task = upcam.List_vs_task_cur[j];
                        if (up_task.ResData.bOK && up_task.ResData.bUpdate)
                        {
                            // Pos_Upcam.x = FeedBackPos.x;
                            //Pos_Upcam.y = WsTriPos[j].st_pos[id].y;  
                            Pos_Upcam.x = up_task.TriPos.x;
                            Pos_Upcam.y = up_task.TriPos.y;
                            res = list_xt[xtnum].XtPickOrPlaceMod(ref bquit, Pos_Upcam, up_task.ResData.PosMM,
                                     dw_task.ResData.PosMM, WsTriPos[j].st_place_pos[id].z, Demo, XT.EM_XTFLOW.PLACEMOD, false, PT_SET.bModPasteUp);
                            if (res == EM_RES.OK)
                            {
                                if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.UP_SCAN && !Demo)
                                {
                                    WsTriPos[j].bardcode = list_xt[xtnum].XtMd.bardcode;
                                }
                                else if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.DW_SCAN && !Demo) WsTriPos[j].bardcode = dw_task.ResData.BarCode;
                                list_xt[xtnum].XtMd = null;
                                WsTriPos[j].res = -1;
                                WsTriPos[j].Ishavemd = true;
                                ws.breschanged = true;
                                WsTriPos[j].IsNormal = VAR.Isnormal;
                              
                                COUNT_DATA.allcnt[ws.id]++;
                                if (PT_SET.bNgControl) COUNT_DATA.ngctrlallcnt++;
                                if (PT_SET.EquipmentMT != 0) COUNT_DATA.CurEquipmentMT++;
                            }
                            else return res;
                            up_task.ResData.bUpdate = false;
                            dw_task.ResData.bUpdate = false;
                            break;
                        }
                    }
                }
            }
            
            bUpdateFBPos_UT = true;
            bUpdateFBPos_OKNG = true;
            #endregion
            return EM_RES.OK;
        }
        /// <summary>
        /// 放模组于工站流程
        /// </summary>
        /// <param name="bquit"></param>
        /// <param name="flycam"></param>
        /// <param name="placemd"></param>
        /// <returns></returns>
        public EM_RES PlaceMdToWs(ref bool bquit, List<WSFEED.MdDat> placemd, WSFEED ws)
        {
            EM_RES res = EM_RES.OK;
            ////放料前旋转90°，不管是否有料；
            //if (PT_SET.isopen_degree)
            //{
            //    res = MT.ZupMove(ref bquit, ref ax_u1, PT_SET.degree, ref ax_u2, PT_SET.degree);
            //    if (res != EM_RES.OK) return res;
            //}
            //放料
            res = FlyPlaceWsMod(ref bquit, placemd, ws);
            if (FindMod == EM_FIN_MOD.HIGH && id == 0 && (placemd[0].Num == 4 || placemd[0].Num == 8))
            {
                UpDownLoad.ud2needwait = true;//当上下料给工站4或者8上料完成后，也需要开启保护
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}中的4、8工位上料完成，上下料2等待开启-----", disc,ws.disc));
            }
            if (FindMod == EM_FIN_MOD.LOW && id == 1 && (placemd[0].Num == 1 || placemd[0].Num == 5))
            {
                UpDownLoad.ud1needwait = true;//当上下料给工站4或者8上料完成后，也需要开启保护
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}对工站{1}中的1、5工位上料完成，上下料1等待开启-----", disc, ws.disc));
            }
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("{0}结束,{1}", disc, res.ToString()) : string.Format("{0} end,{2}              ({1}结束,{2})", disc, disc, res.ToString()));
            if (res == EM_RES.OK)
            {
                ErrTryPickCnt = 0;              
            }
            return res;
        }

        #endregion

        #region 取料盘模组函数
        /// <summary>
        /// 飞拍取料
        /// </summary>
        /// <param name="bquit"></param>
        /// <returns></returns>
        public EM_RES FlyTrayPickMod(ref bool bquit,WSFEED wsfeed)
        {
            List<Cam.VisionTask> upcam_task = new List<Cam.VisionTask>();
            ST_XY Pos_Upcam=new ST_XY();
            ST_XYZA endpos = new ST_XYZA();
            ST_XYA xt_vs_dat = new ST_XYA(-10000, -10000, 0);
            EM_RES res = FlyCamToTray(ref bquit, wsfeed, traybox_fd, ref endpos);
            if (res != EM_RES.OK) return res;
          
            //寻找上相机拍模组数据
            foreach (Cam.VisionTask task in  upcam.List_vs_task_cur)
            {
                if(task.TaskName.Contains(CONST.ModUpFw))
                 upcam_task.Add(task);
            }
            //排列数据
            if (upcam_task.Count > 1)
            {
                upcam_task.Sort((a, b) =>
                {
                    if (a.TriPos.y < b.TriPos.y) return 1;
                    else return -1;
                });
            }

            //吸料
            for (int i = list_xt.Count - 1; i >= 0; i--)
            {
                if ((list_xt[i].cy_zk.isOFFByChkSen && !Demo) || Demo)
                {
                    foreach (Cam.VisionTask uptask in modupcam_task)
                    {
                        if (uptask.ResData.bOK && uptask.ResData.bUpdate)
                        {
                            Product.Tray.PosInf posinf = traybox_fd.tray_cur.list_pos[uptask.TriPos.n];
                            Pos_Upcam.x = uptask.TriPos.x;
                            Pos_Upcam.y = uptask.TriPos.y;
                            res = list_xt[i].XtPickOrPlaceMod(ref bquit, Pos_Upcam, uptask.ResData.PosMM, xt_vs_dat, posinf.Pos[id].z, Demo);
                            if (res == EM_RES.OK)
                            {
                                list_xt[i].XtMd = posinf.md;
                                COUNT_DATA.SuctionAllcnt++;
                                if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.UP_SCAN && !Demo)
                                {
                                    list_xt[i].XtMd.bardcode = uptask.ResData.BarCode;
                                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}------barcode---{1}!", list_xt[i].disc, list_xt[i].XtMd.bardcode));
                                }
                                posinf.md = null;

                            }
                            else return res;
                            uptask.ResData.bUpdate = false;
                            break;
                        }
                    }
                }

            }

            return EM_RES.OK;
        }

        /// <summary>
        /// 料盘取料
        /// </summary>
        /// <param name="bquit"></param>
        /// <param name="IsFly"></param>
        /// <param name="Try"></param>
        /// <returns></returns>
        public EM_RES TrayPickMod(ref bool bquit, ref bool IsFly, WSFEED ws)
        {
            EM_RES res = EM_RES.OK;
            EM_RES ret = EM_RES.OK;
            string str = "";
            string barcodetemp = "";
            int trycnt = 0;
            bool search = false;
            int TryPickCnt = 0, TryCnt = 0;
            bool pickone = false;
            List<Product.Tray.PosInf> ListPickMod = new List<Product.Tray.PosInf>();
            if (!traybox_fd.IsReady) return EM_RES.ABORT;
            if (!Demo && list_xt[0].cy_zk.isONByChkSen && list_xt[0].XtMd != null && list_xt[1].cy_zk.isONByChkSen && list_xt[1].XtMd != null) return EM_RES.OK;
            //判断料盘状态
            if (traybox_fd.tray_cur == null) return EM_RES.PARA_OUTOFRANG;
            if(ws.id==0&&id==0)
            {
                ListPickMod = traybox_fd.tray_cur.GetPosList();
            }
            else
            {
                ListPickMod = traybox_fd.tray_cur.GetPosListOrder();
            }


            if (ListPickMod != null && ListPickMod.Count == 0) return EM_RES.END;
            GetWsPosTeam(ref CurPlaceMod, ws, Product.EM_CM_RES.EMPTY, FindMod);
            int xtzkcnt = 0;
            if (!Demo)
            {
                foreach (XT xt in list_xt)
                {
                    if (xt.cy_zk.isON && xt.cy_zk.isONByChkSen) xtzkcnt++;
                    if (xt.cy_zk.isON && !xt.cy_zk.isONByChkSen) xt.cy_zk.SetOff();
                }
                if (CurPlaceMod.Count == xtzkcnt) return EM_RES.OK;
            }
            if (CurPlaceMod.Count == 1) pickone = true;
            if (traybox_fd.ischangetray && PT_SET.TrayBarcodeEn)
            {
                traybox_fd.ischangetray = false;
                ret = MT.Move(ref VAR.gsys_set.bquit, ref MT.AXIS_UDL_FD_X, id == COM.UDLoad1.id ? traybox_fd.tray_barcode_a1 : traybox_fd.tray_barcode_a2);
                if (ret != EM_RES.OK) return ret;
                ret = MT.Move(ref VAR.gsys_set.bquit, ref ax_x, id == COM.UDLoad1.id ? traybox_fd.tray_barcode_x1 : traybox_fd.tray_barcode_x2, ref ax_y, id == COM.UDLoad1.id ? traybox_fd.tray_barcode_y1 : traybox_fd.tray_barcode_y2);
                if (ret != EM_RES.OK) return ret;
                while (trycnt++ < 3)
                {
                    if (id == COM.UDLoad1.id)
                        ret = COM.CamUp1.FindTaskTriAndWait(CONST.TrayUpFw, Demo);
                    else
                        ret = COM.CamUp2.FindTaskTriAndWait(CONST.TrayUpFw, Demo);
                    if (ret != EM_RES.OK) continue;
                    Cam.VisionTask task = id == COM.UDLoad1.id ? COM.CamUp1.curTask : COM.CamUp2.curTask;
                    if (task.ResData.BarCode == "") continue;
                    barcodetemp = task.ResData.BarCode;
                    if (barcodetemp != barcodestr)
                    {

                        bool delete = database.DeleteIndex(barcodestr, out str);
                        if (!delete)
                        {
                            delete = database.DeleteIndex(barcodestr, out str);
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, str);
                        }
                        barcodestr = barcodetemp;
                    }
                    //search = database.IsExitIndex(barcodestr, out str);
                    //if (!search)
                    //{
                    //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, str);
                    //    break;                      
                    //}
                    //VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "当前供料盘已进行过震动。");
                    //break;
                }
                if (trycnt == 4)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("连续3次拍不到料盘二维码"));
                    //界面提示                 
                    VAR.sys_inf.Set(EM_ALM_STA.WAR_YELLOW_FLASH, "未震动", 10, true);
                    MT.ST_WARN warn = new MT.ST_WARN();
                    warning fr_warn = new warning();
                    warn.ws = null;
                    warn.ok_txt = VAR.IsChinese ? "更换料盘" : "Replace tray";
                    warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                    warn.title = VAR.IsChinese ? "提示:供料盘未震动!" : "Tip: Feed tray is not shaken!";
                    warn.msg = VAR.IsChinese ? disc + "供料盘没有震动请确认后按确定继续!" :disc + "The feed tray is not vibrating. Please confirm after pressing 'Replace tray' key to continue!" + disc + "供料盘没有震动请确认后按确定继续!";
                    warn.lb_msg = VAR.IsChinese ? "提示:" + disc + "供料盘没有震动请确认后按确定继续!" : "Tip:" + disc + "The feed tray is not vibrating, please confirm after pressing 'Replace tray' key to continue!" + "\r\n提示:" + disc + "供料盘没有震动请确认后按确定继续!";
                    DialogResult logres = MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.Null);
                    if (logres == DialogResult.OK)
                    {
                        VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, "运行", 0, true);
                        MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                        return EM_RES.END;
                    }
                    else if (logres == DialogResult.Cancel)
                    {
                        res = EM_RES.ERR;
                        return res;
                    }
                }

            }

            //取料
            while (TryCnt++ < 5)
            {
                if (!IsFly)
                {
                    foreach (XT xt in list_xt)
                    {
                        //如果只有一个目标，根据需求吸料
                        if (pickone && !PT_SET.xt1firsten && xt.id == list_xt[0].id)
                            continue;
                        else if (pickone && PT_SET.xt1firsten && xt.id == list_xt[1].id)
                            continue;
                        if (!Demo)
                        {
                            if ((xt.cy_zk.isONByChkSen && xt.XtMd == null) || (!xt.cy_zk.isONByChkSen && xt.XtMd != null))
                            {
                                xt.XtMd = null;
                                xt.cy_zk.SetOff();
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? disc + "检查到（真空达到阀值但没有模组信息)或(有模组信息真空达不到阀值)" : disc + "checked (vacuum reaches threshold but no module information) or (with module information vacuum cannot reach threshold)                    (检查到（真空达到阀值但没有模组信息)或(有模组信息真空达不到阀值))", DReport.EmErrCode.PickFailed, (int)DReport.EmHareware.UpDownLoad + id);
                                Thread.Sleep(100);
                            }
                        }
                        if ((Demo && xt.XtMd == null) || (!xt.cy_zk.isONByChkSen && xt.XtMd == null && !Demo))
                        {
                            if (ws.id==0&&id==0)
                            {
                                ListPickMod = traybox_fd.tray_cur.GetPosList();
                            }
                            else
                            {
                                ListPickMod = traybox_fd.tray_cur.GetPosListOrder();
                            }
                           

                            if (ListPickMod != null && ListPickMod.Count == 0)
                            {
                                if (VAR.ClearMt)
                                {
                                    VAR.sys_inf.Set(EM_ALM_STA.WAR_YELLOW_FLASH, VAR.IsChinese ? "清料提示!" : "Clear warning", 20, true);
                                    MT.ST_WARN st_warn = new MT.ST_WARN();
                                    warning fr_warn = new warning();
                                    //st_warn.ok_txt = VAR.IsChinese ? "清料" : "Clear";
                                    st_warn.ws = null;
                                    st_warn.cancle_txt = VAR.IsChinese ? "取消" : "Cancel";
                                    st_warn.msg = VAR.IsChinese ? "清料提示!" : "Clear tip";
                                    st_warn.title = VAR.IsChinese ? "提示:清料提示!" : "Tip:Clear";
                                    st_warn.lb_msg = VAR.IsChinese ? "提示:是否要清料?\r\n  1.按清料键进行清料流程" : "Tip: Do you want to clear the material? \r\n 1.Press the 'Clear' button to clear the material\r\n提示:是否要清料?\r\n  1.按清料键进行清料流程";
                                    DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.Null);
                                    if (DialogResult.OK == logres)
                                    {
                                        VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                        return EM_RES.NEXT;
                                    }
                                    else
                                    {
                                        VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                        return EM_RES.END;
                                    }

                                }
                                else
                                {
                                    return EM_RES.END;
                                }

                            }
                             double temppos = 0;
                            //if (FindMod == EM_FIN_MOD.HIGH&&Math.Abs(ax_x.fenc_pos) > 10.01) temppos = ws.list_md[3].st_place_pos[id].x - 40;//避让40mm，模块1靠右，模块2靠左
                            //else if (FindMod == EM_FIN_MOD.LOW && Math.Abs(ax_x.fenc_pos) >=0) temppos = ws.list_md[0].st_place_pos[id].x + 40;
                            //else temppos = ax_x.fenc_pos;

                            if (FindMod == EM_FIN_MOD.HIGH && Math.Abs(ax_x.fenc_pos) >= 0) temppos = (ws.list_md[3].st_place_pos[id].x - 50) > 2 ? (ws.list_md[3].st_place_pos[id].x - 50) : 2;//避让40mm，模块1靠右，模块2靠左
                            else if (FindMod == EM_FIN_MOD.LOW && Math.Abs(ax_x.fenc_pos) >= 0) temppos = ws.list_md[0].st_place_pos[id].x + 50;
                            else temppos = ax_x.fenc_pos;

                            #region 取放料保护
                            if (ws.id == 0 && id == 0)
                            {
                                res = SafeProtect(ref bquit);
                                if (res != EM_RES.OK)
                                {
                                    return res;
                                }
                            }

                            #endregion
                            //定拍取料
                            //  res = xt.XtCapMovMod(ref bquit, ListPickMod[0].Pos[id], CONST.ModUpFw, CurPlaceMod[0].st_place_pos[id].x, XT.EM_XTFLOW.PICKMOD, true, Demo);
                            Product.Tray.PosInf selectedPos = ListPickMod[0];
                            Product.Tray.PosInf p0Pos = traybox_fd.tray_cur.list_pos.Find(s => s.idx == 0);
                            string pickOrder = ws.id == 0 && id == 0 ? "正序" : "倒序";
                            string selectedState = selectedPos.md == null ? "空" : ((Product.EM_CM_RES)selectedPos.md.res).ToString();
                            string p0State = p0Pos == null ? "缺失" : p0Pos.md == null ? "空" : ((Product.EM_CM_RES)p0Pos.md.res).ToString();
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format(
                                "{0}供料盘选料追踪: UD={1},WS={2},方向={3},候选数={4},选中[Idx={5},Row={6},Col={7},Pos={8},状态={9}],P0=[Idx={10},Row={11},Col={12},Pos={13},状态={14},禁用={15},临时禁用={16}]",
                                disc, id + 1, ws.id + 1, pickOrder, ListPickMod.Count,
                                selectedPos.idx, selectedPos.Row, selectedPos.Col, selectedPos.Pos[id], selectedState,
                                p0Pos == null ? -1 : p0Pos.idx, p0Pos == null ? -1 : p0Pos.Row, p0Pos == null ? -1 : p0Pos.Col,
                                p0Pos == null ? "缺失" : p0Pos.Pos[id].ToString(), p0State, p0Pos != null && p0Pos.isDisable, p0Pos != null && p0Pos.isDisableTemp));
                            res = xt.XtCapMovMod(ref bquit, id == 0 ? COM.UDLoad1 : COM.UDLoad2,ws, ListPickMod[0].Pos[id], CONST.ModUpFw, temppos, XT.EM_XTFLOW.PICKMOD, true, Demo);
                            if (res == EM_RES.OK)
                            {
                                //更新料盘
                                TryPickCnt = 0;
                                xt.XtMd = ListPickMod[0].md;
                                COUNT_DATA.SuctionAllcnt++;
                                if (PT_SET.BarcodeMode == (int)PT_SET.BAR_SCAN.UP_SCAN&&!Demo)
                                {
                                    xt.XtMd.bardcode = upcam.curTask.ResData.BarCode;
                                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}------barcode---{1}!", xt.disc, xt.XtMd.bardcode));
                                }
                                ListPickMod[0].md = null;

                                #region 取放料保护
                                if (ws.id == 0 && id == 0)
                                {
                                    res = SafeProtect(ref bquit);
                                    if (res != EM_RES.OK)
                                    {
                                        return res;
                                    }
                                }                               

                                #endregion

                            }
                            else if (res == EM_RES.CAM_ERR || res == EM_RES.TIMEOUT || res == EM_RES.PICK_ERR)
                            {
                               // COUNT_DATA.ErrTemp = System.DateTime.Now;
                                ListPickMod[0].md.res = (int)Product.EM_CM_RES.CAMERR;
                                TryPickCnt++;
                                COUNT_DATA.SuctionAllcnt++;
                                
                                if (TryPickCnt > 4)
                                {
                                    SQLData.TesterrDataAdd(99, 99, "料盘取料", string.Format("{0}料盘取料异常", disc));
                                    COUNT_DATA.Errcnt++;
                                    VAR.sys_inf.Set(EM_ALM_STA.WAR_YELLOW_FLASH, VAR.IsChinese ? "吸料异常!" : "Draw ERR", 20, true);
                                    MT.ST_WARN st_warn = new MT.ST_WARN();
                                    warning fr_warn = new warning();
                                    st_warn.ok_txt = VAR.IsChinese ? "归位确认" : "Home";
                                    st_warn.ws = null;
                                    st_warn.cancle_txt = VAR.IsChinese ? "更换料盘" : "Replace tray";
                                   // st_warn.abort_txt = VAR.IsChinese ? "清料" : "Clear";
                                    st_warn.title = VAR.IsChinese ? "提示:吸料异常!" : "Tip: Abnormal suction!";
                                    st_warn.msg = VAR.IsChinese ? string.Format("{0}连续取料5次模组失败!", xt.disc) : string.Format("{0} failed to pick mod 5 times\r\n{0}连续取料5次模组失败!", xt.disc);
                                    st_warn.lb_msg = VAR.IsChinese ? string.Format("提示:{0}连续取料5次失败,请确认料盘是否放反!\r\n  1.如果放反请按更换料盘键进行料盘更换!\r\n  2.如没有放反请按归位确认键分析原因,注意请等轴归位完成后再打开门!\r\n  3.如清料请按清料键!", xt.disc) : string.Format("Tip:{0} failed to pick mod 5 times.Please confirm whether the tray is turned upside down!\r\n 1.If it is reversed, press the Replace Tray key to change the tray! 2.If there is no reversal, please press the home key to analyze the cause. Note that please wait for the axis to complete the homing before opening the door! \r\n 3.If you want to clear the material, please press the key \r\n提示:{0}连续取料5次失败,请确认料盘是否放反!\r\n  1.如果放反请按更换料盘键进行料盘更换!\r\n  2.如没有放反请按归位确认键分析原因,注意请等轴归位完成后再打开门!\r\n  3.如清料请按清料键!", xt.disc);
                                    DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);
                                    if (DialogResult.Cancel == logres)
                                    {
                                        VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                        return EM_RES.END;

                                    }
                                    else if (DialogResult.Abort == logres)
                                    {
                                        VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                        //   VAR.ClearMt = true;
                                        //   return EM_RES.NEXT;
                                        return EM_RES.END;

                                    }
                                    return res;
                                }
                                else continue;
                            }
                            else return res;
                        }
                    }

                }
                else
                {
                    //飞拍取料
                    IsFly = false;
                    res = FlyTrayPickMod(ref bquit, ws);
                    if (res != EM_RES.OK)
                    {
                        if (res == EM_RES.CAM_ERR || res == EM_RES.TIMEOUT || res == EM_RES.PICK_ERR) continue;
                        else return res;
                    }
                }

                if ((((list_xt[0].cy_zk.isONByChkSen && list_xt[0].XtMd != null && list_xt[1].cy_zk.isONByChkSen && list_xt[1].XtMd != null && !pickone) || (list_xt[1].cy_zk.isONByChkSen && list_xt[1].XtMd != null && pickone)) && !Demo) || (Demo && list_xt[0].XtMd != null && list_xt[1].XtMd != null) || VAR.ClearMt)
                {
                    List<Product.Tray.PosInf> ListPickModnew1 = new List<Product.Tray.PosInf>();//增加提前下料
                    ListPickModnew1 = traybox_fd.tray_cur.GetPosList();
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "取料后剩余物料数量：" + ListPickModnew1.Count);
                    //if (ListPickModnew1 != null && ListPickModnew1.Count == 0)
                    //{
                    //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "取料后剩余物料数量为0，提前下料");
                    //    //更换料盘
                    //    if (traybox_fd.IsReady)
                    //    {
                    //        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "取料后剩余物料数量为0，开始提前下料");
                    //        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("请更换{0}====================", traybox_fd.disc) : string.Format("Please replace {0}====================               (请更换{1}====================)", traybox_fd.name, traybox_fd.disc));
                    //        traybox_fd.IsReady = false;
                    //        Task fdtsk = new Task(() => { traybox_fd.Tray_Action(Demo); });
                    //        fdtsk.Start();
                    //    }
                    //}
                    return EM_RES.OK;
                }
            }
            if (TryCnt >= 5)
            {
              //  COUNT_DATA.ErrTemp = System.DateTime.Now;
                VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "取料失败!" : "Draw ERR", 30, true);
                SQLData.TesterrDataAdd(99, 99, "料盘取料", string.Format("{0}连续5次取料失败异常", disc));
                MT.ST_WARN st_warn = new MT.ST_WARN();
                warning fr_warn = new warning();
                st_warn.ok_txt = VAR.IsChinese ? "确定" : "ok";
                st_warn.abort_txt = VAR.IsChinese ? "确定" : "abort";
                st_warn.ws = null;
                st_warn.cancle_txt = VAR.IsChinese ? "取消" : "cancle";
                st_warn.title = VAR.IsChinese ? "提示:连续取料尝试次数超过5次!" : "Tip: Abnormal suction!";
                st_warn.msg = string.Format("{0}连续取料尝试次数超过5次,请确认后按下启动键!", disc);
                st_warn.lb_msg = "提示:" + st_warn.msg;
                DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);

                if (DialogResult.Cancel == logres) return EM_RES.ERR;
                //VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                if (DialogResult.OK == logres)
                {
                    MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                    return EM_RES.CAM_ERR;
                    
                }
                else if (DialogResult.Abort == logres)
                {
                    MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                    return EM_RES.CAM_ERR;
                    
                }
                else if (DialogResult.Retry == logres)
                {
                    MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                    return EM_RES.CAM_ERR;
                    
                }
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? disc + "连续取料尝试次数超过5次!" : disc + "Pick failed five times!       (连续取料尝试次数超过5次!)", DReport.EmErrCode.PickFailed, (int)DReport.EmHareware.UpDownLoad + id);
                return EM_RES.CAM_ERR;
            }
            return EM_RES.OK;
        }

        /// <summary>
        /// 料盘取料流程
        /// </summary>
        /// <param name="bquit"></param>
        /// <param name="ws"></param>
        /// <returns></returns>
        /// 
        public EM_RES PickMdToTray(ref bool bquit, ref bool flycam, WSFEED ws)
        {
            //取料
            EM_RES res = EM_RES.OK;
            // bool ChangeFdTray = false;
            //料盘取料前 旋转0°
            if (PT_SET.isopen_degree)
            {
                res = MT.ZupMove(ref bquit, ref ax_u1, 0, ref ax_u2, 0);
                if (res != EM_RES.OK) return res;
            }
            while (!bquit)
            {
                res = TrayPickMod(ref bquit, ref flycam, ws);
                if (res != EM_RES.ABORT)
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("上料线程取料结束,{0}", res.ToString()) : string.Format("Upload thread end,{0}       (上料线程取料结束,{0})", res.ToString()));

                if (res == EM_RES.OK)
                {
                    List<Product.Tray.PosInf> ListPickMod = new List<Product.Tray.PosInf>();
                    if (ws.id == 0 && id == 0)
                    {
                        ListPickMod = traybox_fd.tray_cur.GetPosList();
                    }
                    else
                    {
                        ListPickMod = traybox_fd.tray_cur.GetPosListOrder();
                    }


                    //if (ListPickMod != null && ListPickMod.Count == 0)
                    //{
                    //    //更换料盘
                    //    if (traybox_fd.IsReady)
                    //    {
                    //        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("请更换{0}====================", traybox_fd.disc) : string.Format("Please replace {0}====================               (请更换{1}====================)", traybox_fd.name, traybox_fd.disc));
                    //        traybox_fd.IsReady = false;
                    //        Task fdtsk = new Task(() => { traybox_fd.Tray_Action(Demo); });
                    //        fdtsk.Start();
                    //    }
                    //}
                    break;
                }
                else if (res == EM_RES.PARA_OUTOFRANG || res == EM_RES.END)
                {
                    //更换料盘
                    if (traybox_fd.IsReady)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("请更换{0}====================", traybox_fd.disc) : string.Format("Please replace {0}====================               (请更换{1}====================)", traybox_fd.name, traybox_fd.disc));
                        traybox_fd.IsReady = false;
                        Task fdtsk = new Task(() => { traybox_fd.Tray_Action(Demo); });
                        fdtsk.Start();
                    }
                }
                else if (res == EM_RES.NEXT)
                {
                    if (VAR.ClearMt) res = EM_RES.OK;
                    return res;

                }
                else
                {
                    if (res != EM_RES.ABORT) return res;
                    Thread.Sleep(100);

                }
            }

            return res;
        }
        #endregion

        #region GRR测试流程
        public EM_RES GRRFlow(ref bool bquit, ref WSFEED ws, UpDownLoad ud, bool IsDemo = false)
        {
            int xtid = 0;
            EM_RES res = EM_RES.OK;
            List<WSFEED.MdDat> WsPickmd = new List<WSFEED.MdDat>();
            Product.MdDat xt_md = new Product.MdDat();
            ST_XY xtdwvs = new ST_XY();
            ST_XYZA pickpos = new ST_XYZA();
            List<Cam.VisionOutPutData> dwData = new List<Cam.VisionOutPutData>();
            bool Isfinal = false;
            while (!bquit && !VAR.gsys_set.bquit)
            {
                if (ws.GrrTestCnt >= PT_SET.GRRTestCnt)
                {
                   // ws.GrrTestCnt = 0;
                    if (GrrProc == EM_STA.PICKWS)
                    {
                        foreach (XT xt in ud.list_xt)
                        {
                            if (((!xt.cy_zk.isONByChkSen && !IsDemo) || IsDemo) && xt.XtMd == null)
                            {
                            NEXT:
                                WsPickmd.Clear();
                                GetWsOrderPosTeam(ref WsPickmd, ws, Product.EM_CM_RES.OK);
                                if (WsPickmd.Count > 0)
                                {
                                    //xtid = WsPickmd[0].Num / 5;
                                    xtdwvs = xt.st_vs_pos_xtshp.ToXY();
                                    pickpos = WsPickmd[0].st_pickpos[id].ToXYZA() + xt.st_rol_cap.ToXYZA() -
                                              list_xt[0].st_rol_cap.ToXYZA() + xtdwvs.ToXYZA();
                                    if (id==0)pickpos.z = xt.id == 0 ? pickpos.z : -pickpos.z;
                                    if (id == 1) pickpos.z = xt.id == 2 ? pickpos.z : -pickpos.z;
                                    pickpos.a = 0;
                                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG,
                                        VAR.IsChinese ? string.Format("{0}工站取料: X:{1} Y:{2}", xt.disc, pickpos.x, pickpos.y) : string.Format("{0},WS Pickmod:X:{1} Y:{2}      ({0}工站取料: X:{1} Y:{2})", xt.disc, pickpos.x, pickpos.y));
                                REPICK:
                                    res = xt.Pick(ref bquit, pickpos, true, IsDemo, true);
                                    if (res == EM_RES.OK)
                                    {
                                        xt_md.res = WsPickmd[0].res;
                                        xt_md.bardcode = WsPickmd[0].bardcode;
                                        xt_md.Num = WsPickmd[0].Num;
                                        xt_md.WS_ID = WsPickmd[0].WS_ID;
                                        xt_md.PC_ID = WsPickmd[0].PC_ID;
                                        xt.XtMd = xt_md.Clone();
                                        WsPickmd[0].res = -2;
                                        WsPickmd[0].Ishavemd = false;
                                        WsPickmd[0].bardcode = null;
                                    }
                                    else if (res == EM_RES.PICK_ERR)
                                    {
                                        COUNT_DATA.ErrTemp = System.DateTime.Now;
                                        VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "吸料异常!" : "Draw ERR", 20, true);
                                        MT.ST_WARN st_warn = new MT.ST_WARN();
                                        warning fr_warn = new warning();
                                        st_warn.ok_txt = VAR.IsChinese ? "放弃吸取" : "Give up";
                                        st_warn.abort_txt = VAR.IsChinese ? "重新吸取" : "Try again";
                                        st_warn.ws = ws;
                                        st_warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                                        st_warn.msg = VAR.IsChinese ? "提示:吸料异常" : "Tip: Abnormal suction";
                                        st_warn.msg = VAR.IsChinese ? string.Format("{0}下料失败,{1}无法吸起当前工站第{2}模组!", disc, xt.disc,
                                            WsPickmd[0].Num) : string.Format("{0} download filed,{2} can't suck up module {3} of the current station!\r\n{1}下料失败,{2}无法吸起当前工站第{3}模组!", disc, disc, xt.disc,
                                            WsPickmd[0].Num);
                                        st_warn.lb_msg =
                                            VAR.IsChinese ? "提示:" + st_warn.msg + "请确认!\r\n  1.如果工站上没有物料,请按放弃吸取键继续吸下一个物料!\r\n  " +
                                            "2.如有物料请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按重新吸取键则吸嘴再次吸取!" : "Tip:" + st_warn.msg + "\r\nPlease confirm!\r\n  1.If there is no material on the station, please press the Give up button to continue sucking the next material!\r\n  " +
                                                                                                         "2.If there is any material, please press the stop running button to exit the operation. After the ready is displayed in the upper left corner of the interface, press the run button again.!\r\n 3.If the 'Try again' button is pressed, the suction nozzle sucks again!" + "\r\n提示:" + st_warn.msg + "请确认!\r\n  1.如果工站上没有物料,请按放弃吸取键继续吸下一个物料!\r\n  " +
                                                                                                         "2.如有物料请按停止运行键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按重新吸取键则吸嘴再次吸取!";
                                        DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);
                                        if (DialogResult.Cancel == logres) return EM_RES.ERR;
                                        VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                        if (DialogResult.OK == logres)
                                        {
                                            WsPickmd[0].res = -2;
                                            WsPickmd[0].Ishavemd = false;
                                            WsPickmd[0].bardcode = null;
                                            MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                        }
                                        else if (DialogResult.Abort == logres)
                                        {
                                            MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                            goto REPICK;

                                        }
                                        else if (DialogResult.Retry == logres)
                                        {
                                            MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                            goto NEXT;
                                        }


                                    }
                                    else if (res != EM_RES.ABORT) return res;
                                }
                            }
                        }

                        GrrProc = EM_STA.GRR_CAM;
                    }

                    if (GrrProc == EM_STA.GRR_CAM)
                    {
                        //确认是否有可取模组
                        WsPickmd.Clear();
                        GetWsOrderPosTeam(ref WsPickmd, ws, Product.EM_CM_RES.OK);
                        if (WsPickmd.Count == 0) Isfinal = true;

                        //拍照
                        dwData.Clear();
                        for (int i = 0; i < ud.list_xt.Count; i++)
                        {
                            XT xt = ud.list_xt[(i + 1) % 2];
                            if (((xt.cy_zk.isONByChkSen && !IsDemo) || IsDemo) && xt.XtMd != null &&
                                (Isfinal || (!Isfinal && (xt.id == 1&&id==0)||(xt.id==3&&id==1))))
                            {
                                res = MT.ZupMove(ref bquit, ref xt.ax_y, xt.st_cap_pos.y, ref xt.ax_u, 0, true);
                                if (res != EM_RES.OK) return res;
                                Thread.Sleep(50);
                                int xtid1 = 0;
                                if (xt.id == 0 || xt.id ==2) xtid1 = 0;
                                if (xt.id == 1 || xt.id == 3) xtid1 = 1;
                                res = dwcam.FindTaskTriAndWait(CONST.ModDwFw[xtid1], IsDemo);
                                if (res != EM_RES.OK) return res;
                                dwData.Add(dwcam.curTask.ResData);
                            }
                        }

                        GrrProc = EM_STA.PLACEWS;
                    }

                    //放料
                    if (GrrProc == EM_STA.PLACEWS)
                    {
                        for (int i = 0; i < ud.list_xt.Count; i++)
                        {
                            XT xt = ud.list_xt[(i + 1) % 2];
                            if (((xt.cy_zk.isONByChkSen && !IsDemo) || IsDemo) && xt.XtMd != null &&
                                (Isfinal || (!Isfinal && (xt.id == 1 && id == 0) || (xt.id == 3 && id == 1))))
                            {
                                WsPickmd.Clear();
                                GetWsOrderPosTeam(ref WsPickmd, ws);
                                if (WsPickmd.Count > 0)
                                {
                                    res = MT.ZupMove(ref bquit, ref xt.ax_x, WsPickmd[0].st_place_pos[ud.id].x, ref xt.ax_y,
                                        WsPickmd[0].st_place_pos[ud.id].y, true);
                                    if (res != EM_RES.OK) return res;
                                    Thread.Sleep(50);
                                    res = upcam.FindTaskTriAndWait(CONST.WsUpFw, IsDemo);
                                    if (res != EM_RES.OK)
                                    {
                                        COUNT_DATA.ErrTemp = System.DateTime.Now;
                                        VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "拍照错误!" : "Cam ERR", 20, true);
                                        MT.ST_WARN warn = new MT.ST_WARN();
                                        warning fr_warn = new warning();
                                        warn.ok_txt = VAR.IsChinese ? "确定" : "OK";
                                        warn.ws = null;
                                        warn.title = VAR.IsChinese ? "提示:拍照错误" : "Tip:Failed to take a picture";
                                        warn.msg = VAR.IsChinese ? string.Format("工站位置{0}重拍失败,请检查对应的工站位置是否有异物!", WsPickmd[0].Num) : string.Format("Workstation location {0} failed to retake, please check if there is a foreign object in the corresponding station \r\n工站位置{0}重拍失败,请检查对应的工站位置是否有异物!", WsPickmd[0].Num);
                                        warn.lb_msg =
                                            VAR.IsChinese ? string.Format("提示:工站位置{0}重拍失败,请检查对应的工站位置是否有异物或物料，按确定键停止,确认无异物后再按运行键继续!",
                                                WsPickmd[0].Num) : string.Format("Tip: Workstation location {0} failed to retake. Please check whether there is any foreign object or material in the corresponding station location. Press the OK key to stop. After confirming that there are no foreign objects, press the Run key to continue!\r\n提示:工站位置{0}重拍失败,请检查对应的工站位置是否有异物或物料，按确定键停止,确认无异物后再按运行键继续!",
                                                WsPickmd[0].Num);
                                        MT.Display_frwarn(fr_warn, warn, ERR_ALM.EmErrItem.CaptureAbnomal);
                                        return EM_RES.ERR;
                                    }

                                    res = xt.XtPickOrPlaceMod(ref bquit, WsPickmd[0].st_place_pos[ud.id].ToXY(),
                                        upcam.curTask.ResData.PosMM, dwData[i].PosMM, WsPickmd[0].st_place_pos[ud.id].z, Demo,
                                        XT.EM_XTFLOW.PLACEMOD, false, PT_SET.bModPasteUp);
                                    if (res == EM_RES.OK)
                                    {
                                        WsPickmd[0].bardcode = xt.XtMd.bardcode;
                                        WsPickmd[0].res = -1;
                                        xt.XtMd = null;
                                    }
                                    else return res;

                                }
                                else break;
                            }
                        }
                        GrrProc = EM_STA.PICKWS;
                    }
                    //判断是否完成
                    WsPickmd.Clear();
                    GetWsOrderPosTeam(ref WsPickmd, ws);
                    if (WsPickmd.Count == 0)
                    {
                        GrrProc = EM_STA.PICKWS;
                        break;
                    }
                }
                else
                {
                  ws.ResetResultOfMd();
                }
                
                return EM_RES.OK;
            }
            return EM_RES.OK;
        }
        #endregion

        #region 上下料动作

        public static void DelateImage()
        {
            string folderPath1 = string.Format("{0}\\product\\{1}\\image\\CamUp1", Path.GetFullPath(".."), VAR.gsys_set.cur_product_name);
            string folderPath2 = string.Format("{0}\\product\\{1}\\image\\CamUp2", Path.GetFullPath(".."), VAR.gsys_set.cur_product_name);
            string folderPath3 = string.Format("{0}\\product\\{1}\\image\\CamDw1", Path.GetFullPath(".."), VAR.gsys_set.cur_product_name);
            string folderPath4 = string.Format("{0}\\product\\{1}\\image\\CamDw2", Path.GetFullPath(".."), VAR.gsys_set.cur_product_name);

            // 设置要处理的文件夹路径  
            //@"C:\example\folder";

            // 获取文件夹中的所有文件  
            DirectoryInfo directoryInfo1 = new DirectoryInfo(folderPath1);
            DirectoryInfo directoryInfo2 = new DirectoryInfo(folderPath2);
            DirectoryInfo directoryInfo3 = new DirectoryInfo(folderPath3);
            DirectoryInfo directoryInfo4 = new DirectoryInfo(folderPath4);


            List<DirectoryInfo> files = new List<DirectoryInfo>();
            files.Add(directoryInfo1);
            files.Add(directoryInfo2);
            files.Add(directoryInfo3);
            files.Add(directoryInfo4);

            foreach (DirectoryInfo directoryInfo in files)
            {
                DirectoryInfo[] subDirectories = directoryInfo.GetDirectories();
                if (!directoryInfo.Exists)
                {
                    //  Console.WriteLine("文件夹不存在。");
                    continue;
                }
                foreach (DirectoryInfo subDirectory in subDirectories)
                {
                    DateTime folderCreationDate = subDirectory.CreationTime;
                    DateTime fiveDaysAgo = DateTime.Now.AddDays(-5);

                    if (folderCreationDate < fiveDaysAgo)
                    {
                        try
                        {
                            subDirectory.Delete(true); // 删除文件夹及其内容  
                            Console.WriteLine($"已删除文件夹: {subDirectory.FullName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"删除文件夹时出错: {subDirectory.FullName}\n{ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 上料动作
        /// </summary>
        /// <param name="bquit"></param>
        /// <param name="wsfeed">上下料位置工装</param>
        /// <returns></returns>
        public EM_RES UpDownLoadModleAct(ref bool bquit, WSFEED wsfeed)
        {
            bfinal = false;
            bflycam = false;
            double FeedBackPos_temp=0;
            string msg;
            int num = 10000, num_1 = 10000, num_2 = 10000, num_3 = 10000, num_4 = 10000;
            EM_RES res = EM_RES.OK;
            bool bChkWait = false;
            int msg_outcnt = 0;
            EM_STA Proc1 = EM_STA.CHECK;
            traybox_fd.MoveToLastPos[id] = true;
            traybox_ng.MoveToLastPos[id] = true;
            traybox_ok.MoveToLastPos[id] = true;
            //取料
            double tick = Environment.TickCount;
            while (!bquit && !bfinal)
            {
                if (COM.wsfeed1.isclearfinsh && COM.wsfeed2.isclearfinsh && VAR.ClearMt)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("清料完成!"));
                    VAR.ClearMt = false;
                    break;
                }

                wsfeed.breschanged = true;
                if (wsfeed.FeedStatus != WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD && !Demo)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}上料时，状态异常，{1}!", wsfeed.disc, wsfeed.FeedStatus.ToString()));
                    return EM_RES.ERR;
                }
                if (bquit)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}线程退出->系统强制退出!", disc));
                    return EM_RES.QUIT;
                }

                //检测工站目前状态(是取还是放)
                if (Proc == EM_STA.CHECK)
                {
                    if (feedbackBatch.HasPending)
                    {
                        res = RecoverPendingFeedbackBeforeNextAction(ref bquit, wsfeed);
                        if (res != EM_RES.OK) return res;
                        Proc = EM_STA.CHECK;
                        continue;
                    }

                    if (FindMod != EM_FIN_MOD.NONE)
                    {
                        if (!PT_SET.bGrrFlow || (PT_SET.bGrrFlow && PT_SET.GRRFirst || VAR.ClearMt))
                        {
                            Proc = ChkNextProc(ref num, ref num_3, wsfeed, id);
                            #region 原先之前的防护
                            //if (FindMod == EM_FIN_MOD.LOW && id == COM.UDLoad2.id)//原先的安全防护
                            //{
                            //    Proc1 = ChkNextProc(ref num_1, ref num_4, wsfeed, 0);
                            //    if (num_1 < 10000)
                            //        num_1 = num_1 % 4;

                            //    bChkWait = false;
                            //    num_2 = 20000;
                            //    if (COM.UDLoad1.FeedBackWs != null && COM.UDLoad1.FeedBackWs.Count > 0 && Proc1 != EM_STA.UPDOWNLOADEND)
                            //    {
                            //        num_2 = (COM.UDLoad1.FeedBackWs[0].Num - 1) % 4 % 2;
                            //    }
                            //    else
                            //    {
                            //        if (num_3 < 10000)
                            //        {
                            //            if (Math.Abs(wsfeed.list_md[num_3 - 1].st_place_pos[1].x) + Math.Abs(COM.UDLoad1.ax_x.fenc_pos) + MT.xsafedis > DisAxisX) bChkWait = true;
                            //        }
                            //    }
                            //    if (Proc != EM_STA.UPDOWNLOADEND &&
                            //        //((!VAR.ClearMt && num_1 == num && Proc >= COM.UDLoad1.Proc )||(VAR.ClearMt && num_1 == num) || num_1 < num || ((!VAR.ClearMt || (VAR.ClearMt && !PT_SET.issmall)) && num - num_2 > 1) || bChkWait))
                            //        ((!VAR.ClearMt && num_1 == num && Proc >= COM.UDLoad1.Proc) || (VAR.ClearMt && num_1 == num) || num_1 < num || num - num_2 > 1 || bChkWait || (Proc1 == EM_STA.UPDOWNLOADEND && !COM.UDLoad1.bfinal)))
                            //    {
                            //        msg_outcnt++;
                            //        if (msg_outcnt > 30)
                            //        {
                            //            msg_outcnt = 0;
                            //            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, VAR.IsChinese ? string.Format("0->Proc:{0},Num_C:{1},Num_O:{2},FeedbackNum:{3},1-> Proc{4},Num_C:{5},Num_O{6},模块2等待3S", COM.UDLoad1.Proc, num_1, num_4, num_2, Proc, num, num_3) : string.Format("0->Proc:{0},Num_C:{1},Num_O:{2},FeedbackNum:{3},1-> Proc{4},Num_C:{5},Num_O{6},UpDwload2 waited 3s.               (0->Proc:{0},Num_C:{1},Num_O:{2},FeedbackNum:{3},1-> Proc{4},Num_C:{5},Num_O{6},模块2等待3S)", COM.UDLoad1.Proc, num_1, num_4, num_2, Proc, num, num_3));
                            //        }

                            //        Proc = EM_STA.CHECK;
                            //        Thread.Sleep(100);
                            //        continue;
                            //    }
                            //    msg_outcnt = 0;
                            //}
                            #endregion 
                        }
                        else
                        {
                            Proc = ChkGrrNextProc(wsfeed, id);
                        }
                    }

                    if (Proc == EM_STA.UPDOWNLOADEND || FindMod == EM_FIN_MOD.NONE)
                    {
                        if (PT_SET.BCamcfgset)
                            Camcfgset(false);
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}工站放料完成->系统归位!", disc));
                        Proc = EM_STA.CHECK;
                        if (FindMod != EM_FIN_MOD.NONE)
                        {
                            ST_XYZA enpos = new ST_XYZA();
                            res = FlyCamToLastPos(ref bquit, wsfeed, ref enpos);
                            if (res != EM_RES.OK && res != EM_RES.NEXT&& res != EM_RES.GIVEWAY) return res;
                        }
                        //归位
                        bUpdateFBPos_UT = false;
                        bUpdateFBPos_OKNG = false;
                        FeedBackPos.x = 0;
                        FeedBackPos.y = 0;
                        res = GoZero(ref bquit, wsfeed, false);
                        if (res != EM_RES.OK) return res;
                        if (PT_SET.bGrrFlow && ((id == 0 && PT_SET.GRRWSID == 1) || (id == 1 && PT_SET.GRRWSID == 2))) PT_SET.GRRFirst = false;
                        if (!VAR.ClearMt && wsfeed.GrrTestCnt >= PT_SET.GRRTestCnt && PT_SET.bGrrFlow && ((id == 0 && PT_SET.GRRWSID == 1) || (id == 1 && PT_SET.GRRWSID == 2)))
                        {
                            wsfeed.ResetResultOfMd();
                            wsfeed.GrrTestCnt = 0;
                        }
                        bfinal = true;
                        if (id == 0 && wsfeed.id == 0)
                        {
                            UpDownLoad.ud2needwait = false;
                            UpDownLoad.ud2ws2needwait = false;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}归位完成，上下料2等待释放-----", disc));
                        }
                        if (id == 1 && wsfeed.id == 0)
                        {
                            UpDownLoad.ud1needwait = false;
                            UpDownLoad.ud1ws3needwait = false;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}归位完成，上下料1等待释放-----", disc));
                        }
                        break;
                    }
                    if (FindMod != EM_FIN_MOD.ALL || FindMod != EM_FIN_MOD.NONE)
                    {

                        if (FeedBackPos_temp != FeedBackPos.x)
                        {
                            FeedBackPos_temp = FeedBackPos.x;
                            double temppos = 0;
                            if (FindMod == EM_FIN_MOD.HIGH) temppos = FeedBackPos.x - 40;//避让40mm，模块1靠右，模块2靠左
                            else if (FindMod == EM_FIN_MOD.LOW) temppos = FeedBackPos.x + 40;
                            else temppos = FeedBackPos.x;
                            TrayGoPos = temppos;
                        }
                        else TrayGoPos = 0;

                    }
                    else TrayGoPos = 0;

                }

                //GRR测试流程
                if (Proc == EM_STA.GRRTEST && !bquit)
                {
                    res = GRRFlow(ref bquit, ref wsfeed, this, Demo);
                    if (res == EM_RES.OK) Proc = EM_STA.CHECK;
                    else return res;
                }
                //工站取料
                lock (pickws)
                {
                    if (Proc == EM_STA.PICKWS && !bquit)
                    {
                        #region 取放料保护
                        res = SafeProtect(ref bquit);
                        if (res != EM_RES.OK && res != EM_RES.GIVEWAY) return res;
                        else if (res == EM_RES.GIVEWAY)
                        {
                            Proc = EM_STA.CHECK;
                        }
                        else if (res == EM_RES.OK)
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();

                            if (PT_SET.BCamcfgset)
                                Camcfgset(false);

                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}工站取料进入!", disc));
                            res = PickWsMod(ref bquit, ref wsfeed);
                            sw.Stop();
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}工站取料，耗时{1:F}s-----", disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds / 1000, 3))));
                            if (res == EM_RES.ERR || res == EM_RES.SAFE_PROTECT|| res == EM_RES.MOVE_ERR)
                            {
                                return res;

                            }
                            else
                            {
                                if (((list_xt[0].cy_zk.isONByChkSen || list_xt[1].cy_zk.isONByChkSen) && !Demo) || Demo)
                                {
                                    Proc = EM_STA.PLACETRAY;
                                }
                                else Proc = EM_STA.CHECK;
                            }
                        }

                        #endregion

                    }
                }
                //料盘放料
                lock (placetray)
                {
                    if (Proc == EM_STA.PLACETRAY && !bquit)
                    {
                        if (PT_SET.BCamcfgset)
                            Camcfgset(false);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}料盘放料进入", disc));
                        res = PlaceMdToTray(ref bquit, wsfeed);
                        sw.Stop();
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}料盘放料，耗时{1:F}s-----", disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds / 1000, 3))));
                        if (res != EM_RES.OK && res != EM_RES.GIVEWAY) return res;
                        else if (res == EM_RES.GIVEWAY)
                        {
                            Proc = EM_STA.CHECK;
                        }
                        else
                        {
                            if (!VAR.ClearMt)
                            {
                                Proc = EM_STA.PICKTRAY;
                                bflycam = true;
                            }
                            else
                            {
                                Proc = EM_STA.CHECK;
                                bflycam = false;

                            }
                        }
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}料盘放料退出!", disc));
                    }
                }

                lock (picktray)
                {
                    //飞拍失败放回
                    if (Proc == EM_STA.FLYERRPLACE && !bquit)
                    {
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        SQLData.TesterrDataAdd(99, 99, "工站放料", string.Format("{0}模组飞拍失败放回", disc));
                        COUNT_DATA.Errcnt++;
                        res = SafeProtectws23(ref bquit);
                        if (res != EM_RES.OK)
                        {
                            return res;
                        }
                        res = DwCamErrPalceBack(ref bquit);
                        sw.Stop();
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}飞拍失败放回，耗时{1:F}s-----", disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds / 1000, 3))));
                        if (res == EM_RES.OK)
                        {
                            ErrTryPickCnt++;
                            if (ErrTryPickCnt > 1)
                            {
                                ErrTryPickCnt = 0;
                                msg = string.Format("重取物料,{0}飞拍失败,请确认原因!", dwcam.disc);

                                VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "飞拍失败!" : "Draw ERR", 30, true);
                                MT.ST_WARN st_warn = new MT.ST_WARN();
                                warning fr_warn = new warning();
                                st_warn.ok_txt = VAR.IsChinese ? "确定" : "ok";
                                st_warn.abort_txt = VAR.IsChinese ? "确定" : "abort";
                                st_warn.ws = null;
                                st_warn.cancle_txt = VAR.IsChinese ? "取消" : "cancle";
                                st_warn.title = VAR.IsChinese ? "提示:重取物料,{0}飞拍失败,请确认原因!!" : "Tip: Abnormal suction!";
                                st_warn.msg = string.Format("{0}重取物料,{0}飞拍失败,请确认原因! 请确认后按下启动键!", dwcam.disc);
                                st_warn.lb_msg = "提示:" + st_warn.msg;
                                DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);

                                if (DialogResult.Cancel == logres) return EM_RES.ERR;
                                //VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                if (DialogResult.OK == logres)
                                {
                                    return EM_RES.ERR;
                                }
                                else if (DialogResult.Abort == logres)
                                {
                                    return EM_RES.ERR;
                                }
                                else if (DialogResult.Retry == logres)
                                {
                                    return EM_RES.ERR;
                                }
                            }
                            else
                            {
                                msg = string.Format("{0}飞拍失败,重取物料!", dwcam.disc);
                                Proc = EM_STA.PICKTRAY;

                            }
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, msg);
                        }

                        if (res != EM_RES.OK) return res;
                    }
                    //料盘取料
                    if (Proc == EM_STA.PICKTRAY && !bquit)
                    {
                        // GetWsPosTeam(ref CurPlaceMod, wsfeed, Product.EM_CM_RES.EMPTY);
                        if (PT_SET.BCamcfgset)
                            Camcfgset(true);
                        GetWsPosTeam(ref CurPlaceMod, wsfeed, Product.EM_CM_RES.EMPTY, FindMod);
                        foreach (var ws in CurPlaceMod)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, $"{FindMod}模式下{wsfeed.id}当前取料{ws.Num}");
                        }
                        if (CurPlaceMod.Count != 0 && !VAR.ClearMt)
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}料盘取料进入!", disc));
                            res = PickMdToTray(ref bquit, ref bflycam, wsfeed);
                            sw.Stop();
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}料盘取料，耗时{1:F}s-----", disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds / 1000, 3))));
                            if (res != EM_RES.OK && res != EM_RES.GIVEWAY) return res;
                            else if (res == EM_RES.GIVEWAY)
                            {
                                Proc = EM_STA.CHECK;
                            }
                        }
                        if (((list_xt[0].cy_zk.isONByChkSen || list_xt[1].cy_zk.isONByChkSen) && !Demo) || Demo)
                        {
                            Proc = EM_STA.PLACEWS;

                        }
                        else Proc = EM_STA.CHECK;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}料盘取料退出!", disc));
                    }
                }
                //工站放料
                lock (placews)
                {

                    if (Proc == EM_STA.PLACEWS && !bquit)
                    {
                        #region 取放料保护
                        res = SafeProtect(ref bquit);
                        if (res != EM_RES.OK && res != EM_RES.GIVEWAY) return res;
                        else if (res == EM_RES.GIVEWAY)
                        {
                            Proc = EM_STA.CHECK;
                        }
                        else if (res == EM_RES.OK)
                        {
                            if (PT_SET.BCamcfgset)
                                Camcfgset(false);
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}工站放料进入!", disc));

                            GetWsPosTeam(ref CurPlaceMod, wsfeed, Product.EM_CM_RES.EMPTY, FindMod);
                            if (CurPlaceMod.Count != 0)
                            {
                                bool needsafe = false;
                                if ((id == 0 && (CurPlaceMod[0].Num == 3 || CurPlaceMod[0].Num == 7)) || (id == 1 && (CurPlaceMod[0].Num == 2 || CurPlaceMod[0].Num == 6)))
                                {
                                    res = SafeProtectws23(ref bquit);
                                    if (res != EM_RES.OK && res != EM_RES.GIVEWAY) return res;
                                    else if (res == EM_RES.GIVEWAY)
                                    {
                                        Proc = EM_STA.CHECK;
                                        needsafe = true;
                                    }
                                }
                                #region 料盘提前定位 （适用于上锁的情况）
                                //新增内容
                                Task tsk_fd = new Task(() =>
                                {
                                    if (id == 0)
                                    {
                                        EM_RES res_fd = MoveNextTrayX1Pos(ref UpDownLoad.bquit, traybox_fd, wsfeed, COM.UDLoad2);
                                        if (res_fd != EM_RES.OK)
                                        {
                                            UpDownLoad.bquit = true;
                                            if (res == EM_RES.MOVE_ERR || res == EM_RES.MOVE_TIMEOUT || res == EM_RES.MOVE_PARA_ERR)
                                                VAR.gsys_set.bquit = true;
                                        }
                                    }
                                    else if (id == 1 && wsfeed == COM.wsfeed1)
                                    {
                                        EM_RES res_fd = MoveNextTrayX1Pos(ref UpDownLoad.bquit, traybox_fd, COM.wsfeed2, COM.UDLoad1);
                                        if (res_fd != EM_RES.OK)
                                        {
                                            UpDownLoad.bquit = true;
                                            if (res == EM_RES.MOVE_ERR || res == EM_RES.MOVE_TIMEOUT || res == EM_RES.MOVE_PARA_ERR)
                                                VAR.gsys_set.bquit = true;
                                        }
                                    }
                                    else if (id == 1 && wsfeed == COM.wsfeed2)
                                    {
                                        //  EM_RES res_fd = MoveNextTrayX1Pos(ref UpDownLoad.bquit, traybox_fd, COM.wsfeed1, COM.UDLoad1);
                                        //if (res_fd != EM_RES.OK)
                                        //{
                                        //    UpDownLoad.bquit = true;
                                        //    if (res == EM_RES.MOVE_ERR || res == EM_RES.MOVE_TIMEOUT || res == EM_RES.MOVE_PARA_ERR)
                                        //        VAR.gsys_set.bquit = true;
                                        //}
                                    }

                                });
                                tsk_fd.Start();


                                //新增内容
                                Task tsk_ng = new Task(() =>
                                {
                                    if (id == 0)
                                    {
                                        EM_RES res_ng = MoveNextTrayX1Pos(ref UpDownLoad.bquit, traybox_ng, wsfeed, COM.UDLoad2);
                                        if (res_ng != EM_RES.OK)
                                        {
                                            UpDownLoad.bquit = true;
                                            if (res == EM_RES.MOVE_ERR || res == EM_RES.MOVE_TIMEOUT || res == EM_RES.MOVE_PARA_ERR)
                                                VAR.gsys_set.bquit = true;
                                        }
                                    }
                                    else if (id == 1 && wsfeed == COM.wsfeed1)
                                    {
                                        EM_RES res_ng = MoveNextTrayX1Pos(ref UpDownLoad.bquit, traybox_ng, COM.wsfeed2, COM.UDLoad1);
                                        if (res_ng != EM_RES.OK)
                                        {
                                            UpDownLoad.bquit = true;
                                            if (res == EM_RES.MOVE_ERR || res == EM_RES.MOVE_TIMEOUT || res == EM_RES.MOVE_PARA_ERR)
                                                VAR.gsys_set.bquit = true;
                                        }
                                    }
                                    else if (id == 1 && wsfeed == COM.wsfeed2)
                                    {
                                        //EM_RES res_ng = MoveNextTrayX1Pos(ref UpDownLoad.bquit, traybox_ng, COM.wsfeed1, COM.UDLoad1);
                                        //if (res_ng != EM_RES.OK)
                                        //{
                                        //    UpDownLoad.bquit = true;
                                        //    if (res == EM_RES.MOVE_ERR || res == EM_RES.MOVE_TIMEOUT || res == EM_RES.MOVE_PARA_ERR)
                                        //        VAR.gsys_set.bquit = true;
                                        //}
                                    }

                                });
                                tsk_ng.Start();

                                Task tsk_ok = new Task(() =>
                                {
                                    if (id == 0)
                                    {
                                        EM_RES res_ok = MoveNextTrayX1Pos(ref UpDownLoad.bquit, traybox_ok, wsfeed, COM.UDLoad2);
                                        if (res_ok != EM_RES.OK)
                                        {
                                            UpDownLoad.bquit = true;
                                            if (res == EM_RES.MOVE_ERR || res == EM_RES.MOVE_TIMEOUT || res == EM_RES.MOVE_PARA_ERR)
                                                VAR.gsys_set.bquit = true;
                                        }
                                    }
                                    else if (id == 1 && wsfeed == COM.wsfeed1)
                                    {
                                        EM_RES res_ok = MoveNextTrayX1Pos(ref UpDownLoad.bquit, traybox_ok, COM.wsfeed2, COM.UDLoad1);
                                        if (res_ok != EM_RES.OK)
                                        {
                                            UpDownLoad.bquit = true;
                                            if (res == EM_RES.MOVE_ERR || res == EM_RES.MOVE_TIMEOUT || res == EM_RES.MOVE_PARA_ERR)
                                                VAR.gsys_set.bquit = true;
                                        }
                                    }
                                    else if (id == 1 && wsfeed == COM.wsfeed2)
                                    {
                                        //EM_RES res_ok = MoveNextTrayX1Pos(ref UpDownLoad.bquit, traybox_ok, COM.wsfeed1, COM.UDLoad1);
                                        //if (res_ok != EM_RES.OK)
                                        //{
                                        //    UpDownLoad.bquit = true;
                                        //    if (res == EM_RES.MOVE_ERR || res == EM_RES.MOVE_TIMEOUT || res == EM_RES.MOVE_PARA_ERR)
                                        //        VAR.gsys_set.bquit = true;
                                        //}
                                    }

                                });
                                tsk_ok.Start();
                                #endregion

                                if (CurPlaceMod.Count != 0 && !needsafe)
                                {

                                    Stopwatch sw = new Stopwatch();
                                    sw.Start();
                                    res = PlaceMdToWs(ref bquit, CurPlaceMod, wsfeed);
                                    sw.Stop();
                                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}工站放料，耗时{1:F}s-----", disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds / 1000, 3))));
                                    if (res == EM_RES.OK)
                                    {
                                        Proc = EM_STA.CHECK;
                                        bflycam = true;
                                    }
                                    else if (res == EM_RES.CAM_ERR_PLACE_BACK)
                                    {
                                        bflycam = false;
                                        Proc = EM_STA.FLYERRPLACE;
                                    }
                                    else if (res == EM_RES.RETRY)
                                    {
                                        Proc = EM_STA.CHECK;
                                        bflycam = false;
                                    }
                                    else if (res == EM_RES.GIVEWAY)
                                    {
                                        Proc = EM_STA.CHECK;
                                    }
                                    //else if (res == EM_RES.MOTOR_CAM_ERR_PLACE_BACK)
                                    //{
                                    //    bflycam = false;
                                    //    Proc = EM_STA.MOTORCAMERRPLACE;
                                    //}
                                    else if (res != EM_RES.OK && res != EM_RES.GIVEWAY) return res;
                                }
                                else
                                {
                                    Proc = EM_STA.CHECK;
                                }
                            }
                            #endregion
                        }
                    }

                }
            }           
            Ct_ud = (Environment.TickCount - tick) / 1000.0;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}完成退出用时{1:F3}s",disc, Ct_ud));           
            return EM_RES.OK;
        }      
        #endregion

        #region  上下料线程
        public Task UpDownTask = null;
        public Task UpDownClearTask = null;
        public  bool brun_ud = false;
        public  bool brun_clearud = false;
        public  double Ct_ud = 0;
        public  void RunUdTask()
        {
            if (UpDownTask == null || UpDownTask != null && UpDownTask.IsCompleted)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, string.Format("创建{0}线程!",disc));
                if (UpDownTask != null) UpDownTask.Dispose();
                UpDownTask = new Task(UdLoad);
                brun_ud = true;
                UpDownTask.Start();
            }
        }

        public  EM_RES WaitUdTask(ref bool bequit)
        {
            if (UpDownTask == null) return EM_RES.PARA_ERR;
            Thread.Sleep(100);
            int cnt = 0;
            while (brun_ud)
            {
                Thread.Sleep(10);
                Application.DoEvents();
                if (UpDownTask.IsCompleted) break;
                if (bequit||VAR.gsys_set.bquit)
                {
                    if (cnt++ > 200)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, string.Format("等待{0}线程停止超时2S!", disc));
                        status_ud = EM_STA.ERR;
                        break;
                    }                  
                }
              
            }

            if (status_ud == EM_STA.READY)
                return EM_RES.OK;
            else
                return EM_RES.ERR;
        }

        public void UdLoad()
        {
            EM_RES res = EM_RES.OK;
            WSFEED wsfeed = null;
            if (status_ud == EM_STA.UNKNOW || status_ud == EM_STA.HOME)
            {
                brun_ud = false;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}没有复位,禁止上下料!", disc));
            }
            else
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}线程开始!", disc));

                if (PT_SET.UpDownRunMode == (int)PT_SET.RUN_MD.ALONE_WORK)
                {
                    wsfeed = COM.wsfeed1.GetWSOnFeedPosAlone(id);
                }
                else
                {
                    //if (Demo) wsfeed =COM.wsfeed1;//没有测试机的情况下
                    //else
                    //{
                        wsfeed = WSFEED.GetWSOnFeedPos;
                    //}

                }              
                if (((PT_SET.UpDownRunMode == (int)PT_SET.RUN_MD.ALONE_WORK) && ((id == 0 && wsfeed.id == 1)|| (id == 1&& wsfeed.id == 0)))||wsfeed==null)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}单独工作模式下上下料对测试工位不同，不进行取放料!", disc));
                    //brun_ud = false;
                }
                else
                {
                    res = UpDownLoadModleAct(ref bquit, wsfeed);
                    if (res != EM_RES.OK)
                    {
                        if (res == EM_RES.QUIT) status_ud = EM_STA.QUIT;
                        else status_ud = EM_STA.ERR;
                        MT.BeQuitEn(true);
                        if (res == EM_RES.MOVE_ERR || res == EM_RES.MOVE_TIMEOUT || res == EM_RES.MOVE_PARA_ERR)
                            MT.BeQuitEn(true, true);
                    }
                    else
                    {
                        status_ud = EM_STA.READY;
                    }
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}线程结束!", disc));
                    brun_ud = false;
                }
                
            }
            
        }

        #endregion
     
        #region 总上下料线程
        public static Task UpDownLoadTask = null;
        public static Task UpDownLoadClearTask = null;
        public static bool brun = false;
        public static bool bquit = false;
        public static double Ct = 0;
        public static EM_STA status = EM_STA.UNKNOW;
               
        public static void RunTask()
        {
            if (UpDownLoadTask == null || UpDownLoadTask != null && UpDownLoadTask.IsCompleted)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, string.Format("创建上下料线程!"));
                if (UpDownLoadTask != null) UpDownLoadTask.Dispose();
                UpDownLoadTask = new Task(UpDownload);
                brun = true;
                UpDownLoadTask.Start();
                status = EM_STA.BUSY;
            }
        }

        public static EM_RES WaitTask(ref bool bequit)
        {
            if (UpDownLoadTask == null) return EM_RES.PARA_ERR;

            while (!bequit)
            {
                Thread.Sleep(10);
                Application.DoEvents();
                if (UpDownLoadTask.IsCompleted) break;
                if (!brun) break;
            }

            if (status == EM_STA.READY)
                return EM_RES.OK;
            else if (status == EM_STA.QUIT)
                return EM_RES.QUIT;
            else
                return EM_RES.ERR;
        }

        public static void UpDownload()
        {
            EM_RES res = EM_RES.OK;
            EM_RES res1 = EM_RES.OK;
            EM_RES res2 = EM_RES.OK;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("上下料线程开始!"));
            bquit = false;
            while (!VAR.gsys_set.bquit && bquit == false)
            {
                MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                status = EM_STA.WAIT;
                brun = true;

                #region 单独运行模式
                if (PT_SET.UpDownRunMode == (int)PT_SET.RUN_MD.ALONE_WORK)
                {
                    WSFEED wsfeedleft = WSFEED.GetWsfeed2FeedPos;
                    WSFEED wsfeedright = WSFEED.GetWsfeed1FeedPos;
                    if (wsfeedleft == null && wsfeedright == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    #region 上下料模块2运行
                    if (wsfeedleft != null && wsfeedleft.FeedStatus == WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD)
                    {
                        if (wsfeedleft.FeedStatus != WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD)
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        //检查当前上料模块的状态
                        if (wsfeedleft.TestStatus == WSFEED.EM_TEST_STA.ERROR)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("上料线程检测到{0}状态异常!", wsfeedleft.disc));
                            status = EM_STA.ERR;
                            break;
                        }
                        //确保当前工站准备好上料
                        if (!wsfeedleft.isInFeedPos)
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        Thread.Sleep(100);
                        //上下料动作
                        status = EM_STA.UPDOWNLOADEND;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}开始上下料...", wsfeedleft.disc));
                        int tick1 = Environment.TickCount;
                        wsfeed2cangotest = false;
                        wsfeedleft.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD;
                        COM.UDLoad2.RunUdTask();
                        Thread.Sleep(100);
                        res1 = COM.UDLoad2.WaitUdTask(ref bquit);
                        bWaitforUpDownload = true;
                        SetCalResult();
                        while (bWaitforUpDownload && !bquit && !VAR.gsys_set.bquit)
                        {
                            Thread.Sleep(100);
                        }
                        if (res1 != EM_RES.OK || bquit || VAR.gsys_set.bquit)
                        {
                            wsfeedleft.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD;
                            break;
                        }

                        //此处加一个标志位
                        //if (!VAR.ClearMt) wsfeedleft.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORTEST;//20210914屏蔽
                        int clearcnt = 0;
                        int benablecnt = 0;
                        bool cangotest = false;
                        foreach (WSFEED.MdDat md in wsfeedleft.list_md)
                        {
                            if (!md.benable) continue;
                            benablecnt++;
                            if (md.res == -1) clearcnt++;
                        }
                        if (clearcnt > 0)
                        {
                            cangotest = true;
                        }
                        //上完闭合
                        if (wsfeedleft.id == 1&& benablecnt > 0 && (VAR.ClearMt == false || cangotest))
                        {
                            wsfeed2cangotest = true;
                            cangotest = false;
                            //  if (PT_SET.turnon) wsfeed1openchart = true;
                        }
                        if (COM.wsfeed1.isclearfinsh && COM.wsfeed2.isclearfinsh && VAR.ClearMt)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("清料完成!"));
                            VAR.ClearMt = false;
                            break;
                        }
                        //到达后对待测模组状态进行复位更新
                        int cnt1= 0;
                        foreach (WSFEED.MdDat md in wsfeedleft.list_md)
                        {
                            if (!md.benable) continue;
                            if (md.res == -1) cnt1++;
                        }
                        if (cnt1 > 0)
                        {
                            //复位测试结果
                            //  wsfeed.ResetResultOfMd();//用于正常状态    
                            // wsfeed.ResetResultOfMdTest();//用于没有测试机台的状态                  
                            //更新物料状态
                            wsfeedleft.TestStatus = WSFEED.EM_TEST_STA.UNTEST;
                        }
                        else
                        {
                            wsfeedleft.TestStatus = WSFEED.EM_TEST_STA.EMPTY;
                            wsfeedleft.isclearfinsh = true;
                        }
                        //计时开始
                        wsfeedleft.tmr = System.Environment.TickCount;
                        //通知测试
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} 通知REDAYFORTEST", wsfeedleft.disc));
                        wsfeedleft.FeedStatus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORTEST;
                        wsfeedleft.testprostaus = WSFEED.EM_PC_STA.READY;
                        //  wsfeed.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORTEST;
                        Ct = (Environment.TickCount - tick1) / 1000.0;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("上下料模块2完成用时{0:F3}s", Ct));
                        if (res != EM_RES.OK || res1 != EM_RES.OK)
                        {
                            if ((res == EM_RES.OK || res == EM_RES.QUIT) && (res1 == EM_RES.QUIT || res1 == EM_RES.OK))
                                status = EM_STA.QUIT;
                            else
                                status = EM_STA.ERR;
                        }
                        else
                        {
                            status = EM_STA.READY;
                        }
                        brun = false;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("上下料模块2线程结束!"));
                    }
                    #endregion 

                    #region 上下料模块1运行
                    if (wsfeedright != null)
                    {
                        if (wsfeedright.FeedStatus != WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD)
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        //检查当前上料模块的状态
                        if (wsfeedright.TestStatus == WSFEED.EM_TEST_STA.ERROR)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("上料线程检测到{0}状态异常!", wsfeedright.disc));
                            status = EM_STA.ERR;
                            break;
                        }
                        //确保当前工站准备好上料
                        if (!wsfeedright.isInFeedPos)
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        Thread.Sleep(100);
                        //上下料动作
                        status = EM_STA.UPDOWNLOADEND;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}开始上下料...", wsfeedright.disc));
                        int tick2 = Environment.TickCount;
                        wsfeed1cangotest = false;
                        wsfeedright.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD;
                        COM.UDLoad1.RunUdTask();
                        Thread.Sleep(100);
                        res2 = COM.UDLoad1.WaitUdTask(ref bquit);
                        bWaitforUpDownload = true;
                        SetCalResult();
                        while (bWaitforUpDownload && !bquit && !VAR.gsys_set.bquit)
                        {
                            Thread.Sleep(100);
                        }
                        if (res2 != EM_RES.OK || bquit || VAR.gsys_set.bquit)
                        {
                            wsfeedright.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD;
                            break;
                        }

                        //此处加一个标志位
                        //if (!VAR.ClearMt) wsfeedright.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORTEST;//20210914屏蔽
                        int clearcnt = 0;
                        int benablecnt = 0;
                        bool cangotest = false;
                        foreach (WSFEED.MdDat md in wsfeedright.list_md)
                        {
                            if (!md.benable) continue;
                            benablecnt++;
                            if (md.res == -1) clearcnt++;
                        }
                        if (clearcnt > 0)
                        {
                            cangotest = true;
                        }
                        //上完闭合
                        if (wsfeedright.id == 0 && benablecnt > 0 && (VAR.ClearMt == false || cangotest))
                        {
                            wsfeed1cangotest = true;
                            cangotest = false;
                            //  if (PT_SET.turnon) wsfeed1openchart = true;
                        }
                        if (COM.wsfeed1.isclearfinsh && COM.wsfeed2.isclearfinsh && VAR.ClearMt)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("清料完成!"));
                            VAR.ClearMt = false;
                            break;
                        }
                        //到达后对待测模组状态进行复位更新
                        int cnt2 = 0;
                        foreach (WSFEED.MdDat md in wsfeedright.list_md)
                        {
                            if (!md.benable) continue;
                            if (md.res == -1) cnt2++;
                        }
                        if (cnt2 > 0)
                        {
                            //复位测试结果
                            //  wsfeed.ResetResultOfMd();//用于正常状态    
                            // wsfeed.ResetResultOfMdTest();//用于没有测试机台的状态                  
                            //更新物料状态
                            wsfeedright.TestStatus = WSFEED.EM_TEST_STA.UNTEST;
                        }
                        else
                        {
                            wsfeedright.TestStatus = WSFEED.EM_TEST_STA.EMPTY;
                            wsfeedright.isclearfinsh = true;
                        }
                        //计时开始
                        wsfeedright.tmr = System.Environment.TickCount;
                        //通知测试
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} 通知REDAYFORTEST", wsfeedright.disc));
                        wsfeedright.FeedStatus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORTEST;
                        wsfeedright.testprostaus = WSFEED.EM_PC_STA.READY;
                        //  wsfeed.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORTEST;
                        Ct = (Environment.TickCount - tick2) / 1000.0;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("上下料模块1完成用时{0:F3}s", Ct));
                        if (res != EM_RES.OK || res2 != EM_RES.OK)
                        {
                            if ((res == EM_RES.OK || res == EM_RES.QUIT) && (res2 == EM_RES.QUIT || res2 == EM_RES.OK))
                                status = EM_STA.QUIT;
                            else
                                status = EM_STA.ERR;
                        }
                        else
                        {
                            status = EM_STA.READY;
                        }
                        brun = false;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("上下料模块1线程结束!"));

                    }
                    #endregion 
                }
                #endregion
                #region 正常模式
                else
                {
                    WSFEED wsfeed = null;
                    wsfeed = WSFEED.GetWSOnFeedPos;
                    if (wsfeed == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    //等待上下料  //临时测试关闭           
                    if (wsfeed.FeedStatus != WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    //检查当前上料模块的状态
                    if (wsfeed.TestStatus == WSFEED.EM_TEST_STA.ERROR)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("上料线程检测到{0}状态异常!", wsfeed.disc));
                        status = EM_STA.ERR;
                        break;
                    }
                    //确保当前工站准备好上料
                    if (!wsfeed.isInFeedPos)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    Thread.Sleep(100);
                    //上下料动作
                    status = EM_STA.UPDOWNLOADEND;

                    #region 同工位NG是否超范围
                    bool bSame = false;
                    //同一工位同样NG是否超范围
                    if (PT_SET.bSameNGTip && VAR.bSameNGTip_Temp)
                    {
                        try
                        {
                            List<int> NgSameResNum = new List<int>();
                            // List<int> NgSameRes= new List<int>();
                            string StrNgSameResNum = string.Empty;
                            string strNgSameRes = string.Empty;
                            foreach (WSFEED.MdDat md in wsfeed.list_md)
                            {
                                if (md.NgSameRes_cnt >= PT_SET.SameNGTipCnt)
                                {
                                    NgSameResNum.Add(md.Num);
                                    strNgSameRes += md.last_res.ToString();
                                    if (VAR.Isnormal) md.benable = false;
                                    bSame = true;
                                }
                            }

                            if (bSame)
                            {
                                wsfeed.SaveCfg(VAR.gsys_set.cur_product_name);
                                wsfeed.LoadCfg(VAR.gsys_set.cur_product_name);
                                bSame = false;
                            }
                            if (NgSameResNum.Count > 0)
                            {
                                VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "工位连续异常!" : "Same NG", 20, true);
                                MT.ST_WARN st_warn = new MT.ST_WARN();
                                warning fr_warn = new warning();
                                st_warn.ok_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                                st_warn.ws = wsfeed;
                                st_warn.ws_num = NgSameResNum;
                                foreach (int num in st_warn.ws_num)
                                {
                                    StrNgSameResNum += num.ToString() + ",";
                                }

                                st_warn.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                                st_warn.title = VAR.IsChinese ? "提示:工位连续异常!" : "Tip: Workstation is continuously abnormal!";
                                st_warn.msg = VAR.IsChinese ? string.Format("{0}有以下工位:{1}连续出现相同NG分别对应:{2}!", wsfeed.disc, StrNgSameResNum, strNgSameRes) : string.Format("{0} There are the following stations: {1}, Consecutive occurrences of the same NG respectively correspond to: {2} \r\n{0}有以下工位:{1}连续出现相同NG分别对应:{2}!", wsfeed.disc, StrNgSameResNum, strNgSameRes);
                                st_warn.lb_msg = VAR.IsChinese ? "提示:" + st_warn.msg + "请确认!\r\n  1.按'继续运行'键则继续运行!\r\n  " +
                                                 "2.如需确认问题请按'停止运行'键退出运行，待界面左上角显示就绪后再按'运行'键!\r\n" : "Tip:" + st_warn.msg + "\r\nPlease confirm!\r\n  1.Press 'Keep running' to keep running!\r\n  " +
                                                                                                  "2.If you need to confirm the problem, press the 'Stop Running' button to exit the operation. After the ready in the upper left corner of the interface, press the 'Run' button!\r\n" + "\r\n提示:" + st_warn.msg + "请确认!\r\n  1.按'继续运行'键则继续运行!\r\n  " +
                                                                                                  "2.如需确认问题请按'停止运行'键退出运行，待界面左上角显示就绪后再按'运行'键!\r\n";
                                DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);
                                if (DialogResult.Cancel == logres)
                                {
                                    res = EM_RES.ERR;
                                    break;
                                }
                                MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                            }
                        }
                        finally
                        {
                            foreach (WSFEED.MdDat md in wsfeed.list_md)
                            {
                                if (md.NgSameRes_cnt >= PT_SET.SameNGTipCnt) md.NgSameRes_cnt = 0;
                            }
                        }
                    }
                    #endregion
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}开始上下料...", wsfeed.disc));
                    if(wsfeed.id == 0)//撞机防呆，如果是右测试模块进行上下料的话，左侧夹具不能打开
                    {
                        COM.wsfeed2.wsfeedcanopen = false;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}撞机防呆，左侧夹具不可以打开-----", wsfeed.disc));
                    }
                    else if (wsfeed.id == 1)
                    {
                        COM.wsfeed1.wsfeedcanopen = false;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}撞机防呆，右侧夹具不可以打开-----", wsfeed.disc));
                    }

                    int tick = Environment.TickCount;
                    if (wsfeed.id == 0 && PT_SET.UpDownRunMode != (int)PT_SET.RUN_MD.ALONE_WORK)
                    {
                        wsfeed1cangotest = false;
                        wsfeed2isworking = false;
                    }
                    else if (wsfeed.id == 1 && PT_SET.UpDownRunMode != (int)PT_SET.RUN_MD.ALONE_WORK)
                    {
                        wsfeed2cangotest = false;
                        wsfeed2isworking = true;
                    }
                    wsfeed.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD;
                    COM.UDLoad1.RunUdTask();
                    Thread.Sleep(5);
                    COM.UDLoad2.RunUdTask();
                    res1 = COM.UDLoad1.WaitUdTask(ref bquit);
                    res2 = COM.UDLoad2.WaitUdTask(ref bquit);
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("完成{0}上下料动作...,Res:{1}", wsfeed.disc, res.ToString()));
                    if (wsfeed.id == 0)//撞机防呆，如果是右测试模块进行上下料的话，左侧夹具不能打开
                    {
                        COM.wsfeed2.wsfeedcanopen = true;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}撞机防呆，左侧夹具可以打开-----", wsfeed.disc));
                    }
                    else if (wsfeed.id == 1)
                    {
                        COM.wsfeed1.wsfeedcanopen = true;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}撞机防呆，右侧夹具可以打开-----", wsfeed.disc));
                    }
                    bWaitforUpDownload = true;
                    SetCalResult();
                    while (bWaitforUpDownload && !bquit && !VAR.gsys_set.bquit)
                    {
                        Thread.Sleep(100);
                    }
                    if (res1 != EM_RES.OK || res2 != EM_RES.OK || bquit || VAR.gsys_set.bquit)
                    {
                        wsfeed.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD;
                        break;
                    }
                    //gy0123夹具扫码
                    if (PT_SET.bJigSan)
                    {
                        if (PT_SET.UpDownRunMode == (int)PT_SET.RUN_MD.ALONE_WORK)
                        {
                            if(wsfeed.id==0) res = COM.UDLoad1.JigSan(ref bquit, ref wsfeed,false);
                            if(wsfeed.id ==1) res = COM.UDLoad2.JigSan(ref bquit, ref wsfeed, true);
                        }
                        else if(PT_SET.UpDownRunMode == (int)PT_SET.RUN_MD.BOTH_WORK)
                        {
                            res = COM.UDLoad2.JigSan(ref bquit, ref wsfeed, false);//先2后1
                            res = COM.UDLoad1.JigSan(ref bquit, ref wsfeed, true);
                        }
                        else if (PT_SET.UpDownRunMode == (int)PT_SET.RUN_MD.MD1_WORK)
                        {
                            res = COM.UDLoad1.JigSan(ref bquit, ref wsfeed, true);
                        }
                        else res = COM.UDLoad2.JigSan(ref bquit, ref wsfeed, true);
                        if (res != EM_RES.OK) break;
                    }
                    //此处加一个标志位
                    //if (!VAR.ClearMt) wsfeed.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORTEST;//20210914屏蔽
                    int clearcnt = 0;
                    int benablecnt = 0;
                    bool cangotest = false;
                    foreach (WSFEED.MdDat md in wsfeed.list_md)
                    {
                        if (!md.benable) continue;
                        benablecnt++;
                        if (md.res == -1) clearcnt++;
                    }
                    if (clearcnt > 0)
                    {
                        cangotest = true;
                    }
                    //上完闭合
                    if (wsfeed.id == 0&& benablecnt>0 && (VAR.ClearMt == false || cangotest))
                    {
                        wsfeed1cangotest = true;
                        cangotest = false;
                        //  if (PT_SET.turnon) wsfeed1openchart = true;
                    }
                    else if (wsfeed.id == 1 && benablecnt > 0 && (VAR.ClearMt == false || cangotest))
                    {
                        wsfeed2cangotest = true;
                        cangotest = false;
                        //  if (PT_SET.turnon) wsfeed2openchart = true;
                        wsfeed2isworking = false;
                    }
                    if (COM.wsfeed1.isclearfinsh && COM.wsfeed2.isclearfinsh && VAR.ClearMt)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("清料完成!"));
                        VAR.ClearMt = false;
                        break;
                    }
                    //到达后对待测模组状态进行复位更新
                    int cnt = 0;
                    foreach (WSFEED.MdDat md in wsfeed.list_md)
                    {
                        if (!md.benable) continue;
                        if (md.res == -1) cnt++;
                    }
                    if (cnt > 0)
                    {
                        //复位测试结果
                        //  wsfeed.ResetResultOfMd();//用于正常状态    
                        // wsfeed.ResetResultOfMdTest();//用于没有测试机台的状态                  
                        //更新物料状态
                        wsfeed.TestStatus = WSFEED.EM_TEST_STA.UNTEST;
                    }
                    else
                    {
                        wsfeed.TestStatus = WSFEED.EM_TEST_STA.EMPTY;
                        wsfeed.isclearfinsh = true;
                    }
                    //计时开始
                    wsfeed.tmr = System.Environment.TickCount;
                    //通知测试
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} 通知去测试位置", wsfeed.disc));
                    wsfeed.FeedStatus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORTEST;
                    wsfeed.testprostaus = WSFEED.EM_PC_STA.READY;
                    //  wsfeed.wsfeedfeedandteststaus = WSFEED.EM_TESTPROGRESS_STA.REDAYFORTEST;
                    Ct = (Environment.TickCount - tick) / 1000.0;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("上下料完成用时{0:F3}s", Ct));
                }
            }
            if (res != EM_RES.OK || res1 != EM_RES.OK || res2 != EM_RES.OK)
            {
                if ((res == EM_RES.OK || res == EM_RES.QUIT) && (res1 == EM_RES.QUIT || res1 == EM_RES.OK) && (res2 == EM_RES.OK || res2 == EM_RES.QUIT))
                    status = EM_STA.QUIT;
                else
                    status = EM_STA.ERR;
            }
            else
            {
                status = EM_STA.READY;
            }

            brun = false;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("上下料线程结束!"));
            #endregion
        }

        #endregion

        #region 夹具扫码
        //发送二维码夹具数据，仅仅开机发送一次gy0123
        public bool bSendJig = true;
        /// 夹具扫码
        public EM_RES JigSan(ref bool bquit, ref WSFEED ws,bool Isclose)
        {
            if (!(PT_SET.bJigSan && ws.bjigSan))//都开启才扫码
                return EM_RES.OK;
            string[] Errmsg = new string[3];
            bool bAlmJig = false;
            foreach (WSFEED.MdDat md in ws.list_md)
            {
                if (PT_SET.UpDownRunMode == (int)PT_SET.RUN_MD.BOTH_WORK)
                {
                    if (id == 0 && (md.Num == 1 || md.Num == 2 || md.Num == 5 || md.Num == 6))continue;
                    if (id == 1 && (md.Num == 3 || md.Num ==4 || md.Num == 7 || md.Num == 8)) continue;
                }
                REJIGCAP:
                if (md.Num % 2 == 0) continue;//偶数编号不扫码，从1开始gy0123
                if (!md.benable && !ws.list_md[md.Num].benable) continue;//一个夹具上的两个模组全部屏蔽，不扫码

                var res = MT.ZupMove(ref bquit, ref ax_x, md.st_jigpos[id].x, ref ax_y, md.st_jigpos[id].y);
                //ax_y.SetToWorkSpd();
                if (res != EM_RES.OK) return res;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "夹具扫码:" + md.Num.ToString());
                if (PT_SET.BCamcfgset)
                    Camcfgset(false, true);
                var BarMode = PT_SET.BarcodeMode;//记录当前模式
                PT_SET.BarcodeMode = (int)PT_SET.BAR_SCAN.UP_SCAN;//临时设置上相机扫码模式，扫码完成之后设置回来
                res = upcam.FindTaskTriAndWait(CONST.JigSanFw, Demo);
                PT_SET.BarcodeMode = (int)BarMode;
                if (res != EM_RES.OK) return EM_RES.ERR;
                else if ((upcam.curTask.ResData.BarCode == null || upcam.curTask.ResData.BarCode.Length < 1) && !Demo)
                {
                    Errmsg[0] = VAR.IsChinese ? "没检到夹具二维码!" : "No Barcode";
                    Errmsg[1] = VAR.IsChinese
                        ? string.Format("{0}_工位{1}上夹具二维码无法识别.", ws.disc, md.Num)
                        : string.Format("{0}_ws{1}can't recognize barcode.", ws.disc, md.Num);
                    if ((res == EM_RES.OK && (upcam.curTask.ResData.BarCode == null || upcam.curTask.ResData.BarCode.Length < 1)) && !Demo)
                    {
                        Errmsg[0] = VAR.IsChinese ? "没检到二维码!" : "No Barcode";
                        Errmsg[1] = VAR.IsChinese
                            ? string.Format("{0}_工位{1}上夹具二维码无法识别.", ws.disc, md.Num)
                            : string.Format("{0}_ws{1}can't recognize barcode.", ws.disc, md.Num);
                        bAlmJig = true;
                    }
                }
                else
                {
                    string StrCode = upcam.curTask.ResData.BarCode;
                    string jigcode = StrCode + "_1";
                    if (md.jig_ID != jigcode)//夹具有更新，夹具生产数量清空
                    {

                        GetModJigData(ws.id, md);//发送更新之前的数据
                        GetModJigData(ws.id, ws.list_md[md.Num]);

                        md.jig_ID = jigcode;
                        ws.list_md[md.Num].jig_ID = StrCode + "_2";//同一个夹具的另外一个模组位
                        md.cnt_ng_exposure = 0;
                        md.cnt_ng_iic = 0;
                        md.cnt_ng_image = 0;
                        md.cnt_ng_miss_pix = 0;
                        md.cnt_ng_OS = 0;
                        md.cnt_ng_other = 0;
                        md.cnt_ok = 0;
                        ws.list_md[md.Num].cnt_ng_exposure = 0;
                        ws.list_md[md.Num].cnt_ng_iic = 0;
                        ws.list_md[md.Num].cnt_ng_image = 0;
                        ws.list_md[md.Num].cnt_ng_miss_pix = 0;
                        ws.list_md[md.Num].cnt_ng_OS = 0;
                        ws.list_md[md.Num].cnt_ng_other = 0;
                        ws.list_md[md.Num].cnt_ok = 0;
                        GetModJigData(ws.id, md);//发送更新之后的数据清零
                        GetModJigData(ws.id, ws.list_md[md.Num]);
                    }
                    else if (bSendJig)
                    {
                        GetModJigData(ws.id, md);//发送更新之前的数据
                        GetModJigData(ws.id, ws.list_md[md.Num]);
                    }
                }
                if (bAlmJig)
                {
                    List<int> NgSameResNum = new List<int>();
                    bAlmJig = false;
                    VAR.sys_inf.Set(EM_ALM_STA.WAR_YELLOW_FLASH, Errmsg[0], 20, true);
                    MT.ST_WARN st_warn1 = new MT.ST_WARN();
                    warning fr_warn1 = new warning();
                    st_warn1.ok_txt = VAR.IsChinese ? "继续运行" : "Keep running";
                    st_warn1.abort_txt = VAR.IsChinese ? "重新拍照" : "Take a photo";
                    st_warn1.ws = null;
                    NgSameResNum.Add(md.Num);
                    st_warn1.cancle_txt = VAR.IsChinese ? "停止运行" : "Stop running";
                    st_warn1.msg = Errmsg[1];
                    st_warn1.title = VAR.IsChinese ? "提示:" + Errmsg[0] : "Tip:" + Errmsg[0];
                    st_warn1.lb_msg = VAR.IsChinese ? "提示:" + st_warn1.msg + "请确认!\r\n  1.按'继续运行'键则放弃当前检查继续运行!\r\n  " +
                                     "2.如需确认问题请按'停止运行'键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按'重新拍照'键则重新获取当前位置图像!" : "Tip:" + st_warn1.msg + "\r\nPlease confirm!\r\n  1.Press the 'Keep running' button to abandon the current check and continue running!\r\n  " + "2.If you need to confirm the problem, press the 'Stop running' button to exit the operation. After the ready is displayed in the upper left corner of the interface, press the Run button.!\r\n 3.If you press the 'Take a photo' button, you will get the current position image again!" + "\r\n提示:" + st_warn1.msg + "请确认!\r\n  1.按'继续运行'键则放弃当前检查继续运行!\r\n  " + "2.如需确认问题请按'停止运行'键退出运行，待界面左上角显示就绪后再按运行键!\r\n 3.如按'重新拍照'键则重新获取当前位置图像!";
                    DialogResult logres1 = MT.Display_frwarn(fr_warn1, st_warn1, ERR_ALM.EmErrItem.UpDownLoadAbnormal);
                    if (DialogResult.Cancel == logres1) return EM_RES.ERR;
                    VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                    if (DialogResult.Abort == logres1)
                    {
                        goto REJIGCAP;
                    }
                }
            }
            var res1 = MT.ZupMove(ref bquit, ref ax_x, 0, ref ax_y, list_xt[1].st_cap_pos.y - 50);//gy0123扫码结束归位。
            //ax_y.SetToWorkSpd();
            if (res1 != EM_RES.OK) return res1;
            if(Isclose)
            {
                ws.bjigSan = false;//关闭夹具扫码
                bSendJig = false;//关闭开机上报数据
                res1 = ws.SaveCfg(VAR.gsys_set.cur_product_name);
            }
            return res1;

        }
        public static void GetAllJigData()
        {
            string SendData = "";
            foreach (var ws in COM.list_wsfeed)
            {
                foreach (var md in ws.list_md)
                {
                    if (SendData != "") SendData = SendData + ";";
                    SendData = SendData + string.Format(@"{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                            ws.id, md.Num, md.jig_ID, md.cnt_ok, md.cnt_ng_image, md.cnt_ng_OS, md.
                            cnt_ng_miss_pix, md.cnt_ng_exposure, md.cnt_ng_iic, md.cnt_ng_other);
                }
            }
            Msg.secsManager.Send(new BaseInfo() { Id = 7, Value = SendData });
            //Msg.secsManager.Send(new BaseInfo() { Id = 4 }, TypeId: 2);
        }
        /// <summary>
        /// 更新单个夹具的生产数据
        /// </summary>
        /// <param name="wsID"></param>
        /// <param name="md"></param>
        public void GetModJigData(int wsID, WSFEED.MdDat md)
        {
            string SendData = "";
            if (SendData != "") SendData = SendData + ";";
            SendData = SendData + string.Format(@"{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                    wsID, md.Num, md.jig_ID, md.cnt_ok, md.cnt_ng_image, md.cnt_ng_OS, md.
                    cnt_ng_miss_pix, md.cnt_ng_exposure, md.cnt_ng_iic, md.cnt_ng_other);

            VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, $"工站{wsID}夹具{md.Num}有更新");
            Msg.secsManager.Send(new BaseInfo() { Id = 8, Value = SendData });
            Msg.secsManager.Send(new BaseInfo() { Id = 4 }, TypeId: 2);
            Task.Delay(10).Wait();
        }
        #endregion

    }
}
