using System.ComponentModel;

namespace SuperSocketAoma.Model
{
    public abstract class MessageHeader
    {
        [DisplayName("消息ID")]
        public ushort MessageId { get; set; }

        //[DisplayName("消息体属性")]
        //public ushort Properties { get; set; }

        public bool IsMultiPacket { get; set; }
        public bool IsEncrypted { get; set; }
        public ushort MessageLength { get; set; }

        [DisplayName("协议版本号")]
        public byte Version => 1;

        [DisplayName("终端ID")]
        public byte[] TerminalId { get; set; }

        [DisplayName("消息流水号")]
        public ushort SerialNum { get; set; }

        [DisplayName("消息总包数")]
        public ushort TotalPack { get; set; }

        [DisplayName("包序号")]
        public ushort PackNum { get; set; }

        [DisplayName("校验码")]
        public byte CheckCode { get; set; }

        public abstract override string ToString();
    }

    public enum EventType
    {
        抽烟 = 0x01,
        打电话 = 0x02,
        疲劳打哈欠 = 0x03,
        长闭眼 = 0x04,
        长时间视线不在前方 = 0x06,
        前车碰撞预警 = 0xa5,
        道路偏离预警 = 0xa6
    }

    public enum Manufacturer
    {
        中安 = 0xf0,
        新型媒体 = 0xf1,
        强生 = 0xf2,
        澳马 = 0xf3
    }

}