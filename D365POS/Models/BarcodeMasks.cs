using SQLite;

namespace D365POS.Models
{
    [Table("BarcodeMasks")]
    public class BarcodeMasks
    {
        [PrimaryKey, AutoIncrement]
        public int BarcodeMasksId { get; set; }
        public int MaskId { get; set; }
        public string Mask { get; set; }
        public int Prefix { get; set; }
        public int Length { get; set; }
    }
}
