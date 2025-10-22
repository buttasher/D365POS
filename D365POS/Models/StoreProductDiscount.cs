using SQLite;

namespace D365POS.Models
{
    [Table("StoreProductDiscount")]
    public class StoreProductDiscount
    {
        [PrimaryKey, AutoIncrement]
        public int StoreProductDiscountId { get; set; }
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string Unit { get; set; }
        public DateOnly EffectiveDate { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public decimal PercentageOff { get; set; }
        public decimal AmountOff { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal StoreId { get; set; }
      
    }
}
