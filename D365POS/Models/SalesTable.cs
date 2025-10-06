using SQLite;

namespace D365POS.Models
{
    [Table("SalesTable")]
    public class SalesTable
    {
        [PrimaryKey, AutoIncrement]
        public int SalesId { get; set; }
        public string StoreId { get; set; }
        public DateTime TransDateTime { get; set; }
        public string SalesRegId { get; set; }
        public string OperatorId { get; set; }
        public string ShiftId { get; set; }
        public string ReceiptId { get; set; }
        public int SalesType { get; set; }
        public string ItemId { get; set; }
        public string ItemDescription { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public string Discount { get; set; }
        public decimal VAT { get; set; }
        public decimal LineAmountExclVAT { get; set; }
        public decimal LineAmountInclVAT { get; set; }
        public string CustAccount { get; set; }

        public enum Sales
        {
            None,
            Sales,
            SalesReturn,
            DeclareTender,
            SuspendTransaction,
            VoidTransaction
        }
    }
}
