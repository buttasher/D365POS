using CommunityToolkit.Maui.Views;
using D365POS.Models;
using System;

namespace D365POS.Popups;

public partial class SetQuantityPopup : Popup
{
    private readonly StoreProducts _selectedProduct;

    public SetQuantityPopup(StoreProducts selectedProduct)
    {
        InitializeComponent();
        _selectedProduct = selectedProduct;

        // Pre-fill with current quantity
        ItemIdEntry.Text = _selectedProduct.Quantity.ToString();
    }

    private void OnSetButtonClicked(object sender, EventArgs e)
    {
        if (decimal.TryParse(ItemIdEntry.Text, out decimal newQty) && newQty > 0)
        {
            _selectedProduct.Quantity = newQty;
            Close(true); // return success to parent page
        }
        else
        {
            Application.Current.MainPage.DisplayAlert("Invalid Quantity", "Please enter a valid number greater than 0.", "OK");
        }
    }
}