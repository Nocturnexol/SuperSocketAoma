using System.Collections.Generic;
using SuperSocket.SocketBase.Protocol;
using SuperSocketAoma.Model;

namespace SuperSocketAoma.SuperSocket
{
    public class BsProtocolRequestInfo : RequestInfo<List<AnalysisAlert>>
    {
        public BsProtocolRequestInfo(List<AnalysisAlert> bsDataList)
        {
            Initialize("BsProtocolRequestInfoCommand", bsDataList);
        }
    }
}
