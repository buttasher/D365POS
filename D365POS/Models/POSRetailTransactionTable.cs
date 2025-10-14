using SQLite;

namespace D365POS.Models
{
    [Table("POSRetailTransactionTable")]
    public class POSRetailTransactionTable
    {
        [PrimaryKey, AutoIncrement]
        public int TransactionId { get; set; }
        public string StoreId { get; set; }
        public string TerminalId { get; set; } = string.Empty;
        public string ShiftId { get; set; }
        public string ShiftStaffId { get; set; }
        public string ReceiptId { get; set; }
        public DateTime BusinessDate { get; set; }
        public string Currency { get; set; }
        public decimal Total { get; set; }
        public TransactionTypeEnum TransactionType { get; set; }
        public enum TransactionTypeEnum
        {
            Sale = 0,
            Return = 1,
            Void = 2,
            Exchange = 3
        }
    }
}
