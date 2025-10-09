using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using D365POS.Models;
using System.Linq;

namespace D365POS.Converters
{
    public class UnitPriceConverter : IValueConverter
    {
        public static List<StoreProductsUnit> AllUnits { get; set; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not StoreProducts product)
                return "0.0000";

            var matching = AllUnits?.FirstOrDefault(u => u.ItemId == product.ItemId);

            if (matching != null)
            {
                decimal total = product.Quantity * matching.UnitPrice;
                return total.ToString("N4");
            }

            return "0.0000";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
