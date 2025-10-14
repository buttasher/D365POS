using SQLite;


namespace D365POS.Models
{
    [Table("POSRetailTransactionTaxTrans")]
    public class POSRetailTransactionTaxTrans
    {
        [PrimaryKey, AutoIncrement]
        public int TaxTransId { get; set; }

        [Indexed]
        public int TransactionId { get; set; }
        public decimal TaxAmount { get; set; }
        public double TaxRate { get; set; }
        public string TaxName { get; set; }
    }
}
