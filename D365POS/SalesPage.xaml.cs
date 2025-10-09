using CommunityToolkit.Maui.Views;
using D365POS.Converters;
using D365POS.Models;
using D365POS.Popups;
using D365POS.Services;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using static SQLite.SQLite3;


namespace D365POS
{
    public partial class SalesPage : ContentPage
    {
        private readonly DatabaseService _db;
        private List<StoreProducts> _allProducts;
        private readonly DatabaseService _dbService = new DatabaseService();
        private List<StoreProductsUnit> _allUnits = new();

        public ObservableCollection<StoreProducts> FilteredProducts { get; set; }
        public ObservableCollection<StoreProducts> AddedProducts { get; set; }

        private bool _isSearchListVisible;
        public bool IsSearchListVisible
        {
            get => _isSearchListVisible;
            set
            {
                _isSearchListVisible = value;
                OnPropertyChanged(nameof(IsSearchListVisible));
            }
        }

        public SalesPage(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            FilteredProducts = new ObservableCollection<StoreProducts>();
            AddedProducts = new ObservableCollection<StoreProducts>();
            BindingContext = this;

        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _allProducts = await _db.GetAllProducts();
            _allUnits = await _dbService.GetAllProductsUnit();
            UnitPriceConverter.AllUnits = _allUnits;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(e.NewTextValue);
            IsSearchListVisible = !string.IsNullOrWhiteSpace(e.NewTextValue) && FilteredProducts.Any();
        }

        private void ApplyFilter(string keyword)
        {
            keyword = keyword?.ToLower() ?? string.Empty;

            var filtered = _allProducts
                .Where(p => p.Description.ToLower().Contains(keyword)
                         || p.ItemBarCode.ToLower().Contains(keyword)
                         || p.ItemId.ToLower().Contains(keyword))
                .ToList();

            FilteredProducts.Clear();
            foreach (var item in filtered)
                FilteredProducts.Add(item);
        }

        private void OnProductSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is StoreProducts selectedProduct)
            {
                AddProductToTable(selectedProduct);

                // Hide overlay
                IsSearchListVisible = false;

                // Clear search bar but keep visible
                ProductSearchBar.Text = string.Empty;

                // Deselect item
                SearchResultsList.SelectedItem = null;
            }
        }
        private void AddProductToTable(StoreProducts product)
        {
            var existing = AddedProducts.FirstOrDefault(p => p.ItemId == product.ItemId);

            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                product.Quantity = 1;
                AddedProducts.Add(product);
            }

            ProductsList.ItemsSource = null;
            ProductsList.ItemsSource = AddedProducts;
        }
        private async void OnPayCashClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(PayCashPage));
        }
        private async void OnPriceCheckClicked(object sender, EventArgs e)
        {
            loaderOverlay.IsVisible = true;
            activityIndicator.IsRunning = true;

            var productPriceSyncService = new ProductPriceSyncService();
            var popup = new PriceCheckPopup();

            var storeId = Preferences.Get("Store", string.Empty);
            var company = Preferences.Get("Company", string.Empty);

            try
            {
                await productPriceSyncService.SyncProductsPricesAsync(company, storeId);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to sync products: {ex.Message}", "OK");
                return;
            }
            finally
            {
                loaderOverlay.IsVisible = false;
                activityIndicator.IsRunning = false;
                await this.ShowPopupAsync(popup);
            }
        }
    }
}
