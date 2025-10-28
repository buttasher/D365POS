using SQLite;

namespace D365POS.Models
{
    [Table("POSRetailTransactionSalesTrans")]
    public class POSRetailTransactionSalesTrans
    {
        [PrimaryKey, AutoIncrement]
        public int SalesTransId { get; set; }

        [Indexed]
        public int TransactionId { get; set; }
        public decimal LineNum { get; set; }
        public string ItemId { get; set; }

        [Ignore] 
        public string ItemDescription { get; set; }
        public decimal Qty { get; set; }
        public string UnitId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscAmount { get; set; }
        public decimal DiscAmountWithoutTax { get; set; }
    }
}
