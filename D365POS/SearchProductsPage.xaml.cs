using D365POS.Models;
using D365POS.Services;
using System.Collections.ObjectModel;

namespace D365POS;

public partial class SearchProductsPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly Action<StoreProducts> _onProductSelected; // ✅ Callback
    private List<StoreProducts> _allProducts = new();
    public ObservableCollection<StoreProducts> FilteredProducts { get; set; } = new();

    public SearchProductsPage(DatabaseService db, Action<StoreProducts> onProductSelected)
    {
        InitializeComponent();
        _db = db;
        _onProductSelected = onProductSelected;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _allProducts = await _db.GetAllProducts();
            ProductsListView.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load products: {ex.Message}", "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string query = e.NewTextValue?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(query))
        {
            ProductsListView.IsVisible = false;
            FilteredProducts.Clear();
            return;
        }

        var results = _allProducts
            .Where(p =>
                (!string.IsNullOrEmpty(p.Description) && p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.ItemBarCode) && p.ItemBarCode.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.ItemId) && p.ItemId.Contains(query, StringComparison.OrdinalIgnoreCase))
            )
            .Take(20)
            .ToList();

        FilteredProducts.Clear();
        foreach (var item in results)
            FilteredProducts.Add(item);

        ProductsListView.IsVisible = FilteredProducts.Any();
    }

    // New: Handle product tap
    private async void OnProductSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is StoreProducts selectedProduct)
        {
            _onProductSelected?.Invoke(selectedProduct); // Send selected item back
            await Navigation.PopAsync(); // Go back to SalesPage
        }

        // Reset selection
        ((CollectionView)sender).SelectedItem = null;
    }
}
