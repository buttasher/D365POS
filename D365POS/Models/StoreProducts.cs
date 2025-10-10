using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace D365POS.Models
{
    [Table("StoreProducts")]
    public class StoreProducts : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int StoreProductId { get; set; }

        public string ItemId { get; set; }
        public string Description { get; set; }
        public string DescriptionAr { get; set; }
        public string UnitId { get; set; }
        public string PLUCode { get; set; }
        public string ItemBarCode { get; set; }
        public string SalesTaxGroup { get; set; }

        private decimal _quantity;
        
       
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(Total));
                }
            }
        }
        
        private decimal _unitPrice;
        [Ignore]
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged(nameof(UnitPrice));
                    OnPropertyChanged(nameof(Total));
                }
            }
        }
        [Ignore]
        // FRONT-END ONLY: Total
        public decimal Total => Quantity * UnitPrice;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
