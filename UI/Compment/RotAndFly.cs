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
    public partial class RotAndFly : UserControl
    {
        public List<XT> list_xt = new List<XT>();
        public XT FlyXt = COM.xt1;

        XT mcurXt = null;
        XT mcurUdloadXt = null;
        public List<UpDownLoad> list_UDLoad = new List<UpDownLoad>();
        public UpDownLoad mcurUDLoad = new UpDownLoad();
        Cam dwcam = null;
        /// <summary>
        /// 获取当前吸头
        /// </summary>
        public XT curXt
        {
            get
            {
                if (cb_xt.SelectedIndex >= 0 && cb_xt.SelectedIndex < list_xt.Count)
                {
                    mcurXt = list_xt.ElementAt(cb_xt.SelectedIndex);
                    dwcam = mcurXt.dwcam;

                }
                return mcurXt;
            }
        }

        /// <summary>
        /// 获取当前组装模块
        /// </summary>
        public UpDownLoad curUDload
        {
            get
            {
                if (cb_UDload.SelectedIndex >= 0 && cb_UDload.SelectedIndex < list_UDLoad.Count)
                {
                    mcurUDLoad = list_UDLoad.ElementAt(cb_UDload.SelectedIndex);
                    mcurUdloadXt = mcurUDLoad.list_xt[0];
                }
                return mcurUDLoad;
            }
        }


        public RotAndFly()
        {
            InitializeComponent();
        }


        public void PmsEn(User.PERMISSION pms)
        {
            if (pms <= User.PERMISSION.Engineer)
            {
                this.Enabled = false;
            }
            else
            {
                this.Enabled = true;
            }
        }

        private void cb_xt_SelectedIndexChanged(object sender, EventArgs e)
        {
            mcurXt = curXt;
            if (mcurXt == null) return;
            txb_rot_x.Text = mcurXt.st_rol.x.ToString("f3");
            txb_rot_y.Text = mcurXt.st_rol.y.ToString("f3");
            txb_rot_rmse.Text = mcurXt.st_rol.z.ToString("f3");
            nud_rot_downcam_y.Value = (decimal)mcurXt.rol_cap_befrcali;
           
           
        }

          private void cb_UDload_SelectedIndexChanged(object sender, EventArgs e)
        {
            mcurUDLoad = curUDload;
            tb_xt1_cap.Text = mcurUdloadXt.xt_cap_ofs.ToString("F3");
            tb_xt2_cap.Text = mcurUdloadXt.xt_cap_ofs.ToString("F3");
            tb_ws_cap_before.Text = mcurUdloadXt.ws_cap_near_ofs.ToString("F3");
            tb_ws_cap_after.Text = mcurUdloadXt.ws_cap_far_ofs.ToString("F3");
        }

        public void UpdateComBox()
        {
            string cur_sel = cb_xt.Text;
            //吸头更新
            cb_xt.Items.Clear();
            foreach (XT xt in list_xt) cb_xt.Items.Add(xt.disc);
            if (cb_xt.Items.Count > 0)
            {
                int idx = cb_xt.Items.IndexOf(cur_sel);
                if (idx < 0)
                 cb_xt.SelectedIndex = 0;
            }
            mcurXt = curXt;
           
            //模组更新
             cur_sel = cb_UDload.Text;
            cb_UDload.Items.Clear();

            foreach (UpDownLoad ud in list_UDLoad)
            {
                cb_UDload.Items.Add(ud.disc);
            }

            if (cb_UDload.Items.Count > 0)
            {
                int idx = cb_UDload.Items.IndexOf(cur_sel);
                if (idx < 0)
                    cb_UDload.SelectedIndex = 0;
            }
            mcurUDLoad = curUDload;


        }

        public void AddXt(XT xt)
        {
            if (xt != null)
            {
                foreach(XT Xt in list_xt)
                {
                    if (Xt.id == xt.id) return;
                }

                list_xt.Add(xt);
            }
        }

        public void AddXt(List<XT> listXt)
        {
            if (listXt != null)
            {
                foreach(XT xt in listXt)
                {
                    AddXt(xt);
                }
            }
        }

        public void AddUDLoad(UpDownLoad ud)
        {
            bool IsDif = false;
            if (ud != null)
            {
                foreach (UpDownLoad _ud in list_UDLoad)
                {
                    if (ud.id == _ud.id)
                        IsDif = true;
                }
                if (!IsDif)
                    list_UDLoad.Add(ud);
            }
        }

        public void AddUDLoad(List<UpDownLoad> list_UDLoad)
        {
            if (list_UDLoad != null)
            {
                foreach (UpDownLoad _ud in list_UDLoad)
                {
                    AddUDLoad(_ud);
                    AddXt(_ud.list_xt);
                }
                UpdateComBox();
            }
        }


        public EM_RES CalcRotCenterAction()
        {
            EM_RES ret;
            ST_XYZA RotCenter = new ST_XYZA();
            Cam.VisionTask task = null;
            int id = 0;
         
            if (mcurXt == null)
            {
                MessageBox.Show("未找到可用的吸头!");
                return EM_RES.ERR;
            }

            if (mcurXt.id == 1 || mcurXt.id == 3)
                id = 1;

            VAR.gsys_set.bquit = false;
            MT.SetAllAxToManualSpd();
            //if (Math.Abs(UploadModle.ax_z.fenc_pos) > 0.5)
            //{
            //    ret = UploadModle.ZHome(ref VAR.gsys_set.bquit);
            //    if (ret != EM_RES.OK) return EM_RES.ERR;
            //}
            //ret = MT.Move(ref VAR.gsys_set.bquit, ref mcurXt.ax_z, 0);
            //if (ret != EM_RES.OK) return ret;
            ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref mcurXt.ax_y, (double)nud_rot_downcam_y.Value, ref mcurXt.ax_u, 0,true);
            if (ret != EM_RES.OK) return EM_RES.ERR;
            dwcam.List_vs_task_cur.Clear(); 
            task = dwcam.List_vs_task.Find(s => s.TaskName.Equals((id+1).ToString()+"_XtSharp"));
            dwcam.List_vs_task_cur.Add(task);
            ret = dwcam.CalRotCenter(ref VAR.gsys_set.bquit, ref RotCenter, task, id == 0 ? true : false, FrMain.frrun.cogDisplayer_run.cogRecordDisplay);
            if (ret != EM_RES.OK) return EM_RES.ERR;
            //again
            ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref mcurXt.ax_y, (double)nud_rot_downcam_y.Value + RotCenter.y, ref mcurXt.ax_u, 0);
            if (ret != EM_RES.OK) return EM_RES.ERR;

            ret = dwcam.CalRotCenter(ref VAR.gsys_set.bquit, ref RotCenter, task, id == 0  ? true : false, FrMain.frrun.cogDisplayer_run.cogRecordDisplay);
            if (ret != EM_RES.OK) return EM_RES.ERR;

            mcurXt.st_rol.x = RotCenter.x;
            mcurXt.st_rol.y = RotCenter.y;
            mcurXt.st_rol.z = RotCenter.a;
            mcurXt.st_rol_cap.x = mcurXt.ax_x.fenc_pos;
            mcurXt.st_rol_cap.y = mcurXt.ax_y.fenc_pos;
            mcurXt.st_cap_pos.y = mcurXt.st_rol_cap.y;// + xt.cap_offset;
            mcurXt.rol_cap_befrcali = mcurXt.st_rol_cap.y;
            mcurXt.SaveHwCfg();

            //show data
            txb_rot_x.Text = mcurXt.st_rol.x.ToString("f3");
            txb_rot_y.Text = mcurXt.st_rol.y.ToString("f3");
            txb_rot_rmse.Text = mcurXt.st_rol.z.ToString("f3");
            nud_rot_downcam_y.Value = (decimal)mcurXt.rol_cap_befrcali;
            return EM_RES.OK;
        }
        private void btn_rot_action_Click(object sender, EventArgs e)
        {
            EM_RES ret;
            ret = CalcRotCenterAction();
            if (ret == EM_RES.OK)
            {
                MessageBox.Show(mcurXt.disc+"旋转中心校正成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Question);
            }
            else
            {
                MessageBox.Show(mcurXt.disc + "旋转中心校正失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            dwcam.graphics_other.Clear();
        }

        /// <summary>
        /// 吸头飞拍动作
        /// </summary>
        /// <param name="udl"></param>
        /// <returns></returns>
        public EM_RES XtFlyAction(ref UpDownLoad udl)
        {
            EM_RES ret;                        
            //Cam.VisionTask task = null;
            List<double> StaticCap = new List<double>();
            List<double> MoveCap = new List<double>();
            List<double> MoveCap1 = new List<double>();
            double[] ofs = new double[4];
            MT.SetAllAxToWorkSpd();
            ST_XY pos_flystop = new ST_XY();                
            List<WSFEED.MdDat> WsTriPos = new List<WSFEED.MdDat>();
            WSFEED  ws = WSFEED.GetWSOnFeedPos;
            if (ws == null)
            {
                MessageBox.Show(VAR.IsChinese ? "没有找到当前上料位工站！" : "No current loading station was found!\r\n没有找到当前上料位工站！");
                return EM_RES.ERR;
            }
            //X方向定位
            if (cb_UDload.SelectedIndex == 0)
            {
                ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_x, COM.wsfeed1.list_md[3].st_place_pos[udl.id].x, true);
                if (ret != EM_RES.OK) return ret;
            }
            else
            {
                ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_x, COM.wsfeed1.list_md[0].st_place_pos[udl.id].x, true);
                if (ret != EM_RES.OK) return ret;
            }

            //ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_x, COM.wsfeed1.list_md[2].st_place_pos[udl.id].x, true);
            //if (ret != EM_RES.OK) return ret;

            //吸头2定拍
            ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_y, udl.list_xt[1].st_cap_pos.y, ref udl.ax_u2, 0,true);
            if (ret != EM_RES.OK) return ret;
            Thread.Sleep(500);
            ret = udl.dwcam.FindTaskTriAndWait(CONST.ModDwFw[1]);
            if (ret != EM_RES.OK) return ret;
            StaticCap.Add(udl.dwcam.curTask.ResData.PosMM.y);


            //吸头1定拍
            ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_y, udl.list_xt[0].st_cap_pos.y, ref udl.ax_u1, 0,true);
            if (ret != EM_RES.OK) return ret;
            Thread.Sleep(500);
            ret = udl.dwcam.FindTaskTriAndWait(CONST.ModDwFw[0]);
            if (ret != EM_RES.OK) return ret;
            StaticCap.Add(udl.dwcam.curTask.ResData.PosMM.y);
            //模组1定拍
            if (cb_UDload.SelectedIndex == 0)
            {
                ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_y, COM.wsfeed1.list_md[3].st_place_pos[udl.id].y, true);              
            }
            else
            {
                ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_y, COM.wsfeed1.list_md[0].st_place_pos[udl.id].y, true);
            }

            //ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_y, COM.wsfeed1.list_md[3].st_place_pos[udl.id].y,true);
            if (ret != EM_RES.OK) return EM_RES.ERR;
            Thread.Sleep(500);
            ret = udl.upcam.FindTaskTriAndWait(CONST.WsUpFw);
            if (ret != EM_RES.OK) return ret;
            StaticCap.Add(udl.upcam.curTask.ResData.PosMM.y);
            //模组2定拍
            if (cb_UDload.SelectedIndex == 0)
            {
                ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_y, COM.wsfeed1.list_md[7].st_place_pos[udl.id].y, true);
            }
            else
            {
                ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_y, COM.wsfeed1.list_md[4].st_place_pos[udl.id].y, true);
            }
            //ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_y, COM.wsfeed1.list_md[2].st_place_pos[udl.id].y,true);
            if (ret != EM_RES.OK) return EM_RES.ERR;
            Thread.Sleep(500);
            ret = udl.upcam.FindTaskTriAndWait(CONST.WsUpFw);
            if (ret != EM_RES.OK) return ret;
            StaticCap.Add(udl.upcam.curTask.ResData.PosMM.y);

            udl.upcam.mAcqFifo.Flush();
            udl.dwcam.mAcqFifo.Flush();

            ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_y, 300,true);
            if (ret != EM_RES.OK) return ret;
            if (cb_UDload.SelectedIndex == 0)
            {
                WsTriPos.Add(COM.wsfeed1.list_md[3]);
                WsTriPos.Add(COM.wsfeed1.list_md[7]);
            }
            else
            {
                WsTriPos.Add(COM.wsfeed1.list_md[0]);
                WsTriPos.Add(COM.wsfeed1.list_md[4]);
            }            
            //这里暂定COM.wsfeed1
            if (cb_UDload.SelectedIndex == 0)
            {
                ret = udl.FlyToWsaction(ref  VAR.gsys_set.bquit, ref pos_flystop, WsTriPos, false);
            }
            else
            {
                ret = udl.FlyToWsaction(ref  VAR.gsys_set.bquit, ref pos_flystop, WsTriPos, false);
            }
            //ret = udl.FlyToWs(ref  VAR.gsys_set.bquit, ref pos_flystop, WsTriPos,COM.wsfeed1 ,false);
            if (ret != EM_RES.OK) return ret;


            //确认视觉回传结果 
            ret = udl.WaitCamRes(udl.dwcam, WsTriPos.Count);
            if (ret != EM_RES.OK) return ret;
            ret = udl.WaitCamRes(udl.upcam, WsTriPos.Count);
            if (ret != EM_RES.OK) return ret;

            foreach (Cam.VisionTask tsk in udl.dwcam.List_vs_task_cur)
            {
                MoveCap.Add(tsk.ResData.PosMM.y);
            }
            foreach (Cam.VisionTask tsk in udl.upcam.List_vs_task_cur)
            {
                MoveCap.Add(tsk.ResData.PosMM.y);
            }

            for (int i = 0; i < MoveCap.Count; i++)
            {
                ofs[i] = MoveCap[i] - StaticCap[i];

            }
 
            udl.list_xt[0].xt_cap_ofs = ofs[1];
            udl.list_xt[0].ws_cap_near_ofs = ofs[2];
            udl.list_xt[0].ws_cap_far_ofs = ofs[3];
            udl.list_xt[1].xt_cap_ofs = ofs[0];
            udl.list_xt[1].ws_cap_near_ofs = ofs[2];
            udl.list_xt[1].ws_cap_far_ofs = ofs[3];
            foreach (XT xt in udl.list_xt)
            {
                xt.SaveHwCfg();
            }

            //update show
            //tb_xt1_cap.BeginInvoke(new Action(() =>
            //{
            //    tb_xt1_cap.Text = ofs[1].ToString("F3");
            //}));
            //tb_xt2_cap.BeginInvoke(new Action(() =>
            //{
            //    tb_xt2_cap.Text = ofs[0].ToString("F3");
            //}));
            //tb_ws_cap_before.BeginInvoke(new Action(() =>
            //{
            //    tb_ws_cap_before.Text = ofs[2].ToString("F3");
            //}));
            //tb_ws_cap_after.BeginInvoke(new Action(() =>
            //{
            //    tb_ws_cap_after.Text = ofs[3].ToString("F3");
            //}));
            tb_xt1_cap.Text = ofs[1].ToString("F3");
            tb_xt2_cap.Text = ofs[0].ToString("F3");
            tb_ws_cap_before.Text = ofs[2].ToString("F3");
            tb_ws_cap_after.Text = ofs[3].ToString("F3");
            //Utility.WriteStrToCSV(VAR.gsys_set.GetCurProductPath + "ofsdata.csv", string.Format("{0},{1:f3},{2:f3},{3:f3},{4:f3}", DateTime.Now.ToString("hh:mm:ss:fff"), ofs[1], ofs[0], ofs[2], ofs[3]));
            Thread.Sleep(300);
            ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref udl.ax_y, 300,true);
            Thread.Sleep(300);
            if (ret != EM_RES.OK) return ret;

            udl.upcam.mAcqFifo.Flush();
            udl.dwcam.mAcqFifo.Flush();
            //这里暂定COM.wsfeed1
            ret = udl.FlyToWsaction(ref  VAR.gsys_set.bquit, ref pos_flystop, WsTriPos, true);
            if (ret != EM_RES.OK) return ret;
            //确认视觉回传结果 
            ret = udl.WaitCamRes(udl.dwcam, WsTriPos.Count);
            if (ret != EM_RES.OK) return ret;
            ret = udl.WaitCamRes(udl.upcam, WsTriPos.Count);
            if (ret != EM_RES.OK) return ret;

            foreach (Cam.VisionTask tsk in udl.dwcam.List_vs_task_cur)
            {
                MoveCap1.Add(tsk.ResData.PosMM.y);
            }
            foreach (Cam.VisionTask tsk in udl.upcam.List_vs_task_cur)
            {
                MoveCap1.Add(tsk.ResData.PosMM.y);
            }

            for (int i = 0; i < MoveCap1.Count; i++)
            {
                ofs[i] = MoveCap1[i] - StaticCap[i];

            }
            //tb_xt1_ofs.BeginInvoke(new Action(() =>
            //{
            //    tb_xt1_ofs.Text = ofs[1].ToString("F3");
            //}));
            //tb_xt2_ofs.BeginInvoke(new Action(() =>
            //{
            //    tb_xt2_ofs.Text = ofs[0].ToString("F3");
            //}));
            //tb_ws_ofs_before.BeginInvoke(new Action(() =>
            //{
            //    tb_ws_ofs_before.Text = ofs[2].ToString("F3");
            //}));
            //tb_ws_ofs_after.BeginInvoke(new Action(() =>
            //{
            //    tb_ws_ofs_after.Text = ofs[3].ToString("F3");
            //}));
            tb_xt1_ofs.Text = ofs[1].ToString("F3");
            tb_xt2_ofs.Text = ofs[0].ToString("F3");
            tb_ws_ofs_before.Text = ofs[2].ToString("F3");
            tb_ws_ofs_after.Text = ofs[3].ToString("F3");
            VAR.msg.AddMsg(Msg.EM_MSGTYPE.NOR, string.Format("飞拍Y偏差,XT1:{0:f3},XT2:{1:f3},NEAR:{1:f3},FAR:{1:f3}", ofs[1], ofs[0], ofs[2], ofs[3]));
            // Utility.WriteStrToCSV(VAR.gsys_set.GetCurProductPath + "//CsvData//ofsdata.csv", string.Format("{0},{1:f3},{2:f3},{3:f3},{4:f3}", DateTime.Now.ToString("hh:mm:ss:fff"), ofs[1], ofs[0], ofs[2], ofs[3]));
            
            return EM_RES.OK;
        }

        private void btn_fly_action_Click(object sender, EventArgs e)
        {
            EM_RES ret;
            DialogResult result = MessageBox.Show(string.Format("确认要飞拍?"), "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.No) return;
            VAR.gsys_set.bquit = false;
            if (!COM.wsfeed1.isInFeedPos || !COM.wsfeed2.isInFeedPos)
            {
                MessageBox.Show("模块一或者测试模块二未在上料位置!", "危险", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!COM.wsfeed1.IsAllcylOpen() || !COM.wsfeed2.IsAllcylOpen())
            {
                MessageBox.Show("测试模块一或者测试模块二的气缸未打开!", "危险", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            ret =XtFlyAction(ref mcurUDLoad);
            if (ret == EM_RES.OK)
            {
                MessageBox.Show("吸头飞拍校正成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Question);
            }
            else
            {
                MessageBox.Show("吸头飞拍校正失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            ////graphics_other.Clear();
           
        }

        private void btn_rot_downcam_movpos_Click(object sender, EventArgs e)
        {
            EM_RES ret = EM_RES.OK;
            VAR.gsys_set.bquit = false;
            MT.SetAllAxToManualSpd();
            //if (Math.Abs(UploadModle.ax_z.fenc_pos) > 0.5)
            //{
            //    ret = UploadModle.ZHome(ref VAR.gsys_set.bquit);
            //    if (ret != EM_RES.OK) return;
            //}
            if (ret != EM_RES.OK) return;
            ret = MT.ZupMove(ref VAR.gsys_set.bquit, ref mcurXt.ax_y, (double)nud_rot_downcam_y.Value,ref  mcurXt.ax_u ,0,true);
            if (ret != EM_RES.OK) return;
           
        }

        private void btn_zk_ctrl_Click(object sender, EventArgs e)
        {
            if (mcurXt.cy_zk.isOFF)
                mcurXt.cy_zk.SetOn();
            else
                mcurXt.cy_zk.SetOff();
        }

        private void btn_cam_Click(object sender, EventArgs e)
        {
            //EM_RES res;
            //VAR.gsys_set.bquit = false;
            //for (int i = 0; i < 50 && !VAR.gsys_set.bquit; i++)
            //{
            //    res = UploadModle.test_FLyCapToTray(ref VAR.gsys_set.bquit);
            //    if (res != EM_RES.OK) return;
            //    Application.DoEvents();
            //}
                
           
          
        }

        private void btn_rot_downcam_getpos_Click(object sender, EventArgs e)
        {
            nud_rot_downcam_y.Value = (decimal)mcurXt.ax_y.fenc_pos;
        }

      
    }
}
