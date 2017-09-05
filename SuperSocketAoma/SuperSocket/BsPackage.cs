using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
        //private static readonly byte MarkByte = Convert.ToByte(Mark, 16);
        //public static readonly List<byte> SourceList = new List<byte>();
        public static readonly ConcurrentQueue<List<byte>> PacketQueue = new ConcurrentQueue<List<byte>>();
        private static bool _isRunning;
        //private static readonly ILog Logger = LogManager.GetLogger(typeof(BsPackage));

        public static void SeparatePacket(this IList<byte> source,BsProtocolSession session)
        {
            if(!source.Any()) return;
            if (source[0] == Mark)
            {
                if (source.Count >= 76)
                {
                    PacketQueue.Enqueue(source.Where((t, i) => i <= 76).ToList());
                    SeparatePacket(source.Skip(76+1).ToList(),session);
                }
                else
                {
                    session.FragBytes.AddRange(source);
                }
            }
            else
            {
                var firstIndex = source.IndexOf(Mark);
                if (firstIndex == -1)
                {
                    if (!session.FragBytes.Any())
                    {
                        //废包
                    }
                    else
                    {
                        session.FragBytes.AddRange(source);
                    }
                }
                else if (firstIndex > 0)
                {
                    session.FragBytes.AddRange(source.Where((t, i) => i <= firstIndex));
                    SeparatePacket(source.Skip(firstIndex+1).ToList(), session);
                }
            }
        }
        public static void StartConsuming()
        {
            _isRunning = true;
            var t = new Thread(() =>
            {
                for (; _isRunning;)
                {
                    List<byte> packet;
                    var qCount = PacketQueue.Count;
                    if (qCount > 0 && PacketQueue.TryDequeue(out packet))
                    {
                        try
                        {
                            var list = packet.Construct();
                            if (list != null && list.Any())
                            {
                                foreach (var data in list)
                                {
                                    MainForm.OracleWriter.Enqueue(new AnalysisAlertData
                                    {
                                        MessageId = data.MessageId,
                                        Content = data.SourceHexStr,
                                        DateTime = DateTime.Parse(data.GetDateTimeStr()),
                                        SaveTime = DateTime.Now
                                    });

                                }
                            }
                        }
                        catch (Exception e)
                        {
                            LogManager.Error(packet.ByteArrToHexStr(), e);
                        }
                    }
                    else
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
        public static List<AnalysisAlert> Construct(this IList<byte> source)
        {
            var res = new List<AnalysisAlert>();
            try
            {
                //if (string.IsNullOrWhiteSpace(Mark))
                //{
                //    var ex = new Exception("无法获取分包标识位！");
                //    //Logger.Error("无法获取分包标识位！", ex);
                //    LogManager.Error("无法获取分包标识位！", ex);
                //    throw ex;
                //}
                var packetList = new List<byte[]>();
                //分包
                if (source[0]!=Mark)
                {
                    var ex = new Exception("数据包格式错误！");
                    //Logger.Error("数据包格式错误！", ex);
                    LogManager.Error("数据包格式错误！", ex);
                    throw ex;
                }
                List<byte> packet;
                while ((source = SeparateSinglePacket(source, out packet)).Count >= 0 && packet.Count > 0)
                {
                    packetList.Add(packet.ToArray());
                }

                foreach (var p in packetList)
                {
                    //反转义
                    var pack = p.Unescape().ToArray();

                    var aomaData = new AnalysisAlert();
                    aomaData.SourceHexStr = p.ByteArrToHexStr();

                    aomaData.MessageId = Convert.ToUInt16(pack.CloneRange(0, 2).ByteArrToHexStr(), 16);
                    var properties = Convert.ToInt16(pack.CloneRange(2, 2).ByteArrToHexStr(), 16);
                    var propertiesStr = Convert.ToString(properties, 2).PadLeft('0');
                    aomaData.IsMultiPacket = (properties & (int) Math.Pow(2, 13)) == properties;
                    aomaData.IsEncrypted = (properties & (int)Math.Pow(2, 10)) == properties;
                    aomaData.MessageLength = Convert.ToUInt16(propertiesStr.Substring(6), 2);
                    aomaData.TerminalId = pack.CloneRange(5, 7);
                    aomaData.SerialNum = Convert.ToUInt16(pack.CloneRange(12, 2).ByteArrToHexStr(), 16);
                    if (aomaData.IsMultiPacket)
                    {
                        aomaData.TotalPack = Convert.ToUInt16(pack.CloneRange(14, 2).ByteArrToHexStr(), 16);
                        aomaData.PackNum = Convert.ToUInt16(pack.CloneRange(16, 2).ByteArrToHexStr(), 16);
                    }

                    aomaData.DateTime = pack.CloneRange(18, 6);
                    aomaData.EventType = (EventType) pack[24];
                    aomaData.Manufacturer = (Manufacturer) pack[25];
                    aomaData.FileNameLength = pack[26];
                    aomaData.FileName = Encoding.Default.GetString(pack.CloneRange(27, aomaData.FileNameLength));
                    aomaData.CheckCode = pack[pack.Length - 2];

                    res.Add(aomaData);
                }


            }
            catch (Exception e)
            {
                LogManager.Error(e.Message,e);
                throw;
                //return null;
            }

            return res;
        }

        private static IList<byte> SeparateSinglePacket(IList<byte> source, out List<byte> packet)
        {
            packet = new List<byte>();
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
            source = source.Skip(packet.Count).ToArray();
            //var markIndex = source.IndexOf(mark, 0, source.Length);
            //if (markIndex != 0)
            //{
            //    source = source.Skip(markIndex).ToArray();
            //}
            return source;
        }

        public static string Parse(this AnalysisAlert src)
        {
            var res=new List<byte>();
            try
            {
                #region 数据头
                res.AddRange(src.MessageId.UshortToBytesBig());
                var prop =
                    $"00{(src.IsMultiPacket ? 1 : 0)}00{(src.IsEncrypted ? 1 : 0)}{Convert.ToString(src.MessageLength, 2).PadLeft('0').Substring(6)}";
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
                res.AddRange(Encoding.Default.GetBytes(src.FileName));
                #endregion

                //校验码
                res.Add(res.Aggregate(res[0], (current, b) => (byte) (current ^ b)));

                //转义 
                res =res.Escape().ToList();

                //添加头尾标识
                res.Insert(0, Mark);
                res.Add(Mark);

            }
            catch (Exception e)
            {
                LogManager.Error(e.Message,e);
                throw;
            }
            return  res.ToArray().ByteArrToHexStr();
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
