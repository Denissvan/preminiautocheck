using ControlzEx.Standard;
using MotionCtrl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static UI.Class.Grpc;

namespace UI.Class
{
    public class Grpc
    {
        [DllImport("CommunicationUtils.dll", EntryPoint = "?CommunicationUtils_InitDll@@YAXXZ", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CommunicationUtils_InitDll(); //DllImport请参照MSDN

        [DllImport("CommunicationUtils.dll", EntryPoint = "?CommunicationUtils_DeinitDll@@YAXXZ", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CommunicationUtils_DeinitDll(); //DllImport请参照MSDN

        public delegate int MegaData_Get_Callback(string strPeer, string data, int iDataSize, IntPtr handle, IntPtr CustomContext);
        [DllImport("CommunicationUtils.dll", EntryPoint = "?CommunicationUtils_MegaData_Server_StartServer@@YAPEAHHP6AHPEBD0HPEAHPEAX@Z@Z", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CommunicationUtils_MegaData_Server_StartServer(int iPort, MegaData_Get_Callback megaData_Get_CallBack); //DllImport请参照MSDN

        [DllImport("CommunicationUtils.dll", EntryPoint = "?CommunicationUtils_MegaData_SetMegaDataProxyHandleData@@YAHPEAHPEAEH@Z", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CommunicationUtils_MegaData_SetMegaDataProxyHandleData(IntPtr handle, string data, int iDataSize); //DllImport请参照MSDN

        [DllImport("CommunicationUtils.dll", EntryPoint = "?CommunicationUtils_MegaData_Server_StopServer@@YA_NPEAH@Z", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CommunicationUtils_MegaData_Server_StopServer(IntPtr serverHandle); //DllImport请参照MSDN

        public  IntPtr serverHandle;
        private static readonly Object LockObj = new object();
        public Grpc()
        {

        }

        public  int MegaData_Get_Callback_Impl(string strPeer, string data, int iDataSize, IntPtr handle, IntPtr CustomContext)
        {
            
            lock (LockObj)
            {
                string strCom = data;
                if (strCom.Contains("GetSNAndTainData1"))
                {
                    string strBackData;
                    strBackData = "{\"SN\":\""+ "COM6" + "\"}";
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("左测试发送测试软件数据为：{0}!", strBackData));
                    CommunicationUtils_MegaData_SetMegaDataProxyHandleData(handle, strBackData, strBackData.Length);
                }
                else if (strCom.Contains("GetSNAndTainData2"))
                {
                    string strBackData;
                    strBackData = "{\"SN\":\"" + "COM7" + "\"}";
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("右测试发送测试软件数据为：{0}!", strBackData));
                    CommunicationUtils_MegaData_SetMegaDataProxyHandleData(handle, strBackData, strBackData.Length);
                }
                else
                {
                    CommunicationUtils_MegaData_SetMegaDataProxyHandleData(handle, "", 0);
                }
                return 0;
            }
               
        }
        MegaData_Get_Callback megaData_Get_Callback;
        public  void Startsevice()
        {
            CommunicationUtils_InitDll();  //第一步：初始化
            megaData_Get_Callback = MegaData_Get_Callback_Impl;
            serverHandle = CommunicationUtils_MegaData_Server_StartServer(51230, megaData_Get_Callback);    //第二步：开启服务器，端口可自行调整
        }
        public  void Stopsevice()
        {
            CommunicationUtils_MegaData_Server_StopServer(serverHandle); //第三步：关闭服务器，软件关闭时调用
            CommunicationUtils_DeinitDll();//第四步：销毁
        }
    }
}
