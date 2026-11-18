using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MotionCtrl;
using DevReport;
using System.Diagnostics;
using LINK;
using System.IO.Ports;

namespace UI
{
    public static class MT
    {
        #region 板卡定义
        public static CARD CARD_DMC3C00 = new CARD(0, 0, 12, 16, 16, CARD.BRAND.LEADSHINE, CARD.TYPE.MOTION, "DMC3C00", "上下料模块3C00");
        public static CARD CARD_ECI2400 = new CARD(1, "192.168.0.103", 4, 24, 8, CARD.BRAND.ZMOTION, CARD.TYPE.MOTION, "ECI2400", "上下料模块2400");
        public static CARD CARD_ECI2600_1 = new CARD(2, "192.168.0.11", 6, 24, 8, CARD.BRAND.ZMOTION, CARD.TYPE.MOTION, "ECI2600-1", "测试机台卡1");
        public static CARD CARD_ECI2600_2 = new CARD(3, "192.168.0.12", 6, 24, 8, CARD.BRAND.ZMOTION, CARD.TYPE.MOTION, "ECI2600-2", "测试机台卡2");
        public static CARD CARD_ECI1616 = new CARD(4, "192.168.0.11 ", 0, 64, 64, CARD.BRAND.ZMOTION, CARD.TYPE.CAN_IO, "ECI1616", "测试机台IO扩展卡");
        public static List<CARD> CardList = new List<CARD> { CARD_DMC3C00, CARD_ECI2400,CARD_ECI2600_1, CARD_ECI2600_2, CARD_ECI1616 };

        #endregion
        #region 轴定义

        #region 测试机部分
        //左边测试工位
        public static AXIS AXIS_WSFEED2_Y = new AXIS(4, CARD_ECI2600_1, "左工位前后Y轴", "Left Ws_Y", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_WSFEED2_X = new AXIS(0, CARD_ECI2600_2, "左工位增距镜", "Left Ws_X", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_WSFEED2_U = new AXIS(2, CARD_ECI2600_2, "左工位翻转轴", "Left Ws_U", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_WSFEED2_Z1 = new AXIS(0, CARD_ECI2600_1, "左工位前Z轴", "Left Ws_Z1", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_WSFEED2_Z2 = new AXIS(1, CARD_ECI2600_1, "左工位后Z轴", "Left Ws_Z2", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        //右边测试工位
        public static AXIS AXIS_WSFEED1_Y = new AXIS(5, CARD_ECI2600_1, "右工位前后Y轴", "Right Ws_Y", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_WSFEED1_X = new AXIS(1, CARD_ECI2600_2, "右工位增距镜", "Right Ws_X", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_WSFEED1_U = new AXIS(3, CARD_ECI2600_2, "右工位翻转轴", "Right Ws_U", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_WSFEED1_Z1 = new AXIS(2, CARD_ECI2600_1, "右工位前Z轴", "Right Ws_Z1", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_WSFEED1_Z2 = new AXIS(3, CARD_ECI2600_1, "右工位后Z轴", "Right Ws_Z2", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
       
        #endregion

        #region 上下料部分
        public static AXIS AXIS_UDL1_X = new AXIS(1, CARD_DMC3C00, "上下料1-X轴", "UpDwLoad1_X", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL1_Y = new AXIS(0, CARD_DMC3C00, "上下料1-Y轴", "UpDwLoad1_Y", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL1_Z = new AXIS(2, CARD_DMC3C00, "上下料1-Z轴", "UpDwLoad1_Z", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL1_U2 = new AXIS(9, CARD_DMC3C00, "上下料1-U2轴", "UpDwLoad1_U2", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL1_U1 = new AXIS(8, CARD_DMC3C00, "上下料1-U1轴", "UpDwLoad1_U1", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL2_X = new AXIS(4, CARD_DMC3C00, "上下料2-X轴", "UpDwLoad2_X", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL2_Y = new AXIS(3, CARD_DMC3C00, "上下料2-Y轴", "UpDwLoad2_Y", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL2_Z = new AXIS(5, CARD_DMC3C00, "上下料2-Z轴", "UpDwLoad2_Z", AXIS.MT_TYPE.SEVER, AXIS.ENC_TYPE.YES, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL2_U2 = new AXIS(11, CARD_DMC3C00, "上下料2-U2轴", "UpDwLoad2_U2", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL2_U1 = new AXIS(10, CARD_DMC3C00, "上下料2-U1轴", "UpDwLoad2_U1", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);
        #endregion

        #region 料仓部分
        public static AXIS AXIS_UDL_FD_X = new AXIS(0, CARD_ECI2400, "供料X轴", "FEED_X", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);       
        public static AXIS AXIS_UDL_OK_X = new AXIS(1, CARD_ECI2400, RuntimeMachineMode.IsTrayBoxSwapped ? "NG料X轴" : "OK料X轴", RuntimeMachineMode.IsTrayBoxSwapped ? "NG_X" : "OK_X", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL_NG_X = new AXIS(2, CARD_ECI2400, RuntimeMachineMode.IsTrayBoxSwapped ? "OK料X轴" : "NG料X轴", RuntimeMachineMode.IsTrayBoxSwapped ? "OK_X" : "NG_X", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL_FD_Z = new AXIS(3, CARD_ECI2400, "供料Z轴", "FEED_Z", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL_OK_Z = new AXIS(6, CARD_DMC3C00, RuntimeMachineMode.IsTrayBoxSwapped ? "NG料Z轴" : "OK料Z轴", RuntimeMachineMode.IsTrayBoxSwapped ? "NG_Z" : "OK_Z", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);
        public static AXIS AXIS_UDL_NG_Z = new AXIS(7, CARD_DMC3C00, RuntimeMachineMode.IsTrayBoxSwapped ? "OK料Z轴" : "NG料Z轴", RuntimeMachineMode.IsTrayBoxSwapped ? "OK_Z" : "NG_Z", AXIS.MT_TYPE.STEP, AXIS.ENC_TYPE.NO, GPIO.IO_STA.OUT_ON);
        #endregion

        //测试模块
        public static List<AXIS> AxList_WSFEED = new List<AXIS> { AXIS_WSFEED1_Y, AXIS_WSFEED1_X, AXIS_WSFEED1_U, AXIS_WSFEED1_Z1, AXIS_WSFEED1_Z2, AXIS_WSFEED2_Y, AXIS_WSFEED2_X, AXIS_WSFEED2_U, AXIS_WSFEED2_Z1, AXIS_WSFEED2_Z2 };
        //右工位测试模块
        public static List<AXIS> AxList_WSFEED1= new List<AXIS> { AXIS_WSFEED1_Y, AXIS_WSFEED1_X, AXIS_WSFEED1_U, AXIS_WSFEED1_Z1, AXIS_WSFEED1_Z2 };
        //左工位测试模块
        public static List<AXIS> AxList_WSFEED2 = new List<AXIS> { AXIS_WSFEED2_Y, AXIS_WSFEED2_X, AXIS_WSFEED2_U, AXIS_WSFEED2_Z1, AXIS_WSFEED2_Z2 };
        public static List<AXIS> AxList_WSFEED_y = new List<AXIS> { AXIS_WSFEED1_Y, AXIS_WSFEED2_Y};

        //上下料1
        public static List<AXIS> Axlist_UDL1 = new List<AXIS> { AXIS_UDL1_X, AXIS_UDL1_Y, AXIS_UDL1_Z, AXIS_UDL1_U1, AXIS_UDL1_U2, AXIS_UDL_FD_X , AXIS_UDL_OK_X, AXIS_UDL_NG_X};
        public static List<AXIS> Axlist_UDL1_ExpLC = new List<AXIS> { AXIS_UDL1_X, AXIS_UDL1_Y, AXIS_UDL1_Z, AXIS_UDL1_U1, AXIS_UDL1_U2 };
        //上下料2
        public static List<AXIS> Axlist_UDL2 = new List<AXIS> { AXIS_UDL2_X, AXIS_UDL2_Y, AXIS_UDL2_Z, AXIS_UDL2_U1, AXIS_UDL2_U2, AXIS_UDL_FD_X, AXIS_UDL_OK_X, AXIS_UDL_NG_X};
        public static List<AXIS> Axlist_UDL2_ExpLC = new List<AXIS> { AXIS_UDL2_X, AXIS_UDL2_Y, AXIS_UDL2_Z, AXIS_UDL2_U1, AXIS_UDL2_U2};
        //料仓
        public static List<AXIS> Axlist_UDL_LC = new List<AXIS> {AXIS_UDL_FD_X, AXIS_UDL_OK_X, AXIS_UDL_NG_X, AXIS_UDL_FD_Z, AXIS_UDL_OK_Z, AXIS_UDL_NG_Z };
        #endregion
        #region IO 定义
        public static GPIO GPIO_OUT_ON_NULL  = new GPIO(0, (CARD)null, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.NULL, "OUT_ON_NULL", "OUT_ON_NULL", GPIO.IO_STA.OUT_ON);
        public static GPIO GPIO_IN_ON_NULL   = new GPIO(0, (CARD)null, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.NULL, "IN_ON_NULL", "OUT_ON_NULL", GPIO.IO_STA.IN_ON);
        public static GPIO GPIO_OUT_OFF_NULL = new GPIO(0, (CARD)null, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.NULL, "OUT_OFF_NULL", "OUT_ON_NULL", GPIO.IO_STA.OUT_OFF);
        public static GPIO GPIO_IN_OFF_NULL  = new GPIO(0, (CARD)null, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.NULL, "IN_OFF_NULL", "OUT_ON_NULL", GPIO.IO_STA.IN_OFF);
        #region OUT  
        //上料
        public static GPIO GPIO_OUT_UDL1_ZK_N1  = new GPIO(0, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "上料1吸头1真空", "XT1 vacuum");
        public static GPIO GPIO_OUT_UDL1_ZK_N2  = new GPIO(1, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "上料1吸头2真空", "XT2 vacuum");
        public static GPIO GPIO_OUT_UDL2_ZK_N1  = new GPIO(4, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "上料2吸头1真空", "XT3 vacuum");
        public static GPIO GPIO_OUT_UDL2_ZK_N2  = new GPIO(5, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "上料2吸头2真空", "XT4 vacuum");
        public static GPIO GPIO_OUT_UDL1_PZK_N1 = new GPIO(2, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "上料1吸头1破真空", "XT1 vacuum break");
        public static GPIO GPIO_OUT_UDL1_PZK_N2 = new GPIO(3, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "上料1吸头2破真空","XT2 vacuum break");
        public static GPIO GPIO_OUT_UDL2_PZK_N1 = new GPIO(6, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "上料2吸头1破真空","XT3 vacuum break");
        public static GPIO GPIO_OUT_UDL2_PZK_N2 = new GPIO(7, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "上料2吸头2破真空", "XT4 vacuum break");
        
        public static GPIO GPIO_OUT_KL_START    = new GPIO(8, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "开始按键灯", "Start Ctrl-DEL");
        public static GPIO GPIO_OUT_KL_STOP     = new GPIO(10, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "停止按键灯", "Stop Ctrl-DEL");
        public static GPIO GPIO_OUT_KL_RESET    = new GPIO(9, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "复位按键灯", "Reset Ctrl-DEL");
        //public static GPIO GPIO_OUT_KL_FD_TRAY  = new GPIO(11, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "供料料仓灯");
        //public static GPIO GPIO_OUT_KL_OK_TRAY  = new GPIO(7, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "OK料仓灯");
        //public static GPIO GPIO_OUT_KL_NG_TRAY = new GPIO(11, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "NG料仓灯");

        //public static GPIO GPIO_OUT_DL_OK_TRAY_ELE = new GPIO(8, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "OK料仓电磁铁");
        //public static GPIO GPIO_OUT_UL_NG_TRAY_ELE = new GPIO(9, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "NG料仓电磁铁");
        //public static GPIO GPIO_OUT_DL_FD_TRAY_ELE = new GPIO(10, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "供料料仓电磁铁");
                
        //警报
        public static GPIO GPIO_OUT_ALM_RED    = new GPIO(0, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "红色塔灯", "Red Alarm Light");
        public static GPIO GPIO_OUT_ALM_YELLOW = new GPIO(1, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "黄色塔灯", "Yellow Alarm Light");
        public static GPIO GPIO_OUT_ALM_GREEN  = new GPIO(2, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "绿色塔灯", "Green Alarm Light");
        public static GPIO GPIO_OUT_ALM_BEEPER = new GPIO(3, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "蜂鸣器", "Alarm Beeper");
        //相机
        public static GPIO GPIO_OUT_UL_CAM_UP1 = new GPIO(12, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "上相机1光源", "UpCam1 trigger");
        public static GPIO GPIO_OUT_UL_CAM_DW1 = new GPIO(13, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "下相机1光源", "DwCam1 trigger");
        public static GPIO GPIO_OUT_UL_CAM_UP2 = new GPIO(14, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "上相机2光源", "UpCam2 trigger");
        public static GPIO GPIO_OUT_UL_CAM_DW2 = new GPIO(15, CARD_DMC3C00, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "下相机2光源", "DwCam2 trigger");
        //料盘夹紧
        public static GPIO GPIO_OUT_UL_FD_TRAY = new GPIO(4, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "供料料盘夹紧", "Feed Tray clamp");
        public static GPIO GPIO_OUT_DL_OK_TRAY = new GPIO(5, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "OK料盘夹紧", "OK Tray clamp");
        public static GPIO GPIO_OUT_DL_NG_TRAY = new GPIO(6, CARD_ECI2400, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "NG料盘夹紧", "NG Tray clamp");

        public static GPIO GPIO_OUT_WSFEED2_HenWen = new GPIO(6, CARD_ECI2600_1, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "左工位恒温", "  LWS vacuum", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED1_HenWen = new GPIO(7, CARD_ECI2600_1, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "右工位恒温", "  LWS vacuum", GPIO.IO_STA.NULL);

        //左工位
        public static GPIO GPIO_OUT_WSFEED2_OPENFR = new GPIO(1, CARD_ECI2600_1, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "左工位开真空", "  LWS vacuum", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED2_cyup1 = new GPIO(32, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "左工位气缸1升起", " WS2 fixture1 open", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED2_cydown1 = new GPIO(33, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "左工位气缸1下降", " WS2 fixture1 down", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED2_cyup2 = new GPIO(34, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "左工位气缸2升起", " WS2 fixture2 open", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED2_cydown2 = new GPIO(35, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "左工位气缸2下降", " WS2 fixture1 down", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED2_cyup3 = new GPIO(36, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "左工位气缸3升起", " WS2 fixture3 open", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED2_cydown3 = new GPIO(37, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "左工位气缸3下降", " WS2 fixture3 down", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED2_cyup4 = new GPIO(38, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "左工位气缸4升起", " WS2 fixture4 open", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED2_cydown4 = new GPIO(39, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "左工位气缸4下降", " WS2 fixture4 down", GPIO.IO_STA.NULL);
        //右工位
        public static GPIO GPIO_OUT_WSFEED1_OPENFR = new GPIO(2, CARD_ECI2600_1, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "右工位开真空", "WS RIGHT vacuum", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED1_cyup1 = new GPIO(40, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "右工位气缸1升起", " WS1 fixture1 open", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED1_cydown1 = new GPIO(41, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "右工位气缸1下降", " WS1 fixture1 down", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED1_cyup2 = new GPIO(42, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "右工位气缸2升起", " WS1 fixture2 open", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED1_cydown2 = new GPIO(43, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "右工位气缸2下降", " WS1 fixture2 down", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED1_cyup3 = new GPIO(44, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "右工位气缸3升起", " WS1 fixture3 open", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED1_cydown3 = new GPIO(45, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "右工位气缸3下降", " WS1 fixture3 down", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED1_cyup4 = new GPIO(46, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "右工位气缸4升起", " WS1 fixture4 open", GPIO.IO_STA.NULL);
        public static GPIO GPIO_OUT_WSFEED1_cydown4 = new GPIO(47, CARD_ECI1616, GPIO.IO_DIR.OUT, GPIO.IO_TYPE.MT_CARD, "右工位气缸4下降", " WS1 fixture4down", GPIO.IO_STA.NULL);

        //左工位气缸抬起
        public static List<GPIO> List_GPIO_OUTWSFEED1_OUT_UP= new List<GPIO> { GPIO_OUT_WSFEED2_cyup1, GPIO_OUT_WSFEED2_cyup2, GPIO_OUT_WSFEED2_cyup3, GPIO_OUT_WSFEED2_cyup4 };
        //左工位气缸下降
        public static List<GPIO> List_GPIO_OUTWSFEED1_OUT_DOWN = new List<GPIO> { GPIO_OUT_WSFEED2_cydown1, GPIO_OUT_WSFEED2_cydown2, GPIO_OUT_WSFEED2_cydown3, GPIO_OUT_WSFEED2_cydown4 };
        //左右工位真空
        public static List<GPIO> List_GPIO_OPRNFR = new List<GPIO> { GPIO_OUT_WSFEED2_OPENFR, GPIO_OUT_WSFEED1_OPENFR };
        //左工位
        public static List<GPIO> List_GPIO_OUTWSFEED2 = new List<GPIO> { GPIO_OUT_WSFEED2_OPENFR, GPIO_OUT_WSFEED2_cyup1, GPIO_OUT_WSFEED2_cyup2, GPIO_OUT_WSFEED2_cyup3, GPIO_OUT_WSFEED2_cyup4, GPIO_OUT_WSFEED2_cydown1, GPIO_OUT_WSFEED2_cydown2, GPIO_OUT_WSFEED2_cydown3, GPIO_OUT_WSFEED2_cydown4 };
        //右工位气缸抬起
        public static List<GPIO> List_GPIO_OUTWSFEED2_OUT_UP = new List<GPIO> { GPIO_OUT_WSFEED1_cyup1, GPIO_OUT_WSFEED1_cyup2, GPIO_OUT_WSFEED1_cyup3, GPIO_OUT_WSFEED1_cyup4 };
        //右工位气缸下降
        public static List<GPIO> List_GPIO_OUTWSFEED2_OUT_DOWN = new List<GPIO> { GPIO_OUT_WSFEED1_cydown1, GPIO_OUT_WSFEED1_cydown2, GPIO_OUT_WSFEED1_cydown3, GPIO_OUT_WSFEED1_cydown4 };
        //右工位
        public static List<GPIO> List_GPIO_OUTWSFEED1 = new List<GPIO> { GPIO_OUT_WSFEED1_OPENFR, GPIO_OUT_WSFEED1_cyup1, GPIO_OUT_WSFEED1_cyup2, GPIO_OUT_WSFEED1_cyup3, GPIO_OUT_WSFEED1_cyup4, GPIO_OUT_WSFEED1_cydown1, GPIO_OUT_WSFEED1_cydown2, GPIO_OUT_WSFEED1_cydown3, GPIO_OUT_WSFEED1_cydown4 };
       
        #endregion

        #region IN
            //急停
        public static GPIO GPIO_IN_EMG1 = new GPIO(0, CARD_ECI2600_1, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "测试机台急停", "EMG(WS)");
        public static GPIO GPIO_IN_EMG3 = new GPIO(0, CARD_ECI2400,   GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "料仓模块急停", "EMG(UpDwLoad)");
        public static GPIO GPIO_IN_EMG4 = new GPIO(0, CARD_DMC3C00,   GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "上下料模块急停", "EMG(UpDwLoad)");
        //上料
        public static GPIO GPIO_IN_UDL1_ZK_N1 = new GPIO(6, CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "上料1吸头1真空感应", "XT1 vacuum sense");
        public static GPIO GPIO_IN_UDL1_ZK_N2 = new GPIO(7, CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "上料1吸头2真空感应", "XT2 vacuum sense");
        public static GPIO GPIO_IN_UDL2_ZK_N1 = new GPIO(8, CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "上料2吸头1真空感应", "XT3 vacuum sense");
        public static GPIO GPIO_IN_UDL2_ZK_N2 = new GPIO(9, CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "上料2吸头2真空感应", "XT4 vacuum sense");

        //按键
        public static GPIO GPIO_IN_KEY_START = new GPIO(10, CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "开始键", "Start");
        public static GPIO GPIO_IN_KEY_STOP = new GPIO(12, CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "停止键", "Stop");
        public static GPIO GPIO_IN_KEY_RESET = new GPIO(11, CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "复位键", "Reset");
        //门禁        
        public static GPIO GPIO_IN_UDL_CC_DOOR = new GPIO(1,CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "上下料仓储门禁", "UDL storage entrance Guard");
        public static GPIO GPIO_IN_UDL_FR_DOOR = new GPIO(14,CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "上下料前门门禁", "UDL front door entrance Guard");
        public static GPIO GPIO_IN_UDL_BK_DOOR = new GPIO(17, CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "上下料后门门禁", "UDL back door entrance Guard");
        public static GPIO GPIO_IN_UDL_VIEW_DOOR= new GPIO(16, CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "上下料参观门禁", "UDL view entrance Guard");
  
        public static GPIO GPIO_IN_TM_LEFT_DOOR = new GPIO(13, CARD_ECI2600_1, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "测试机机台左门门禁", "WS Left door entrance Guard");
        public static GPIO GPIO_IN_TM_RIGHT_DOOR = new GPIO(14, CARD_ECI2600_1, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "测试机机台右门门禁", "WS Right door entrance Guard");
        public static GPIO GPIO_IN_TM_FRONT_DOOR = new GPIO(15, CARD_ECI2600_1, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "测试机机台前门门禁", "WS front door entrance Guard");
        /// <summary>
        /// 上下料机台所有门禁
        /// </summary>
        public static List<GPIO> List_UDL_DOOR = new List<GPIO> { GPIO_IN_UDL_FR_DOOR, GPIO_IN_UDL_VIEW_DOOR, GPIO_IN_UDL_BK_DOOR };
        /// <summary>
        /// 测试机机台所有门禁
        /// </summary>
        public static List<GPIO> List_TM_DOOR = new List<GPIO> { GPIO_IN_TM_LEFT_DOOR, GPIO_IN_TM_RIGHT_DOOR, GPIO_IN_TM_FRONT_DOOR };

        //料盘夹紧缩回感应
        public static GPIO GPIO_IN_UDL_FD_TRAY_ON  = new GPIO(10, CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "供料盘夹紧感应", "Feed Tray clamp sense");
        public static GPIO GPIO_IN_UDL_FD_TRAY_OFF = new GPIO(11, CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "供料盘缩回感应", "Feed Tray draw back sense");
        public static GPIO GPIO_IN_UDL_OK_TRAY_ON  = new GPIO(12, CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "OK料盘夹紧感应", "OK Tray clamp sense");
        public static GPIO GPIO_IN_UDL_OK_TRAY_OFF = new GPIO(13, CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "OK料盘缩回感应", "OK Tray draw back sense");
        public static GPIO GPIO_IN_UDL_NG_TRAY_ON  = new GPIO(14, CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "NG料盘夹紧感应", "NG Tray clamp sense");
        public static GPIO GPIO_IN_UDL_NG_TRAY_OFF = new GPIO(15, CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "NG料盘缩回感应", "NG Tray draw back sense");
        //public static GPIO GPIO_IN_UDL_FD_TRAY_CFIN = new GPIO(17, CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "供料料盘换料完成按钮");
        //public static GPIO GPIO_IN_UDL_OK_TRAY_CFIN = new GPIO(19, CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "OK料盘换料完成按钮");
        //public static GPIO GPIO_IN_UDL_NG_TRAY_CFIN = new GPIO(18, CARD_ECI2400, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "NG料盘换料完成按钮");
        //料夹在位
        public static GPIO GPIO_IN_UL_INP_FD_TRAYBOX = new GPIO(13, CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "供料仓在位感应", "Feed TrayBox INP");
        public static GPIO GPIO_IN_DL_INP_OK_TRAYBOX = new GPIO(1,  CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "OK料仓在位感应", "OK TrayBox INP");
        public static GPIO GPIO_IN_DL_INP_NG_TRAYBOX = new GPIO(2,  CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "NG料仓在位感应", "NG TrayBox INP");   
             
        //料夹有料
        public static GPIO GPIO_IN_UL_RDY_FD_TRAY = new GPIO(3, CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "供料仓有料感应", "Feed TrayBox Ready");
        public static GPIO GPIO_IN_DL_RDY_OK_TRAY = new GPIO(4, CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "OK料仓有料感应", "OK TrayBox Ready");
        public static GPIO GPIO_IN_DL_RDY_NG_TRAY = new GPIO(5, CARD_DMC3C00, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "NG料仓有料感应", "NG TrayBox Ready");
        
        //左工位气缸感应
        public static GPIO GPIO_IN_WSFEED2_cyup1 = new GPIO(32, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "左工位气缸1上感应", " WS2 fixture1 up sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED2_cydown1 = new GPIO(33, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "左工位气缸1下感应", " WS2 fixture1 down sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED2_cyup2 = new GPIO(34, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "左工位气缸2上感应", " WS2 fixture2 up sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED2_cydown2 = new GPIO(35, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "左工位气缸2下感应", " WS2 fixture2 down sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED2_cyup3 = new GPIO(36, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "左工位气缸3上感应", " WS2 fixture3 up sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED2_cydown3 = new GPIO(37, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "左工位气缸3下感应", " WS2 fixture3 down sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED2_cyup4 = new GPIO(38, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "左工位气缸4上感应", " WS2 fixture4 up sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED2_cydown4 = new GPIO(39, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "左工位气缸4下感应", " WS2 fixture4 down sense", GPIO.IO_STA.NULL);
        //右工位气缸感应
        public static GPIO GPIO_IN_WSFEED1_cyup1 = new GPIO(40, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "右工位气缸1上感应", " WS1 fixture1 up sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED1_cydown1 = new GPIO(41, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "右工位气缸1下感应", " WS1 fixture1 down sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED1_cyup2 = new GPIO(42, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "右工位气缸2上感应", " WS1 fixture2 up sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED1_cydown2 = new GPIO(43, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "右工位气缸2下感应", " WS1 fixture2 down sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED1_cyup3 = new GPIO(44, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "右工位气缸3上感应", " WS1 fixture3 up sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED1_cydown3 = new GPIO(45, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "右工位气缸3下感应", " WS1 fixture3 down sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED1_cyup4 = new GPIO(46, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "右工位气缸4上感应", " WS1 fixture4 up sense", GPIO.IO_STA.NULL);
        public static GPIO GPIO_IN_WSFEED1_cydown4 = new GPIO(47, CARD_ECI1616, GPIO.IO_DIR.IN, GPIO.IO_TYPE.MT_CARD, "右工位气缸4下感应", " WS1 fixture4 down sense", GPIO.IO_STA.NULL);
        //左工位感应
        public static List<GPIO> List_GPIO_IN_WSFEED2_ZK = new List<GPIO> { GPIO_IN_WSFEED2_cyup1, GPIO_IN_WSFEED2_cydown1, GPIO_IN_WSFEED2_cyup2, GPIO_IN_WSFEED2_cydown2, GPIO_IN_WSFEED2_cyup3, GPIO_IN_WSFEED2_cydown3, GPIO_IN_WSFEED2_cyup4, GPIO_IN_WSFEED2_cydown4};
        //右工位感应
        public static List<GPIO> List_GPIO_IN_WSFEED1_ZK = new List<GPIO> { GPIO_IN_WSFEED1_cyup1, GPIO_IN_WSFEED1_cydown1, GPIO_IN_WSFEED1_cyup2, GPIO_IN_WSFEED1_cydown2, GPIO_IN_WSFEED1_cyup3, GPIO_IN_WSFEED1_cydown3, GPIO_IN_WSFEED1_cyup4, GPIO_IN_WSFEED1_cydown4 };
        #endregion

        #region 光源定义

        public static IlluminationController COM2 = new IlluminationController("右光箱其他光源", "COM2", 57600);
        //public static IlluminationController COM3 = new IlluminationController("轩十佳OTP光源", "COM15", 9600);
        //public static IlluminationController COM4 = new IlluminationController("轩十佳右光箱", "COM7", 9600);
        public static SerialCommunication COM3 = new SerialCommunication("COM6", 9600, Parity.None, 8, StopBits.One);
        public static SerialCommunication COM4 = new SerialCommunication("COM7", 9600, Parity.None, 8, StopBits.One);
        public static IlluminationController COM10 = new IlluminationController("左侧4C光箱", "COM10", 19200);
        public static IlluminationController COM11 = new IlluminationController("右侧4C光箱", "COM11", 19200);
        #endregion

        public static void BeQuitEn(bool ben= false,bool sysbquit=false)
        {
            if(sysbquit) VAR.gsys_set.bquit = ben;
            UpDownLoad.bquit = ben;
            if (!WSFEED.bpause)
            {
                WSFEED.bquit = ben;
            }                    
        }
        #region 气缸定义
        //上下料
        public static Cylinder CLD_UDL1_N1 = new Cylinder(GPIO_OUT_UDL1_ZK_N1, GPIO_IN_UDL1_ZK_N1);
        public static Cylinder CLD_UDL1_N2 = new Cylinder(GPIO_OUT_UDL1_ZK_N2, GPIO_IN_UDL1_ZK_N2);
        public static Cylinder CLD_UDL2_N1 = new Cylinder(GPIO_OUT_UDL2_ZK_N1, GPIO_IN_UDL2_ZK_N1);
        public static Cylinder CLD_UDL2_N2 = new Cylinder(GPIO_OUT_UDL2_ZK_N2, GPIO_IN_UDL2_ZK_N2);
        //料盘
        public static Cylinder CLD_UDL_FDTRAY_HD = new Cylinder(GPIO_OUT_UL_FD_TRAY, GPIO_IN_UDL_FD_TRAY_ON, GPIO_IN_UDL_FD_TRAY_OFF);
        public static Cylinder CLD_UDL_OKTRAY_HD = new Cylinder(GPIO_OUT_DL_OK_TRAY, GPIO_IN_UDL_OK_TRAY_ON, GPIO_IN_UDL_OK_TRAY_OFF);
        public static Cylinder CLD_UDL_NGTRAY_HD = new Cylinder(GPIO_OUT_DL_NG_TRAY, GPIO_IN_UDL_NG_TRAY_ON, GPIO_IN_UDL_NG_TRAY_OFF);
        //左工位气缸
        public static Cylinder CLD_WSFEED2_CY1 = new Cylinder(GPIO_OUT_WSFEED2_cyup1, GPIO_IN_WSFEED2_cyup1,GPIO_IN_WSFEED2_cydown1, GPIO_OUT_WSFEED2_cydown1);
        public static Cylinder CLD_WSFEED2_CY2 = new Cylinder(GPIO_OUT_WSFEED2_cyup2, GPIO_IN_WSFEED2_cyup2, GPIO_IN_WSFEED2_cydown2, GPIO_OUT_WSFEED2_cydown2);
        public static Cylinder CLD_WSFEED2_CY3 = new Cylinder(GPIO_OUT_WSFEED2_cyup3, GPIO_IN_WSFEED2_cyup3, GPIO_IN_WSFEED2_cydown3, GPIO_OUT_WSFEED2_cydown3);
        public static Cylinder CLD_WSFEED2_CY4 = new Cylinder(GPIO_OUT_WSFEED2_cyup4, GPIO_IN_WSFEED2_cyup4, GPIO_IN_WSFEED2_cydown4, GPIO_OUT_WSFEED2_cydown4);

        //右工位气缸
        public static Cylinder CLD_WSFEED1_CY1 = new Cylinder( GPIO_OUT_WSFEED1_cyup1,GPIO_IN_WSFEED1_cyup1 ,GPIO_IN_WSFEED1_cydown1,GPIO_OUT_WSFEED1_cydown1 );
        public static Cylinder CLD_WSFEED1_CY2 = new Cylinder(GPIO_OUT_WSFEED1_cyup2, GPIO_IN_WSFEED1_cyup2, GPIO_IN_WSFEED1_cydown2, GPIO_OUT_WSFEED1_cydown2);
        public static Cylinder CLD_WSFEED1_CY3 = new Cylinder(GPIO_OUT_WSFEED1_cyup3, GPIO_IN_WSFEED1_cyup3, GPIO_IN_WSFEED1_cydown3, GPIO_OUT_WSFEED1_cydown3);
        public static Cylinder CLD_WSFEED1_CY4 = new Cylinder(GPIO_OUT_WSFEED1_cyup4, GPIO_IN_WSFEED1_cyup4, GPIO_IN_WSFEED1_cydown4, GPIO_OUT_WSFEED1_cydown4);
        public static List<Cylinder> List_Cylinder_WSFEED1 = new List<Cylinder> { CLD_WSFEED1_CY1, CLD_WSFEED1_CY2, CLD_WSFEED1_CY3, CLD_WSFEED1_CY4 };
        public static List<Cylinder> List_Cylinder_WSFEED2 = new List<Cylinder> { CLD_WSFEED2_CY1, CLD_WSFEED2_CY2, CLD_WSFEED2_CY3, CLD_WSFEED2_CY4 };

        #endregion
        #endregion
        #region 错误框显示
        //private static  object lockdisplay = new object();       
        //  public static warning fr_warn= new warning();
        public static string DoorAlarmMsg = string.Empty;
        public static string DoorAlarmMsgTemp = string.Empty;
        //public static bool IsErrAlm = false;
        public class ST_WARN
        {
            public string ok_txt;
            public string cancle_txt;
            public string abort_txt;
            public string msg;
            public List<int> ws_num;
            public string lb_msg;
            public WSFEED ws = null;
            public string title;

            public ST_WARN(string ok_txt = "确定", WSFEED ws = null, List<int> ws_num = null, string cancle_txt = "不用", string abort_txt = "不用", string msg = "信息", string lb_msg = "信息", string title = "")
            {
                this.ok_txt = ok_txt;
                this.cancle_txt = cancle_txt;
                this.abort_txt = abort_txt;
                this.msg = msg;
                this.ws_num = ws_num;
                this.lb_msg = lb_msg;
                this.ws = ws;
                this.title = title;
            }
        }

        public static DialogResult Display_frwarn(warning fr_warn, ST_WARN warn, ERR_ALM.EmErrItem erritem, bool IsYellow = true)
        {
            //lock (lockdisplay)
            //{
            try
            {
                fr_warn.btn_ok.Text = warn.ok_txt;
                fr_warn.btn_cancle.Text = warn.cancle_txt;
                fr_warn.btn_ok.Visible = true;
                fr_warn.btn_abort.Text = warn.abort_txt;
                fr_warn.ws = warn.ws;
                if (fr_warn.ws == null) fr_warn.pnl_ws.Visible = false;
                else fr_warn.pnl_ws.Visible = true;
                fr_warn.ws_num = warn.ws_num;
                if (fr_warn.ws_num == null || fr_warn.ws_num.Count <= 0) fr_warn.pnl_wspos.Visible = false;
                else fr_warn.pnl_wspos.Visible = true;
                if (fr_warn.btn_cancle.Text != "不用") fr_warn.btn_cancle.Visible = true;
                else fr_warn.btn_cancle.Visible = false;
                if (fr_warn.btn_abort.Text != "不用") fr_warn.btn_abort.Visible = true;
                else fr_warn.btn_abort.Visible = false;
                if (erritem == ERR_ALM.EmErrItem.MaterialPosErr
                    && fr_warn.btn_ok.Visible
                    && (fr_warn.btn_ok.Text.Contains("拍照") || fr_warn.btn_ok.Text.Contains("Take a photo")))
                {
                    fr_warn.btn_ok.UseVisualStyleBackColor = false;
                    fr_warn.btn_ok.BackColor = Color.OrangeRed;
                    fr_warn.btn_ok.ForeColor = Color.White;
                    fr_warn.btn_ok.FlatStyle = FlatStyle.Flat;
                    fr_warn.btn_ok.FlatAppearance.BorderColor = Color.Blue;
                    fr_warn.btn_ok.FlatAppearance.BorderSize = 6;
                }
                else if (erritem == ERR_ALM.EmErrItem.MaterialPosErr
                    && fr_warn.btn_abort.Visible
                    && (fr_warn.btn_abort.Text.Contains("拍照") || fr_warn.btn_abort.Text.Contains("Take a photo")))
                {
                    fr_warn.AcceptButton = fr_warn.btn_abort;
                    fr_warn.ActiveControl = fr_warn.btn_abort;
                    fr_warn.KeyPreview = true;
                    fr_warn.btn_abort.TabIndex = 0;
                    fr_warn.btn_ok.TabIndex = 1;
                    fr_warn.btn_cancle.TabIndex = 2;
                    fr_warn.btn_ok.NotifyDefault(false);
                    fr_warn.btn_abort.NotifyDefault(true);
                    fr_warn.btn_abort.UseVisualStyleBackColor = false;
                    fr_warn.btn_abort.BackColor = Color.Yellow;
                    fr_warn.btn_abort.FlatStyle = FlatStyle.Flat;
                    fr_warn.btn_abort.FlatAppearance.BorderColor = Color.Red;
                    fr_warn.btn_abort.FlatAppearance.BorderSize = 4;
                    fr_warn.KeyDown += (sender, args) =>
                    {
                        if (args.KeyCode == Keys.Enter)
                        {
                            args.SuppressKeyPress = true;
                            fr_warn.btn_abort.PerformClick();
                        }
                    };
                    fr_warn.Shown += (sender, args) =>
                    {
                        fr_warn.BeginInvoke(new Action(() =>
                        {
                            fr_warn.AcceptButton = fr_warn.btn_abort;
                            fr_warn.ActiveControl = fr_warn.btn_abort;
                            fr_warn.btn_ok.NotifyDefault(false);
                            fr_warn.btn_abort.NotifyDefault(true);
                            fr_warn.btn_abort.Focus();
                            fr_warn.btn_abort.Select();
                        }));
                    };
                }
                fr_warn.BackColor = IsYellow ? Color.Yellow : Color.Red;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, warn.msg, DReport.EmErrCode.Null, 0, erritem);
                fr_warn.lb_msg.Text = warn.lb_msg;
                fr_warn.title = warn.title;
                fr_warn.TopMost = true;
                MT.OnlyLightOn(GPIO_OUT_ALM_YELLOW);
                if (!VAR.sys_inf.info.Contains(VAR.IsChinese ? "更换仓储" : "Change Tray")) VAR.IsErrAlm = true;
                DialogResult logres = fr_warn.ShowDialog();
                return logres;
            }
            finally
            {
                VAR.IsErrAlm = false;
            }

            //}


        }


        //门禁提示
        public static void ErrAlmTip()
        {
            MT.ST_WARN st_warn = new MT.ST_WARN();
            warning fr_warn = new warning();
            st_warn.ok_txt = VAR.IsChinese ? "确认" : "OK";
            st_warn.ws = null;
            if (MT.DoorAlarmMsg != string.Empty)
            {
                st_warn.msg = MT.DoorAlarmMsg;
                //DRpt.Report_Status(DReport.EmStatus.Ready, DReport.EmHareware.Null, DReport.EmStatus.Ready.GetDescription());
                if (MT.DoorAlarmMsg != string.Format("急停键按下，请确认,系统需重新复位!") || MT.DoorAlarmMsg != "Press the emergency stop button, please confirm, the system needs to be reset!          (急停键按下，请确认,系统需重新复位!)")
                {
                    VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "门禁打开!" : "Door Alarm!", 20, true);
                    st_warn.title = VAR.IsChinese ? "提示:门禁打开!" : "Tip: The entrance guard is open!";
                    st_warn.lb_msg = VAR.IsChinese ? "门禁打开提示:\r\n" + MT.DoorAlarmMsg + "请把相关的门关好，按确认键如想继续请按开始键!" : "Access control opening reminder:\r\n" + MT.DoorAlarmMsg + "Please close the relevant doors and press the OK button. If you want to continue, press the Start button!" + "\r\n门禁打开提示:\r\n" + MT.DoorAlarmMsg + "请把相关的门关好，按确认键如想继续请按开始键!";
                }
                else
                {
                    VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "急停按下!" : "EMG", 20, true);
                    st_warn.title = VAR.IsChinese ? "提示:急停按下!" : "Tip:EMG button was pressed";
                    st_warn.lb_msg = MT.DoorAlarmMsg;
                }
                MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.Null);
            }
            else if (VAR.SysErrAlm.ErrItem != ERR_ALM.EmErrItem.Null && VAR.sys_inf.info.Contains(VAR.IsChinese ? "运行" : "RUN"))
            {
                // DRpt.Report_Status(DReport.EmStatus.Error, DReport.EmHareware.Null, DReport.EmStatus.Error.GetDescription());
                st_warn.msg = VAR.SysErrAlm.ErrStr;
                st_warn.lb_msg = VAR.SysErrAlm.ErrStr;
                VAR.sys_inf.Set(EM_ALM_STA.WAR_YELLOW_FLASH, Utility.GetDescription(VAR.SysErrAlm.ErrItem, VAR.IsChinese), 20, true);
                MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.Null);
            }
            VAR.SysErrAlm.ErrItem = ERR_ALM.EmErrItem.Null;
            VAR.SysErrAlm.ErrStr = string.Empty;
            MT.DoorAlarmMsg = string.Empty;
            MT.DoorAlarmMsgTemp = string.Empty;
        }

        #endregion
        #region 安全监测
        public static void SetSafeFunc()
        {
            foreach (AXIS ax in Axlist_UDL1_ExpLC)//上料模块1xyzu1u2
            {
                ax.ChkSafeSen = ChkSafeSen;
                ax.ChkSafePos = ChkSafePos;
            }

            foreach (AXIS ax in Axlist_UDL2_ExpLC)//上料模块2xyzu1u2
            {
                ax.ChkSafeSen = ChkSafeSen;
                ax.ChkSafePos = ChkSafePos;
            }

            foreach (AXIS ax in Axlist_UDL_LC)//料仓模块
            {
                ax.ChkSafeSen = ChkSafeSen;
                ax.ChkSafePos = ChkSafePos;
            }
            foreach (AXIS ax in AxList_WSFEED)//测试机上下料模块
            {
                ax.ChkSafeSen = ChkSafeSenwsfeed;
                //ax.ChkSafePos = ChkSafePos;
            }
          

        }

        public const double xsafedis = 89;
        public static EM_RES ChkSafePos(int id, double targe_pos = double.MaxValue)
        {
            EM_RES ret=EM_RES.OK;           
            UpDownLoad Other_ud;
            //门禁保护
            //ret = ChkSafeSen(id);
            //if (ret != EM_RES.OK) return ret;
            //位置保护
            foreach (UpDownLoad ud in COM.List_UDLoad)
            {
                if (id == ud.ax_x.id || id == ud.ax_y.id)
                {
                    if (ud.ax_z.home_status == AXIS.HOME_STA.OK)
                    {
                        if (Math.Abs(ud.ax_z.fenc_pos) >3)
                        {
                            Thread.Sleep(20);
                            if (Math.Abs(ud.ax_z.fenc_pos) >3)
                           {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}->{1}当前坐标>3，禁止移动{2}或{3}!", ud.disc, ud.ax_z.disc, ud.ax_x.disc, ud.ax_y.disc));
                            return EM_RES.MOVE_PROTECT;
                           }
                        }
                    }
                    else
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}{1}未复位 ，禁止移动{2}或{3}!", ud.disc, ud.ax_z.disc, ud.ax_x.disc, ud.ax_y.disc));
                        return EM_RES.MOVE_PROTECT;
                    }
                   
                }
               if (id == ud.ax_x.id)
                {
                    if (ud.id == 0) Other_ud = COM.UDLoad2;
                    else Other_ud = COM.UDLoad1;
                    TRYAGAIN:
                    if (Math.Abs(targe_pos) + Math.Abs(Other_ud.ax_x.fenc_pos) + xsafedis > UpDownLoad.DisAxisX && COM.UDLoad1.ax_x.home_status == AXIS.HOME_STA.OK && COM.UDLoad2.ax_x.home_status == AXIS.HOME_STA.OK)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}的目标坐标为{1}，{2}的当前坐标为{3}，禁止{4}移动因移动后两X轴距离小于安全距离{5},总距离{6}", ud.disc, targe_pos, Other_ud.ax_x.disc, Other_ud.ax_x.fenc_pos, ud.disc, xsafedis, UpDownLoad.DisAxisX));
                        UpDownLoad.ud2needwait = true;
                        if (ud.id == 0) COM.UDLoad1.SafeProtectMT(ref WSFEED.bquit, ud.id, targe_pos);
                        if (ud.id == 1) COM.UDLoad2.SafeProtectMT(ref WSFEED.bquit, ud.id, targe_pos);
                        if (ud.id == 1 && UpDownLoad.ud1needwait == true) break;
                        goto TRYAGAIN; 
                    }
                    else if (COM.UDLoad1.ax_x.home_status != AXIS.HOME_STA.OK && COM.UDLoad1.status_ud != UpDownLoad.EM_STA.HOME)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}未复位,{1}禁止移动!",COM.UDLoad1.ax_x.disc, ud.disc));
                        return EM_RES.MOVE_PROTECT;
                    }
                    else if (COM.UDLoad2.ax_x.home_status != AXIS.HOME_STA.OK && COM.UDLoad2.status_ud != UpDownLoad.EM_STA.HOME)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}未复位,{1}禁止移动!",COM.UDLoad2.ax_x.disc, ud.disc));
                        return EM_RES.MOVE_PROTECT;
                    }
                }

            }

            if (id == COM.traybox_fd.ax_z.id)
            {
                if ((Math.Abs(COM.traybox_fd.ax_x.fenc_pos) > COM.traybox_fd.fd_safe_x+1) && (Math.Abs(targe_pos - COM.traybox_fd.ax_z.fenc_pos) > COM.traybox_fd.tray_feed_ofs_h + 0.1))
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("供料仓X轴插入供料仓，供料Z轴移动距离{0}超过{1}!", (Math.Abs(targe_pos - COM.traybox_fd.ax_z.fenc_pos)), COM.traybox_fd.tray_feed_ofs_h));
                    return EM_RES.MOVE_PROTECT;
                }
            }

            if (id == COM.traybox_ng.ax_z.id)
            {
                if ((Math.Abs(COM.traybox_ng.ax_x.fenc_pos) > COM.traybox_ng.fd_safe_x+1) && (Math.Abs(targe_pos - COM.traybox_ng.ax_z.fenc_pos) > COM.traybox_ng.tray_feed_ofs_h + 0.1))
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("NG料仓X轴插入NG料仓，NG料Z轴移动距离{0}大于{1}!", (Math.Abs(targe_pos - COM.traybox_ng.ax_z.fenc_pos)), COM.traybox_ng.tray_feed_ofs_h));
                    return EM_RES.MOVE_PROTECT;
                }
            }

            if (id == COM.traybox_ok.ax_z.id)
            {
                if ((Math.Abs(COM.traybox_ok.ax_x.fenc_pos) > COM.traybox_ok.fd_safe_x+1) && (Math.Abs(targe_pos - COM.traybox_ok.ax_z.fenc_pos) > COM.traybox_ok.tray_feed_ofs_h + 0.1))
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("OK料仓X轴插入OK料仓，OK料Z轴移动距离{0}大于{1}!", (Math.Abs(targe_pos - COM.traybox_ok.ax_z.fenc_pos)), COM.traybox_ok.tray_feed_ofs_h));
                    return EM_RES.MOVE_PROTECT;
                }
            }

            return EM_RES.OK;
        }

        public static EM_RES ChkSafeSen(int id = 0, double target_pos = double.MaxValue)
        {
            EM_RES ret = EM_RES.OK;
         //上下料门禁
            //ret=ChkAllSafeSen(0x01);
            ret = ChkAllSafeSen();
            //if (GPIO_IN_EMG.isOFF) return EM_RES.EMG;
            return ret;
        }
        public static EM_RES ChkSafeSenwsfeed(int id = 0, double target_pos = double.MaxValue)
        {
            EM_RES ret = EM_RES.OK;
            //上下料门禁
            //ret = ChkAllSafeSen(0x02);
            ret = ChkAllSafeSen();
            //if (GPIO_IN_EMG.isOFF) return EM_RES.EMG;
            return ret;
        }

        public static bool isSafeSen
        {
            get
            {
                if (ChkSafeSen() == EM_RES.OK) return true;
                return false;
            }
        }

        #region 门禁以及急停检测
        public static EM_RES ChkAllSafeSen(int ChkEn=0xff)
        {
            //上下料门禁
            if (PT_SET.bEnUpDownDr && (ChkEn & 0x01) == 0x01)
            {
                foreach (GPIO dr in MT.List_UDL_DOOR)
                {
                    if (dr.isON) continue;

                    if (dr.AssertOFF())
                    {
                        MT.DoorAlarmMsg = string.Format("{0}打开!", dr.str_disc);
                        if (MT.DoorAlarmMsg != MT.DoorAlarmMsgTemp)
                        {
                            SQLData.TesterrDataAdd(99, 99, "硬件报警", string.Format("{0}门禁打开", dr.str_disc));
                            COUNT_DATA.Errcnt++;
                            MT.DoorAlarmMsgTemp = MT.DoorAlarmMsg;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}打开!", dr.str_disc));
                        }
                        return EM_RES.SAFE_PROTECT;
                    }
                }
            }

            //测试机
            if (PT_SET.bEnTMDr && (ChkEn & 0x02) == 0x02)
            {
                foreach (GPIO dr in MT.List_TM_DOOR)
                {
                    if (dr.isON) continue;
                    if (dr.AssertOFF())
                    {
                        MT.DoorAlarmMsg = string.Format("{0}打开!", dr.str_disc);
                        if (MT.DoorAlarmMsg != MT.DoorAlarmMsgTemp)
                        {
                            MT.DoorAlarmMsgTemp = MT.DoorAlarmMsg;
                            COUNT_DATA.Errcnt++;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}打开!", dr.str_disc));
                            SQLData.TesterrDataAdd(99, 99, "硬件报警", string.Format("{0}门禁打开", dr.str_disc));
                        }
                        return EM_RES.SAFE_PROTECT;
                    }
                }
            }
            if (GPIO_IN_EMG1.isOFF || GPIO_IN_EMG3.isOFF || GPIO_IN_EMG4.isOFF)
            {
                Thread.Sleep(20);
                if (GPIO_IN_EMG1.isOFF ||  GPIO_IN_EMG3.isOFF || GPIO_IN_EMG4.isOFF)
                {
                    MT.DoorAlarmMsg = string.Format("急停键按下，请确认,系统需重新复位!");
                    WSFEED.bquit = true;
                    UpDownLoad.bquit = true;
                    VAR.gsys_set.bquit = true;
                    MT.OnlyKeyLOn(MT.GPIO_OUT_KL_STOP);
                    if (MT.DoorAlarmMsg != MT.DoorAlarmMsgTemp)
                    {
                        MT.DoorAlarmMsgTemp = MT.DoorAlarmMsg;
                        COUNT_DATA.Errcnt++;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("急停键按下，请确认,系统需重新复位!"));
                        SQLData.TesterrDataAdd(99, 99, "硬件报警", "急停键按下");
                    }

                    return EM_RES.EMG;
                }
            }
            return EM_RES.OK;
        }
        #endregion
        public static bool isAllSafeSen
        {
            get
            {
                if (ChkAllSafeSen() == EM_RES.OK) return true;
                return false;
            }
        }
        #endregion
        #region 板卡初始化
        public static bool isReady
        {
            get
            {
                foreach (CARD card in CardList)
                {
                    if (!card.isReady) return false;
                }
                return true;
            }
        }
        public static EM_RES Init(String filename = "", int timeout = 15000)
        {
            if (isReady) return EM_RES.OK;

            //start init
            List<Task> ListTask = new List<Task>();
            foreach (CARD card in CardList)
            {
                if (!card.isReady)
                {
                    Task TaskHWInit = new Task(() =>
                    {
                        card.Init();
                    });
                    ListTask.Add(TaskHWInit);
                    TaskHWInit.Start();
                }
            }

            if (timeout == 0) return EM_RES.OK;

            //wait
            bool bEnd = true;
            int time = 0;
            do
            {
                //timer
                time += 10;
                if (time >= timeout) return EM_RES.TIMEOUT;

                //check task
                bEnd = true;
                foreach (Task task in ListTask)
                {
                    if (!task.IsCompleted) bEnd = false;
                }
                if (bEnd) break;

                //delay
                Thread.Sleep(10);
                Application.DoEvents();

            } while (true);

            if (isReady) return EM_RES.OK;
            else return EM_RES.ERR;
        }
        #endregion
        #region 检查/重连
        /// <summary>
        /// 检查所有板卡是否在线，否在重连
        /// </summary>
        /// <returns></returns>
        public static EM_RES ChkAndReConnect(string filename = "")
        {
            EM_RES res = EM_RES.OK;
            bool bok = true;
            foreach (CARD card in CardList)
            {
                res = card.ChkOnline(filename);
                if (res != EM_RES.OK) bok = false;
            }
            if (bok) return EM_RES.OK;
            else return EM_RES.ERR;
        }
        #endregion
        #region 关闭控制卡
        public static EM_RES Close()
        {
            EM_RES res = EM_RES.OK;
            bool bok = true;
            foreach (CARD card in CardList)
            {
                res = card.Close();
                if (res != EM_RES.OK) bok = false;
            }
            if (bok) return EM_RES.OK;
            else return EM_RES.ERR;
        }
        #endregion

        #region 设置轴到工作速度
        public static EM_RES SetAllAxToWorkSpd(double persent = 1.0)
        {
            EM_RES ret = EM_RES.OK;
            foreach (CARD card in CardList)
            {
                if (card.AxList == null) continue;
                foreach (AXIS ax in card.AxList)
                {
                    ret = ax.SetToWorkSpd(persent);
                    if (ret != EM_RES.OK) return ret;
                }
            }
            return ret;
        }
        public static EM_RES SetAllAxToWorkSpdPre(double persent = 1.0)
        {
            EM_RES ret = EM_RES.OK;

            foreach (AXIS ax in Axlist_UDL2)
            {
                ret = ax.SetToWorkSpd(persent);
                if (ret != EM_RES.OK) return ret;
            }

            foreach (AXIS ax in Axlist_UDL_LC)
            {
                ret = ax.SetToWorkSpd(persent);
                if (ret != EM_RES.OK) return ret;
            }


            return ret;
        }
        
        public static EM_RES SetAllAxToManualSpd()
        {
            EM_RES ret = EM_RES.OK;
            foreach (CARD card in CardList)
            {
                if (card.AxList == null) continue;
                foreach (AXIS ax in card.AxList)
                {
                    ret = ax.SetToManualHighSpd();
                    if (ret != EM_RES.OK) return ret;
                }
            }
            return ret;
        }
        //public static EM_RES SetMainAxToCaliSpd()
        //{
        //    EM_RES ret = EM_RES.OK;
        //    foreach (AXIS ax in AxisListMain)
        //    {
        //        ret = ax.SetSpeed(ax.spd_start, ax.spd_work / 3, ax.spd_stop, 1, 1);
        //        if (ret != EM_RES.OK) return ret;
        //    }
        //    return ret;
        //}
        #endregion
        #region 所有轴停止
        public static void AllAxStop(List<AXIS> list_ax = null)
        {
            if (list_ax == null)
            {
                foreach (CARD card in CardList)
                {
                    if (card.AxList == null) continue;
                    card.AllCardStop();
                }
            }
            else
            {
                foreach (AXIS ax in list_ax)
                {
                    ax.Stop();
                }
            }

        }
        public static bool AllAxSvrOn()
        {
            bool all_on = true;
            foreach (CARD card in CardList)
            {
                if (card.AxList == null) continue;
                foreach (AXIS ax in card.AxList)
                {
                    if (ax.mt_type != AXIS.MT_TYPE.VIRTUAL && !ax.isSVRON)
                    {
                        all_on = false;
                        ax.SVRON = true;
                    }
                }
            }
            return all_on;
        }
        #endregion
        #region 先升Z再定位
        public static EM_RES ZupMove(ref bool bquit, ref AXIS ax_x, double xpos, bool OtherMov=false, int time_out_ms = 20000, bool bdoevent = false)
        {
            AXIS ax_null = null;
            return ZupMove(ref bquit, ref ax_x, xpos, ref ax_null, 0, ref ax_null, 0, ref ax_null, 0,false,time_out_ms, bdoevent);
        }
        public static EM_RES ZupMove(ref bool bquit, ref AXIS ax_x, double xpos, ref AXIS ax_y, double ypos, bool OtherMov = false, int time_out_ms = 20000, bool bdoevent = false)
        {
            AXIS ax_null = null;
            return ZupMove(ref bquit, ref ax_x, xpos, ref ax_y, ypos, ref ax_null, 0, ref ax_null, 0, false, time_out_ms, bdoevent);
        }
        public static EM_RES ZupMove(ref bool bquit, ref AXIS ax_x, double xpos, ref AXIS ax_y, double ypos, ref AXIS ax_z, double zpos, bool OtherMov = false, int time_out_ms = 20000, bool bdoevent = false)
        {
            AXIS ax_null = null;
            return ZupMove(ref bquit, ref ax_x, xpos, ref ax_y, ypos, ref ax_z, zpos, ref ax_null, 0, false, time_out_ms, bdoevent);
        }
        public static EM_RES ZupMove(ref bool bquit, ref AXIS ax_x, double xpos, ref AXIS ax_y, double ypos, ref AXIS ax_z, double zpos, ref AXIS ax_r, double rpos, bool OtherMov = false, int time_out_ms = 20000, bool bdoevent = false,bool hand=false)
        {
            EM_RES ret;
            bool bz1_up = false;
            bool bz2_up = false;
            double gopos=0;

            if (ax_x != null)
            {
                if (ax_x.m_id == MT.AXIS_UDL1_X.m_id || ax_x.m_id == MT.AXIS_UDL1_Y.m_id || ax_x.m_id == MT.AXIS_UDL1_U1.m_id || ax_x.m_id == MT.AXIS_UDL1_U2.m_id) bz1_up = true;
                if (ax_x.m_id == MT.AXIS_UDL2_X.m_id || ax_x.m_id == MT.AXIS_UDL2_Y.m_id || ax_x.m_id == MT.AXIS_UDL2_U1.m_id || ax_x.m_id == MT.AXIS_UDL2_U2.m_id) bz2_up = true;
            }

            if (ax_x == null && ax_y != null)
            {
                if (ax_y.m_id == MT.AXIS_UDL1_X.m_id || ax_y.m_id == MT.AXIS_UDL1_Y.m_id || ax_y.m_id == MT.AXIS_UDL1_U1.m_id || ax_y.m_id == MT.AXIS_UDL1_U2.m_id) bz1_up = true;
                if (ax_y.m_id == MT.AXIS_UDL2_X.m_id || ax_y.m_id == MT.AXIS_UDL2_Y.m_id || ax_y.m_id == MT.AXIS_UDL2_U1.m_id || ax_y.m_id == MT.AXIS_UDL2_U2.m_id) bz2_up = true;
            }

            if (ax_x == null && ax_y == null && ax_z != null)
            {
                if (ax_z.m_id == MT.AXIS_UDL1_X.m_id || ax_z.m_id == MT.AXIS_UDL1_Y.m_id || ax_z.m_id == MT.AXIS_UDL1_U1.m_id || ax_z.m_id == MT.AXIS_UDL1_U2.m_id) bz1_up = true;
                if (ax_z.m_id == MT.AXIS_UDL2_X.m_id || ax_z.m_id == MT.AXIS_UDL2_Y.m_id || ax_z.m_id == MT.AXIS_UDL2_U1.m_id || ax_z.m_id == MT.AXIS_UDL2_U2.m_id) bz2_up = true;
            }

            if (ax_x == null && ax_y == null && ax_z == null && ax_r != null)
            {
                if (ax_r.m_id == MT.AXIS_UDL1_X.m_id || ax_r.m_id == MT.AXIS_UDL1_Y.m_id || ax_r.m_id == MT.AXIS_UDL1_U1.m_id || ax_r.m_id == MT.AXIS_UDL1_U2.m_id) bz1_up = true;
                if (ax_r.m_id == MT.AXIS_UDL2_X.m_id || ax_r.m_id == MT.AXIS_UDL2_Y.m_id || ax_r.m_id == MT.AXIS_UDL2_U1.m_id || ax_r.m_id == MT.AXIS_UDL2_U2.m_id) bz2_up = true;
            }

            if (ax_x != null && (ax_x.m_id == MT.AXIS_UDL1_X.m_id || ax_x.m_id == MT.AXIS_UDL2_X.m_id)) gopos = xpos;
            else if (ax_y != null && (ax_y.m_id == MT.AXIS_UDL1_X.m_id || ax_y.m_id == MT.AXIS_UDL2_X.m_id)) gopos = ypos;
            else if (ax_z != null &&(ax_z.m_id == MT.AXIS_UDL1_X.m_id || ax_z.m_id == MT.AXIS_UDL2_X.m_id)) gopos = zpos;
            else if (ax_r != null && (ax_r.m_id == MT.AXIS_UDL1_X.m_id || ax_r.m_id == MT.AXIS_UDL2_X.m_id)) gopos = rpos;

            if (bz1_up)
            {
                if (Math.Abs(MT.AXIS_UDL1_Z.fenc_pos) > 0.5)
                {
                    ret = Move(ref bquit, ref MT.AXIS_UDL1_Z, 0, time_out_ms, bdoevent);
                    if (ret != EM_RES.OK) return ret;
                }
                if ((ax_x !=null && ax_x.m_id == MT.AXIS_UDL1_X.m_id) || (ax_y !=null &&ax_y.m_id == MT.AXIS_UDL1_X.m_id) || (ax_z !=null && ax_z.m_id == MT.AXIS_UDL1_X.m_id) || (ax_r !=null &&ax_r.m_id == MT.AXIS_UDL1_X.m_id))
                {
                    if (Math.Abs(gopos) + Math.Abs(MT.AXIS_UDL2_X.fenc_pos) + xsafedis > UpDownLoad.DisAxisX)
                     {
                        if (VAR.gsys_set.status != EM_SYS_STA.RUN)
                        {
                            ret = MT.Move(ref VAR.gsys_set.bquit, ref COM.UDLoad2.ax_z, 0);
                            if (ret != EM_RES.OK) return ret;
                            ret = MT.Move(ref VAR.gsys_set.bquit, ref COM.UDLoad2.ax_x, 0, ref COM.UDLoad2.ax_y, 0);
                            if (ret != EM_RES.OK) return ret;
                        }
                        else
                        {
                            bool refresh2 = true;
                            int n = 0;
                            while (Math.Abs(gopos) + Math.Abs(MT.AXIS_UDL2_X.fenc_pos) + xsafedis > UpDownLoad.DisAxisX)
                            {
                                if (refresh2)
                                {
                                    refresh2 = false;
                                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("需要安全防护判断-----"));
                                }
                                if (n++ > 100) break;
                                Thread.Sleep(10);
                                Application.DoEvents();
                            }
                            if (n > 100)
                            {
                                ret = MT.Move(ref VAR.gsys_set.bquit, ref COM.UDLoad2.ax_z, 0);
                                if (ret != EM_RES.OK) return ret;
                                ret = MT.Move(ref VAR.gsys_set.bquit, ref COM.UDLoad2.ax_y, COM.UDLoad2.ax_y.fenc_pos, ref COM.UDLoad2.ax_x, COM.UDLoad2.ax_x.fenc_pos < -41 ? COM.UDLoad2.ax_x.fenc_pos + 40 : 0);
                                if (ret != EM_RES.OK) return ret;
                            }
                        }                      
                     }
                }
            }
           
            else if (bz2_up)
            {
                if( Math.Abs(MT.AXIS_UDL2_Z.fenc_pos) > 0.5)
                {
                    ret = Move(ref bquit, ref MT.AXIS_UDL2_Z, 0, time_out_ms, bdoevent);
                    if (ret != EM_RES.OK) return ret;
                }
                if ((ax_x !=null &&ax_x.m_id == MT.AXIS_UDL2_X.m_id) || (ax_y !=null &&ax_y.m_id == MT.AXIS_UDL2_X.m_id) || (ax_z !=null &&ax_z.m_id == MT.AXIS_UDL2_X.m_id) || (ax_r !=null &&ax_r.m_id == MT.AXIS_UDL2_X.m_id))
                {
                    if (Math.Abs(gopos) + Math.Abs(MT.AXIS_UDL1_X.fenc_pos) + xsafedis > UpDownLoad.DisAxisX)
                    {
                        if (VAR.gsys_set.status != EM_SYS_STA.RUN)
                        {
                            ret = MT.Move(ref VAR.gsys_set.bquit, ref COM.UDLoad1.ax_z, 0);
                            if (ret != EM_RES.OK) return ret;
                            ret = MT.Move(ref VAR.gsys_set.bquit, ref COM.UDLoad1.ax_x, 0, ref COM.UDLoad1.ax_y, 0);
                            if (ret != EM_RES.OK) return ret;
                        }
                        else
                        {
                            bool refresh1 = true;
                            int n = 0;
                            while (Math.Abs(gopos) + Math.Abs(MT.AXIS_UDL1_X.fenc_pos) + xsafedis > UpDownLoad.DisAxisX)
                            {
                                if (refresh1)
                                {
                                    refresh1 = false;
                                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("需要安全防护判断-----"));
                                }
                                if (n++ > 100) break;
                                Thread.Sleep(10);
                                Application.DoEvents();
                            }
                            if (n > 100)
                            {
                                ret = MT.Move(ref VAR.gsys_set.bquit, ref COM.UDLoad1.ax_z, 0);
                                if (ret != EM_RES.OK) return ret;
                                ret = MT.Move(ref VAR.gsys_set.bquit, ref COM.UDLoad1.ax_y, 0, ref COM.UDLoad1.ax_x, 0);
                                if (ret != EM_RES.OK) return ret;
                            }
                        }
                    }
                }
            }
            bool refresh = true;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            if (ax_x != null)
            {
                if (VAR.gsys_set.status != EM_SYS_STA.RUN)
                {
                }
                else
                {
                    while ((ax_x.m_id == MT.AXIS_UDL1_X.m_id && WSFEED.wsfeed1unsafe && xpos != 0) || (ax_x.m_id == MT.AXIS_UDL2_X.m_id && WSFEED.wsfeed2unsafe && xpos != 0) && !bquit)
                    {
                        if (refresh)
                        {
                            refresh = false;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}移动时，夹具不在安全状态，等待夹具开合到安全状态-----", ax_x.disc));
                        }
                        Thread.Sleep(10);
                        Application.DoEvents();
                    }
                    sw.Stop();
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}移动时，-----，耗时{1:F}s-----", ax_x.disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds / 1000, 3))));
                }
            }
            ret = Move(ref bquit, ref ax_x, xpos, ref ax_y, ypos, ref ax_z, zpos, ref ax_r, rpos, time_out_ms, bdoevent);
            return ret;
        }
        #endregion
        #region 定位
        public static EM_RES MoveHandle(ref bool bquit, bool bEnX, bool bEnY, bool bEnZ, bool bEnA,bool bIsU1, ref ST_XYZA StPos, string CamName="", int Delay = 200)
        {
            EM_RES ret;
            AXIS ax_x = null, ax_y = null, ax_z = null, ax_u = null, ax_null = null;
            AXIS ax_zz = MT.AXIS_UDL1_Z;
            if (CamName == "CamUp2" || CamName == "CamDw2")
            {
                if (bEnX) ax_x = MT.AXIS_UDL2_X;
                if (bEnY) ax_y = MT.AXIS_UDL2_Y;
                if (bEnZ) ax_z = MT.AXIS_UDL2_Z;
                if (bEnA && bIsU1) ax_u = MT.AXIS_UDL2_U1;
                if (bEnA && !bIsU1) ax_u = MT.AXIS_UDL2_U2;
                ax_zz = MT.AXIS_UDL2_Z;
            }
            else
            {
                if(bEnX) ax_x = MT.AXIS_UDL1_X;
                if (bEnY) ax_y = MT.AXIS_UDL1_Y;
                if (bEnZ) ax_z = MT.AXIS_UDL1_Z;
                if (bEnA && bIsU1) ax_u = MT.AXIS_UDL1_U1;
                if (bEnA && !bIsU1) ax_u = MT.AXIS_UDL1_U2;

            }
  
            ret = Move(ref bquit, ref ax_null, 0, ref ax_null, 0, ref ax_zz, 0, ref ax_null, 0);
            if (ret != EM_RES.OK) return ret;
            //定位
            ret = Move(ref bquit, ref ax_x, StPos.x, ref ax_y, StPos.y, ref ax_null, 0, ref ax_u, StPos.a);
            if (ret != EM_RES.OK) return ret;
            //Z下降
            //ret = Move(ref bquit, ref ax_null,0, ref ax_null,0, ref ax_z, StPos.z, ref ax_null,0);
            //if (ret != EM_RES.OK) return ret;
            Thread.Sleep(Delay);
            if (ax_x != null) StPos.x = ax_x.fenc_pos;
            if (ax_y != null) StPos.y = ax_y.fenc_pos;
            //if (ax_z != null) StPos.z = ax_z.fenc_pos;
            return EM_RES.OK;
        }
        public static EM_RES Move(ref bool bquit, ref AXIS ax_x, double xpos, int time_out_ms = 20000, bool bdoevent = false)
        {
            AXIS ax_null = null;
            return Move(ref bquit, ref ax_x, xpos, ref ax_null, 0, ref ax_null, 0, ref ax_null, 0, ref ax_null, 0, ref ax_null, 0, time_out_ms, bdoevent);
        }
        public static EM_RES Move(ref bool bquit, ref AXIS ax_x, double xpos, ref AXIS ax_y, double ypos, int time_out_ms = 20000, bool bdoevent = false)
        {
            AXIS ax_null = null;
            return Move(ref bquit, ref ax_x, xpos, ref ax_y, ypos, ref ax_null, 0, ref ax_null, 0, ref ax_null, 0, ref ax_null, 0, time_out_ms, bdoevent);
        }
        public static EM_RES Move(ref bool bquit, ref AXIS ax_x, double xpos, ref AXIS ax_y, double ypos, ref AXIS ax_z, double zpos, int time_out_ms = 20000, bool bdoevent = false)
        {
            AXIS ax_null = null;
            return Move(ref bquit, ref ax_x, xpos, ref ax_y, ypos, ref ax_z, zpos, ref ax_null, 0, ref ax_null, 0, ref ax_null, 0, time_out_ms, bdoevent);
        }
        public static EM_RES Move(ref bool bquit, ref AXIS ax_x, double xpos, ref AXIS ax_y, double ypos, ref AXIS ax_z, double zpos, ref AXIS ax_r, double rpos, int time_out_ms = 20000, bool bdoevent = false)
        {
            AXIS ax_null = null;
            return Move(ref bquit, ref ax_x, xpos, ref ax_y, ypos, ref ax_z, zpos,ref ax_r,rpos, ref ax_null, 0, ref ax_null, 0, time_out_ms, bdoevent);
        }
        public static EM_RES Move(ref bool bquit, ref AXIS ax_x, double xpos, ref AXIS ax_y, double ypos, ref AXIS ax_z, double zpos, ref AXIS ax_r, double rpos, ref AXIS ax_w, double wpos, int time_out_ms = 20000, bool bdoevent = false)
        {
            AXIS ax_null = null;
            return Move(ref bquit, ref ax_x, xpos, ref ax_y, ypos, ref ax_z, zpos, ref ax_r, rpos,ref ax_w,wpos,ref ax_null,0, time_out_ms, bdoevent);
        }
        public static EM_RES Move(ref bool bquit, ref AXIS ax_x, double xpos, ref AXIS ax_y, double ypos, ref AXIS ax_z, double zpos, ref AXIS ax_r, double rpos,ref AXIS ax_w,double wpos,ref AXIS ax_u,double upos, int time_out_ms = 20000, bool bdoevent = false)
        {
            EM_RES ret = EM_RES.OK;
            AXIS ax_xtemp;

            bool refresh = true;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            if(ax_x!=null)
            {
                while ((ax_x.m_id == MT.AXIS_UDL1_X.m_id && WSFEED.wsfeed1unsafe && xpos != 0) || (ax_x.m_id == MT.AXIS_UDL2_X.m_id && WSFEED.wsfeed2unsafe && xpos != 0) && !bquit)
                {
                    if (refresh)
                    {
                        refresh = false;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}移动时，夹具不在安全状态，等待夹具开合到安全状态-----", ax_x.disc));
                    }
                    Thread.Sleep(10);
                    Application.DoEvents();
                }
                sw.Stop();
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}移动时-----，耗时{1:F}s-----", ax_x.disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds / 1000, 3))));
            }
           
            //start move
            if (ax_x != null)
            {
                while ((ax_x.m_id == MT.AXIS_UDL1_X.m_id && WSFEED.wsfeed1unsafe && xpos != 0) || (ax_x.m_id == MT.AXIS_UDL2_X.m_id && WSFEED.wsfeed2unsafe && xpos != 0) && !bquit)
                {
                    if (refresh)
                    {
                        refresh = false;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}再次安全判断，夹具不在安全状态，等待夹具开合到安全状态-----", ax_x.disc));
                    }
                    Thread.Sleep(10);
                    Application.DoEvents();
                }
                ret = ax_x.MoveTo(ref bquit, xpos);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_x;
                    goto MOVE_END;
                }
                    
            }
            if (ax_y != null)
            {
                ret = ax_y.MoveTo(ref bquit, ypos);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_y;
                    goto MOVE_END;
                }

            }
            if (ax_z != null)
            {
                ret = ax_z.MoveTo(ref bquit, zpos);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_z;
                    goto MOVE_END;
                }
            }
            if (ax_r != null)
            {
                ret = ax_r.MoveTo(ref bquit, rpos);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_r;
                    goto MOVE_END;
                }
            }
           
            if(ax_w!=null)
            {
                ret = ax_w.MoveTo(ref bquit, wpos);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_w;
                    goto MOVE_END;
                }
            }

            if (ax_u != null)
            {
                ret = ax_u.MoveTo(ref bquit, upos);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_u;
                    goto MOVE_END;
                }
            }
            //wait
            if (ax_x != null)
            {
                ret = ax_x.WaitForMoveDone(ref bquit, xpos, time_out_ms, bdoevent);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_x;
                    goto MOVE_END;
                }
            }
            if (ax_y != null)
            {
                ret = ax_y.WaitForMoveDone(ref bquit, ypos, time_out_ms, bdoevent);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_y;
                    goto MOVE_END;
                }
            }
            if (ax_z != null)
            {
                ret = ax_z.WaitForMoveDone(ref bquit, zpos, time_out_ms, bdoevent);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_z;
                    goto MOVE_END;
                }
            }
            if (ax_r != null)
            {
                ret = ax_r.WaitForMoveDone(ref bquit, rpos, time_out_ms, bdoevent);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_r;
                    goto MOVE_END;
                }
            }
            if (ax_w != null)
            {
                ret = ax_w.WaitForMoveDone(ref bquit, wpos, time_out_ms, bdoevent);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_w;
                    goto MOVE_END;
                }
            }
            if (ax_u != null)
            {
                ret = ax_u.WaitForMoveDone(ref bquit, upos, time_out_ms, bdoevent);
                if (ret != EM_RES.OK)
                {
                    ax_xtemp = ax_u;
                    goto MOVE_END;
                }
            }
            return EM_RES.OK;

            MOVE_END:
            if (ax_x != null) ax_x.Stop();
            if (ax_y != null) ax_y.Stop();
            if (ax_z != null) ax_z.Stop();
            if (ax_r != null) ax_r.Stop();
            if (ax_w != null) ax_w.Stop();
            if (ax_u != null) ax_u.Stop();
            COUNT_DATA.Errcnt++;
            SQLData.TesterrDataAdd(99, 99, "硬件报警", string.Format("{0}轴状态异常", ax_xtemp==null?"":ax_xtemp.disc));
            return ret;
        }
        #endregion
        #region 按键灯
        public static void OnlyKeyLOn(GPIO keyio=null)
        {
            MT.GPIO_OUT_KL_START.SetOff();
            MT.GPIO_OUT_KL_STOP.SetOff();
            MT.GPIO_OUT_KL_RESET.SetOff();
            if (keyio!= null)
            keyio.SetOn();
        }
        public static void OnlyLightOn(GPIO keyio = null)
        {
            MT.GPIO_OUT_ALM_RED.SetOff();
            MT.GPIO_OUT_ALM_YELLOW.SetOff();
            MT.GPIO_OUT_ALM_GREEN.SetOff();
            if (keyio != null)
                keyio.SetOn();
        }
        #endregion
        #region 蜂鸣器
        public static EM_RES Beeper(int tmr)
        {
            Task beep = new Task(() =>
                {
                    if (tmr > 0)
                    {
                        EM_RES ret = GPIO_OUT_ALM_BEEPER.SetOn();
                        if (ret == EM_RES.OK)
                        {
                            Thread.Sleep(tmr);
                            ret = GPIO_OUT_ALM_BEEPER.SetOff();
                        }
                    }
                }
            );
            beep.Start();
            return EM_RES.OK;
        }
        #endregion
        #region 轴复位
        public static EM_RES AxisHome(ref bool bquit, params AXIS[] axs)
        {
            //home task start
            foreach (AXIS ax in axs)
            {
                if (ax != null) ax.HomeTask(10000);
            }

            //wait end
            bool bok = true;
            while (true)
            {
                bok = true;
                foreach (AXIS ax in axs)
                {
                    if (ax != null && !ax.HomeTaskisEnd) bok = false;
                }
                if (bok) break;

                Application.DoEvents();
                Thread.Sleep(10);

                //quit
                if (bquit)
                {
                    foreach (AXIS ax in axs)
                    {
                        if (ax != null && !ax.HomeTaskisEnd) ax.Stop();
                    }
                    return EM_RES.ERR;
                }
            }

            //check result
            bok = true;
            foreach (AXIS ax in axs)
            {
                if (ax != null && ax.home_status != AXIS.HOME_STA.OK) bok = false;
            }
            if (bok == false)
            {
                foreach (AXIS ax in axs)
                {
                    if (ax != null && !ax.HomeTaskisEnd) ax.Stop();
                }
                return EM_RES.ERR;
            }

            return EM_RES.OK;
        }
        public static void AxisHomeQuit(params AXIS[] axs)
        {
            foreach (AXIS ax in axs)
            {
                if (ax != null)
                {
                    ax.bhomequit = true;
                    ax.Stop();
                }
            }
        }
        public static void AxisHomeQuit(List<AXIS> axs)
        {
            foreach (AXIS ax in axs)
            {
                if (ax != null)
                {
                    ax.bhomequit = true;
                    ax.Stop();
                }
            }
        }

        #endregion
        #region 归位或避让
        public static EM_RES gotopos(bool breset = true, bool brun = false)
        {
            EM_RES ret = EM_RES.OK;

            ////检查是否需要移动
            //bool bneedmove = false;
            //if (breset && ((Math.Abs(MT.AXIS_X.fenc_pos) > 0.05))) bneedmove = true;
            //if (breset && ((Math.Abs(MT.AXIS_Y.fenc_pos) > 0.05))) bneedmove = true;
            //if (breset && ((Math.Abs(MT.AXIS_R.fenc_pos) > 0.05))) bneedmove = true;
            //if (!bneedmove) return CONST.RES_OK;

            ////safe check
            //ret = ChkSafeSen();
            //if (ret != CONST.RES_OK) return ret;

            ////正在进出料，不能移动
            //if (!brun && FDH.binuse)
            //{
            //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "出料中gotopos定位");
            //    Thread.Sleep(10);
            //    if (FDH.binuse) return CONST.RES_MOVE_PROTECT;
            //}

            ////运行中，不能移动
            //if (COM.MounterGetRunStatus() == CONST.SYS_STATUS_RUN)
            //{
            //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, "运行中gotopos定位");
            //    return CONST.RES_ERR;
            //}

            //if (brun || VAR.gsys_set.status == CONST.SYS_STATUS_STANDBY || VAR.gsys_set.status == CONST.SYS_STATUS_PAUSE)
            //{
            //    VAR.gsys_set.bquit = false;
            //    ret = MT.ChkSafeSen();
            //    if (ret != CONST.RES_OK) return ret;

            //    if (VAR.gsys_set.status == CONST.SYS_STATUS_STANDBY) VAR.sys_inf.Set(CONST.EM_ALM_STA.NOR_GREEN, "就绪", -1, true);
            //    else if (VAR.gsys_set.status == CONST.SYS_STATUS_PAUSE) VAR.sys_inf.Set(CONST.EM_ALM_STA.NOR_GREEN, "暂停", -1, true);

            //    //set to workspd
            //    foreach (AXIS ax in AxisListExceptFd) ax.SetToWorkSpd();

            //    try
            //    {
            //        bgotopos = true;
            //        if (breset)
            //        {
            //            ret = ZupMove(ref VAR.gsys_set.bquit, ref AXIS_X, 0, ref AXIS_Y, 0, ref AXIS_R, 0, 10000, true);
            //            if (ret != CONST.RES_OK) return CONST.RES_ERR;
            //        }
            //        else
            //        {
            //            ret = ZupMove(ref VAR.gsys_set.bquit, ref AXIS_X, FDH.st_pos_ready.x, ref AXIS_Y, COM.MTER1.pos_br_y, ref AXIS_R, 0, 10000, true);
            //            if (ret != CONST.RES_OK) return CONST.RES_ERR;
            //        }
            //    }
            //    finally
            //    {
            //        bgotopos = false;
            //    }
            //}
            //else
            //{
            //    if (breset) VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "非待机状态禁止复位！");
            //    else VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "非待机状态禁止定位避让位!");
            //    return CONST.RES_ERR;
            //}
            return ret;
        }
        #endregion
        #region 相机触发
        public static EM_RES CamUp1Triger(int dly = 100)
        {
            //EM_RES res = GPIO_OUT_UL_CAM_UP1.SetOn();
            //Thread.Sleep(dly);
            //res = GPIO_OUT_UL_CAM_UP1.SetOff();          
            EM_RES res = GPIO_OUT_UL_CAM_UP1.SetOff();
            Thread.Sleep(dly);
            GPIO_OUT_UL_CAM_UP1.SetOn();

            return res;
        }
        public static EM_RES CamUp2Triger(int dly = 100)
        {
            //EM_RES res = GPIO_OUT_UL_CAM_UP2.SetOn();
            //Thread.Sleep(dly);
            //res = GPIO_OUT_UL_CAM_UP2.SetOff();    
            EM_RES res = GPIO_OUT_UL_CAM_UP2.SetOff();
            Thread.Sleep(dly);
            GPIO_OUT_UL_CAM_UP2.SetOn();
  
            return res;
        }
        public static EM_RES CamDw1Triger(int dly = 100)
        {
            //EM_RES res = GPIO_OUT_UL_CAM_DW1.SetOn();
            //Thread.Sleep(dly);
            //res = GPIO_OUT_UL_CAM_DW1.SetOff();   
            EM_RES res = GPIO_OUT_UL_CAM_DW1.SetOff();
            Thread.Sleep(dly);
            res = GPIO_OUT_UL_CAM_DW1.SetOn();

            return res;
        }
        public static EM_RES CamDw2Triger(int dly = 100)
        {
            //EM_RES res = GPIO_OUT_UL_CAM_DW2.SetOn();
            //Thread.Sleep(dly);
            //res = GPIO_OUT_UL_CAM_DW2.SetOff();
            EM_RES res = GPIO_OUT_UL_CAM_DW2.SetOff();
            Thread.Sleep(dly);
            res = GPIO_OUT_UL_CAM_DW2.SetOn();
            //VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "CamDown Triger");
            return res;
        }
        #endregion
        #region SecsParam
        public static bool IsAllowStart = false;
        public static bool IsAllowStartUpdate = false;
        public static bool IsAllowPointStart = false;//点检启动检查
        public static bool IsAllowPointStartUpdate = false;//点检启动信息更新

        #endregion
    }
}
