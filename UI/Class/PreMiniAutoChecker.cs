using MotionCtrl;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace UI
{
    public class PreMiniAutoChecker
    {
        public sealed class AutoCheckReading
        {
            public double Distance { get; set; }
            public double Lux { get; set; }
            public double Cct { get; set; }
        }

        public sealed class LightChannelReading
        {
            public int Channel { get; set; }
            public double Lux { get; set; }
            public double Cct { get; set; }
        }

        private class SensorReader : IDisposable
        {
            private readonly byte[] header = new byte[] { 0x55, 0xAA, 0xAA, 0x55 };
            private readonly SerialPort port;
            private readonly object locker = new object();
            private readonly string traceOperation;
            private double distance = -1;
            private double lux = -1;
            private double cct = -1;
            private List<LightChannelReading> lightChannels = new List<LightChannelReading>();
            private long lightFrameSequence;
            private readonly List<byte> receiveBuffer = new List<byte>();
            private int commandCount;
            private int receiveCount;

            public SensorReader(string portName, string traceOperation = null, int baudRate = 115200)
            {
                if (string.IsNullOrEmpty(portName)) throw new ArgumentNullException("portName");

                this.traceOperation = traceOperation;
                port = new SerialPort(portName, baudRate);
                port.DataReceived += Port_DataReceived;
                port.Open();
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini点检串口打开成功,COM:{0},Baud:{1}", portName, baudRate));
                Trace(string.Format("开始,COM:{0},Baud:{1}", portName, baudRate));
            }

            public void Dispose()
            {
                if (port != null)
                {
                    port.DataReceived -= Port_DataReceived;
                    if (port.IsOpen) port.Close();
                    port.Dispose();
                }
            }

            public double ReadDistance(int timeoutMs)
            {
                distance = -1;
                int tick = Environment.TickCount;
                while (Environment.TickCount - tick < timeoutMs)
                {
                    SendCommand(0x8001);
                    Thread.Sleep(200);
                    lock (locker)
                    {
                        if (distance > 0) return distance;
                    }
                }
                Trace(string.Format("距离读取超时,Timeout:{0}ms,{1}", timeoutMs, TraceSummary()));
                throw new TimeoutException(string.Format("读取距离超时,COM:{0},Timeout:{1}ms", port.PortName, timeoutMs));
            }

            public Tuple<double, double> ReadLuxCct(int timeoutMs)
            {
                return ReadLuxCct(1, timeoutMs);
            }

            public Tuple<double, double> ReadLuxCct(int channel, int timeoutMs)
            {
                if (channel < 1 || channel > 3) throw new ArgumentOutOfRangeException("channel");
                List<LightChannelReading> channels = ReadLightChannels(timeoutMs);
                LightChannelReading reading = channels.Find(item => item.Channel == channel);
                if (reading == null) throw new TimeoutException(string.Format("读取Lux/CCT通道缺失,CH:{0},COM:{1}", channel, port.PortName));
                return Tuple.Create(reading.Lux, reading.Cct);
            }

            public List<LightChannelReading> ReadLightChannels(int timeoutMs)
            {
                long startSequence;
                lock (locker)
                {
                    startSequence = lightFrameSequence;
                }
                int tick = Environment.TickCount;
                while (Environment.TickCount - tick < timeoutMs)
                {
                    SendCommand(0x7002);
                    Thread.Sleep(500);
                    lock (locker)
                    {
                        if (lightFrameSequence > startSequence && lightChannels.Count == 3) return CopyLightChannels(lightChannels);
                    }
                }
                Trace(string.Format("三通道Lux/CCT读取超时,Timeout:{0}ms,{1}", timeoutMs, TraceSummary()));
                throw new TimeoutException(string.Format("读取三通道Lux/CCT超时,COM:{0},Timeout:{1}ms", port.PortName, timeoutMs));
            }

            public double ReadLux(int timeoutMs)
            {
                lux = -1;
                int tick = Environment.TickCount;
                while (Environment.TickCount - tick < timeoutMs)
                {
                    SendCommand(0x7002);
                    Thread.Sleep(500);
                    lock (locker)
                    {
                        if (lux > 0) return lux;
                    }
                }
                Trace(string.Format("Lux读取超时,Timeout:{0}ms,{1}", timeoutMs, TraceSummary()));
                throw new TimeoutException(string.Format("读取Lux超时,COM:{0},Timeout:{1}ms", port.PortName, timeoutMs));
            }

            public double ReadCct(int timeoutMs)
            {
                cct = -1;
                int tick = Environment.TickCount;
                while (Environment.TickCount - tick < timeoutMs)
                {
                    SendCommand(0x7002);
                    Thread.Sleep(500);
                    lock (locker)
                    {
                        if (cct > 0) return cct;
                    }
                }
                Trace(string.Format("CCT读取超时,Timeout:{0}ms,{1}", timeoutMs, TraceSummary()));
                throw new TimeoutException(string.Format("读取CCT超时,COM:{0},Timeout:{1}ms", port.PortName, timeoutMs));
            }

            public void SetLaserStatus(bool on)
            {
                SendCommand(0x8003, string.Format("{{\"laser\": {0} }}", on ? 1 : 0));
            }

            private void SendCommand(ushort cmd, string jsonPayload = null)
            {
                byte[] payload = string.IsNullOrEmpty(jsonPayload) ? new byte[0] : Encoding.ASCII.GetBytes(jsonPayload);
                byte[] packet = new byte[header.Length + 4 + payload.Length];
                Buffer.BlockCopy(header, 0, packet, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(cmd), 0, packet, 4, 2);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)payload.Length), 0, packet, 6, 2);
                if (payload.Length > 0) Buffer.BlockCopy(payload, 0, packet, 8, payload.Length);
                port.Write(packet, 0, packet.Length);
                commandCount++;
                Trace(string.Format("发送命令,Cmd:0x{0:X4},Payload:{1},Count:{2}", cmd, string.IsNullOrEmpty(jsonPayload) ? "<empty>" : jsonPayload, commandCount));
            }

            private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
            {
                try
                {
                    Thread.Sleep(50);
                    int len = port.BytesToRead;
                    byte[] buffer = new byte[len];
                    port.Read(buffer, 0, len);
                    receiveCount++;
                    Trace(string.Format("收到串口数据,Bytes:{0},Hex:{1},Count:{2}", len, BitConverter.ToString(buffer), receiveCount));

                    List<string> jsonFrames = new List<string>();
                    string reason = string.Empty;
                    lock (locker)
                    {
                        receiveBuffer.AddRange(buffer);
                        while (true)
                        {
                            string json = ExtractJsonFromBuffer(out reason);
                            if (string.IsNullOrEmpty(json)) break;
                            jsonFrames.Add(json);
                        }
                    }

                    if (jsonFrames.Count == 0)
                    {
                        Trace(string.Format("串口数据未解析为完整帧,Reason:{0}", reason));
                        return;
                    }

                    foreach (string json in jsonFrames)
                    {
                        JObject data = JObject.Parse(json);
                        Trace(string.Format("串口JSON解析成功,Data:{0}", json));
                        lock (locker)
                        {
                            JToken token;
                            if (data.TryGetValue("distance", StringComparison.OrdinalIgnoreCase, out token))
                            {
                                distance = Math.Abs(token.Value<double>());
                            }
                            if (data.TryGetValue("lux", StringComparison.OrdinalIgnoreCase, out token))
                            {
                                lux = Math.Floor(Math.Abs(token.Value<double>()));
                            }
                            if (data.TryGetValue("cct", StringComparison.OrdinalIgnoreCase, out token))
                            {
                                cct = Math.Floor(Math.Abs(token.Value<double>()));
                            }

                            List<LightChannelReading> parsedChannels = ParseLightChannels(data);
                            if (parsedChannels.Count == 3)
                            {
                                lightChannels = parsedChannels;
                                lightFrameSequence++;
                                if (!data.TryGetValue("lux", StringComparison.OrdinalIgnoreCase, out token)) lux = parsedChannels[0].Lux;
                                if (!data.TryGetValue("cct", StringComparison.OrdinalIgnoreCase, out token)) cct = parsedChannels[0].Cct;
                                Trace(string.Format("三通道读数,CH1 Lux:{0:F3},CCT:{1:F3};CH2 Lux:{2:F3},CCT:{3:F3};CH3 Lux:{4:F3},CCT:{5:F3}",
                                    parsedChannels[0].Lux, parsedChannels[0].Cct, parsedChannels[1].Lux, parsedChannels[1].Cct, parsedChannels[2].Lux, parsedChannels[2].Cct));
                            }
                            else if (parsedChannels.Count > 0)
                            {
                                Trace(string.Format("光源色温返回通道数不足,Expected:3,Actual:{0}", parsedChannels.Count));
                            }
                            Trace(string.Format("解析后读数,Distance:{0:F3},Lux:{1:F3},CCT:{2:F3}", distance, lux, cct));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace(string.Format("串口解析异常,Err:{0}", ex.Message));
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.WAR, string.Format("pre-mini点检串口解析失败:{0}", ex.Message));
                }
            }

            private string ExtractJsonFromBuffer(out string reason)
            {
                reason = string.Empty;
                int headerIndex = FindHeader();
                if (headerIndex < 0)
                {
                    if (receiveBuffer.Count > header.Length - 1) receiveBuffer.RemoveRange(0, receiveBuffer.Count - (header.Length - 1));
                    reason = "未找到协议帧头55-AA-AA-55";
                    return null;
                }
                if (headerIndex > 0) receiveBuffer.RemoveRange(0, headerIndex);
                if (receiveBuffer.Count < 8)
                {
                    reason = string.Format("协议帧不完整,Actual:{0}", receiveBuffer.Count);
                    return null;
                }

                ushort len = BitConverter.ToUInt16(receiveBuffer.ToArray(), 6);
                if (receiveBuffer.Count < 8 + len)
                {
                    reason = string.Format("协议帧不完整,PayloadLength:{0},Actual:{1}", len, receiveBuffer.Count);
                    return null;
                }

                byte[] payload = receiveBuffer.GetRange(8, len).ToArray();
                receiveBuffer.RemoveRange(0, 8 + len);
                return Encoding.ASCII.GetString(payload);
            }

            private int FindHeader()
            {
                for (int i = 0; i <= receiveBuffer.Count - header.Length; i++)
                {
                    if (receiveBuffer[i] == header[0] && receiveBuffer[i + 1] == header[1] && receiveBuffer[i + 2] == header[2] && receiveBuffer[i + 3] == header[3]) return i;
                }
                return -1;
            }

            private static List<LightChannelReading> ParseLightChannels(JObject data)
            {
                List<LightChannelReading> result = new List<LightChannelReading>();
                for (int channel = 1; channel <= 3; channel++)
                {
                    JToken luxToken;
                    JToken cctToken;
                    if (data.TryGetValue("lux" + channel, StringComparison.OrdinalIgnoreCase, out luxToken) &&
                        data.TryGetValue("cct" + channel, StringComparison.OrdinalIgnoreCase, out cctToken))
                    {
                        result.Add(new LightChannelReading
                        {
                            Channel = channel,
                            Lux = Math.Floor(Math.Abs(luxToken.Value<double>())),
                            Cct = Math.Floor(Math.Abs(cctToken.Value<double>()))
                        });
                    }
                }
                return result;
            }

            private static List<LightChannelReading> CopyLightChannels(List<LightChannelReading> source)
            {
                List<LightChannelReading> copy = new List<LightChannelReading>();
                foreach (LightChannelReading item in source)
                {
                    copy.Add(new LightChannelReading { Channel = item.Channel, Lux = item.Lux, Cct = item.Cct });
                }
                return copy;
            }

            private void Trace(string message)
            {
                if (string.IsNullOrEmpty(traceOperation)) return;
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动{0},{1}", traceOperation, message));
            }

            private string TraceSummary()
            {
                return string.Format("SendCount:{0},ReceiveCount:{1}", commandCount, receiveCount);
            }
        }

        public static EM_RES RunPointCheck(WSFEED station, WSFEED.PosDef pos)
        {
            if (station == null || pos == null) return EM_RES.PARA_ERR;

            try
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} pre-mini自动点检开始:{1}", station.disc, pos.Name));
                bool runDistance = NeedsDistanceCheck(station);
                bool runLuxCct = NeedsLuxCctCheck(station, pos);
                if (!runDistance && !runLuxCct)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} {1}距离和光源色温点检均未启用,跳过本测点", station.disc, pos.Name));
                    return EM_RES.OK;
                }

                string com = station.id == 0 ? PT_SET.PreMiniWs1AutoCheckCom : PT_SET.PreMiniWs2AutoCheckCom;
                if (string.IsNullOrWhiteSpace(com))
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} {1}综合点检失败:未设置COM", station.disc, pos.Name));
                    return EM_RES.PARA_ERR;
                }

                using (SensorReader reader = new SensorReader(com))
                {
                    if (runDistance)
                    {
                        while (CheckDistance(reader, station, pos, com) != EM_RES.OK)
                        {
                            if (!ShowRetryOrStop(station, pos, "距离")) return EM_RES.QUIT;
                        }
                    }

                    if (runLuxCct)
                    {
                        while (CheckLuxCct(reader, station, pos, com) != EM_RES.OK)
                        {
                            if (!ShowRetryOrStop(station, pos, "光源色温")) return EM_RES.QUIT;
                        }
                    }
                    else
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} {1}光源色温点检未启用或非色温测点,仅执行距离点检", station.disc, pos.Name));
                    }
                }

                SaveToTxt(station.disc, pos.Name, "OK");
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} pre-mini自动点检完成:{1}", station.disc, pos.Name));
                return EM_RES.OK;
            }
            catch (Exception ex)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} pre-mini自动点检异常:{1},ID:{2},{3}", station.disc, pos.Name, pos.ID, ex.Message));
                return EM_RES.ERR;
            }
        }

        public static AutoCheckReading ManualReadAll(string com)
        {
            if (string.IsNullOrWhiteSpace(com)) throw new ArgumentException("未设置综合点检COM");

            using (SensorReader reader = new SensorReader(com))
            {
                try
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动读取综合点检开始,COM:{0}", com));
                    reader.SetLaserStatus(true);
                    Thread.Sleep(1000);
                    double distance = reader.ReadDistance(30000);
                    Tuple<double, double> light = reader.ReadLuxCct(20000);
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动读取综合点检完成,COM:{0},Distance:{1:F3},Lux:{2:F3},CCT:{3:F3}", com, distance, light.Item1, light.Item2));
                    return new AutoCheckReading { Distance = distance, Lux = light.Item1, Cct = light.Item2 };
                }
                finally
                {
                    reader.SetLaserStatus(false);
                }
            }
        }

        public static double ManualReadDistance(string com)
        {
            if (string.IsNullOrWhiteSpace(com)) throw new ArgumentException("未设置综合点检COM");

            using (SensorReader reader = new SensorReader(com, "ManualDistance"))
            {
                try
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量距离开始,COM:{0}", com));
                    reader.SetLaserStatus(true);
                    Thread.Sleep(1000);
                    double distance = reader.ReadDistance(30000);
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量距离完成,COM:{0},Distance:{1:F3}", com, distance));
                    return distance;
                }
                finally
                {
                    reader.SetLaserStatus(false);
                }
            }
        }

        public static Tuple<double, double> ManualReadLuxCct(string com)
        {
            if (string.IsNullOrWhiteSpace(com)) throw new ArgumentException("未设置综合点检COM");

            using (SensorReader reader = new SensorReader(com, "ManualLuxCct"))
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量照度/色温开始,COM:{0}", com));
                Tuple<double, double> value = reader.ReadLuxCct(20000);
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量照度/色温完成,COM:{0},Lux:{1:F3},CCT:{2:F3}", com, value.Item1, value.Item2));
                return value;
            }
        }

        public static List<LightChannelReading> ManualReadLightChannels(string com)
        {
            if (string.IsNullOrWhiteSpace(com)) throw new ArgumentException("未设置综合点检COM");

            using (SensorReader reader = new SensorReader(com, "ManualLightChannels"))
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量三通道光源色温开始,COM:{0}", com));
                List<LightChannelReading> values = reader.ReadLightChannels(20000);
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量三通道光源色温完成,COM:{0},CH1 Lux:{1:F3},CCT:{2:F3};CH2 Lux:{3:F3},CCT:{4:F3};CH3 Lux:{5:F3},CCT:{6:F3}",
                    com, values[0].Lux, values[0].Cct, values[1].Lux, values[1].Cct, values[2].Lux, values[2].Cct));
                return values;
            }
        }

        public static double ManualReadLux(string com)
        {
            if (string.IsNullOrWhiteSpace(com)) throw new ArgumentException("未设置综合点检COM");

            using (SensorReader reader = new SensorReader(com, "ManualLux"))
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量照度开始,COM:{0}", com));
                double lux = reader.ReadLux(20000);
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量照度完成,COM:{0},Lux:{1:F3}", com, lux));
                return lux;
            }
        }

        public static double ManualReadCct(string com)
        {
            if (string.IsNullOrWhiteSpace(com)) throw new ArgumentException("未设置综合点检COM");

            using (SensorReader reader = new SensorReader(com, "ManualCct"))
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量色温开始,COM:{0}", com));
                double cct = reader.ReadCct(20000);
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("pre-mini手动测量色温完成,COM:{0},CCT:{1:F3}", com, cct));
                return cct;
            }
        }

        private static bool ShowRetryOrStop(WSFEED station, WSFEED.PosDef pos, string stage)
        {
            DialogResult result = MessageBox.Show(
                string.Format("{0} {1}{2}点检失败。\r\n是：重新测量\r\n否：停止测试", station.disc, pos.Name, stage),
                "pre-mini自动点检",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            return result == DialogResult.Yes;
        }

        private static EM_RES CheckDistance(SensorReader reader, WSFEED station, WSFEED.PosDef pos, string com)
        {
            try
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} {1}距离点检开始,COM:{2},标准:{3:F3},阈值:{4:F3},补偿:{5:F3}", station.disc, pos.Name, com, pos.ActualDistanceParam, pos.DistanceThreshold, pos.DistanceComp));
                reader.SetLaserStatus(true);
                Thread.Sleep(1000);
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    reader.ReadDistance(30000);
                    Thread.Sleep(1000);
                    double value = reader.ReadDistance(60000);
                    double checkValue = value + pos.DistanceComp;
                    SaveToTxt(station.disc, pos.Name, "Distance", attempt, com, value.ToString("F3"), checkValue.ToString("F3"));
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} {1}距离点检第{2}次:{3:F3},补偿后:{4:F3}", station.disc, pos.Name, attempt, value, checkValue));
                    if (pos.DistanceThreshold <= 0 || Math.Abs(checkValue - pos.ActualDistanceParam) <= pos.DistanceThreshold) return EM_RES.OK;

                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} {1}距离超限,第{2}次,标准:{3:F3},实测:{4:F3},阈值:{5:F3}", station.disc, pos.Name, attempt, pos.ActualDistanceParam, checkValue, pos.DistanceThreshold));
                }
                return EM_RES.ERR;
            }
            catch (Exception ex)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} {1}距离点检读取失败,COM:{2},Err:{3}", station.disc, pos.Name, com, ex.Message));
                return EM_RES.ERR;
            }
            finally
            {
                try
                {
                    reader.SetLaserStatus(false);
                }
                catch (Exception ex)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.WAR, string.Format("{0} {1}关闭测距激光失败,COM:{2},Err:{3}", station.disc, pos.Name, com, ex.Message));
                }
            }
        }

        private static bool NeedsDistanceCheck(WSFEED station)
        {
            return station != null && (station.id == 0 ? PT_SET.PreMiniWs1DistanceCheckEn : PT_SET.PreMiniWs2DistanceCheckEn);
        }

        private static bool NeedsLuxCctCheck(WSFEED station, WSFEED.PosDef pos)
        {
            if (station == null || pos == null || string.IsNullOrEmpty(pos.Name) || !pos.Name.Contains("色温")) return false;
            return station.id == 0 ? PT_SET.PreMiniWs1LuxCctCheckEn : PT_SET.PreMiniWs2LuxCctCheckEn;
        }

        private static EM_RES CheckLuxCct(SensorReader reader, WSFEED station, WSFEED.PosDef pos, string com)
        {
            double[] zList = station.id == 0
                ? new double[] { PT_SET.PreMiniWs1LightCheckZ1, PT_SET.PreMiniWs1LightCheckZ2, PT_SET.PreMiniWs1LightCheckZ3 }
                : new double[] { PT_SET.PreMiniWs2LightCheckZ1, PT_SET.PreMiniWs2LightCheckZ2, PT_SET.PreMiniWs2LightCheckZ3 };

            if (Math.Abs(zList[0]) < 0.001 && Math.Abs(zList[1]) < 0.001 && Math.Abs(zList[2]) < 0.001)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.WAR, string.Format("{0} {1}Lux/CCT三点Z均为0,使用当前测试位Z1:{2:F3}", station.disc, pos.Name, pos.Z1));
                zList[0] = pos.Z1;
                zList[1] = pos.Z1;
                zList[2] = pos.Z1;
            }

            EM_RES result = EM_RES.OK;
            AXIS axis = station.ax_z_fr;
            try
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} {1}Lux/CCT点检开始,COM:{2},Z1:{3:F3},Z2:{4:F3},Z3:{5:F3},标准Lux:{6:F3},Lux阈值:{7:F3},标准CCT:{8:F3},CCT阈值:{9:F3}",
                    station.disc, pos.Name, com, zList[0], zList[1], zList[2], pos.ActualLuxParam, pos.LuxThreshold, pos.ActualCctParam, pos.CctThreshold));

                for (int moveIndex = 0; moveIndex < zList.Length; moveIndex++)
                {
                    if (axis != null)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} {1}Lux/CCT点检移动前Z{2},Axis:{3},Cur:{4:F3},Target:{5:F3}", station.disc, pos.Name, moveIndex + 1, axis.str_disc, axis.fenc_pos, zList[moveIndex]));
                        EM_RES moveRes = MT.Move(ref VAR.gsys_set.bquit, ref axis, zList[moveIndex]);
                        if (moveRes != EM_RES.OK)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} {1}Lux/CCT点检Z{2}移动失败,目标:{3:F3}", station.disc, pos.Name, moveIndex + 1, zList[moveIndex]));
                            result = moveRes;
                            break;
                        }
                        Thread.Sleep(2000);
                    }

                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} {1}Lux/CCT点检读取开始,Z点:{2},COM:{3},Z:{4:F3},等待本次完整三通道响应", station.disc, pos.Name, moveIndex + 1, com, zList[moveIndex]));
                    List<LightChannelReading> channels = reader.ReadLightChannels(20000);
                    if (channels.Count < 3)
                    {
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} {1}光源色温返回通道数不足,Expected:3,Actual:{2}", station.disc, pos.Name, channels.Count));
                        result = EM_RES.ERR;
                        break;
                    }

                    foreach (LightChannelReading channel in channels)
                    {
                        int pointIndex = moveIndex * 3 + channel.Channel;
                        double pointLux = channel.Lux;
                        double pointCct = channel.Cct;
                        SaveToTxt(station.disc, pos.Name, "LuxCCT", pointIndex, string.Format("{0}-CH{1}", com, channel.Channel), zList[moveIndex].ToString("F3"), pointLux.ToString("F3"), pointCct.ToString("F3"));
                        VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} {1}Lux/CCT点检点{2},Z点:{3},CH:{4},Z:{5:F3},COM:{6},Lux:{7:F3},CCT:{8:F3}", station.disc, pos.Name, pointIndex, moveIndex + 1, channel.Channel, zList[moveIndex], com, pointLux, pointCct));

                        if (pos.LuxThreshold > 0 && Math.Abs(pointLux - pos.ActualLuxParam) > pos.LuxThreshold)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} {1}光照超限,点:{2},CH:{3},COM:{4},标准:{5:F3},实测:{6:F3},阈值:{7:F3}", station.disc, pos.Name, pointIndex, channel.Channel, com, pos.ActualLuxParam, pointLux, pos.LuxThreshold));
                            result = EM_RES.ERR;
                        }
                        if (pos.CctThreshold > 0 && Math.Abs(pointCct - pos.ActualCctParam) > pos.CctThreshold)
                        {
                            VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} {1}色温超限,点:{2},CH:{3},COM:{4},标准:{5:F3},实测:{6:F3},阈值:{7:F3}", station.disc, pos.Name, pointIndex, channel.Channel, com, pos.ActualCctParam, pointCct, pos.CctThreshold));
                            result = EM_RES.ERR;
                        }
                    }

                    if (result != EM_RES.OK) break;
                }
            }
            catch (Exception ex)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, string.Format("{0} {1}Lux/CCT点检读取失败,COM:{2},Err:{3}", station.disc, pos.Name, com, ex.Message));
                result = EM_RES.ERR;
            }
            finally
            {
                if (axis != null)
                {
                    VAR.msg.AddMsg(Msg.EM_MSGTYPE.DBG, string.Format("{0} {1}Lux/CCT点检结束,前Z回测试位Z1:{2:F3}", station.disc, pos.Name, pos.Z1));
                    EM_RES moveRes = MT.Move(ref VAR.gsys_set.bquit, ref axis, pos.Z1);
                    if (result == EM_RES.OK && moveRes != EM_RES.OK) result = moveRes;
                }
            }

            return result;
        }

        private static void SaveToTxt(params object[] values)
        {
            try
            {
                string filePath = string.Format("{0}\\product\\{1}\\PreMiniAutoCheckData.ini", Path.GetFullPath(".."), VAR.gsys_set.cur_product_name);
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

                string content = string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), string.Join(",", values));
                File.AppendAllText(filePath, content + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.WAR, string.Format("pre-mini点检数据保存失败:{0}", ex.Message));
            }
        }
    }
}
