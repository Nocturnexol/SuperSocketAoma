using System;
using System.ComponentModel.DataAnnotations;
using SuperSocketAoma.Model;

namespace SuperSocketAoma.Db
{
    public class AnalysisAlertData
    {
        [Key]
        public Guid Guid { get; set; }
        public string TerminalId { get; set; }
        public int MessageId { get; set; }
        public string Content { get; set; }
        public DateTime DateTime { get; set; }
        public EventType EventType { get; set; }
        public Manufacturer Manufacturer { get; set; }
        public byte FileNameLength { get; set; }
        public string FileName { get; set; }
        public DateTime SaveTime { get; set; }
    }
}
