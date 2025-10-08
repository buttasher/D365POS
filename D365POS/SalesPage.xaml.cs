using D365POS.Models;
using D365POS.Services;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace D365POS
{
    public partial class SalesPage : ContentPage
    {
        private readonly DatabaseService _db;
        private List<StoreProducts> _allProducts;

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

        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            var keyword = ProductSearchBar.Text?.ToLower();

            var match = _allProducts.FirstOrDefault(p =>
                p.ItemBarCode.ToLower() == keyword ||
                p.ItemId.ToLower() == keyword ||
                p.Description.ToLower().Contains(keyword));

            if (match != null)
            {
                AddProductToTable(match);
                IsSearchListVisible = false;
                ProductSearchBar.Text = string.Empty;
            }
            else
            {
                DisplayAlert("Not found", "No matching item found.", "OK");
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
    }
}
