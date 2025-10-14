using SQLite;

namespace D365POS.Models
{
    [Table("POSRetailTransactionPaymentTrans")]
    public class POSRetailTransactionPaymentTrans
    {
        [PrimaryKey, AutoIncrement]
        public int PaymentTransId { get; set; }

        [Indexed]
        public int TransactionId { get; set; }
        public DateTime PaymentDateTime { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentType { get; set; }
        public string Currency { get; set; }
        public decimal PaymentAmount { get; set; }
    }
}