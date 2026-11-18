using DevReport;
using MotionCtrl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UI
{
    public class WSFEED
    {
        #region 参数
        public static bool bquit = false;
        public static bool bpause = false;
        public static bool Demo = false;//空运行  
        public bool Gotostarttestpos = false;
        public bool Isstartchart = false; //是否已经开图
        public int id;
        private readonly HashSet<int> preMiniAutoCheckedPosIds = new HashSet<int>();
        private bool preMiniAutoCheckStarted = false;
        public bool bjigSan = false;//夹具二维码扫码gy0123
        public bool wsfeedcanopen = true;
        public string disc
        {
            get
            {
                if (id == 0) return string.Format("右测试模块");
                if (id == 1) return string.Format("左测试模块");
                else return string.Format("未知模块");
            }
        }
        public AXIS ax_y = null;//移动Y轴
        public AXIS ax_z_fr = null;//光源Z轴
        public AXIS ax_z_bk = null;//增距镜Z轴
        public AXIS ax_x = null;//增距镜X轴
        public AXIS ax_u = null;//翻转U轴
        public GPIO gpio_openfr;//真空       
        public List<Cylinder> list_cyl = null;//测试工位上面的气缸列表
        public const int cnt = 2;
        public bool benable = true;//是否使用该工位
        public bool issfr = false;//是否是SFR测试 
        public static bool IsFirst = true;//首次开始标志位
        public bool IsTestFirst = true;   //测试首次运行开始标志位           
        public int cm_row = 2; //测试模组行列
        public int cm_col = 4;
        public int cm_per_box = 1; //每工装测试模组
        public ST_XYZA feedpos;//上料位置,y是上下料Y轴
        public ST_XYZZA testpos;//普通测试位置，z是光源的前Z轴，Y是上下料Y轴,U 轴测试位置
        //public ST_XYZA sfrtestpos;//sfr测试位置，x、z是增距镜，y是上下料Y轴,U 轴测试位置
        //public ST_XYZA sfrtestpos2;//sfr测试位置2，x、z是增距镜，y是上下料Y轴,U 轴测试位置
        public ST_XYZA safeuturnpos;//U轴翻转位置,Y 是上下料y轴
        public bool breschanged = true;
        public List<MdDat> list_md = new List<MdDat>();//模组列表
        public int ReTest = 0;
        public bool bRetest = false;
        public bool isfinsh = false;
        public bool isclearfinsh = false;
        public bool iswaitfeed = false;

        public bool Iscanopen = false;//用于一起打开夹具的判断
        public bool Iscanclose = false;//用于一起关闭夹具的判断
        //GRR测试流程参数
        public int GrrTestCnt = 0;
        public int GrrUDLcnt = 0;
        //点检测试次数
        public int ChkCnt = 0;
        public double safeypos = 114;//测试Y轴安全防护位置，超过需要进行防护
        public double safeyposbk = 259;//测试Y轴安全防护位置，超过需要进行防护 199
        public static bool wsfeed1unsafe = false;
        public static bool wsfeed2unsafe = false;
        #endregion

        #region 模组信息
        public List<List<MdDat>> list_list_md = new List<List<MdDat>>();

        public enum Md_RES
        {
            [Description("OK")]
            OK = 0,
            [Description("开图NG1")]
            NG_IMAGE = 258,
            [Description("开图NG2")]
            NG_IMAGE2 = 266,
            [Description("OS异常")]
            NG_OS = 270,
            [Description("图像丢桢")]
            NG_MISS_PIX = 274,
            [Description("曝光NG")]
            NG_EXPOSURE = 512,
            [Description("IIC读写NG")]
            NG_IIC = 271,
        }
        /// <summary>
        /// 按PC_ID分类,按序号排序。在加载/保存时候调用。存储于list_list_md
        /// </summary>
        /// <returns></returns>
        public EM_RES SortMdDat()
        {
            List<MdDat> list_md_temp = new List<MdDat>();
            foreach (MdDat md in list_md) list_md_temp.Add(md);

            //按PC_ID排序            
            list_md_temp.Sort(delegate (MdDat x, MdDat y) { return x.PC_ID.CompareTo(y.PC_ID); });
            //提取            
            List<MdDat> list_pc = new List<MdDat>();
            int id_temp = -1;
            list_list_md.Clear();
            foreach (MdDat md in list_md_temp)
            {
                if (md.PC_ID < 1) continue;
                if (id_temp != md.PC_ID)
                {
                    list_pc = new List<MdDat>();
                    list_list_md.Add(list_pc);
                }
                list_pc.Add(md);
                id_temp = md.PC_ID;
            }

            //按序号排序
            foreach (List<MdDat> list in list_list_md)
            {
                list.Sort(delegate (MdDat x, MdDat y) { return x.SN.CompareTo(y.SN); });
            }

            //初始化PC信息
            list_pc_dat.Clear();
            foreach (List<MdDat> list in list_list_md)
            {
                if (list.Count == 0) continue;
                PCDat pc = new PCDat();
                pc.ID = list[0].PC_ID;
                pc.status = EM_PC_STA.READY;
                MdDat md = list.Find(s => { return s.benable == true; });
                if (md == null) pc.status = EM_PC_STA.DISABLE;
                list_pc_dat.Add(pc);
            }

            return EM_RES.OK;
        }

        /// <summary>
        /// 复位所有模组状态到待测(-1)
        /// </summary>
        /// <returns></returns>
        public int ResetResultOfMd()
        {
            foreach (MdDat md in list_md)
            {
                if (!md.benable) continue;
                md.res = -1;
            }
            breschanged = true;
            return 0;
        }
        public int ResetResultOfMdTest()
        {
            Random rd = new Random();

            foreach (MdDat md in list_md)
            {
                if (!md.benable) continue;
                md.res = rd.Next(0, 1);
            }
            breschanged = true;
            return 0;
        }
        /// <summary>
        /// 返回指定状态的模组列表
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public List<MdDat> GetMdByRes(int res = -1)
        {
            return list_md.FindAll(s => { return s.res == res; });
        }

        /// <summary>
        /// 取出指定编号指定状态的模组
        /// </summary>
        /// <param name="idx">取指定编号，且不为空的模组</param>
        /// <param name="res">取不指定编号，第一个满足指定状态的模组</param>
        /// <returns></returns>
        public EM_RES Pull(ref MdDat md, int idx = -1, int res = -2)
        {
            MdDat md_temp = list_md.Find(s =>
            {
                //取指定编号且不为空的模组
                if (idx != -1 && s.Num == idx && res != -2) return true;
                //取不指定编号，第一个满足状态的模组
                if (idx == -1 && s.res == res) return true;
                return false;
            });
            if (md_temp == null) return EM_RES.PARA_OUTOFRANG;
            //复制
            md = md_temp.Clone();
            //清空
            md_temp.res = -2;
            md_temp.bardcode = "";
            return EM_RES.OK;
        }
        /// <summary>
        /// 放置模组到测试工位
        /// </summary>
        /// <param name="md">模组包含二维码bardcode，要放置的位置Num(-1为第一空位置），</param>
        /// <returns></returns>
        public EM_RES Push(MdDat md)
        {
            MdDat md_temp = list_md.Find(s =>
            {
                //为空
                if (s.res == -2)
                {
                    if (md.Num == -1 || md.Num == s.Num)
                        return true;
                    else return false;
                }
                return false;
            });
            if (md_temp == null) return EM_RES.PARA_OUTOFRANG;

            md_temp.res = md.res;
            md_temp.bardcode = md.bardcode;
            md_temp.ct = 0;
            md_temp.test_idx = 0;
            return EM_RES.OK;
        }
        #endregion

        #region 位置信息
        public class PosDef
        {
            public int ID;
            public string Name;
            public double X;
            public double Y;
            public double U;
            public double Z1;
            public double Z2;
            public int Delay;
            public int Channel;
            public bool AutoCheckEnable;
            public double ActualDistanceParam;
            public double DistanceThreshold;
            public double DistanceComp;
            public double ActualLuxParam;
            public double LuxThreshold;
            public double ActualCctParam;
            public double CctThreshold;
            /// <summary>
            ///// 对应工站补偿
            ///// </summary>
            //public PosCmpDef[] Cmp = new PosCmpDef[4];
            ///// <summary>
            ///// 提取工站补偿
            ///// </summary>
            ///// <param name="cmp_idx">工站号</param>
            ///// <returns></returns>
            //public PosCmpDef GetCmp(int cmp_idx)
            //{
            //    if (cmp_idx >= 0 && cmp_idx < Cmp.Length && Cmp[cmp_idx] != null)
            //    {
            //        PosCmpDef cmp = new PosCmpDef();
            //        cmp.X1 = Math.Abs(Cmp[cmp_idx].X1) < 1000 ? Cmp[cmp_idx].X1 : 0;
            //        cmp.X2 = Math.Abs(Cmp[cmp_idx].X2) < 1000 ? Cmp[cmp_idx].X2 : 0;
            //        cmp.Y1 = Math.Abs(Cmp[cmp_idx].Y1) < 1000 ? Cmp[cmp_idx].Y1 : 0;
            //        cmp.Z1 = Math.Abs(Cmp[cmp_idx].Z1) < 1000 ? Cmp[cmp_idx].Z1 : 0;
            //        cmp.Z2 = Math.Abs(Cmp[cmp_idx].Z2) < 1000 ? Cmp[cmp_idx].Z2 : 0;
            //        if (cmp.X1 != 0 || cmp.Y1 != 0 || cmp.X2 != 0 || cmp.Z1 != 0 || cmp.Z2 != 0)
            //        {
            //            return cmp;
            //        }
            //    }
            //    return null;
            //}
        }

        public PosDef CurPosDef
        {
            get
            {
                PosDef pos = new PosDef();
                pos.X = ax_x != null ? ax_x.fcmd_pos : 0;
                pos.Y = ax_y != null ? ax_y.fenc_pos : 0;
                pos.U = ax_u != null ? ax_u.fcmd_pos : 0;
                pos.Z1 = ax_z_fr != null ? ax_z_fr.fenc_pos : 0;
                pos.Z2 = ax_z_bk != null ? ax_z_bk.fenc_pos : 0;

                return ListPos.Find(s => (Math.Abs(s.X - pos.X) < 0.05 && Math.Abs(s.Y - pos.Y) < 0.05 && Math.Abs(s.U - pos.U) < 0.05 && Math.Abs(s.Z1 - pos.Z1) < 0.05 && Math.Abs(s.Z2 - pos.Z2) < 0.05));
            }
        }
        public List<PosDef> ListPos = new List<PosDef>();

        #endregion

        #region 构造函数
        public WSFEED()
        {
        }
        public WSFEED(int id, AXIS ax_y, AXIS ax_z_fr, AXIS ax_z_bk, AXIS ax_x, AXIS ax_u, GPIO gpio_openfr, List<Cylinder> list_cyl, int cm_row = 2, int cm_col = 4, int cm_per_box = 1)
        {
            this.id = id;
            this.ax_y = ax_y;
            this.ax_z_fr = ax_z_fr;
            this.ax_z_bk = ax_z_bk;
            this.ax_x = ax_x;
            this.ax_u = ax_u;
            this.gpio_openfr = gpio_openfr;
            this.list_cyl = list_cyl;
            this.cm_row = cm_row;
            this.cm_col = cm_col;
            this.cm_per_box = cm_per_box;

            //if (ax_y != null) ax_y.ChkSafeSen = safe_chk;
            //if (ax_z_fr != null) ax_z_fr.ChkSafeSen = safe_chk;
            //if (ax_z_bk != null) ax_z_bk.ChkSafeSen = safe_chk;
            //if (ax_x != null) ax_x.ChkSafeSen = safe_chk;
            //if (ax_u != null) ax_u.ChkSafeSen = safe_chk;

            //if (ax_y != null) ax_y.ChkSafePos = safe_chk;
            //if (ax_z_fr != null) ax_z_fr.ChkSafePos = safe_chk;
            //if (ax_z_bk != null) ax_z_bk.ChkSafePos = safe_chk;
            //if (ax_x != null) ax_x.ChkSafePos = safe_chk;
            //if (ax_u != null) ax_u.ChkSafePos = safe_chk;

            Random rdm = new Random();
            list_md.Clear();
            for (int m = 0; m < 8; m++)
            {
                MdDat md = new MdDat();
                md.res = rdm.Next(-2, 2);
                if (md.res > 0) md.res += 10000;
                list_md.Add(md);
            }
        }
        #endregion

        #region 参数存取
        public EM_RES LoadCfg(string productname)
        {
            if (productname.Length < 3)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}初始化对应产品名{1}异常(<3个字符)!", disc, productname));
                return EM_RES.PARA_ERR;
            }
            //产品参数
            string filename = string.Format("{0}\\product\\{1}\\WSFEED{2}Cfg.ini", Path.GetFullPath(".."), productname, id + 1);
            if (!Directory.Exists(Path.GetDirectoryName(filename))) return EM_RES.PARA_ERR;
            IniFile inf = new IniFile(filename);
            string str_section = "";
            issfr = inf.ReadBool("SFR", "SFR", false);
            feedpos.y = inf.ReadDouble("FEEDPOS", "FEED_POS_Y", 0);
            testpos.x = inf.ReadDouble("TESTPOS", "TEST_POS_X", 0);
            testpos.y = inf.ReadDouble("TESTPOS", "TEST_POS_Y", 0);
            testpos.z1 = inf.ReadDouble("TESTPOS", "TEST_POS_Z1", 0);
            testpos.z2 = inf.ReadDouble("TESTPOS", "TEST_POS_Z2", 0);
            testpos.a = inf.ReadDouble("TESTPOS", "TEST_POS_U", 0);
            safeuturnpos.y = inf.ReadDouble("SAFEUTURNPOS", "SAFEUTURNPOS_Y", 0);
            List<MdDat> list_temp = new List<MdDat>();
            // list_md.Clear();
            for (int n = 1; n <= 32; n++)
            {
                MdDat md = new MdDat();
                str_section = string.Format("MD{0}", n);
                md.Num = inf.ReadInteger(str_section, "NUM", -1);
                if (md.Num < 0) break;

                for (int i = 0; i < cnt; i++)
                {
                    md.st_place_pos[i].x = inf.ReadDouble(str_section, "X" + (i + 1).ToString(), double.MaxValue);
                    md.st_place_pos[i].y = inf.ReadDouble(str_section, "Y" + (i + 1).ToString(), double.MaxValue);
                    md.st_place_pos[i].z = inf.ReadDouble(str_section, "Z" + (i + 1).ToString(), double.MaxValue);

                    md.st_pickpos[i].x = inf.ReadDouble(str_section, "PX" + (i + 1).ToString(), double.MaxValue);
                    md.st_pickpos[i].y = inf.ReadDouble(str_section, "PY" + (i + 1).ToString(), double.MaxValue);
                    md.st_pickpos[i].z = inf.ReadDouble(str_section, "PZ" + (i + 1).ToString(), double.MaxValue);
                    //夹具拍照位置 gy0123
                    md.st_jigpos[i].x = inf.ReadDouble(str_section, "JX" + (i + 1).ToString(), double.MaxValue);
                    md.st_jigpos[i].y = inf.ReadDouble(str_section, "JY" + (i + 1).ToString(), double.MaxValue);
                    md.st_jigpos[i].z = inf.ReadDouble(str_section, "JZ" + (i + 1).ToString(), double.MaxValue);

                    md.st_CapQrcodePos[i].x = inf.ReadDouble(str_section, "CapQrcodeX" + (i + 1).ToString(), double.MaxValue);
                    md.st_CapQrcodePos[i].y = inf.ReadDouble(str_section, "CapQrcodeY" + (i + 1).ToString(), double.MaxValue);
                    md.st_CapQrcodePos[i].z = inf.ReadDouble(str_section, "CapQrcodeZ" + (i + 1).ToString(), double.MaxValue);
                }
                //夹具二维码gy0123
                md.jig_ID = inf.ReadString(str_section, "JIG_ID", "null");
                md.cnt_ok = inf.ReadInteger(str_section, "CNT_OK", 0);
                md.cnt_ng_exposure = inf.ReadInteger(str_section, "CNT_EXSPOSURE", 0);
                md.cnt_ng_iic = inf.ReadInteger(str_section, "CNT_IIC", 0);
                md.cnt_ng_image = inf.ReadInteger(str_section, "CNT_IMAGE", 0);
                md.cnt_ng_miss_pix = inf.ReadInteger(str_section, "CNT_MISS_PIX", 0);
                md.cnt_ng_OS = inf.ReadInteger(str_section, "CNT_OS", 0);
                md.cnt_ng_other = inf.ReadInteger(str_section, "CNT_OTHER", 0);

                md.PC_ID = inf.ReadInteger(str_section, "PC_ID", -1);
                if (md.PC_ID < 0) break;
                md.PC_IP = inf.ReadString(str_section, "PC_IP", "");
                md.TestBox_ID = inf.ReadInteger(str_section, "TESTBOX_ID", -1);
                if (md.PC_ID < 0) break;
                md.SN = inf.ReadInteger(str_section, "SN", md.SN);
                if (md.SN < 0) break;
                md.benable = inf.ReadBool(str_section, "EN", true);
                if (IsFirst) md.res = -2;
                else md.res = list_md[n - 1].res;
                md.bardcode = list_md[n - 1].bardcode;
                md.WS_ID = id;
                list_temp.Add(md);
            }

            if (list_temp.Count > 0)
            {
                list_md.Clear();
                list_md = list_temp;
                SortMdDat();
                return EM_RES.OK;
            }
            else return EM_RES.ERR;
        }


        public EM_RES SaveCfg(string productname)
        {
            if (productname.Length < 3)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}初始化对应产品名{1}异常(<3个字符)!", disc, productname));
                return EM_RES.PARA_ERR;
            }
            //产品参数
            string filename = string.Format("{0}\\product\\{1}\\WSFEED{2}Cfg.ini", Path.GetFullPath(".."), productname, id + 1);
            if (!Directory.Exists(Path.GetDirectoryName(filename))) return EM_RES.PARA_ERR;

            IniFile inf = new IniFile(filename);
            string str_section = "";
            bool ischange = false;
            inf.WriteBool("SFR", "SFR", issfr, ref ischange);
            inf.WriteDouble("FEEDPOS", "FEED_POS_Y", feedpos.y, ref ischange);
            inf.WriteDouble("TESTPOS", "TEST_POS_X", testpos.x, ref ischange);
            inf.WriteDouble("TESTPOS", "TEST_POS_Y", testpos.y, ref ischange);
            inf.WriteDouble("TESTPOS", "TEST_POS_Z1", testpos.z1, ref ischange);
            inf.WriteDouble("TESTPOS", "TEST_POS_Z2", testpos.z2, ref ischange);
            inf.WriteDouble("TESTPOS", "TEST_POS_U", testpos.a, ref ischange);
            //inf.WriteDouble("SFRTESTPOS", "SFRTEST_POS_X", sfrtestpos.x, ref ischange);
            //inf.WriteDouble("SFRTESTPOS", "SFRTEST_POS_Y", sfrtestpos.y, ref ischange);
            //inf.WriteDouble("SFRTESTPOS", "SFRTEST_POS_Z", sfrtestpos.z, ref ischange);
            //inf.WriteDouble("SFRTESTPOS", "SFRTEST_POS_U", sfrtestpos.a, ref ischange);
            //inf.WriteDouble("SFRTESTPOS", "SFRTEST_POS2_X", sfrtestpos2.x, ref ischange);
            //inf.WriteDouble("SFRTESTPOS", "SFRTEST_POS_Y", sfrtestpos2.y, ref ischange);
            //inf.WriteDouble("SFRTESTPOS", "SFRTEST_POS2_Z", sfrtestpos2.z, ref ischange);
            //inf.WriteDouble("SFRTESTPOS", "SFRTEST_POS_U", sfrtestpos2.a, ref ischange);
            inf.WriteDouble("SAFEUTURNPOS", "SAFEUTURNPOS_Y", safeuturnpos.y, ref ischange);

            for (int n = 0; n < 500; n++)
            {
                str_section = string.Format("POS{0}", n);
                PosDef pos = ListPos.Find(s => s.ID == n);
                if (null != pos)
                {
                    inf.WriteString(str_section, "Name", pos.Name, ref ischange, true, filename);
                    inf.WriteInteger(str_section, "ID", pos.ID, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "X", pos.X, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "Y", pos.Y, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "U", pos.U, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "Z1", pos.Z1, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "Z2", pos.Z2, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "Channel", pos.Channel, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "Delay", pos.Delay, ref ischange, true, filename);
                }
                else
                {
                    inf.WriteString(str_section, null, null, ref ischange, false);
                }
            }


            //delete
            foreach (MdDat md in list_md)
            {
                str_section = string.Format("MD{0}", md.Num);
                inf.WriteInteger(str_section, "NUM", md.Num, ref ischange);
                for (int i = 0; i < cnt; i++)
                {
                    inf.WriteDouble(str_section, "X" + (i + 1).ToString(), md.st_place_pos[i].x, ref ischange);
                    inf.WriteDouble(str_section, "Y" + (i + 1).ToString(), md.st_place_pos[i].y, ref ischange);
                    inf.WriteDouble(str_section, "Z" + (i + 1).ToString(), md.st_place_pos[i].z, ref ischange);

                    inf.WriteDouble(str_section, "PX" + (i + 1).ToString(), md.st_pickpos[i].x, ref ischange);
                    inf.WriteDouble(str_section, "PY" + (i + 1).ToString(), md.st_pickpos[i].y, ref ischange);
                    inf.WriteDouble(str_section, "PZ" + (i + 1).ToString(), md.st_pickpos[i].z, ref ischange);
                    //夹具拍照位置gy0123
                    inf.WriteDouble(str_section, "JX" + (i + 1).ToString(), md.st_jigpos[i].x, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "JY" + (i + 1).ToString(), md.st_jigpos[i].y, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "JZ" + (i + 1).ToString(), md.st_jigpos[i].z, ref ischange, true, filename);

                    inf.WriteDouble(str_section, "CapQrcodeX" + (i + 1).ToString(), md.st_CapQrcodePos[i].x, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "CapQrcodeY" + (i + 1).ToString(), md.st_CapQrcodePos[i].y, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "CapQrcodeZ" + (i + 1).ToString(), md.st_CapQrcodePos[i].z, ref ischange, true, filename);
                }
                //夹具二维码gy0123和夹具统计
                inf.WriteString(str_section, "JIG_ID", md.jig_ID, ref ischange, true, filename);
                inf.WriteInteger(str_section, "CNT_OK", md.cnt_ok, ref ischange, true, filename);
                inf.WriteInteger(str_section, "CNT_EXSPOSURE", md.cnt_ng_exposure, ref ischange, true, filename);
                inf.WriteInteger(str_section, "CNT_IIC", md.cnt_ng_iic, ref ischange, true, filename);
                inf.WriteInteger(str_section, "CNT_IMAGE", md.cnt_ng_image, ref ischange, true, filename);
                inf.WriteInteger(str_section, "CNT_MISS_PIX", md.cnt_ng_miss_pix, ref ischange, true, filename);
                inf.WriteInteger(str_section, "CNT_OS", md.cnt_ng_OS, ref ischange, true, filename);
                inf.WriteInteger(str_section, "CNT_OTHER", md.cnt_ng_other, ref ischange, true, filename);

                inf.WriteInteger(str_section, "PC_ID", md.PC_ID, ref ischange);
                inf.WriteString(str_section, "PC_IP", md.PC_IP, ref ischange);
                inf.WriteInteger(str_section, "TESTBOX_ID", md.TestBox_ID, ref ischange);
                inf.WriteInteger(str_section, "SN", md.SN, ref ischange);//序号
                inf.WriteBool(str_section, "EN", md.benable, ref ischange);//是否使用
            }
            SortMdDat();
            return EM_RES.OK;
        }

        public EM_RES LoadCfgws(string productname)
        {
            if (productname.Length < 3)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}初始化对应产品名{1}异常(<3个字符)!", disc, productname));
                return EM_RES.PARA_ERR;
            }
            //产品参数
            string filename = string.Format("{0}\\product\\{1}\\WSFEEDWS{2}Cfg.ini", Path.GetFullPath(".."), productname, id + 1);
            if (!Directory.Exists(Path.GetDirectoryName(filename))) return EM_RES.PARA_ERR;
            IniFile inf = new IniFile(filename);
            string str_section = "";
            ListPos.Clear();
            for (int n = 0; n < 500; n++)
            {
                PosDef pos = new PosDef();
                str_section = string.Format("POS{0}", n);
                //ID
                pos.ID = inf.ReadInteger(str_section, "ID", -1);
                if (pos.ID < 0) continue;
                pos.ID = n;
                //Name
                pos.Name = inf.ReadString(str_section, "Name", "");
                if (pos.Name.Length < 1) continue;

                pos.X = inf.ReadDouble(str_section, "X", double.MaxValue);
                pos.Y = inf.ReadDouble(str_section, "Y", double.MaxValue);
                pos.U = inf.ReadDouble(str_section, "U", double.MaxValue);
                pos.Z1 = inf.ReadDouble(str_section, "Z1", double.MaxValue);
                pos.Z2 = inf.ReadDouble(str_section, "Z2", double.MaxValue);
                pos.Channel = inf.ReadInteger(str_section, "Channel", int.MaxValue);
                pos.Delay = inf.ReadInteger(str_section, "Delay", 0);
                pos.AutoCheckEnable = inf.ReadBool(str_section, "AutoCheckEnable", false);
                pos.ActualDistanceParam = inf.ReadDouble(str_section, "ActualDistanceParam", 0);
                pos.DistanceThreshold = inf.ReadDouble(str_section, "DistanceThreshold", 0);
                pos.DistanceComp = inf.ReadDouble(str_section, "DistanceComp", 0);
                pos.ActualLuxParam = inf.ReadDouble(str_section, "ActualLuxParam", 0);
                pos.LuxThreshold = inf.ReadDouble(str_section, "LuxThreshold", 0);
                pos.ActualCctParam = inf.ReadDouble(str_section, "ActualCctParam", 0);
                pos.CctThreshold = inf.ReadDouble(str_section, "CctThreshold", 0);
                ListPos.Add(pos);
            }
            return EM_RES.OK;
        }

        public EM_RES SaveCfgws(string productname)
        {
            if (productname.Length < 3)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}初始化对应产品名{1}异常(<3个字符)!", disc, productname));
                return EM_RES.PARA_ERR;
            }
            //产品参数
            string filename = string.Format("{0}\\product\\{1}\\WSFEEDWS{2}Cfg.ini", Path.GetFullPath(".."), productname, id + 1);
            if (!Directory.Exists(Path.GetDirectoryName(filename))) return EM_RES.PARA_ERR;

            IniFile inf = new IniFile(filename);
            string str_section = "";
            bool ischange = false;
            for (int n = 0; n < 500; n++)
            {
                str_section = string.Format("POS{0}", n);
                PosDef pos = ListPos.Find(s => s.ID == n);
                if (null != pos)
                {
                    inf.WriteString(str_section, "Name", pos.Name, ref ischange, true, filename);
                    inf.WriteInteger(str_section, "ID", pos.ID, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "X", pos.X, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "Y", pos.Y, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "U", pos.U, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "Z1", pos.Z1, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "Z2", pos.Z2, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "Channel", pos.Channel, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "Delay", pos.Delay, ref ischange, true, filename);
                    inf.WriteBool(str_section, "AutoCheckEnable", pos.AutoCheckEnable, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "ActualDistanceParam", pos.ActualDistanceParam, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "DistanceThreshold", pos.DistanceThreshold, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "DistanceComp", pos.DistanceComp, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "ActualLuxParam", pos.ActualLuxParam, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "LuxThreshold", pos.LuxThreshold, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "ActualCctParam", pos.ActualCctParam, ref ischange, true, filename);
                    inf.WriteDouble(str_section, "CctThreshold", pos.CctThreshold, ref ischange, true, filename);
                }
                else
                {
                    inf.WriteString(str_section, null, null, ref ischange, false);
                }
            }
            return EM_RES.OK;
        }
        #endregion

        #region 状态

        public static int n = 0;
        public static double ct = 0;
        public int tmr = 0;
        public enum EM_STA
        {
            [Description("上料位置")]
            FEEDPOS,
            [Description("SFR测试位置2")]
            SFRTESTPOS2,
            [Description("SFR测试位置1")]
            SFRTESTPOS1,
            [Description("污坏点测试位置")]
            TESTPOS,
            [Description("U轴翻转位置")]
            UTURNPOS,
            [Description("运行")]
            MOVING,
            [Description("回零中")]
            HOME,
            [Description("错误")]
            ERR,
            [Description("报警")]
            ALM,
            [Description("就绪")]
            READY,
            [Description("未知")]
            UNKNOWN
        }

        public enum EM_TESTPROGRESS_STA
        {
            [Description("未知")]
            UNKNOWN,
            [Description("就绪")]
            REDAY,
            [Description("待测")]
            UNTEST,
            [Description("测试中")]
            TEST,
            [Description("上过料，待测试")]
            NEXT,
            [Description("上料中")]
            UPLOAD,
            [Description("下料中")]
            DOWNLOAD,
            [Description("上下料中")]
            UPDOWNLOAD,
            [Description("准备测试")]
            REDAYFORTEST,
            [Description("准备下料")]
            REDAYFORDOWNLOAD,
            [Description("准备上料")]
            REDAYFORUPLOAD,
            [Description("准备上下料")]
            REDAYFORUPDOWNLOAD,
            [Description("复位中")]
            HOME,
            [Description("联机异常")]
            LINKERR,
            [Description("错误")]
            ERR,
        }
        public enum EM_PC_STA
        {
            [Description("电脑禁用")]
            DISABLE,
            [Description("联机错误")]
            LINK_ERR,
            [Description("就绪")]
            READY,
            [Description("测试中")]
            TEST,
            [Description("测试结束")]
            ENDOFTEST,
            [Description("等待下站")]
            WAIT,
            [Description("测试不同步")]
            NOT_SAME_TESTIDX,
            [Description("上下料")]
            UP_DOWN_LOAD
        };
        public enum EM_TEST_STA { EMPTY, UNTEST, NEXT, COMPLETED, ERROR };

        /// <summary>
        /// 上料状态
        /// </summary>
        public EM_TESTPROGRESS_STA FeedStatus;
        public EM_TESTPROGRESS_STA wsfeedteststaus = EM_TESTPROGRESS_STA.UNKNOWN;
        public EM_TESTPROGRESS_STA wsfeedfeedandteststaus = EM_TESTPROGRESS_STA.UNKNOWN;
        /// <summary>
        /// 测试联机状态
        /// </summary>
        public EM_PC_STA testprostaus;
        /// <summary>
        /// 物料状态
        /// </summary>
        public EM_TEST_STA TestStatus = EM_TEST_STA.UNTEST;
        /// <summary>
        /// 测试模块上下料位置状态
        /// </summary>
        public EM_STA status = EM_STA.UNKNOWN;

        double y = 0, z1 = 0, z2 = 0, x = 0, u = 0;
        bool btsk = false;
        public class MdDat
        {
            //编号
            public int Num;

            //夹具拍照位置gy0123
            public ST_XYZ[] st_jigpos = new ST_XYZ[cnt];

            //放料位置
            public ST_XYZ[] st_place_pos = new ST_XYZ[cnt];//定义了上下料模块1和2的位置，使用时只使用1或者2
            //吸头取料位置
            public ST_XYZ[] st_pickpos = new ST_XYZ[cnt];
            //夹具二维码gy0123
            public string jig_ID;
            //工站编号
            public int WS_ID;
            //对应测试电脑
            public int PC_ID;
            public string PC_IP;
            //当前测试位置
            public int test_idx;
            //工装编号
            public int TestBox_ID;
            //序号
            public int SN;
            //上次结果
            public int last_res;
            //NG相同结果次数
            public int NgSameRes_cnt;
            //结果
            public int res;
            //用时
            public int ct;
            //二维码
            public string bardcode;
            //使用
            public bool benable;
            //
            public bool Ishavemd;
            //类型
            public bool IsNormal;

            //gy0123夹具生产信息统计
            //ok数量
            public int cnt_ok;
            //开图NG
            public int cnt_ng_image;
            //os异常
            public int cnt_ng_OS;
            //图像丢帧
            public int cnt_ng_miss_pix;
            //曝光NG
            public int cnt_ng_exposure;
            //IIC读写NG
            public int cnt_ng_iic;
            //其他NG
            public int cnt_ng_other;
            //新增回检拍二维码位
            public ST_XYZ[] st_CapQrcodePos = new ST_XYZ[cnt];

            public MdDat Clone()
            {
                MdDat md = new MdDat();
                md.Num = Num;
                md.WS_ID = WS_ID;
                md.PC_ID = PC_ID;
                md.PC_IP = PC_IP;
                md.test_idx = test_idx;
                md.TestBox_ID = TestBox_ID;
                md.SN = SN;
                md.res = res;
                md.ct = ct;
                md.bardcode = bardcode;
                md.benable = benable;
                md.IsNormal = IsNormal;
                //gy0123夹具生产信息统计
                md.jig_ID = jig_ID;
                md.cnt_ng_exposure = cnt_ng_exposure;
                md.cnt_ng_iic = cnt_ng_iic;
                md.cnt_ng_image = cnt_ng_image;
                md.cnt_ng_miss_pix = cnt_ng_miss_pix;
                md.cnt_ng_OS = cnt_ng_OS;
                md.cnt_ng_other = cnt_ng_other;
                md.cnt_ok = cnt_ok;
                for (int i = 0; i < cnt; i++)
                {

                    md.st_place_pos[i] = st_place_pos[i].clone();
                    md.st_pickpos[i] = st_pickpos[i].clone();
                    md.st_jigpos[i] = st_jigpos[i].clone();//gy0123
                }


                return md;
            }
        };

        public class PCDat
        {
            //编号
            public int ID;
            //状态
            public EM_PC_STA status;
            //测试位置
            public int test_idx;
            //循环数
            public int temp;
            public int cnt;
        };
        public List<PCDat> list_pc_dat = new List<PCDat>();
        #endregion

        #region 状态检测
        /// <summary>
        /// 联机测试状态检测
        /// </summary>
        public EM_PC_STA GetTestStatus
        {
            get { return testprostaus; }
        }


        EM_RES safe_chk(int id, double target_pos = double.MaxValue)
        {
            EM_RES ret = EM_RES.OK;
            ret = MT.ChkAllSafeSen(0x02);
            if (ret != EM_RES.OK) return ret;
            return EM_RES.OK;
        }



        /// <summary>
        /// 位置获取
        /// </summary>
        public string StrOfPos
        {
            get
            {
                Task tsk = new Task(() =>
                {
                    if (btsk) return;
                    try
                    {
                        btsk = true;

                        y = ax_y != null ? ax_y.fenc_pos : 0;
                        z1 = ax_z_fr != null ? ax_z_fr.fenc_pos : 0;
                        z2 = ax_z_bk != null ? ax_z_bk.fenc_pos : 0;
                        x = ax_x != null ? ax_x.fcmd_pos : 0;
                        u = ax_u != null ? ax_u.fcmd_pos : 0;
                    }
                    finally
                    {
                        btsk = false;
                    }
                });
                tsk.Start();
                return string.Format("Y: {0:000.000}\nZ1: {1:000.000}\nZ2: {2:000.000}\nX: {3:000.000}\nU: {4:000.000}", y, z1, z2, x, u);
            }
        }
        public bool _isUInTestPos = false;
        public bool isUInTestPos
        {
            get
            {
                return _isUInTestPos = ((Math.Abs(ax_u.fcmd_pos - testpos.a) <= 0.3));//U轴在90度
            }
        }

        public bool _isUInFeedPos = false;
        public bool isUInFeedPos
        {
            get
            {
                return _isUInFeedPos = ((Math.Abs(ax_u.fcmd_pos - 0) <= 0.5));//U在原点
            }
        }


        public bool _isup = false;
        public bool IsUp
        {
            get
            {
                return _isup = IsAllcylOpen();
            }
        }

        public bool _isdown = false;
        public bool IsDown
        {
            get
            {
                return _isdown = IsAllcylDown();
            }
        }


        public bool _isFrZInSafePos = false;
        /// <summary>
        /// 测试模块的前Z轴是否在安全位置
        /// </summary>
        public bool isFrZInSafePos
        {
            get
            {
                return _isFrZInSafePos = ((Math.Abs(ax_z_fr.fenc_pos - 0) <= 0.3) && ax_z_fr.isORG);
            }
        }


        public bool _isFrZInTestPos = false;
        /// <summary>
        /// 测试模块的前Z轴是否在测试位置
        /// </summary>
        public bool isFrZInTestPos
        {
            get
            {
                return _isFrZInTestPos = ((Math.Abs(ax_z_fr.fenc_pos - testpos.z1) <= 0.3));
            }
        }
        public bool _isBkZInSafePos = false;
        /// <summary>
        /// 测试模块的后Z轴是否在安全位置
        /// </summary>
        public bool isBkZInSafePos
        {
            get
            {
                return _isBkZInSafePos = ((Math.Abs(ax_z_bk.fenc_pos - 0) <= 0.3) && ax_z_bk.isORG);
            }
        }

        public bool _isBkZIntestPos = false;
        /// <summary>
        /// 测试模块的后Z轴是否在测试位置1
        /// </summary>
        public bool isBkZIntestPos
        {
            get
            {
                return _isBkZIntestPos = ((Math.Abs(ax_z_bk.fenc_pos - testpos.z2) <= 0.3));
            }
        }
        public bool _isBkZInSfrtestPos2 = false;
        /// <summary>
        /// 测试模块的后Z轴是否在测试位置2
        /// </summary>

        public bool _isXIntestPos = false;
        public bool isXIntestPos
        {
            get
            {
                return _isXIntestPos = ((Math.Abs(ax_x.fenc_pos - testpos.x) <= 0.3));
            }
        }
        /// <summary>
        /// 增距镜X轴是否在测试位置1
        /// </summary>

        public bool _isXInSfrtestPos2 = false;
        /// <summary>
        /// 增距镜X轴是否在测试位置2
        /// </summary>

        public bool _isYInFeedPos = false;
        public bool isYInFeedPos
        {
            get
            {
                return _isYInFeedPos = (((Math.Abs(ax_y.fenc_pos - feedpos.y)) <= 0.3) && ax_y.isORG);//
            }
        }

        public bool _isYInTestPos = false;
        public bool isYInTestPos
        {
            get
            {
                return _isYInTestPos = (Math.Abs(ax_y.fenc_pos - testpos.y) <= 0.5);
            }
        }
        public bool _isYInUturnPos = false;
        public bool isYInUturnPos
        {
            get
            {
                return _isYInUturnPos = (Math.Abs(ax_y.fenc_pos - safeuturnpos.y) <= 0.5);
            }
        }
        /// <summary>
        /// 判断是否在上料位置，y轴,u轴都满足条件
        /// </summary>
        public bool isInFeedPos
        {
            get
            {
                if (isUInFeedPos && isYInFeedPos && IsUp)
                {
                    return true;
                }
                else return false;
            }
        }
        public bool isInFeedPostemp
        {
            get
            {
                if (isUInFeedPos && isYInFeedPos)
                {
                    return true;
                }
                else return false;
            }
        }
        public bool isYUInFeedPos
        {
            get
            {
                if (isUInFeedPos && isYInFeedPos)
                {
                    return true;
                }
                else return false;
            }
        }

        public bool isInTestPos
        {
            get
            {
                if (isFrZInTestPos && isYInTestPos && IsDown)
                {
                    return true;
                }
                else return false;
            }
        }

        public static EM_STA mCurPos = EM_STA.UNKNOWN;

        /// <summary>
        /// 获取当前测试上下料模块的位置状态
        /// </summary>
        public static EM_STA GetCurSta(int id)
        {
            if ((id == 0 && COM.wsfeed1.isInFeedPos) || (id == 1 && COM.wsfeed2.isInFeedPos))
            {
                mCurPos = EM_STA.FEEDPOS;
                return EM_STA.FEEDPOS;
            }
            else if ((id == 0 && COM.wsfeed1.isInTestPos) || (id == 1 && COM.wsfeed2.isInTestPos))
            {
                mCurPos = EM_STA.TESTPOS;
                return EM_STA.TESTPOS;
            }
            else if ((id == 0 && COM.wsfeed1.isYInUturnPos) || (id == 1 && COM.wsfeed2.isYInUturnPos))
            {
                mCurPos = EM_STA.UTURNPOS;
                return EM_STA.UTURNPOS;
            }
            else
            {
                mCurPos = EM_STA.UNKNOWN;
                return EM_STA.UNKNOWN;
            }
        }
        /// <summary>
        /// 获取指定编号工装的当前位置状态
        /// </summary>
        /// <param name="ws_num"></param>
        /// <returns></returns>
        public static EM_STA GetWSSta(int id)
        {
            EM_STA sta = GetCurSta(id);
            if (sta < EM_STA.FEEDPOS || sta > EM_STA.UTURNPOS)
                return EM_STA.UNKNOWN;
            return sta;
        }
        /// <summary>
        /// 获取处于上下料位置的测试上料模块的编号，是左边在上料位置还是右边
        /// </summary>
        public static WSFEED GetWSOnFeedPos
        {
            get
            {
                if (VAR.ClearMt)
                {
                    if ((COM.wsfeed2.isInFeedPos) && UpDownLoad.wsfeed2isworking && !COM.wsfeed2.isclearfinsh)
                    {
                        return COM.wsfeed2;
                    }
                    else if (COM.wsfeed1.isInFeedPos && COM.wsfeed2.isInFeedPos && COM.wsfeed1.isclearfinsh)
                    {
                        return COM.wsfeed2;
                    }
                    else if (COM.wsfeed1.isInFeedPos && COM.wsfeed2.isInFeedPos && !COM.wsfeed1.isclearfinsh)
                    {
                        return COM.wsfeed1;
                    }
                    else if ((COM.wsfeed1.isInFeedPos) && (!COM.wsfeed2.isInFeedPos) && !COM.wsfeed1.isclearfinsh)
                    {
                        return COM.wsfeed1;
                    }
                    else if ((!COM.wsfeed1.isInFeedPos) && (COM.wsfeed2.isInFeedPos) && !COM.wsfeed2.isclearfinsh)
                    {
                        return COM.wsfeed2;
                    }
                    else if ((!COM.wsfeed1.isInFeedPos) && (!COM.wsfeed2.isInFeedPos))
                    {
                        return null;
                    }
                    else
                    {
                        return null;
                    }

                }
                else if (PT_SET.bGrrFlow)
                {
                    if (PT_SET.GRRWSID == 1)
                    {
                        if (COM.wsfeed1.isInFeedPos)
                        {
                            return COM.wsfeed1;
                        }
                        else
                        {
                            return null;
                        }

                    }
                    else if (PT_SET.GRRWSID == 2)
                    {
                        if (COM.wsfeed2.isInFeedPos)
                        {
                            return COM.wsfeed2;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    bool feed1benable = false;
                    bool feed2benable = false;
                    foreach (WSFEED.MdDat md in COM.wsfeed1.list_md)
                    {
                        if (!md.benable) continue;
                        feed1benable = true;

                    }
                    foreach (WSFEED.MdDat md in COM.wsfeed2.list_md)
                    {
                        if (!md.benable) continue;
                        feed2benable = true;

                    }
                    if ((COM.wsfeed2.isInFeedPos) && UpDownLoad.wsfeed2isworking && feed2benable)
                    {
                        return COM.wsfeed2;
                    }
                    else if ((COM.wsfeed1.isInFeedPos) && (COM.wsfeed2.isInFeedPos) && feed1benable && (COM.wsfeed1.FeedStatus == EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD || COM.wsfeed1.FeedStatus == EM_TESTPROGRESS_STA.UPDOWNLOAD))
                    {
                        return COM.wsfeed1;
                    }
                    else if ((COM.wsfeed1.isInFeedPos) && (!COM.wsfeed2.isInFeedPos) && feed1benable && (COM.wsfeed1.FeedStatus == EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD || COM.wsfeed1.FeedStatus == EM_TESTPROGRESS_STA.UPDOWNLOAD))
                    {
                        return COM.wsfeed1;
                    }
                    else if ((!COM.wsfeed1.isInFeedPos) && (COM.wsfeed2.isInFeedPos) && feed2benable && (COM.wsfeed2.FeedStatus == EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD || COM.wsfeed2.FeedStatus == EM_TESTPROGRESS_STA.UPDOWNLOAD))
                    {
                        return COM.wsfeed2;
                    }
                    else if (COM.wsfeed2.isInFeedPos && feed2benable && (COM.wsfeed2.FeedStatus == EM_TESTPROGRESS_STA.REDAYFORUPDOWNLOAD || COM.wsfeed2.FeedStatus == EM_TESTPROGRESS_STA.UPDOWNLOAD))
                    {
                        return COM.wsfeed2;
                    }
                    else if ((!COM.wsfeed1.isInFeedPos) && (!COM.wsfeed2.isInFeedPos))
                    {
                        return null;
                    }
                    else
                    {
                        return null;
                    }

                }
            }
        }

        public static WSFEED GetWSOnFeedPosTest
        {
            get
            {
                if ((COM.wsfeed1.isfinsh) && UpDownLoad.wsfeed2isworking)
                {
                    return COM.wsfeed2;
                }
                else if ((COM.wsfeed1.isfinsh) && (!COM.wsfeed2.isfinsh))
                {
                    return COM.wsfeed2;
                }
                else if (!COM.wsfeed1.isfinsh)
                {
                    return COM.wsfeed1;
                }

                else
                {
                    return null;
                }

            }

        }
        public WSFEED GetWSOnFeedPosAlone(int udid)
        {
            if ((COM.wsfeed2.isInFeedPos) && udid == 1)
            {
                return COM.wsfeed2;
            }
            else if ((COM.wsfeed1.isInFeedPos) && udid == 0)
            {
                return COM.wsfeed1;
            }
            else
            {
                return null;
            }
        }
        public static WSFEED GetWsfeed1TestPos
        {
            get
            {
                if (COM.wsfeed1.isInTestPos || COM.wsfeed1.Gotostarttestpos)
                {
                    return COM.wsfeed1;
                }
                else
                {
                    return null;
                }
            }

        }
        public static WSFEED GetWsfeed1FeedPos
        {
            get
            {
                if (COM.wsfeed1.isInFeedPos)
                {
                    return COM.wsfeed1;
                }
                else
                {
                    return null;
                }
            }

        }
        public static WSFEED GetWsfeed2FeedPos
        {
            get
            {
                if (COM.wsfeed2.isInFeedPos)
                {
                    return COM.wsfeed2;
                }
                else
                {
                    return null;
                }
            }

        }
        public static WSFEED GetWsfeed2TestPos
        {
            get
            {
                if (COM.wsfeed2.isInTestPos || COM.wsfeed2.Gotostarttestpos)
                {
                    return COM.wsfeed2;
                }
                else
                {
                    return null;
                }
            }

        }
        public static WSFEED GetWSFEEDOnPos(EM_STA pos)
        {
            if (pos < EM_STA.FEEDPOS || pos > EM_STA.UTURNPOS) return null;
            foreach (WSFEED wsfeed in COM.list_wsfeed)
            {
                if (GetWSSta(wsfeed.id) == pos) return wsfeed;
            }
            //if (GetWSSta(COM.wsfeed2.id) == pos && UpDownLoad.wsfeed2isworking) return COM.wsfeed2;
            //if (GetWSSta(COM.wsfeed1.id) == pos) return COM.wsfeed1;
            //if (GetWSSta(COM.wsfeed2.id) == pos) return COM.wsfeed2;
            return null;
        }

        #endregion

        #region 动作相关

        #region 回零
        public EM_RES ZHome(ref bool bquit1)
        {
            //U home
            EM_RES res = EM_RES.OK;
            if (bquit1) return EM_RES.QUIT;
            res = MT.AxisHome(ref bquit1, ax_z_fr, ax_z_bk);
            if (res != EM_RES.OK) return res;
            return EM_RES.OK;
        }
        public EM_RES UHome(ref bool bquit1)
        {
            //U home
            EM_RES res = EM_RES.OK;
            if (bquit1) return EM_RES.QUIT;
            res = MT.AxisHome(ref bquit1, ax_u);
            if (res != EM_RES.OK) return res;
            return EM_RES.OK;
        }
        public EM_RES Home(ref bool bquit1)
        {
            EM_RES res = EM_RES.OK;
            if (bquit1) return EM_RES.QUIT;
            res = MT.AxisHome(ref bquit1, ax_y, ax_x);
            if (res != EM_RES.OK) return res;
            return EM_RES.OK;
        }
        #endregion

        #region 夹具操作
        public bool IsAllcylOpen()
        {

            foreach (Cylinder cyl in list_cyl)
            {
                if (!(cyl.isONByChkSen && !cyl.isOFFByChkSen && cyl.isON))//夹具是否打开list_cld[n].isONByChkSen
                {
                    // VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}夹具打开失败", disc));
                    return false;
                }
            }
            return true;
        }
        public bool IsAllcylDown()
        {
            foreach (Cylinder cyl in list_cyl)
            {
                if (cyl.isON || !(cyl.isOFFByChkSen && !cyl.isONByChkSen))
                {
                    //  VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}夹具关闭失败", disc));
                    return false;
                }
            }
            return true;
        }
        private void SetFixtureVacuum(bool enable, string reason = "")
        {
            if (gpio_openfr == null) return;
            if (enable)
            {
                if (!gpio_openfr.isON)
                {
                    gpio_openfr.SetOn();
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}夹具真空打开{1}", disc, string.IsNullOrEmpty(reason) ? "" : "," + reason));
                }
            }
            else
            {
                if (gpio_openfr.isON)
                {
                    gpio_openfr.SetOff();
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}夹具真空关闭{1}", disc, string.IsNullOrEmpty(reason) ? "" : "," + reason));
                }
            }
        }
        public EM_RES Cyldown(ref bool bquit)
        {
            EM_RES ret = EM_RES.OK;
            if (IsAllcylDown()) return ret;
            if (!isYInFeedPos)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}上料Y轴不在上料位置，不安全，禁止开关夹具!", disc));
                return EM_RES.MOVE_PROTECT;
            }
            int un = 0;
            bool refresh = true;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            if ((PT_SET.UpDownRunMode == (int)PT_SET.RUN_MD.ALONE_WORK))
            {
                while ((((!COM.UDLoad1.ax_x.isORG) || (COM.UDLoad1.ax_y.fenc_pos > COM.UDLoad1.list_xt[1].st_cap_pos.y)) && id == 0) || (((!COM.UDLoad2.ax_x.isORG) || (COM.UDLoad2.ax_y.fenc_pos > COM.UDLoad2.list_xt[1].st_cap_pos.y)) && id == 1) && !bquit)
                {
                    if (refresh)
                    {
                        refresh = false;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}夹具关闭时，等待上下料回安全位置-----", disc));
                    }
                    //if (un++ > 10000) break;
                    Thread.Sleep(10);
                    Application.DoEvents();
                }
            }
            else
            {
                while (((!COM.UDLoad1.ax_x.isORG) || (!COM.UDLoad2.ax_x.isORG) || (COM.UDLoad1.ax_y.fenc_pos > COM.UDLoad1.list_xt[1].st_cap_pos.y) || (COM.UDLoad2.ax_y.fenc_pos > COM.UDLoad2.list_xt[1].st_cap_pos.y)) && !bquit)
                {
                    if (refresh)
                    {
                        refresh = false;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}夹具关闭时，等待上下料回安全位置-----", disc));
                    }
                    //if (un++ > 2000) break;
                    Thread.Sleep(10);
                    Application.DoEvents();
                }
            }
            sw.Stop();
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}夹具关闭时，等待上下料回安全位置，耗时{1:F}s-----", disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds / 1000, 3))));
            if (bquit) return EM_RES.QUIT;
            SetFixtureVacuum(true, "夹具盖合");
            if (id == 0) wsfeed1unsafe = true;
            if (id == 1) wsfeed2unsafe = true;
            foreach (Cylinder cyl in list_cyl)
            {
                ret = cyl.SetOn();//夹具关闭
                if (bquit) return EM_RES.QUIT;
                //if (ret != EM_RES.OK)
                //{
                //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}夹具关闭失败", disc));
                //    return ret;
                //}
            }
            Thread.Sleep(90);
            foreach (Cylinder cyl in list_cyl)
            {

                //if (cyl.isONByChkSen && !cyl.isOFFByChkSen)
                if (cyl.isON || !(cyl.isOFFByChkSen && !cyl.isONByChkSen))
                {
                    Thread.Sleep(150);
                    if (cyl.isON || !(cyl.isOFFByChkSen && !cyl.isONByChkSen))
                    {
                        Thread.Sleep(200);
                        if (cyl.isON || !(cyl.isOFFByChkSen && !cyl.isONByChkSen))
                        {
                            SetFixtureVacuum(false, "夹具关闭失败");
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}夹具关闭失败", disc));
                            COUNT_DATA.ErrTemp = System.DateTime.Now;
                            VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "夹具关闭异常!" : "Draw ERR", 20, true);
                            MT.ST_WARN st_warn = new MT.ST_WARN();
                            warning fr_warn = new warning();
                            st_warn.ok_txt = VAR.IsChinese ? "确定" : "ok";
                            st_warn.abort_txt = VAR.IsChinese ? "确定" : "abort";
                            st_warn.ws = this;
                            st_warn.cancle_txt = VAR.IsChinese ? "取消" : "cancle";
                            st_warn.title = VAR.IsChinese ? "提示:夹具关闭异常!" : "Tip: Abnormal suction!";
                            st_warn.msg = string.Format("{0}夹具关闭失败,请确认后按下启动键!", disc);
                            st_warn.lb_msg = "提示:" + st_warn.msg;
                            DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);

                            if (DialogResult.Cancel == logres) return EM_RES.ERR;
                            //VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                            if (DialogResult.OK == logres)
                            {
                                MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                return EM_RES.MOVE_ERR;
                            }
                            else if (DialogResult.Abort == logres)
                            {
                                MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                return EM_RES.MOVE_ERR;
                            }
                            else if (DialogResult.Retry == logres)
                            {
                                MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                return EM_RES.MOVE_ERR;
                            }
                        }
                    }
                }
            }
            if (id == 0) wsfeed1unsafe = false;
            if (id == 1) wsfeed2unsafe = false;
            //if (id == 0)//撞机防呆，如果是右测试模块进行上下料的话，左侧夹具不能打开
            //{
            //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}撞机防呆，右侧夹具可以打开-----", disc));
            //    COM.wsfeed1.wsfeedcanopen = true;

            //}
            //else if (id == 1)
            //{
            //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}撞机防呆，左侧夹具可以打开-----", disc));
            //    COM.wsfeed2.wsfeedcanopen = true;
            //}
            return EM_RES.OK;
        }
        public EM_RES Cylup(ref bool bquit, bool Isfinsh = false)//是否需要测试完成判断，用于测试中断的情况
        {
            EM_RES ret = EM_RES.OK;


            if (IsAllcylOpen()) return ret;
            if (!isYInFeedPos)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}上料Y轴不在上料位置，不安全，禁止开关夹具!", disc));
                return EM_RES.MOVE_PROTECT;
            }
            int un = 0;
            bool refresh = true;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            if ((PT_SET.UpDownRunMode == (int)PT_SET.RUN_MD.ALONE_WORK))
            {
                while ((((!COM.UDLoad1.ax_x.isORG) || (COM.UDLoad1.ax_y.fenc_pos > COM.UDLoad1.list_xt[1].st_cap_pos.y)) && id == 0) || (((!COM.UDLoad2.ax_x.isORG) || (COM.UDLoad2.ax_y.fenc_pos > COM.UDLoad2.list_xt[1].st_cap_pos.y)) && id == 1) && !bquit)
                {
                    if (refresh)
                    {
                        refresh = false;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}夹具打开时，等待上下料回安全位置-----", disc));
                    }
                    //if (un++ > 10000) break;
                    Thread.Sleep(10);
                    Application.DoEvents();
                }
            }
            else
            {
                while (((!COM.UDLoad1.ax_x.isORG) || (!COM.UDLoad2.ax_x.isORG) || !wsfeedcanopen || (COM.UDLoad1.ax_y.fenc_pos > COM.UDLoad1.list_xt[1].st_cap_pos.y) || (COM.UDLoad2.ax_y.fenc_pos > COM.UDLoad2.list_xt[1].st_cap_pos.y)) && !bquit)
                {
                    if (refresh)
                    {
                        refresh = false;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}夹具打开时，等待上下料回安全位置-----", disc));
                    }
                    //if (un++ > 10000) break;
                    Thread.Sleep(10);
                    Application.DoEvents();
                }
            }
            sw.Stop();
            if (id == 0) wsfeed1unsafe = true;
            if (id == 1) wsfeed2unsafe = true;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}夹具打开时，等待上下料回安全位置，耗时{1:F}s-----", disc, Convert.ToDecimal(Math.Round((decimal)sw.ElapsedMilliseconds / 1000, 3))));
            if (bquit) return EM_RES.QUIT;
            SetFixtureVacuum(false, "夹具打开/取放料");
            bool canopen = true;
            int enablecnt = 0;
            int unfinshcnt = 0;
            if (Isfinsh)
            {
                foreach (MdDat md in list_md)
                {
                    if (md.benable) enablecnt++;
                    if (md.res > -1)
                    {
                        break;//有测试完成的料就打开夹具，退出循环
                    }
                    if (md.benable && md.res <= -1) unfinshcnt++;
                }
                if ((unfinshcnt == enablecnt) && unfinshcnt > 0 && enablecnt > 0)
                {
                    canopen = false;
                    Gotostarttestpos = true;
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}测试未完成，不打开夹具", disc));
                }

            }
            if (canopen)
            {
                foreach (Cylinder cyl in list_cyl)
                {
                    ret = cyl.SetOff();//夹具打开
                    if (bquit) return EM_RES.QUIT;
                    if (ret != EM_RES.OK)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}夹具打开失败", disc));
                        return ret;
                    }
                }
                Thread.Sleep(90);
                foreach (Cylinder cyl in list_cyl)
                {
                    //if (!cyl.isONByChkSen && cyl.isOFFByChkSen)
                    if (!(cyl.isONByChkSen && !cyl.isOFFByChkSen && cyl.isON))
                    {
                        Thread.Sleep(150);
                        if (!(cyl.isONByChkSen && !cyl.isOFFByChkSen && cyl.isON))
                        {
                            Thread.Sleep(200);
                            if (!(cyl.isONByChkSen && !cyl.isOFFByChkSen && cyl.isON))
                            {
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}夹具打开失败", disc));
                                COUNT_DATA.ErrTemp = System.DateTime.Now;
                                VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "夹具打开失败!" : "Draw ERR", 20, true);
                                MT.ST_WARN st_warn = new MT.ST_WARN();
                                warning fr_warn = new warning();
                                st_warn.ok_txt = VAR.IsChinese ? "确定" : "ok";
                                st_warn.abort_txt = VAR.IsChinese ? "确定" : "abort";
                                st_warn.ws = this;
                                st_warn.cancle_txt = VAR.IsChinese ? "取消" : "cancle";
                                st_warn.title = VAR.IsChinese ? "提示:夹具打开异常!" : "Tip: Abnormal suction!";
                                st_warn.msg = string.Format("{0}夹具打开失败,请确认后按下启动键!", disc);
                                st_warn.lb_msg = "提示:" + st_warn.msg;
                                DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);

                                if (DialogResult.Cancel == logres) return EM_RES.ERR;
                                //VAR.sys_inf.Set(EM_ALM_STA.NOR_GREEN, VAR.IsChinese ? "运行" : "RUN", 0, true);
                                if (DialogResult.OK == logres)
                                {
                                    MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                    return EM_RES.MOVE_ERR;
                                }
                                else if (DialogResult.Abort == logres)
                                {
                                    MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                    return EM_RES.MOVE_ERR;
                                }
                                else if (DialogResult.Retry == logres)
                                {
                                    MT.OnlyLightOn(MT.GPIO_OUT_ALM_GREEN);
                                    return EM_RES.MOVE_ERR;
                                }
                            }
                        }
                    }
                }
            }
            if (id == 0) wsfeed1unsafe = false;
            if (id == 1) wsfeed2unsafe = false;
            return EM_RES.OK;
        }
        #endregion

        #region 轴运动
        public EM_RES FrZMoveToSafePos(ref bool bquit)
        {
            EM_RES ret = EM_RES.OK;
            ret = MT.Move(ref bquit, ref ax_z_fr, 0);
            if (ret != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，光源Z轴移动到安全位置失败!", disc));
                return EM_RES.MOVE_ERR;
            }
            return EM_RES.OK;
        }
        public EM_RES FrZMoveToTestPos(ref bool bquit)//前Z轴移动到测试位置
        {
            EM_RES ret = EM_RES.OK;
            ret = MT.Move(ref bquit, ref ax_z_fr, testpos.z1);
            if (ret != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，光源Z轴移动到测试位置失败!", disc));
                return EM_RES.MOVE_ERR;
            }
            return EM_RES.OK;
        }
        public EM_RES BkZXMoveToTestPos(ref bool bquit)//前Z轴移动到测试位置
        {
            EM_RES ret = EM_RES.OK;
            ret = MT.Move(ref bquit, ref ax_x, testpos.x, ref ax_z_bk, testpos.z2);
            if (ret != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，增距镜X、Z轴移动到测试位置失败!", disc));
                return EM_RES.MOVE_ERR;
            }
            return EM_RES.OK;
        }
        public EM_RES BkZXMoveToSafePos(ref bool bquit)
        {
            EM_RES ret = EM_RES.OK;
            ret = MT.Move(ref bquit, ref ax_x, 0, ref ax_z_bk, 0);
            if (ret != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，增距镜Z轴、X轴移动到安全位置失败!", disc));
                return EM_RES.MOVE_ERR;
            }
            return EM_RES.OK;
        }
        public EM_RES MoveToUFeedPos(ref bool bquit, bool Isuturn)//移动到翻转位置并翻转到0度
        {
            EM_RES ret = EM_RES.OK;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}，Y轴移动到U轴翻转位置{1}!", disc, safeuturnpos.y.ToString()));
            ret = MT.Move(ref bquit, ref ax_y, safeuturnpos.y);
            if (ret != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Y轴移动到U轴翻转位置失败!", disc));
                return EM_RES.MOVE_ERR;
            }
            if (Isuturn)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("U轴移动目标上料位置为： {0}", feedpos.a.ToString()));
                ret = MT.Move(ref bquit, ref ax_u, feedpos.a);
                if (ret != EM_RES.OK)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，U轴翻转到上料位置失败!", disc));
                    return EM_RES.MOVE_ERR;
                }
            }
            return EM_RES.OK;
        }
        public EM_RES MoveToUTestPos(ref bool bquit, bool Isuturn)//移动到翻转位置并翻转90度
        {
            EM_RES ret = EM_RES.OK;
            if (safeuturnpos.y > PT_SET.testsafey)//当目标位置越过安全位置时，需要进行安全操作，前Z轴上移
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，U轴翻转位置不安全，移动失败，请检查!", disc));
                return EM_RES.MOVE_ERR;
            }

            ret = MT.Move(ref bquit, ref ax_y, safeuturnpos.y);
            if (ret != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Y轴移动到U安全翻转位置失败!", disc));
                return EM_RES.MOVE_ERR;
            }
            if (Isuturn)
            {
                if (!IsDown)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0} 夹具未闭合，禁止翻转！", disc));
                    return EM_RES.MOVE_ERR;
                }
                ret = MT.Move(ref bquit, ref ax_u, testpos.a);//U轴翻转
                if (ret != EM_RES.OK)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，U轴翻转到测试位置失败!", disc));
                    return EM_RES.MOVE_ERR;
                }
                if (PT_SET.overturnon)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}U 轴翻转完成，开图允许指令发出", disc));
                    if (id == 0) UpDownLoad.wsfeed1openchart = true;
                    if (id == 1) UpDownLoad.wsfeed2openchart = true;
                }
            }

            return EM_RES.OK;
        }
        public EM_RES MoveToFeedPos(ref bool bquit)
        {
            EM_RES ret = EM_RES.OK;
            //if ((Math.Abs(ax_y.fenc_pos) - Math.Abs(testpos.y) > 40))
            //{
            //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}上料Y轴在测试位置外面，不安全，禁止移动!", disc));
            //    return EM_RES.MOVE_PROTECT;
            //}
            if (Math.Abs(ax_y.fenc_pos) > PT_SET.testsafey)
            {
                if (!isFrZInSafePos)
                {
                    ret = FrZMoveToSafePos(ref bquit);
                    if (ret != EM_RES.OK)
                    {
                        return EM_RES.MOVE_ERR;
                    }
                }
                if ((Math.Abs(ax_y.fenc_pos) > safeyposbk) || PT_SET.bteleconvertergohome)
                {
                    if (!isBkZInSafePos)
                    {
                        ret = BkZXMoveToSafePos(ref bquit);
                        if (ret != EM_RES.OK)
                        {
                            return EM_RES.MOVE_ERR;
                        }
                    }

                }
            }
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("U轴移动前，当前位置为： {0}", ax_u.fenc_pos.ToString()));
            //if (!isUInFeedPos)
            //{
            ret = MoveToUFeedPos(ref bquit, true);
            if (ret != EM_RES.OK)
            {
                return EM_RES.MOVE_ERR;
            }
            //}
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("U轴移动到上料位置后，当前位置为： {0}", ax_u.fcmd_pos.ToString()));
            ret = MT.Move(ref bquit, ref ax_y, feedpos.y);
            if (ret != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Y轴移动到上料位置失败!", disc));
                return EM_RES.MOVE_ERR;
            }
            return EM_RES.OK;
        }
        public EM_RES MoveTocommonTestpos(ref bool bquit)
        {
            EM_RES ret = EM_RES.OK;

            //if ((Math.Abs(ax_y.fenc_pos) - Math.Abs(testpos.y) > 40))
            //{
            //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}上料Y轴在测试位置外面，不安全，禁止移动!", disc));
            //    return EM_RES.MOVE_PROTECT;
            //}
            ret = Cyldown(ref bquit);
            if (ret != EM_RES.OK)
            {
                return EM_RES.MOVE_ERR;
            }
            if (!isUInTestPos)
            {
                ret = MoveToUTestPos(ref bquit, true);
                if (ret != EM_RES.OK)
                {
                    return EM_RES.MOVE_ERR;
                }
            }
            if (testpos.y > PT_SET.testsafey)//当目标位置越过安全位置时，需要进行安全操作，前Z轴上移
            {
                if (!isFrZInSafePos)
                {
                    ret = FrZMoveToSafePos(ref bquit);
                    if (ret != EM_RES.OK)
                    {
                        return EM_RES.MOVE_ERR;
                    }
                }
                if (testpos.y > safeyposbk || PT_SET.bteleconvertergohome)
                {
                    if (!isBkZInSafePos)
                    {
                        ret = BkZXMoveToSafePos(ref bquit);
                        if (ret != EM_RES.OK)
                        {
                            return EM_RES.MOVE_ERR;
                        }
                    }

                }
            }
            ret = MT.Move(ref bquit, ref ax_y, testpos.y);
            if (ret != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Y轴移动到测试位置失败!", disc));
                return EM_RES.MOVE_ERR;
            }
            if (!isFrZInTestPos)
            {
                ret = MT.Move(ref bquit, ref ax_z_fr, testpos.z1);
                if (ret != EM_RES.OK)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，前Z轴移动到测试位置失败!", disc));
                    return EM_RES.MOVE_ERR;
                }
                return EM_RES.OK;
            }
            if (!isBkZIntestPos || !isXIntestPos)
            {
                ret = BkZXMoveToTestPos(ref bquit);
                if (ret != EM_RES.OK)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，后Z轴、X轴移动到测试位置失败!", disc));
                    return EM_RES.MOVE_ERR;
                }
                return EM_RES.OK;
            }
            return EM_RES.OK;
        }
        public EM_RES MoveToTestpos(ref bool bquit)
        {
            EM_RES ret = EM_RES.OK;
            if (!isUInTestPos)
            {
                if (!IsDown)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0} 夹具未闭合，禁止翻转！", disc));
                    return EM_RES.MOVE_ERR;
                }
                ret = MoveToUTestPos(ref bquit, true);
                if (ret != EM_RES.OK)
                {
                    return EM_RES.MOVE_ERR;
                }
                if ((UpDownLoad.wsfeed1openchart && id == 0) || (UpDownLoad.wsfeed2openchart && id == 1))
                {
                    //if (TestStatus == EM_TEST_STA.COMPLETED || TestStatus == EM_TEST_STA.ERROR || TestStatus == EM_TEST_STA.EMPTY)
                    //{
                    //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0} 物料状态异常，不开图，测试结束！", disc));

                    //}
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} 开图", disc));
                    // Thread.Sleep(10000);
                    ret = StartTestFlow(0, Demo);//开图命令
                    if (ret != EM_RES.OK)
                    {
                        Thread.Sleep(1500);
                        ret = StartTestFlow();
                        if (ret != EM_RES.OK)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} StartTestFlow err", disc));
                            testprostaus = EM_PC_STA.LINK_ERR;
                            FeedStatus = EM_TESTPROGRESS_STA.LINKERR;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("测试软件通信异常，请检查", disc));
                            return EM_RES.TIMEOUT;

                        }
                    }
                    Isstartchart = true;
                    if (id == 0) UpDownLoad.wsfeed1openchart = false;
                    if (id == 1) UpDownLoad.wsfeed2openchart = false;
                    Thread.Sleep(PT_SET.OpenDly);
                }
            }
            if (!IsDown)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0} 夹具未闭合，禁止移动！", disc));
                return EM_RES.MOVE_ERR;
            }
            if (testpos.y > PT_SET.testsafey)//当目标位置越过安全位置时，需要进行安全操作，前Z轴上移
            {
                if (!isFrZInSafePos)
                {
                    ret = FrZMoveToSafePos(ref bquit);
                    if (ret != EM_RES.OK)
                    {
                        return EM_RES.MOVE_ERR;
                    }
                }
                if (testpos.y > safeyposbk || PT_SET.bteleconvertergohome)
                {
                    if (!isBkZInSafePos)
                    {
                        ret = BkZXMoveToSafePos(ref bquit);
                        if (ret != EM_RES.OK)
                        {
                            return EM_RES.MOVE_ERR;
                        }
                    }

                }
            }
            ret = MT.Move(ref bquit, ref ax_y, testpos.y);
            if (ret != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Y轴移动到测试位置失败!", disc));
                return EM_RES.MOVE_ERR;
            }
            if (!isFrZInTestPos)
            {
                ret = FrZMoveToTestPos(ref bquit);
                if (ret != EM_RES.OK)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，前Z轴移动到测试位置失败!", disc));
                    return EM_RES.MOVE_ERR;
                }
                return EM_RES.OK;
            }
            if (!isBkZIntestPos || !isXIntestPos)
            {
                ret = BkZXMoveToTestPos(ref bquit);
                if (ret != EM_RES.OK)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，后Z轴、X轴移动到测试位置失败!", disc));
                    return EM_RES.MOVE_ERR;
                }
                return EM_RES.OK;
            }
            return EM_RES.OK;
        }
        //检查到位
        public EM_RES ChkAxisPos(ref AXIS ax, double pos)
        {
            if (Math.Abs(pos - ax.fenc_pos) > 0.3)
            {
                Thread.Sleep(50);
                if (Math.Abs(pos - ax.fenc_pos) > 0.3)
                {
                    Thread.Sleep(100);
                    if (Math.Abs(pos - ax.fenc_pos) > 0.3)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR,
                            VAR.IsChinese ? string.Format("{0}当前位置与目标位置偏差超0.3mm,目标位置:{1}mm,实际位置:{2}mm", ax.str_disc, pos, ax.fenc_pos) : string.Format("{0}The deviation between the current position and the target position is more than 0.3mm, the target position: {2} mm, the actual position: {3} mm    ({1}当前位置与目标位置偏差超0.3mm,目标位置:{2}mm,实际位置:{3}mm)", ax.english_disc, ax.str_disc, pos, ax.fenc_pos), DReport.EmErrCode.MoveError, (int)DReport.EmHareware.LightBox, ERR_ALM.EmErrItem.MoveError);
                        return EM_RES.ERR;
                    }
                }

            }
            return EM_RES.OK;
        }
        public EM_RES ChkAxisPosUX(ref AXIS ax, double pos)
        {
            if (Math.Abs(pos - ax.fcmd_pos) > 0.3)
            {
                Thread.Sleep(50);
                if (Math.Abs(pos - ax.fcmd_pos) > 0.3)
                {
                    Thread.Sleep(100);
                    if (Math.Abs(pos - ax.fcmd_pos) > 0.3)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR,
                            VAR.IsChinese ? string.Format("{0}当前位置与目标位置偏差超0.3mm,目标位置:{1}mm,实际位置:{2}mm", ax.str_disc, pos, ax.fcmd_pos) : string.Format("{0}The deviation between the current position and the target position is more than 0.3mm, the target position: {2} mm, the actual position: {3} mm    ({1}当前位置与目标位置偏差超0.3mm,目标位置:{2}mm,实际位置:{3}mm)", ax.english_disc, ax.str_disc, pos, ax.fenc_pos), DReport.EmErrCode.MoveError, (int)DReport.EmHareware.LightBox, ERR_ALM.EmErrItem.MoveError);
                        return EM_RES.ERR;
                    }
                }

            }
            return EM_RES.OK;
        }
        public EM_RES MoveTo(ref bool bquit, PosDef pos)
        {
            EM_RES ret = EM_RES.OK;
            //切换光源
            if (pos.Channel >= 0 && pos.Channel < 10 && !PT_SET.unlight)
            {
                if (!PT_SET.bG4C)
                {
                    bool res = true;

                    if (!PT_SET.bG4C && id == 1)//轩十佳光源OTP
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "进入轩仕佳");
                        //MT.COM3.XSJSatrt();
                        Thread.Sleep(200);
                        Application.DoEvents();
                        res = MT.COM3.SetChannelXSJ(pos.Channel);

                    }
                    else if (!PT_SET.bG4C && id == 0)//轩十佳光源OTP
                    {

                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "进入轩仕佳");
                        //MT.COM4.XSJSatrt();
                        Thread.Sleep(200);
                        Application.DoEvents();
                        res = MT.COM4.SetChannelXSJ(pos.Channel);

                    }
                    else
                    {
                        res = MT.COM2.SetChannel(pos.Channel);
                        if (res != true)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}切换光源[id={1},ch={2}]时异常!", disc, pos.ID, pos.Channel) : string.Format("{0} Switch light source [id={1},ch={2}] ERR!    ({0}切换光源[id={1},ch={2}]时异常!)", disc, pos.ID, pos.Channel));
                            return EM_RES.ERR;
                        }
                    }
                }
                else
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "进入G4G");
                    if (id == 1)
                    {
                        MT.COM10.G4CSatrt();
                        Thread.Sleep(200);
                        Application.DoEvents();
                        var retemp = MT.COM10.SetChannelG4C(pos.Channel);
                        if (!retemp)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("切换到左侧光源通道{0}时失败，请检查串口是否连接 ...", pos.Channel));
                            return EM_RES.ERR;
                        }
                    }
                    else
                    {
                        MT.COM11.G4CSatrt();
                        Thread.Sleep(200);
                        Application.DoEvents();
                        var retemp = MT.COM11.SetChannelG4C(pos.Channel);
                        if (!retemp)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("切换到右侧光源通道{0}时失败，请检查串口是否连接 ...", pos.Channel));
                            return EM_RES.ERR;
                        }
                    }
                }
            }


            if (Math.Abs(ax_u.fcmd_pos - pos.U) > 0.3)
            {
                if (Math.Abs(ax_y.fenc_pos) > PT_SET.testsafey)//当目标位置越过安全位置时，需要进行安全操作，前Z轴上移
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.WAR, string.Format("{0}，U轴翻转位置不安全，Y轴先移动到安全位置!", disc));

                    ret = MT.Move(ref bquit, ref ax_y, safeuturnpos.y);
                    if (ret != EM_RES.OK)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Y轴移动到U安全翻转位置失败!", disc));
                        return EM_RES.MOVE_ERR;
                    }
                }
                ret = MT.Move(ref bquit, ref ax_u, pos.U);//U轴翻转
                if (ret != EM_RES.OK)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，U轴翻转失败，测试位置到达失败,序号：{1},位置为：{2}!", disc, pos.Name, pos.U));
                    return EM_RES.MOVE_ERR;
                }

            }
            if (Math.Abs(ax_y.fenc_pos - pos.Y) > 0.3)
            {
                if (pos.Y > PT_SET.testsafey)//当目标位置越过安全位置时，需要进行安全操作，前Z轴上移
                {
                    if (!isFrZInSafePos)
                    {
                        ret = FrZMoveToSafePos(ref bquit);
                        if (ret != EM_RES.OK)
                        {
                            return EM_RES.MOVE_ERR;
                        }
                    }
                    if (pos.Y > safeyposbk || PT_SET.bteleconvertergohome)//当目标位置越过安全位置2时，需要进行安全操作，后Z轴上移
                    {
                        if (!isBkZInSafePos)
                        {
                            ret = BkZXMoveToSafePos(ref bquit);
                            if (ret != EM_RES.OK)
                            {
                                return EM_RES.MOVE_ERR;
                            }
                        }
                    }
                }
                ret = MT.Move(ref bquit, ref ax_y, pos.Y);
                if (ret != EM_RES.OK)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Y轴移动到指定测试位置失败,序号：{1},位置为：{2}!", disc, pos.Name, pos.Y));
                    return EM_RES.MOVE_ERR;
                }
            }
            ret = MT.Move(ref bquit, ref ax_x, pos.X, ref ax_z_fr, pos.Z1, ref ax_z_bk, pos.Z2);
            if (ret != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，X轴、前Z轴、后Z轴移动到指定测试位置失败,序号：{1},位置为：{2}!", disc, pos.Name, pos.X, pos.Z1, pos.Z2));
                return EM_RES.MOVE_ERR;
            }
            //ret = ChkAxisPosUX(ref ax_x, pos.X);
            //if (ret != EM_RES.OK)
            //{
            //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，X轴移动到指定测试位置失败,序号：{1},位置为：{2}!", disc, pos.Name, pos.X));
            //    return ret;
            //}
            //ret = ChkAxisPos(ref ax_y, pos.Y);
            //if (ret != EM_RES.OK)
            //{
            //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Y轴移动到指定测试位置失败,序号：{1},位置为：{2}!", disc, pos.Name, pos.Y));
            //    return ret;
            //}
            //ret = ChkAxisPosUX(ref ax_u, pos.U);
            //if (ret != EM_RES.OK)
            //{
            //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，U轴移动到指定测试位置失败,序号：{1},位置为：{2}!", disc, pos.Name, pos.U));
            //    return ret;
            //}
            //ret = ChkAxisPos(ref ax_z_fr, pos.Z1);
            //if (ret != EM_RES.OK)
            //{
            //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Z1轴移动到指定测试位置失败,序号：{1},位置为：{2}!", disc, pos.Name, pos.Z1));
            //    return ret;
            //}
            //ret = ChkAxisPos(ref ax_z_bk, pos.Z2);
            //if (ret != EM_RES.OK)
            //{
            //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Z2轴移动到指定测试位置失败,序号：{1},位置为：{2}!", disc, pos.Name, pos.Z2));
            //    return ret;
            //}
            return ret;
        }
        public EM_RES MoveTo(ref bool bquit, int idx)
        {
            LoadCfgws(VAR.gsys_set.cur_product_name);
            //if (idx < 0 || idx >= ListPos.Count) return EM_RES.END;
            int t = Environment.TickCount;
            PosDef pos = ListPos.Find(delegate (PosDef x) { return x.ID == idx; });
            if (pos == null) return EM_RES.END;
            EM_RES res = MoveTo(ref bquit, pos);
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("{0}定位到 [{1}]{2} 位置为：X {3}，Y {4}，U {5}，Z1 {6}，Z2 {7} ", disc, pos.ID, pos.Name, pos.X, pos.Y, pos.U, pos.Z1, pos.Z2) : string.Format("{0} Move to [{2}]{3} for {4:F1}s       ({1}定位到 [{2}]{3} 用时:{4:F1}s)", disc, disc, pos.ID, pos.Name, (Environment.TickCount - t) / 1000.0));
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("{0}定位到 [{1}]{2} 用时:{3:F1}s", disc, pos.ID, pos.Name, (Environment.TickCount - t) / 1000.0) : string.Format("{0} Move to [{2}]{3} for {4:F1}s       ({1}定位到 [{2}]{3} 用时:{4:F1}s)", disc, disc, pos.ID, pos.Name, (Environment.TickCount - t) / 1000.0));
            if (res != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0}定位到 [{1}]{2} 出错,{3}", disc, pos.ID, pos.Name, Utility.GetDescription(res, VAR.IsChinese)) : string.Format("{0} move to [{2}]{3} err,{4}        ({1}定位到 [{2}]{3} 出错,{4})", disc, disc, pos.ID, pos.Name, Utility.GetDescription(res, VAR.IsChinese)), DReport.EmErrCode.MoveError, (int)DReport.EmHareware.LightBox, ERR_ALM.EmErrItem.MoveError);
            }
            return res;
        }
        #endregion

        #endregion

        #region 测试通信
        public EM_RES StartTestFlow(int status = 0, bool demo = false)
        {
            foreach (List<MdDat> list in list_list_md)
            {
                if (list.Count == 0) continue;
                //全禁用
                // if (list.Find(delegate (MdDat x) { return x.benable&&x.Ishavemd; }) == null) continue;
                if (list.Find(delegate (MdDat x) { return x.benable; }) == null) continue;
                int ret = 0;
                try
                {
                    //整合二维码
                    string str_barcode = "";
                    foreach (MdDat m in list)
                    {
                        // if (m.benable && (!VAR.gsys_set.isChkMode|| (VAR.gsys_set.isChkMode && VAR.ChkPC == m.PC_ID)))
                        if (m.benable)
                        {
                            if (!VAR.ClearMt)
                            {
                                str_barcode += (m.bardcode == null ? "NO BARCODE" : m.bardcode) + "#";
                            }
                            else
                            {
                                str_barcode += (m.bardcode == null ? "NULL" : m.bardcode) + "#";
                            }
                        }
                        else
                            str_barcode += "NULL" + "#";
                    }

                    foreach (MdDat m in list)
                    {
                        // if (m.benable && (!VAR.gsys_set.isChkMode|| (VAR.gsys_set.isChkMode && VAR.ChkPC == m.PC_ID)))
                        if (m.benable)
                        {
                            str_barcode += "1" + "@";
                        }
                        else
                            str_barcode += "0" + "@";
                    }

                    //发送启动命令
                    if (demo)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("PC{0} barcode:{1}", list[0].PC_ID, str_barcode));
                        return EM_RES.OK;
                    }
                    ret = TestPC.StartTestFlow(list[0].PC_ID, status, str_barcode.ToArray());
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("PC{0} barcode:{1}", list[0].PC_ID, str_barcode));
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("PC{0} StartFlow,ret={1},sta={2}", list[0].PC_ID, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese), status));
                    if (ret != (int)TestPC.EM_RES.OK)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.WAR, VAR.IsChinese ? string.Format("PC{0} 重发 StartFlow,ret={1},sta={2}", list[0].PC_ID, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese), status) : string.Format("PC{0} retry StartFlow,ret={1},sta={2}", list[0].PC_ID, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese), status));
                        Thread.Sleep(1000);
                        ret = TestPC.StartTestFlow(list[0].PC_ID, status, str_barcode.ToArray());
                        if (ret != (int)TestPC.EM_RES.OK)
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("PC{0} 重发 StartFlow,ret={1},sta={2}", list[0].PC_ID, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese), status) : string.Format("PC{0} retry StartFlow,ret={1},sta={2}", list[0].PC_ID, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese), status), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.TurnTable + 1, ERR_ALM.EmErrItem.TestAbnormal);
                        return EM_RES.ERR;
                    }
                }
                catch (Exception ex)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("StartTestFlow,ID={0}, {1},{2}", list[0].PC_ID, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese), ex.Message), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.TurnTable + 1, ERR_ALM.EmErrItem.TestAbnormal);
                    return EM_RES.ERR;
                }
            }
            return EM_RES.OK;
        }
        int DemoPos = 0;
        public EM_RES WaitTestResult(ref int status, int delay = 30000, bool demo = false)
        {
            int sta = -1;
            PCDat pc = null;
            status = 0;
            foreach (List<MdDat> list in list_list_md)
            {
                if (list.Count == 0) continue;
                //全禁用
                if (list.Find(delegate (MdDat x) { return x.benable; }) == null) continue;

                //发送命令
                int num = 0;
                TestPC.DeviceStruct pDeviceParam = new TestPC.DeviceStruct();
                int[] res = new int[8];
                if (!demo)
                    res = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 };
                char[] md_barcode = new char[8 * 32];
                int ret = 0;
                try
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} WaitTestResult,ID={1}", disc, list[0].PC_ID));
                    if (!demo)
                    {
                        //ret = TestPC.WaitTestResult(list[0].PC_ID, ref sta, res, ref num, ref VAR.gsys_set.bquit, delay);
                        ret = TestPC.WaitTestResultA(list[0].PC_ID, ref sta, res, ref num, ref pDeviceParam, ref VAR.gsys_set.bquit, delay, md_barcode);
                        if (ret != (int)TestPC.EM_RES.OK && ret != (int)TestPC.EM_RES.ERR_QUIT)
                        {
                            Thread.Sleep(1000);
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.WAR, string.Format("{0} 重发WaitTestResult,ID={1}", disc, list[0].PC_ID));
                            ret = TestPC.WaitTestResultA(list[0].PC_ID, ref sta, res, ref num, ref pDeviceParam, ref VAR.gsys_set.bquit, delay, md_barcode);
                        }
                    }
                    else
                    {
                        ResetResultOfMdTest();
                        break;
                    }
                    if (ret == (int)TestPC.EM_RES.OK)
                    {
                        //其中一个未完成，则取未完成状态
                        status = status >= (int)sta ? status : (int)sta;
                        //sta 测试的位置  
                        string str = string.Format("{0} PC{1},sta={2},n={3}", disc, list[0].PC_ID, sta, num);
                        //测试结束，更新结果
                        if (sta == 0)
                        {
                            string br = new string(md_barcode);
                            string[] lsstr = br.Split('#');
                            for (int n = 0; n < num && n < res.Length; n++)
                            {
                                list[n].res = res[n];
                                list[n].test_idx = 0;
                                str = str + string.Format(",{0}-{1}", res[n], lsstr.Length > n && lsstr[n][0] != '\0' ? lsstr[n] : "");
                                //if (list[n].benable && (lsstr.Length<=n ||list[n].bardcode != lsstr[n] || lsstr[n] == ""))
                                //{
                                //    list[n].res = 111;
                                //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} PC{1},发送:{2},回传{3}->发送与回传barcode不符,结果置为111", disc, list[0].PC_ID, list[n].bardcode, lsstr[n]));
                                //}

                            }
                            //确认个数
                            if (!demo)
                            {
                                int encnt = 0;
                                bRetest = false;
                                for (int i = 0; i < list.Count; i++) { if (list[i].benable) encnt++; }
                                if (encnt > num) { bRetest = true; }
                                if (bRetest)
                                {
                                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.WAR, VAR.IsChinese ? string.Format("{0} PC{1},sta={2},n={3}->接收结果缺失，重置待测!", disc, list[0].PC_ID, sta, num) : string.Format("{0} PC{1},sta={2},n={3}->lost result,reardy to retest!          ({0} PC{1},sta={2},n={3}->接收结果缺失，重置待测!)", disc, list[0].PC_ID, sta, num));
                                    for (int m = 0; m < list.Count; m++)
                                    {
                                        if (list[m].benable)
                                        {
                                            list[m].res = -1;
                                            list[m].test_idx = 0;
                                        }

                                    }
                                }
                            }

                            breschanged = true;
                        }

                        //更新PC数据
                        pc = list_pc_dat.Find(s => { return s.ID == list[0].PC_ID; });
                        if (pc != null)
                        {
                            pc.test_idx = sta;
                            if (sta == 0) pc.status = EM_PC_STA.READY;
                        }

                        //更新测试位置
                        foreach (MdDat md in list)
                        {
                            md.test_idx = sta;
                        }

                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, str);
                    }
                    else
                    {
                        if (!IsTestFirst)
                        {
                            for (int m = 0; m < list.Count; m++)
                            {
                                if (list[m].benable)
                                {
                                    list[m].res = -1;
                                    list[m].test_idx = 0;
                                }

                            }

                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} WaitTestResult,ID={1},sta={2},ret={3}", disc, list[0].PC_ID, status, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese)), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.TurnTable + num + 1);
                            if (ret != (int)TestPC.EM_RES.ERR_QUIT) return EM_RES.QUIT;
                            else return EM_RES.ERR;
                        }

                    }
                }
                catch (Exception ex)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} WaitTestResult,ID={1},ret={2},{3}", disc, list[0].PC_ID, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese), ex.Message), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.TurnTable + num + 1);
                    return EM_RES.ERR;
                }
            }

            //更新测试状态,所有电脑站完成才算完成
            pc = list_pc_dat.Find(s => { return (s.status == EM_PC_STA.TEST || s.status == EM_PC_STA.WAIT); });
            if (pc != null) TestStatus = EM_TEST_STA.NEXT;
            else TestStatus = EM_TEST_STA.COMPLETED;

            //检测同步性
            PCDat pc_temp = null;
            foreach (PCDat p in list_pc_dat)
            {
                if (id == 0 || p.test_idx == 0 || p.status == EM_PC_STA.READY || p.status == EM_PC_STA.DISABLE) continue;
                if (pc_temp == null) pc_temp = p;
                if (p.test_idx != pc_temp.test_idx)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR,
                        VAR.IsChinese ? string.Format("测试位置不同步,PC[{0}]/测试位[{1}]/[{2}],PC[{3}]/测试位[{4}/[{5}]]!", pc_temp.ID,
                            pc_temp.test_idx, pc_temp.status.ToString(), p.ID, p.test_idx, p.status.ToString()) : string.Format("Test locations are out of sync,PC[{0}]/Test locations[{1}]/[{2}],PC[{3}]/Test locations[{4}/[{5}]]!            (测试位置不同步,PC[{0}]/测试位[{1}]/[{2}],PC[{3}]/测试位[{4}/[{5}]]!)", pc_temp.ID,
                            pc_temp.test_idx, pc_temp.status.ToString(), p.ID, p.test_idx, p.status.ToString()), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.TurnTable + id + 1, ERR_ALM.EmErrItem.TestAbnormal);
                    return EM_RES.PARA_ERR;
                }
            }

            //检查结果一致性
            foreach (List<MdDat> list in list_list_md)
            {
                if (list.Count == 0) continue;
                //非禁用模组是否同一测试位
                MdDat md = list.Find(delegate (MdDat x) { return x.benable && (x.test_idx != list[0].test_idx); });
                if (md != null)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0} WaitTestResult,结果不一致,PC={1},idx={2}!", disc, md.PC_ID, md.test_idx) : string.Format("{0} WaitTestResult,Inconsistent results,PC={1},idx={2}!         ({0} WaitTestResult,结果不一致,PC={1},idx={2}!)", disc, md.PC_ID, md.test_idx), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.TurnTable + id + 1, ERR_ALM.EmErrItem.TestAbnormal);
                    return EM_RES.PARA_ERR;
                }
            }
            return EM_RES.OK;
        }
        public EM_RES NextTest(int status, bool demo = false)
        {
            //if (status >= 0)
            {
                int ret = 0;
                foreach (List<MdDat> list in list_list_md)
                {
                    if (list.Count == 0) continue;
                    //全禁用
                    if (list.Find(delegate (MdDat x) { return x.benable; }) == null) continue;

                    //检查当前转盘位置/光箱位置是否满足再发送
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} NextTest,ID={1}, sta={2}", disc, list[0].PC_ID, status));
                    //空运行
                    if (demo) continue;
                    try
                    {
                        ret = TestPC.NextTest(list[0].PC_ID, status, 3000, ref VAR.gsys_set.bquit);
                        if (ret != (int)TestPC.EM_RES.OK && ret != (int)TestPC.EM_RES.ERR_QUIT)
                        {
                            Thread.Sleep(1000);
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.WAR, VAR.IsChinese ? string.Format("{0} 重发 NextTest,ID={1}, sta={2},ret={3}", disc, list[0].PC_ID, status, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese)) : string.Format("{0} retry NextTest,ID={1}, sta={2},ret={3}       ({0} 重发 NextTest,ID={1}, sta={2},ret={3})", disc, list[0].PC_ID, status, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese)));
                            ret = TestPC.NextTest(list[0].PC_ID, status, 3000, ref VAR.gsys_set.bquit);
                            if (ret != (int)TestPC.EM_RES.OK)
                            {
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} NextTest,ID={1}, sta={2},ret={3}", disc, list[0].PC_ID, status, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese)), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.TurnTable + id + 1, ERR_ALM.EmErrItem.TestAbnormal);
                                return EM_RES.ERR;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} NextTest,ID={1}, sta={2},ret={3},{4}", disc, list[0].PC_ID, status, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese), ex.Message), DReport.EmErrCode.ResultNg, (int)DReport.EmHareware.TurnTable + id + 1, ERR_ALM.EmErrItem.TestAbnormal);
                        return EM_RES.ERR;
                    }
                }
            }

            return EM_RES.OK;
        }
        public EM_RES GetTestInfo(ref List<TestPC.InfoData> list_info, bool demo = false)
        {
            if (demo) return EM_RES.OK;
            list_info.Clear();
            foreach (PCDat pc in list_pc_dat)
            {
                if (pc.status != EM_PC_STA.DISABLE)
                {
                    TestPC.InfoData Info = new TestPC.InfoData();
                    int ret = 0;
                    try
                    {
                        ret = TestPC.GetInfo(pc.ID, ref Info);
                        if (ret != (int)TestPC.EM_RES.OK) return EM_RES.ERR;
                        else list_info.Add(Info);
                    }
                    catch (Exception ex)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("GetTestInfo,ID={0}, ret={1},{2}", pc.ID, Utility.GetDescription((TestPC.EM_RES)ret, VAR.IsChinese), ex.Message));
                    }
                }
            }
            return EM_RES.OK;
        }
        public EM_PC_STA GetStatus
        {
            get
            {
                foreach (PCDat pc in list_pc_dat)
                {
                    if (pc.status != EM_PC_STA.DISABLE)
                    {
                        try
                        {
                            int sta = -1;
                            int ret = TestPC.GetStatus(pc.ID, ref sta);
                            if (ret == (int)TestPC.EM_RES.OK)
                            {
                                //检查联机
                                int t = sta & 0xFF;
                                if (pc.temp != t) pc.cnt = 0;
                                else pc.cnt++;
                                pc.temp = t;

                                //检查状态
                                t = sta >> 8;
                                if (t == 1) pc.status = EM_PC_STA.READY;
                                else if (t == 2) pc.status = EM_PC_STA.TEST;
                                else if (t == 3) pc.status = EM_PC_STA.WAIT;
                            }
                            else pc.cnt++;

                            if (pc.cnt > 5) pc.status = EM_PC_STA.LINK_ERR;
                        }
                        catch (Exception ex)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "GetStatus," + ex.Message);
                        }
                    }
                }

                bool bdis = true;
                bool berr = false;
                bool bready = true;
                bool bsametest = true;
                bool bwait = true;
                int temp_idx = -10000;
                foreach (PCDat pc in list_pc_dat)
                {
                    if (pc.status != EM_PC_STA.DISABLE) bdis = false;
                    if (pc.status == EM_PC_STA.LINK_ERR) berr = true;
                    if (pc.status != EM_PC_STA.READY) bready = false;
                    if (pc.status != EM_PC_STA.WAIT) bwait = false;

                    //是否同一位置
                    if (temp_idx == -10000) temp_idx = pc.test_idx;
                    if (pc.test_idx != temp_idx) bsametest = false;
                    temp_idx = pc.test_idx;
                }

                if (bdis) return EM_PC_STA.DISABLE;
                if (berr) return EM_PC_STA.LINK_ERR;
                if (bwait && !bsametest) return EM_PC_STA.NOT_SAME_TESTIDX;
                if (bwait) return EM_PC_STA.WAIT;
                if (bready) return EM_PC_STA.READY;
                else return EM_PC_STA.TEST;
            }
        }
        #endregion

        #region 测试线程
        Task TestTask = null;
        public static Task GoTestTask1 = null;
        public static Task GoTestTask2 = null;
        bool brun = false;
        public void RunTestTask()
        {
            if (TestStatus == EM_TEST_STA.EMPTY || TestStatus == EM_TEST_STA.COMPLETED)
            {
                brun = false;
                return;
            }
            if (TestTask == null || TestTask != null && TestTask.IsCompleted)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "创建子测试线程");
                if (TestTask != null) TestTask.Dispose();
                TestTask = new Task(RunTest);
                brun = true;
                TestTask.Start();
            }
        }

        public EM_RES WaitTestTask(ref bool bquit)
        {
            if (TestTask == null) return EM_RES.PARA_ERR;
            while (!bquit)
            {
                Thread.Sleep(10);
                Application.DoEvents();
                if (TestTask.IsCompleted) break;
                if (!brun) break;
            }
            return EM_RES.OK;
        }

        private EM_RES RunPreMiniAutoCheckIfNeeded(PosDef pos)
        {
            bool enabled = id == 0 ? PT_SET.PreMiniAutoCheckWs1En : PT_SET.PreMiniAutoCheckWs2En;
            bool pending = id == 0 ? VAR.PreMiniAutoCheckWs1Pending : VAR.PreMiniAutoCheckWs2Pending;
            if (!enabled || !pending)
            {
                if (preMiniAutoCheckStarted || preMiniAutoCheckedPosIds.Count > 0)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} pre-mini自动点检状态清除,Enabled:{1},Pending:{2}", disc, enabled, pending));
                }
                preMiniAutoCheckedPosIds.Clear();
                preMiniAutoCheckStarted = false;
                return EM_RES.OK;
            }

            List<PosDef> checkPosList = ListPos == null ? new List<PosDef>() : ListPos.FindAll(x => x.AutoCheckEnable);
            if (checkPosList.Count == 0)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.WAR, string.Format("{0} pre-mini自动点检已触发,但未勾选点检测试位", disc));
                ClearPreMiniAutoCheckPending();
                return EM_RES.OK;
            }

            if (pos == null) return EM_RES.OK;
            if (!preMiniAutoCheckStarted)
            {
                if (pos.ID != PT_SET.PreMiniAutoCheckStartSta)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} pre-mini自动点检等待首项,当前:{1}-{2},首项:{3}", disc, pos.ID, pos.Name, PT_SET.PreMiniAutoCheckStartSta));
                    return EM_RES.OK;
                }

                preMiniAutoCheckStarted = true;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} pre-mini自动点检首项{1}到达,本轮点检开始,点检位:{2}", disc, pos.ID, string.Join(",", checkPosList.Select(x => x.ID.ToString()).ToArray())));
            }

            if (!pos.AutoCheckEnable)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} pre-mini自动点检跳过未勾选测试位:{1}-{2}", disc, pos.ID, pos.Name));
                return EM_RES.OK;
            }

            if (preMiniAutoCheckedPosIds.Contains(pos.ID))
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} pre-mini自动点检跳过已完成测试位:{1}-{2}", disc, pos.ID, pos.Name));
                return EM_RES.OK;
            }

            EM_RES res = RunPreMiniAutoCheckPoint(pos);
            if (res != EM_RES.OK) return res;

            preMiniAutoCheckedPosIds.Add(pos.ID);
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} pre-mini自动点检测试位完成:{1}-{2},已完成:{3}", disc, pos.ID, pos.Name, string.Join(",", preMiniAutoCheckedPosIds.Select(x => x.ToString()).ToArray())));
            if (checkPosList.TrueForAll(x => preMiniAutoCheckedPosIds.Contains(x.ID)))
            {
                ClearPreMiniAutoCheckPending();
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} pre-mini本轮自动点检完成", disc));
            }

            return EM_RES.OK;
        }

        private EM_RES RunPreMiniAutoCheckPoint(PosDef pos)
        {
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} {1}pre-mini自动点检单点执行,ID:{2}", disc, pos.Name, pos.ID));
            EM_RES res = PreMiniAutoChecker.RunPointCheck(this, pos);
            if (res != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} {1}pre-mini自动点检停止,ID:{2},Res:{3}", disc, pos.Name, pos.ID, res));
            }
            return res;
        }

        private void ClearPreMiniAutoCheckPending()
        {
            if (id == 0) VAR.PreMiniAutoCheckWs1Pending = false;
            if (id == 1) VAR.PreMiniAutoCheckWs2Pending = false;
            if (!VAR.PreMiniAutoCheckWs1Pending && !VAR.PreMiniAutoCheckWs2Pending) VAR.PreMiniAutoCheckTimeReached = false;
            preMiniAutoCheckedPosIds.Clear();
            preMiniAutoCheckStarted = false;
        }

        void RunTest()
        {
            EM_RES res = EM_RES.OK;
            WSFEED lb = null;
            if (id == 0) lb = COM.wsfeed1;
            if (id == 1) lb = COM.wsfeed2;
            try
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} 子测试线程开始", disc));
                while (!VAR.gsys_set.bquit)
                {
                    brun = true;
                    //获取当前上下料位置
                    EM_STA pos = GetCurSta(id);
                    //switch (pos)
                    //{
                    //    default:
                    //        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} 测试时，测试上下料模块不在测试位置!", disc));
                    //        break;
                    //    case EM_STA.FEEDPOS:
                    //        lb = null;
                    //        break;
                    //    case EM_STA.TESTPOS:
                    //        if(id==0)lb = COM.wsfeed1;
                    //        if(id==1)lb = COM.wsfeed2;
                    //        break;
                    //}
                    //获取测试软件状态
                    List<TestPC.InfoData> list_info = new List<TestPC.InfoData>();
                    res = GetTestInfo(ref list_info, Demo);
                    if (res != EM_RES.OK)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.WAR, VAR.IsChinese ? string.Format("{0}与测试软件通信异常！", disc) : string.Format("{0} link err!        ({0}与测试软件通信异常！)", disc), DReport.EmErrCode.ComTimout, (int)DReport.EmHareware.TurnTable + id + 1);
                        VAR.gsys_set.status = EM_SYS_STA.STANDBY;
                        testprostaus = EM_PC_STA.LINK_ERR;
                        FeedStatus = EM_TESTPROGRESS_STA.LINKERR;
                        VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "通信异常!" : "Link ERR", 30, true);
                        MT.ST_WARN st_warn = new MT.ST_WARN();
                        warning fr_warn = new warning();
                        st_warn.ok_txt = VAR.IsChinese ? "确定" : "ok";
                        st_warn.abort_txt = VAR.IsChinese ? "确定" : "abort";
                        st_warn.ws = null;
                        st_warn.cancle_txt = VAR.IsChinese ? "取消" : "cancle";
                        st_warn.title = VAR.IsChinese ? "测试软件通信异常,请确认原因!!" : "Tip: Abnormal suction!";
                        st_warn.msg = string.Format("{0}测试软件通信异常!", disc);
                        st_warn.lb_msg = "提示:" + st_warn.msg;
                        DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);

                        if (DialogResult.OK == logres)
                        {
                            break;
                        }
                        else if (DialogResult.Abort == logres)
                        {
                            break;
                        }
                        else if (DialogResult.Retry == logres)
                        {
                            break;
                        }

                        break;
                    }
                    if (VAR.gsys_set.bquit || bquit)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} 取消", disc));
                        res = EM_RES.QUIT;
                        break;
                    }
                    //等待测试结果
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} 等待测试结果", disc));
                    int sta = 0;
                    testprostaus = EM_PC_STA.TEST;
                    FeedStatus = EM_TESTPROGRESS_STA.TEST;
                    breschanged = true;
                    //等待测试结果
                    res = WaitTestResult(ref sta, PT_SET.TestTime * 100000, Demo);
                    if (res == EM_RES.PARA_ERR || res == EM_RES.QUIT)
                    {
                        //不同步等异常
                        break;
                    }
                    else if (res != EM_RES.OK)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} WaitTestResult err", disc), DReport.EmErrCode.TestFailed, (int)DReport.EmHareware.TurnTable + id + 1, ERR_ALM.EmErrItem.TestAbnormal);
                        testprostaus = EM_PC_STA.LINK_ERR;
                        FeedStatus = EM_TESTPROGRESS_STA.LINKERR;
                        VAR.sys_inf.Set(EM_ALM_STA.WAR_RED_FLASH, VAR.IsChinese ? "通信异常!" : "Link ERR", 30, true);
                        MT.ST_WARN st_warn = new MT.ST_WARN();
                        warning fr_warn = new warning();
                        st_warn.ok_txt = VAR.IsChinese ? "确定" : "ok";
                        st_warn.abort_txt = VAR.IsChinese ? "确定" : "abort";
                        st_warn.ws = null;
                        st_warn.cancle_txt = VAR.IsChinese ? "取消" : "cancle";
                        st_warn.title = VAR.IsChinese ? "测试软件通信异常,请确认原因!!" : "Tip: Abnormal suction!";
                        st_warn.msg = string.Format("{0}测试软件通信异常!", disc);
                        st_warn.lb_msg = "提示:" + st_warn.msg;
                        DialogResult logres = MT.Display_frwarn(fr_warn, st_warn, ERR_ALM.EmErrItem.UpDownLoadAbnormal);

                        if (DialogResult.OK == logres)
                        {
                            break;
                        }
                        else if (DialogResult.Abort == logres)
                        {
                            break;
                        }
                        else if (DialogResult.Retry == logres)
                        {
                            break;
                        }
                        break;
                    }
                    if (sta == 0)
                    {
                        Gotostarttestpos = false;
                        if (PT_SET.bGrrFlow || VAR.gsys_set.isChkMode)
                        {
                            if (PT_SET.bGrrFlow) GrrTestCnt++;
                            if (VAR.gsys_set.isChkMode) ChkCnt++;
                        }
                        if (!VAR.gsys_set.isChkMode)
                        {
                            //保存结果
                            int t = System.Environment.TickCount - tmr;
                            foreach (MdDat md in list_md)
                            {
                                md.ct = t;
                                md.test_idx = 0;
                                //夹具统计更新

                            }
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("{0} 存储结果", disc) : string.Format("{0} save result       ({0} 存储结果)", disc));
                            //gy0123夹具数据记录
                            //  JigProductionCnt(ref list_md);
                            SQLData.TestDataAdd(this);
                        }

                        if (lb != null)
                        {
                            res = lb.MoveTo(ref VAR.gsys_set.bquit, 0);

                            if (PT_SET.btlightgohome)
                            {
                                Task tsk = new Task(() =>
                                {
                                    res = MT.Move(ref bquit, ref ax_z_fr, 0);
                                    if (res != EM_RES.OK)
                                    {
                                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，前Z轴移动到原点位置失败", disc));
                                    }

                                });
                                tsk.Start();

                            }
                            // if (res != EM_RES.OK) break;
                        }

                        res = NextTest(sta, Demo);
                        if (res != EM_RES.OK)
                        {
                            TestStatus = EM_TEST_STA.ERROR;
                            break;
                        }
                        breschanged = true;
                        //测试完成
                        TestStatus = EM_TEST_STA.COMPLETED;
                        testprostaus = EM_PC_STA.ENDOFTEST;
                        FeedStatus = EM_TESTPROGRESS_STA.REDAY;
                        break;
                    }
                    else
                    {
                        //if (TestStatus == EM_TEST_STA.COMPLETED || TestStatus == EM_TEST_STA.ERROR || TestStatus == EM_TEST_STA.EMPTY)
                        if (TestStatus == EM_TEST_STA.EMPTY)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0} 物料状态异常，不进行污坏点测试，测试结束！", disc));
                            break;
                        }
                        if (VAR.gsys_set.bquit || bquit)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("{0} 取消", disc) : string.Format("{0} cancel       ({0} 取消)", disc));
                            res = EM_RES.QUIT;
                            break;
                        }
                        if (lb != null)
                        {
                            //光箱定位
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("{0} {1}定位{2}", disc, lb.disc, sta) : string.Format("{0}  {1} move {3}         ({0} {2}定位{3})", disc, lb.disc, lb.disc, sta));
                            try
                            {
                                res = lb.MoveTo(ref VAR.gsys_set.bquit, sta);
                                if (res == EM_RES.END)
                                {
                                    res = lb.MoveTo(ref VAR.gsys_set.bquit, 0);
                                    if (res != EM_RES.OK) break;

                                    //本站测试完成
                                    TestStatus = EM_TEST_STA.NEXT;
                                    testprostaus = EM_PC_STA.TEST;
                                    FeedStatus = EM_TESTPROGRESS_STA.TEST;
                                    res = EM_RES.OK;
                                    break;
                                }
                                if (res != EM_RES.OK) break;
                            }
                            catch (Exception ex)
                            {
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}", ex.Message));
                            }
                        }
                        //else
                        //{
                        //    if (sta < 401 && PT_SET.bDelayTest)
                        //    {
                        //        //本站测试完成
                        //        TestStatus = EM_TEST_STA.NEXT;
                        //        testprostaus = EM_PC_STA.TEST;
                        //        FeedStatus = EM_TESTPROGRESS_STA.TEST;
                        //        res = EM_RES.OK;
                        //        break;
                        //    }
                        //    else break;
                        //}
                        //通知测试
                        testprostaus = EM_PC_STA.TEST;
                        FeedStatus = EM_TESTPROGRESS_STA.TEST;
                        breschanged = true;
                        PosDef pos1 = ListPos.Find(delegate (PosDef x) { return x.ID == sta; });
                        res = RunPreMiniAutoCheckIfNeeded(pos1);
                        if (res != EM_RES.OK)
                        {
                            TestStatus = EM_TEST_STA.ERROR;
                            break;
                        }
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, VAR.IsChinese ? string.Format("{0} 通知测试{1} ", disc, pos1.Name) : string.Format("{0}  notice test!        ({0} 通知测试)", disc));
                        res = NextTest(sta, Demo);
                        if (res != EM_RES.OK)
                        {
                            res = NextTest(sta);
                            if (res != EM_RES.OK)
                            {
                                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, VAR.IsChinese ? string.Format("{0} 通知测试{1}出错 ", disc, pos1.Name) : string.Format("{0}  notice test!        ({0} 通知测试)", disc));
                                TestStatus = EM_TEST_STA.ERROR;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} 测试线程 {1} 结束,{2}", disc, res != EM_RES.OK ? "异常" : "正常", ex.Message));
                res = EM_RES.ERR;
            }
            finally
            {
                if (res != EM_RES.OK)
                {
                    TestStatus = EM_TEST_STA.ERROR;
                    testprostaus = EM_PC_STA.LINK_ERR;
                }
                brun = false;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} 测试线程 {1} 结束", disc, res != EM_RES.OK ? "异常" : "正常"));
            }
        }
        #endregion

        #region 上下料准备
        /// <summary>
        /// 准备上下料，测试机台回原位，上下料轴移动到上料位，真空吸关闭
        /// </summary>
        /// <param name="bquit"></param>
        /// <returns></returns>
        public EM_RES SetupForFeed(ref bool bquit, bool finsh)
        {
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}，准备去往上料位置!", disc));
            iswaitfeed = true;
            Isstartchart = false;
            EM_RES res = EM_RES.OK;
            Iscanclose = false;
            res = MoveToFeedPos(ref bquit);//移动到上料位置
            if (res != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Y、U轴移动到上料位置失败!", disc));
                return EM_RES.ERR;
            }
            else
            {
                //这里加入等待另一个工位到达上料位Openwithbox
                //WSFEED wsfeedtemp=null;
                //if(id==0)
                //{
                //    wsfeedtemp = COM.wsfeed2;
                //}
                //else
                //{
                //    wsfeedtemp = COM.wsfeed1;
                //}
                Iscanopen = true;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，等待另一个测试模块到达上料位置!", disc));
                int n = 0;
                while ((!COM.wsfeed1.Iscanopen || !COM.wsfeed2.Iscanopen) && !WSFEED.bquit && PT_SET.Openwithbox)
                {
                    if (n++ > 20000) break;
                    Thread.Sleep(10);
                    Application.DoEvents();
                }

                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，等待另一个测试模块到达上料位置到达成功!", disc));
                res = Cylup(ref UpDownLoad.bquit, finsh);
                if (res != EM_RES.OK)
                {
                    return EM_RES.MOVE_ERR;
                }
                if (id == 0)
                {
                    MT.GPIO_OUT_WSFEED1_HenWen.SetOff();
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}恒温关闭", COM.wsfeed1.disc));
                }
                else
                {
                    MT.GPIO_OUT_WSFEED2_HenWen.SetOff();
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}恒温关闭", COM.wsfeed2.disc));
                }

                Gotostarttestpos = false;
                Thread.Sleep(50);
                return EM_RES.OK;
            }
        }
        /// <summary>
        /// 准备测试，运动到测试位
        /// </summary>
        /// <param name="bquit"></param>
        /// <returns></returns>
        public EM_RES SetupForTest(ref bool bquit)
        {
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}，准备去往测试位置!", disc));
            EM_RES res = EM_RES.OK;

            if (isInTestPos && Isstartchart) return res;
            if (Gotostarttestpos && Isstartchart)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0},测试未完成，已经在测试位置，初始定位不定位!", disc));
                return res;
            }

            //这里加入等待另一个工位到达上料位Openwithbox
            Iscanclose = true;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，等待另一个测试模块上料完成!", disc));
            int n = 0;
            while ((!COM.wsfeed1.Iscanclose || !COM.wsfeed2.Iscanclose) && !WSFEED.bquit && PT_SET.Openwithbox)
            {
                if (n++ > 20000) break;
                Thread.Sleep(10);
                Application.DoEvents();
            }

            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，等待另一个测试模块上料完成，成功!", disc));
            res = Cyldown(ref UpDownLoad.bquit);
            if (res != EM_RES.OK)
            {
                Isstartchart = false;
                return EM_RES.MOVE_ERR;
            }
            Iscanopen = false;
            if (PT_SET.turnon) VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0}夹具关闭完成，开图允许指令发出", disc));
            if (PT_SET.turnon && id == 0) UpDownLoad.wsfeed1openchart = true;
            if (PT_SET.turnon && id == 1) UpDownLoad.wsfeed2openchart = true;
            if ((UpDownLoad.wsfeed1openchart && id == 0) || (UpDownLoad.wsfeed2openchart && id == 1))
            {
                //if (TestStatus == EM_TEST_STA.COMPLETED || TestStatus == EM_TEST_STA.ERROR || TestStatus == EM_TEST_STA.EMPTY)
                //{
                //    VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("{0} 物料状态异常，不开图，测试结束！", disc));                    
                //}
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} 开图", disc));
                // Thread.Sleep(10000);
                Isstartchart = false;
                res = StartTestFlow(0, Demo);//开图命令
                if (res != EM_RES.OK)
                {
                    Thread.Sleep(1500);
                    res = StartTestFlow();
                    if (res != EM_RES.OK)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} StartTestFlow err", disc));
                        testprostaus = EM_PC_STA.LINK_ERR;
                        FeedStatus = EM_TESTPROGRESS_STA.LINKERR;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("测试软件通信异常，请检查", disc));
                        return EM_RES.TIMEOUT;

                    }
                }
                Isstartchart = true;
                if (id == 0) UpDownLoad.wsfeed1openchart = false;
                if (id == 1) UpDownLoad.wsfeed2openchart = false;
                Thread.Sleep(PT_SET.OpenDly);
            }
            res = MoveToTestpos(ref bquit);//移动到测试位置
            if (res != EM_RES.OK)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}，Y、Z、U轴移动到初始测试位置失败!", disc));
                return EM_RES.ERR;
            }
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}，Y、Z、U轴移动到初始测试位置成功!", disc));
            return EM_RES.OK;
        }
        #endregion

        #region 测试上下料线程
        public static void GoTestRunTask1()
        {
            if (GoTestTask1 == null || GoTestTask1 != null && GoTestTask1.IsCompleted)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, string.Format("创建去往测试位置线程1!"));
                if (GoTestTask1 != null) GoTestTask1.Dispose();
                GoTestTask1 = new Task(GoTest_th1);
                BaseAction.brun3 = true;
                GoTestTask1.Start();
            }
        }
        public static void GoTestRunTask2()
        {
            if (GoTestTask2 == null || GoTestTask2 != null && GoTestTask2.IsCompleted)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, string.Format("创建去往测试位置线程2!"));
                if (GoTestTask2 != null) GoTestTask2.Dispose();
                GoTestTask2 = new Task(GoTest_th2);
                BaseAction.brun4 = true;
                GoTestTask2.Start();
            }
        }
        public static void GoTest_th1()
        {
            EM_RES ret = EM_RES.OK;
            BaseAction.brun3 = true;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "去往测试位置线程1启动");
            try
            {
                while (VAR.gsys_set.bclose == false && VAR.gsys_set.bquit == false && !UpDownLoad.bquit)
                {
                    if (UpDownLoad.wsfeed1cangotest && !UpDownLoad.bquit)
                    {
                        int benablecnt = 0;
                        foreach (MdDat md in COM.wsfeed1.list_md)
                        {
                            if (!md.benable) continue;
                            benablecnt++;
                        }
                        if (benablecnt == 0) return;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}去往测试位置函数开始", COM.wsfeed1.disc));
                        MT.GPIO_OUT_WSFEED1_HenWen.SetOn();
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}恒温打开", COM.wsfeed1.disc));
                        COM.wsfeed1.Iscanopen = false;
                        COM.wsfeed1.Iscanclose = false;
                        ret = COM.wsfeed1.SetupForTest(ref VAR.gsys_set.bquit);
                        if (ret != EM_RES.OK)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}到达测试位置失败", COM.wsfeed1.disc));
                            BaseAction.brun3 = false;
                            return;
                        }

                        UpDownLoad.wsfeed1cangotest = false;
                        int n = 0;
                        //while ((!COM.wsfeed1.isInTestPos) && !bquit)
                        //{
                        //    if (n++ > 8000) break;
                        //    Thread.Sleep(10);
                        //    Application.DoEvents();
                        //}
                        if (bquit)
                        {
                            UpDownLoad.wsfeed1cangotest = true;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}到达测试位置失败,原因：退出", COM.wsfeed1.disc));
                            BaseAction.brun3 = false;
                            return;
                        }
                        if (n > 8000)
                        {
                            UpDownLoad.wsfeed1cangotest = true;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}到达测试位置失败", COM.wsfeed1.disc));
                            BaseAction.brun3 = false;
                            return;
                        }
                        COM.wsfeed1.wsfeedfeedandteststaus = EM_TESTPROGRESS_STA.REDAYFORTEST;
                    }
                }

            }
            finally
            {

                VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "模块1等待去往测试位置线程结束");
            }
        }

        public static void GoTest_th2()
        {
            EM_RES ret = EM_RES.OK;
            BaseAction.brun4 = true;
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "去往测试位置线程2启动");
            try
            {
                while (VAR.gsys_set.bclose == false && VAR.gsys_set.bquit == false && !UpDownLoad.bquit)
                {
                    if (UpDownLoad.wsfeed2cangotest && !UpDownLoad.bquit)
                    {
                        int benablecnt = 0;
                        foreach (MdDat md in COM.wsfeed2.list_md)
                        {
                            if (!md.benable) continue;
                            benablecnt++;
                        }
                        if (benablecnt == 0) return;
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}去往测试位置函数开始", COM.wsfeed2.disc));
                        MT.GPIO_OUT_WSFEED2_HenWen.SetOn();
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0}恒温打开", COM.wsfeed2.disc));
                        COM.wsfeed2.Iscanopen = false;
                        COM.wsfeed2.Iscanclose = false;
                        ret = COM.wsfeed2.SetupForTest(ref VAR.gsys_set.bquit);
                        if (ret != EM_RES.OK)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}到达测试位置失败", COM.wsfeed2.disc));
                            BaseAction.brun4 = false;
                            return;
                        }



                        UpDownLoad.wsfeed2cangotest = false;
                        int t = 0;
                        //while ((!COM.wsfeed2.isInTestPos) && !bquit)
                        //{
                        //    if (t++ > 8000) break;
                        //    Thread.Sleep(10);
                        //    Application.DoEvents();
                        //}
                        if (bquit)
                        {
                            UpDownLoad.wsfeed2cangotest = true;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}到达测试位置失败,原因：退出", COM.wsfeed2.disc));
                            BaseAction.brun4 = false;
                            return;
                        }
                        if (t > 8000)
                        {
                            UpDownLoad.wsfeed2cangotest = true;
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0}到达测试位置失败", COM.wsfeed2.disc));
                            BaseAction.brun4 = false;
                            return;
                        }
                        COM.wsfeed2.wsfeedfeedandteststaus = EM_TESTPROGRESS_STA.REDAYFORTEST;
                    }
                }

            }
            finally
            {

                VAR.msg.AddMsg(Msg.EM_MSGTYPE.SYS, "模块2等待去往测试位置线程结束");
            }
        }
        #endregion
    }

}
