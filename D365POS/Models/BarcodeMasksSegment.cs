using SQLite;

namespace D365POS.Models
{
    [Table("BarcodeMasksSegment")]
    public class BarcodeMasksSegment
    {

        [PrimaryKey, AutoIncrement]
        public int BarcodeMasksSegmentId { get; set; }

        [Indexed]
        public int BarcodeMasksId { get; set; }
        public int SegmentNumber { get; set; }
        public string Type { get; set; }
        public int Length { get; set; }
    }
}
