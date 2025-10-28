using SQLite;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace D365POS.Models
{
    [SQLite.Table("StoreProducts")]
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
        public string ItemSalesTaxGroup { get; set; }
        public decimal TaxFactor {get; set; }

        [Ignore]
        public bool IsVoid { get; set; } = false;
        [Ignore]
        public decimal PriceIncludeTax { get; set; }

        private decimal _quantity;
        [Ignore]
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

        public decimal TaxAmount
        {
            get
            {
                if (TaxFactor <= 0)
                    return 0;

                if (PriceIncludeTax > 0)
                {
                    // Extract tax (price includes tax)
                    return Math.Round(Total - (Total / (1 + TaxFactor)), 3);
                }
                else
                {
                    // Add tax (price excludes tax)
                    return Math.Round(Total * TaxFactor, 3);
                }
            }
        }

        public decimal Subtotal
        {
            get
            {
                return Math.Round(Total + TaxAmount,3);
                
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
