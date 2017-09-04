using SuperSocket.SocketBase.Command;
using SuperSocketAoma.Common;
//using log4net;

namespace SuperSocketAoma.SuperSocket
{
    public class BsProtocolRequestInfoCommand : CommandBase<BsProtocolSession, BsProtocolRequestInfo>
    {
        public override void ExecuteCommand(BsProtocolSession session, BsProtocolRequestInfo requestInfo)
        {
            //var logger = LogManager.GetLogger(GetType());
            requestInfo.Body?.ForEach(t =>
            {
                LogManager.Info(t.ToString());
                //Extensions.AddLog(t.ToString());
            });

        }
    }
}
