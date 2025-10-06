using SQLite;

namespace D365POS.Models
{
    [Table("StoreProductUnit")]
    class StoreProductUnit
    {
        [PrimaryKey, AutoIncrement]
        public int StoreProductUnitId { get; set; }
        public string ItemId { get; set; }
        public string Unit { get; set; }
        public DateOnly EfectiveDate { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public Type CalculationType { get; set; }
        public decimal UnitPrice { get; set; }
        public string storeId { get; set; }
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
        public enum Type
        {
            None = 0,
            UnitPrice = 1,
            Discount = 2
        }
    }
}
