using System;
using System.ComponentModel;
using System.Text;

namespace SuperSocketAoma.Model
{
    public sealed class AnalysisAlert : MessageHeader
    {
        [DisplayName("时间")]
        public byte[] DateTime { get; set; }

        [DisplayName("事件类型")]
        public EventType EventType { get; set; }

        [DisplayName("厂商")]
        public Manufacturer Manufacturer { get; set; }

        [DisplayName("文件名长度")]
        public byte FileNameLength { get; set; }

        [DisplayName("文件名")]
        public string FileName { get; set; }
        public string SourceHexStr { get; set; }
        public string GetDateTimeStr()
        {
            var dateTime = new DateTime(int.Parse("20" + DateTime[0]), DateTime[1], DateTime[2], DateTime[3], DateTime[4], DateTime[5]);
            return dateTime.ToString("yyyy/MM/dd HH:mm:ss");
        }
        public AnalysisAlert GetExample()
        {
            MessageId = 233;
            //Properties = Convert.ToUInt16("0000000000001010", 2);
            IsMultiPacket = false;
            IsEncrypted = false;
            MessageLength = 50;
            TerminalId = new byte[] {0x41, 0x42, 0x31, 0x32, 0x33, 0x34, 0x35};
            SerialNum = 565;

            var now = System.DateTime.Now;
            DateTime = new[]
            {
                (byte) (now.Year - 2000), (byte) now.Month, (byte) now.Day, (byte) now.Hour, (byte) now.Minute,
                (byte) now.Second
            };
            EventType = EventType.疲劳打哈欠;
            Manufacturer = Manufacturer.新型媒体;
            FileNameLength = 3;
            FileName = "文件x";
            return this;
        }

        public override string ToString()
        {
            var  sb=new StringBuilder();
            sb.AppendFormat("消息ID：{0}，是否分包：{1}，是否加密：{2}，消息体长度：{3}，协议版本号：{4}，终端ID：{5}，消息流水号：{6}，", MessageId,
                IsMultiPacket, IsEncrypted, MessageLength, Version, Encoding.Default.GetString(TerminalId), SerialNum);
            if (IsMultiPacket)
                sb.AppendFormat("消息总包数：{0}，包序号：{1}，", TotalPack, PackNum);
            sb.AppendFormat("时间：{0}，事件类型：{1}，厂商：{2}，文件名长度：{3}，文件名：{4}", GetDateTimeStr(), EventType, Manufacturer,
                FileNameLength, FileName);
            return sb.ToString();
        }
    }
}
