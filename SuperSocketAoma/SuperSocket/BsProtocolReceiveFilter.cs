using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using SuperSocket.Common;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using SuperSocketAoma.Common;
using SuperSocketAoma.Model;

//using log4net;

namespace SuperSocketAoma.SuperSocket
{
    public class BsProtocolReceiveFilter : IReceiveFilter<BsProtocolRequestInfo>
    {
        private readonly BsProtocolSession _appSession;
        private readonly object _lockObj = new object();
        private static readonly byte Mark = Convert.ToByte(ConfigurationManager.AppSettings["Mark"], 16);
        //private readonly ILog _logger;
        public BsProtocolReceiveFilter(IAppSession appSession)
        {
            State = FilterState.Normal;
            _appSession = (BsProtocolSession) appSession;
            _appSession.FragBytes = new List<byte>();
            //_logger = LogManager.GetLogger(GetType());
        }

        public BsProtocolRequestInfo Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest)
        {
            rest = 0;
            var res = new ArraySegment<byte>(readBuffer, offset, length);
            var source = res.ToArray();
            //Common.Extensions.AddLog($"收到报文：{source.ByteArrToHexStr()}");
            //LogManager.Info($"收到报文：{source.ByteArrToHexStr()}");

            List<AnalysisAlert> list = null;
            try
            {
                //lock (_lockObj)
                //{
                //    BsPackage.SourceList.AddRange(source);
                //}
                //Common.Extensions.AddLog($"source长度：{source.Length}");
                //Common.Extensions.AddLog($"发送报文：{str}");
                 //LogManager.Info($"发送报文：{str}");
                source.SeparatePacket(_appSession);
                if (_appSession.FragBytes.Any())
                {
                    var fragArr = _appSession.FragBytes.ToArray();
                    //for (var i = 0; i < fragArr.Length / 76; i++)
                    //{
                    //    BsPackage.PacketQueue.Enqueue(fragArr.CloneRange(76 * i, 76).ToList());
                    //}
                    var packet = BsPackage.SeparateSinglePacket(source);
                    if (packet.LastOrDefault() == Mark)
                    {
                        BsPackage.PacketQueue.Enqueue(packet);
                        _appSession.FragBytes = fragArr.Skip(packet.Count).ToList();
                    }
                    //_appSession.FragBytes.Clear();
                }

            }
            catch (Exception e)
            {
                LogManager.Error(e.Message, e);
            }

            return new BsProtocolRequestInfo(list);
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public int LeftBufferSize { get; private set; }
        public IReceiveFilter<BsProtocolRequestInfo> NextReceiveFilter { get; private set; }
        public FilterState State { get; private set; }
    }
}
