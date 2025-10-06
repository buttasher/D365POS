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
        public DisType DiscountType { get; set; }
        public decimal PercentageOff { get; set; }
        public decimal AmountOff { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal StoreId { get; set; }
        public SyncStatus ProductSyncStatus { get; set; }
        public ActiveStatus ProductActiveStatus { get; set; }
        public enum ActiveStatus
        {
            None = 0,
            Active = 1,
            InActive = 2,
            Expired = 3
        }
        public enum SyncStatus
        {
            None = 0,
            Publish = 1,
            Active = 2,
            InActive = 3
        }
        public enum DisType 
        { 
            None = 0,
            Percentage = 1,
            Value = 2
        }

    }
}
