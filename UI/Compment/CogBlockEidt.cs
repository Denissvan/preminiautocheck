using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace UI
{
    public partial class CogBlockEidt : UserControl
    {
        Cam mCam;
        public Cam Camera
        {
            get
            {
                return mCam;
            }
            set
            {
                mCam = value;
                if (mCam.curTask != null) VsTask = mCam.curTask;
            }
        }
        Cam.VisionTask mVsTask;
        public Cam.VisionTask VsTask
        {
            get
            {
                return mVsTask;
            }
            set
            {
                mVsTask =value;
                if(mVsTask!=null && mVsTask.Block !=null )Editor.Subject = mVsTask.Block;
            }
        }
        public CogBlockEidt()
        {
            InitializeComponent();
        }

        private void btn_triger_Click(object sender, EventArgs e)
        {
            if (mCam != null)
                mCam.Triger();
        }

        private void btn_save_Click(object sender, EventArgs e)
        {            
            if (mVsTask != null)
            {
                if (DialogResult.Yes != MessageBox.Show("确定要保存?", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question)) return;
                mVsTask.Save();
                Thread.Sleep(500);
                mVsTask.Load();
            }
        }

        private void btn_load_Click(object sender, EventArgs e)
        {            
            if (mVsTask != null)
            {
                if (DialogResult.Yes != MessageBox.Show("确定要重新加载?", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question)) return;
                mVsTask.Load();
            }
        }
    }
}
