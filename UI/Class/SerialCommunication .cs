using MotionCtrl;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UI;

namespace LINK
{
    public class SerialCommunication
    {

        //SerialCommunication serialComm = new SerialCommunication("COM1", 9600, Parity.None, 8, StopBits.One);
        //serialComm.Open();  

        //serialComm.SendData("Hello, Serial Port!");  

        //// 你可以在这里添加逻辑来处理接收到的数据  

        //serialComm.Close();  


        private SerialPort serialPort;
        private Thread readThread;
        private bool keepReading;
        private AutoResetEvent readWaitHandle = new AutoResetEvent(false);

        // 构造函数，设置串口参数  
        public SerialCommunication(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            serialPort.ErrorReceived += SerialPort_ErrorReceived;
        }

        // 打开串口  
        public void Open()
        {
            if (!serialPort.IsOpen)
            {
                try
                {
                    serialPort.Open();
                    keepReading = true;
                    readThread = new Thread(Read);
                    readThread.IsBackground = true;
                    readThread.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening serial port: " + ex.Message);
                }
            }
        }

        // 关闭串口  
        public void Close()
        {
            if (serialPort.IsOpen)
            {
                keepReading = false;
                readWaitHandle.Set(); // Signal the read thread to exit  
                readThread?.Join(); // Wait for the read thread to exit  
                serialPort.Close();
            }
        }

        // 发送数据  
        public void SendData(byte[] data)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Write(data,0, data.Length);
            }
        }
        public void SendData(string data)
        {
            if (serialPort.IsOpen)
            {
                serialPort.WriteLine(data);
            }
        }

        public byte[] HexStringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public bool SetChannelXSJ(int chset)
        {
            if (PT_SET.bXSjNew)
            {
                byte[] byteArray = { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
                string a = "01";
                string b = "F6";
                switch (chset)
                {
                    case 1:
                        a = "01";
                        b = "F6";
                        break;
                    case 2:
                        a = "02";
                        b = "F5";
                        break;
                    case 3:
                        a = "03";
                        b = "F4";
                        break;
                    default:
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, "切换轩仕佳光源参数错误");
                        return false;
                }

                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, "切换轩仕佳光源通道为" + chset);
                string hexData = "55AA090000" + a + "00" + b;
                byte[] dataToSend = HexStringToByteArray(hexData);
               SendData(dataToSend);
            }
            return true;
        }

        // 读取数据的线程函数  
        private void Read()
        {
            while (keepReading)
            {
                readWaitHandle.WaitOne(); // Wait for signal or timeout  
                try
                {
                    if (serialPort.BytesToRead > 0)
                    {
                        string message = serialPort.ReadExisting();
                        OnDataReceived(message); // Raise an event with the received data  
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading from serial port: " + ex.Message);
                }
            }
        }

        // 数据接收事件处理器  
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            readWaitHandle.Set(); // Signal the read thread to check for data  
        }

        // 错误接收事件处理器  
        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Console.WriteLine("Serial port error: " + e.EventType);
        }

        // 引发数据接收事件  
        protected virtual void OnDataReceived(string data)
        {
            // This method can be overridden by a derived class  
            // to provide custom data handling  
            Console.WriteLine("Data Received: " + data);
        }
    }
}
