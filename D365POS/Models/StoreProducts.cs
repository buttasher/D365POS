using SQLite;

namespace D365POS.Models
{
    [Table("StoreProducts")]
    public class StoreProducts
    {
        [PrimaryKey, AutoIncrement]
        public int StoreProductId { get; set; }
        public string ItemId { get; set; }
        public string Unit { get; set; }
        public string StoreId { get; set; }
        public string ItemBarCode { get; set; }
        public string ItemName { get; set; }
        public string ItemNameArabic { get; set; }
        public decimal PLU { get; set; }
        public Status ProductStatus { get; set; }
        public int Category { get; set; }
        public string SalesTaxGroup { get; set; }

        public decimal Quantity { get; set; }
        public enum Status
        {
            None = 0,
            Publish = 1,
            Active = 2,
            Inactive = 3
        }

    }
}
