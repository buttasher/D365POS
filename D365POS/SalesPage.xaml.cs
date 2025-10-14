using CommunityToolkit.Maui.Views;
using D365POS.Models;
using D365POS.Popups;
using D365POS.Services;
using System.Collections.ObjectModel;
using System.Data;



namespace D365POS
{
    [QueryProperty(nameof(PaymentAmount), "PaymentAmount")]
    [QueryProperty(nameof(NewAmountDue), "NewAmountDue")]
    public partial class SalesPage : ContentPage
    {
        private readonly DatabaseService _db;
        private List<StoreProducts> _allProducts;
        private List<StoreProductsUnit> _allUnits = new();
        private readonly RecordSalesService _recordSalesService = new RecordSalesService();
        public int LinesCount => AddedProducts?.Count ?? 0;
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
        private decimal _paymentAmount;
        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                _paymentAmount = value;
                PaymentLabel.Text = value.ToString("N4");

            }
        }
        private decimal _newAmountDue;
        public decimal NewAmountDue
        {
            get => _newAmountDue;
            set
            {
                _newAmountDue = value;
                AmountDueLabel.Text = value.ToString("N4");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    bool confirmed = await ShowConfirmationPopup(_paymentAmount, _newAmountDue);

                    if (confirmed)
                    {
                        // Call your API after user confirms
                        await RecordSaleAsync("Cash");
                    }
                });
            }
        }

        private decimal _totalTax;
        public decimal TotalTax
        {
            get => _totalTax;
            set
            {
                if (_totalTax != value)
                {
                    _totalTax = value;
                    OnPropertyChanged(nameof(TotalTax));
                }
            }
        }
        private decimal _totalSubtotal;
        public decimal TotalSubtotal
        {
            get => _totalSubtotal;
            set
            {
                if (_totalSubtotal != value)
                {
                    _totalSubtotal = value;
                    OnPropertyChanged(nameof(TotalSubtotal));
                }
            }
        }

        public SalesPage(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            FilteredProducts = new ObservableCollection<StoreProducts>();
            AddedProducts = new ObservableCollection<StoreProducts>();
            BindingContext = this;
            AddedProducts.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(LinesCount));
                RecalculateTotals();
            };

        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            _allProducts = await _db.GetAllProducts();
            _allUnits = await _db.GetAllProductsUnit();
        }
        private void RecalculateTotals()
        {
            TotalTax = AddedProducts.Sum(p => p.TaxAmount);
            TotalSubtotal = AddedProducts.Sum(p => p.Subtotal);
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

            // Get the unit price from StoreProductsUnit table
            var unit = _allUnits.FirstOrDefault(u => u.ItemId == product.ItemId);
            decimal unitPrice = unit?.UnitPrice ?? 0;

            if (existing != null)
            {
                existing.Quantity += 1;
                existing.UnitPrice = unitPrice; // update unit price
            }
            else
            {
                product.Quantity = 1;
                product.UnitPrice = unitPrice; // set unit price for new product
                AddedProducts.Add(product);
            }
            RecalculateTotals();
        }
        private async void OnPayCashClicked(object sender, EventArgs e)
        {
            var totalAmount = AddedProducts.Sum(p => p.Total);
            await Shell.Current.GoToAsync($"{nameof(PayCashPage)}?AmountDue={totalAmount}");
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
        private async void OnPayQuickCashClicked(object sender, EventArgs e)
        {
            await RecordSaleAsync("Cash");
        }

        private async void OnPayCardClicked(object sender, EventArgs e)
        {
            await RecordSaleAsync("Card");
        }
        private async Task RecordSaleAsync(string paymentMethod)
        {
            if (AddedProducts == null || !AddedProducts.Any())
            {
                await DisplayAlert("No items", "Please add at least one product to record a sale.", "OK");
                return;
            }

            loaderOverlay.IsVisible = true;

            try
            {
                var storeId = Preferences.Get("Store", string.Empty);
                var company = Preferences.Get("Company", string.Empty);
                var cashier = Preferences.Get("UserId", string.Empty);
                var total = AddedProducts.Sum(p => p.Total);
                var totalTax = AddedProducts.Sum(p => p.TaxAmount);


                var saleItem = new RecordSalesService.SaleItemDto
                {
                    StoreId = storeId,
                    TransDate = DateOnly.FromDateTime(DateTime.Now),
                    TerminalId = "POS1",
                    StaffId = cashier,
                    ShiftId = "Morning",
                    ReceiptId = $"INV-{DateTime.Now:yyyyMMddHHmmss}",
                    Payments = new List<RecordSalesService.PaymentDto>
                    {
                        new RecordSalesService.PaymentDto
                        {
                            PaymentDateTime = DateTime.Now,
                            PaymentMethod = paymentMethod,
                            PaymentType = "",
                            Currency = "AED",
                            PaymentAmount = total.ToString("F2")
                        }
                    },
                    Taxes = new List<RecordSalesService.TaxDto>
                    {
                        new RecordSalesService.TaxDto
                        {
                            TaxName = "VAT",
                            TaxRate = (double)(AddedProducts.FirstOrDefault()?.TaxFactor ?? 0.05m),
                            TaxValue = totalTax.ToString("F2")
                        }
                    },
                    Items = AddedProducts.Select(p => new RecordSalesService.ItemDto
                    {
                        ItemId = p.ItemId,
                        UnitId = p.UnitId,
                        UnitPrice = p.UnitPrice,
                        Qty = (int)p.Quantity,
                        LineAmount = (int)p.Total,
                        TaxAmount = p.TaxAmount,
                        Action = 2,
                        ActionDateTime = DateTime.Now
                    }).ToList()
                };

                var success = await _recordSalesService.RecordSalesAsync(
                    company,
                    new List<RecordSalesService.SaleItemDto> { saleItem },
                    CancellationToken.None
                );

                if (success)
                {
                    var header = new POSRetailTransactionTable
                    {
                        StoreId = storeId,
                        TerminalId = saleItem.TerminalId,
                        ShiftId = saleItem.ShiftId,
                        ShiftStaffId = cashier,
                        ReceiptId = saleItem.ReceiptId,
                        BusinessDate = DateTime.Now,
                        Currency = saleItem.Payments.FirstOrDefault()?.Currency ?? "AED",
                        Total = total
                    };
                    await _db.CreateTransactionTable(header);

                    foreach (var p in AddedProducts)
                    {
                        decimal netAmount, grossAmount, taxAmount;

                       
                        if (p.PriceIncludeTax > 0)
                        {
                           
                            taxAmount = Math.Round(p.Total - (p.Total / (1 + p.TaxFactor)), 4);
                            netAmount = Math.Round(p.Total / (1 + p.TaxFactor), 4);
                            grossAmount = p.Total;
                        }
                        else
                        {
                            // add tax
                            taxAmount = Math.Round(p.Total * p.TaxFactor, 4);
                            netAmount = p.Total;
                            grossAmount = p.Total + taxAmount;
                        }

                        var line = new POSRetailTransactionSalesTrans
                        {
                            TransactionId = header.TransactionId,
                            LineNum = LinesCount,
                            ItemId = p.ItemId,
                            Qty = p.Quantity,
                            UnitId = p.UnitId,
                            UnitPrice = p.UnitPrice,
                            NetAmount = netAmount,
                            TaxAmount = taxAmount,
                            GrossAmount = grossAmount,
                            DiscAmount = 0,
                            DiscAmountWithoutTax = 0
                        };
                        await _db.CreateTransactionSalesTrans(line);
                    }

                    var payment = new POSRetailTransactionPaymentTrans
                    {
                        TransactionId = header.TransactionId,
                        PaymentDateTime = DateTime.Now,
                        PaymentMethod = paymentMethod,
                        PaymentType = "",
                        Currency = saleItem.Payments.FirstOrDefault()?.Currency ?? "AED",
                        PaymentAmount = AddedProducts.Sum(p => p.Total)
                    };
                    await _db.CreateTransactionPaymentTrans(payment);

                    var tax = new POSRetailTransactionTaxTrans
                    {
                        TransactionId = header.TransactionId,
                        TaxName = saleItem.Taxes.FirstOrDefault()?.TaxName ?? "VAT",
                        TaxRate = saleItem.Taxes.FirstOrDefault()?.TaxRate ?? 0.05,
                        TaxAmount = totalTax
                    };
                    await _db.CreateTransactionTaxTrans(tax);


                    await DisplayAlert("Success", $"Sale recorded successfully via {paymentMethod}!", "OK");
                    AddedProducts.Clear();
                    ProductsList.ItemsSource = null;
                    ProductsList.ItemsSource = AddedProducts;
                    PaymentLabel.Text = "0.0000";
                    AmountDueLabel.Text = "0.0000";
                }
                else
                {
                    await DisplayAlert("Error", "Failed to record sale.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                loaderOverlay.IsVisible = false;
            }
        }

        private async Task<bool> ShowConfirmationPopup(decimal paymentAmount, decimal newAmountDue)
        {
            try
            {
                var popup = new ConfirmationPopup(paymentAmount, newAmountDue);
                var result = await this.ShowPopupAsync(popup);
                return result is bool b && b;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Popup error: {ex.Message}");
                return false;
            }
        }
    }
}
