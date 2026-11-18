using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Win32Lib
{
    public class SecsManager
    {
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private ConcurrentQueue<SecsInfoMessage> SendQueue { get; } = new ConcurrentQueue<SecsInfoMessage>();
        public event Action<SecsInfoMessage> ReceiveMsg;
        private bool _isStarted = false;

        public bool IsStarted => _isStarted;

        public Task Start()
        {
            if (_isStarted) return null;
            //开始
            _stopEvent.Reset();
            var task = Task.Factory.StartNew(() =>
            {
                _isStarted = true;
                while (!this._stopEvent.WaitOne(10))
                {
                    try
                    {
                        SecsInfoMessage msg = null;
                        while (SendQueue.TryDequeue(out msg))
                        {
                            Win32Api.SendToSecsApp(JsonConvert.SerializeObject(msg));
                            Task.Delay(10);
                        }
                    }
                    catch
                    {
                        GC.Collect();
                    }
                }
                _isStarted = false;
            }, TaskCreationOptions.LongRunning);
            return task;
        }

        public void Stop()
        {
            _stopEvent.Set();
            SpinWait.SpinUntil(() => !_isStarted);
            ClearSendQueue();
            GC.Collect();
        }
        public void Send(BaseInfo info,int TypeId = 0)
        {
            var msgs = new SecsInfoMessage(){TypeId = TypeId};
            msgs.Infos.Add(info);
            if (!_isStarted)
            {
                Win32Api.SendToSecsApp(JsonConvert.SerializeObject(msgs));
            }
            else
            {
                SendQueue.Enqueue(msgs);
            }
        }
        public void SendRange(List<BaseInfo> infos, int TypeId = 0)
        {
            var msgs = new SecsInfoMessage() { TypeId = TypeId };
            msgs.Infos.AddRange(infos);
            if (!_isStarted)
            {
                Win32Api.SendToSecsApp(JsonConvert.SerializeObject(msgs));
            }
            else
            {
                SendQueue.Enqueue(msgs);
            }
        }

        private void ClearSendQueue()
        {
            SecsInfoMessage item = null;
            while (SendQueue.TryDequeue(out item))
            {
                // do nothing
            }
        }

        public void DealMessage(Message msg)
        {
            if(msg.Msg== Win32Api.WM_COPYDATA) 
            {
                CopyDataStruct cds = (CopyDataStruct)Marshal.PtrToStructure(msg.LParam, typeof(CopyDataStruct)); // 接收封装的消息
                string txt = cds.LpData;
                try
                {
                    var devMsg = JsonConvert.DeserializeObject<SecsInfoMessage>(txt); 
                    ReceiveMsg?.BeginInvoke(devMsg, null, null);
                }
                catch(Exception ex)
                {
                    // ignored
                }
            }
        }
    }

    public class BaseInfo
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }

    public class SecsInfoMessage
    {
        public int TypeId { get; set; }
        public List<BaseInfo> Infos { get; set; } = new List<BaseInfo>();
    }


}
