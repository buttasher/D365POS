using SQLite;


namespace D365POS.Models
{
    [Table("StoreProductsUnit")]
    public class StoreProductsUnit
    {
        [PrimaryKey, AutoIncrement]
        public int StoreProductsUnitId { get; set; }
        public string ItemId { get; set; }
        public string UnitId { get; set; }
        public decimal UnitPrice { get; set; }
        public string storeId { get; set; }
    }
}
