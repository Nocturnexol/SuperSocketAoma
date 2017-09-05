using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperSocketAoma.Db
{
    public class AnalysisAlertData
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int MessageId { get; set; }
        public string Content { get; set; }
        public DateTime DateTime { get; set; }
        public DateTime SaveTime { get; set; }
    }
}
