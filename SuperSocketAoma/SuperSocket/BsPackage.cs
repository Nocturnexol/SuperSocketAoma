using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using SuperSocket.Common;
using SuperSocketAoma.Common;
using SuperSocketAoma.Db;
using SuperSocketAoma.Model;
//using log4net;
using ThreadState = System.Threading.ThreadState;

namespace SuperSocketAoma.SuperSocket
{
    public static class BsPackage
    {
        private static readonly byte Mark = Convert.ToByte(ConfigurationManager.AppSettings["Mark"], 16);
        private static readonly Encoding Encoding = Encoding.GetEncoding("GBK");
        public static readonly ConcurrentQueue<List<byte>> PacketQueue = new ConcurrentQueue<List<byte>>();
        public static readonly ConcurrentQueue<Exception> ErrorQueue = new ConcurrentQueue<Exception>();
        private static bool _isRunning;
        public static AutoResetEvent AutoResetEvent = new AutoResetEvent(false);
        public static readonly int BorderCount = int.Parse(ConfigurationManager.AppSettings["BorderCount"]);
        //private static readonly ILog Logger = LogManager.GetLogger(typeof(BsPackage));

        public static void SeparatePacket(this IList<byte> source, BsProtocolSession session)
        {
            if (!source.Any()) return;
            if (source.Count > 1 && source[0] == Mark && source[1] != Mark)
            {
                var packet = SeparateSinglePacket(source);
                if (packet.LastOrDefault() == Mark)
                {
                    PacketQueue.Enqueue(packet);
                    if (PacketQueue.Count > BorderCount)
                        AutoResetEvent.Set();
                    SeparatePacket(source.Skip(packet.Count).ToList(), session);
                }
                else
                {
                    session.FragBytes.AddRange(source);
                }
            }
            else
            {
                session.FragBytes.AddRange(source);
            }
        }

        public static void StartConsuming()
        {
            _isRunning = true;
            var t = new Thread(() =>
            {
                for (; _isRunning;)
                {
                    AutoResetEvent.WaitOne(5000);
                    List<byte> packet;
                    while (PacketQueue.Count > 0 && PacketQueue.TryDequeue(out packet))
                    {
                        try
                        {
                            var data = packet.Construct();
                            if (data != null)
                            {
                                MainForm.OracleWriter.Enqueue(new AnalysisAlertData
                                {
                                    Guid = Guid.NewGuid(),
                                    TerminalId = data.TerminalId.ByteArrToHexStr(),
                                    MessageId = data.MessageId,
                                    Content = data.SourceHexStr,
                                    DateTime = DateTime.Parse(data.GetDateTimeStr()),
                                    EventType = data.EventType,
                                    Manufacturer = data.Manufacturer,
                                    FileNameLength = data.FileNameLength,
                                    FileName = data.FileName,
                                    //SaveTime = DateTime.Now
                                });
                            }
                        }
                        catch (Exception e)
                        {
                            //LogManager.Error(packet.ByteArrToHexStr(), e);
                            ErrorQueue.Enqueue(e);
                        }
                    }
                    if (PacketQueue.Count < 10)
                    {
                        Thread.Sleep(20);
                    }

                }
                Thread.CurrentThread.Abort();
            });
            if (t.ThreadState != ThreadState.Running) t.Start();
        }

        public static void StopConsuming()
        {
            _isRunning = false;
        }

        public static AnalysisAlert Construct(this IList<byte> source)
        {
            var aomaData = new AnalysisAlert();
            try
            {
                //if (string.IsNullOrWhiteSpace(Mark))
                //{
                //    var ex = new Exception("无法获取分包标识位！");
                //    //Logger.Error("无法获取分包标识位！", ex);
                //    LogManager.Error("无法获取分包标识位！", ex);
                //    throw ex;
                //}
                //var packetList = new List<byte[]>();
                //分包
                if (source[0] != Mark)
                {
                    var ex = new Exception("数据包格式错误！");
                    //Logger.Error("数据包格式错误！", ex);
                    //LogManager.Error("数据包格式错误！", ex);
                    ErrorQueue.Enqueue(ex);
                    throw ex;
                }
                //List<byte> packet;
                //while ((source = SeparateSinglePacket(source, out packet)).Count >= 0 && packet.Count > 0)
                //{
                //    packetList.Add(packet.ToArray());
                //}

                //foreach (var p in packetList)
                //{
                //反转义
                var pack = source.Unescape().ToArray();

                aomaData.SourceHexStr = source.ByteArrToHexStr();

                aomaData.MessageId = Convert.ToUInt16(pack.CloneRange(1, 2).ByteArrToHexStr(), 16);
                //aomaData.MessageId=BitConverter.ToUInt16(pack.CloneRange(0, 2),0);
                var properties = Convert.ToInt16(pack.CloneRange(3, 2).ByteArrToHexStr(), 16);
                var propertiesStr = Convert.ToString(properties, 2).PadLeft(16, '0');
                aomaData.IsMultiPacket = (properties & (int) Math.Pow(2, 13)) == properties;
                aomaData.IsEncrypted = (properties & (int) Math.Pow(2, 10)) == properties;
                aomaData.MessageLength = Convert.ToUInt16(propertiesStr.Substring(6), 2);
                aomaData.TerminalId = pack.CloneRange(6, 7);
                aomaData.SerialNum = Convert.ToUInt16(pack.CloneRange(13, 2).ByteArrToHexStr(), 16);

                var startIndex = 15;
                if (aomaData.IsMultiPacket)
                {
                    aomaData.TotalPack = Convert.ToUInt16(pack.CloneRange(15, 2).ByteArrToHexStr(), 16);
                    aomaData.PackNum = Convert.ToUInt16(pack.CloneRange(17, 2).ByteArrToHexStr(), 16);
                    startIndex = 19;
                }

                aomaData.DateTime = pack.CloneRange(startIndex, 6);
                aomaData.EventType = (EventType) pack[startIndex + 6];
                aomaData.Manufacturer = (Manufacturer) pack[startIndex + 7];
                aomaData.FileNameLength = pack[startIndex + 8];
                aomaData.FileName =
                    Encoding.GetString(pack.CloneRange(startIndex + 9, pack.Length - 3 - (startIndex + 9) + 1));
                aomaData.CheckCode = pack[pack.Length - 2];

                //}


            }
            catch (Exception e)
            {
                //LogManager.Error(e.Message, e);
                ErrorQueue.Enqueue(e);
                throw;
                //return null;
            }

            return aomaData;
        }

        public static List<byte> SeparateSinglePacket(IList<byte> source)
        {
            var packet = new List<byte>();
            //分包Flag，检测到标识位时加一，偶数时即分出一包。
            var flag = 0;
            foreach (var b in source)
            {
                packet.Add(b);
                if (b == Mark)
                {
                    flag++;
                    if (flag % 2 == 0)
                    {
                        //分出一包
                        break;
                    }
                }
            }
            return packet;
        }

        public static string Parse(this AnalysisAlert src)
        {
            var res = new List<byte>();
            try
            {
                #region 数据头

                res.AddRange(src.MessageId.UshortToBytesBig());
                var prop =
                    $"00{(src.IsMultiPacket ? 1 : 0)}00{(src.IsEncrypted ? 1 : 0)}{Convert.ToString(src.MessageLength, 2).PadLeft(9, '0')}";
                res.AddRange(Convert.ToUInt16(prop, 2).UshortToBytesBig());
                res.Add(src.Version);
                res.AddRange(src.TerminalId);
                res.AddRange(src.SerialNum.UshortToBytesBig());
                if (src.IsMultiPacket)
                {
                    res.AddRange(src.TotalPack.UshortToBytesBig());
                    res.AddRange(src.PackNum.UshortToBytesBig());
                }

                #endregion

                #region 数据体

                res.AddRange(src.DateTime);
                res.Add((byte) src.EventType);
                res.Add((byte) src.Manufacturer);
                res.Add(src.FileNameLength);
                res.AddRange(Encoding.GetBytes(src.FileName));

                #endregion

                //校验码
                res.Add(res.Aggregate(res[0], (current, b) => (byte) (current ^ b)));

                //转义 
                res = res.Escape().ToList();

                //添加头尾标识
                res.Insert(0, Mark);
                res.Add(Mark);

            }
            catch (Exception e)
            {
                //LogManager.Error(e.Message, e);
                ErrorQueue.Enqueue(e);
                throw;
            }
            return res.ToArray().ByteArrToHexStr();
        }

        /// <summary>
        /// 转义
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static IEnumerable<byte> Escape(this IEnumerable<byte> src)
        {
            return
                src.ToArray()
                    .ByteArrToHexStr()
                    .Replace("7D", "7D01")
                    .Replace("7E", "7D02")
                    .HexStrToByteArr();
        }

        /// <summary>
        /// 反转义
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static IEnumerable<byte> Unescape(this IEnumerable<byte> src)
        {
            return
                src.ToArray()
                    .ByteArrToHexStr()
                    .Replace("7D02", "7E")
                    .Replace("7D01", "7D")
                    .HexStrToByteArr();
        }

    }
}
