using CommunityToolkit.Maui.Views;
using D365POS.Models;
using D365POS.Popups;
using D365POS.Services;
using D365POS.Helpers;
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
        private void PrintReceipt(string storeId, string cashier, string receiptId, decimal total, decimal totalTax, string payment)
        {
            string printerName = "EPSON TM-T82 Receipt";
            string receipt = "";

            receipt += "\x1B\x40"; // ESC @ Initialize printer
            receipt += AlignText("Tax Invoice", 48, "center") + "\n";
            receipt += AlignText("AL Douri Signature Specialty Food Store L.L.C", 48, "center") + "\n";
            receipt += AlignText("Creek Harbour", 48, "center") + "\n\n";

            // Widths
            int itemWidth = 20;
            int qtyWidth = 10;
            int priceWidth = 10;
            int amtWidth = 10;
            int receiptWidth = 48;

            // Receipt Info
            receipt += AlignText($"Receipt No: {receiptId}", receiptWidth, "left") + "\n";
            receipt += AlignText($"Store Id: {storeId}", receiptWidth, "left") + "\n";
            receipt += AlignText($"Cashier Id: {cashier}", receiptWidth, "left") + "\n";
            receipt += AlignText($"Date: {DateTime.Now:dd-MM-yyyy}", receiptWidth, "right") + "\n";
            receipt += AlignText($"Time: {DateTime.Now:hh:mm tt}", receiptWidth, "right") + "\n\n";

            // Header
            receipt += AlignText("Item", itemWidth, "left") +
                       AlignText("Qty", qtyWidth, "center") +
                       AlignText("Price", priceWidth, "center") +
                       AlignText("Amt", amtWidth, "right") + "\n";

            receipt += "-----------------------------------------------\n";

            // Items
            foreach (var p in AddedProducts)
            {
                var wrappedNameLines = WrapText(p.Description, itemWidth).ToList();
                for (int i = 0; i < wrappedNameLines.Count; i++)
                {
                    string itemName = AlignText(wrappedNameLines[i], itemWidth, "left");
                    string qty = i == 0 ? AlignText(p.Quantity.ToString(), qtyWidth, "center") : AlignText("", qtyWidth, "center");
                    string price = i == 0 ? AlignText(p.UnitPrice.ToString("F2"), priceWidth, "center") : AlignText("", priceWidth, "center");
                    string amt = i == 0 ? AlignText(p.Total.ToString("F2"), amtWidth, "right") : AlignText("", amtWidth, "right");

                    receipt += $"{itemName}{qty}{price}{amt}\n";
                }
            }

            receipt += "-----------------------------------------------\n";

            // Totals
            decimal totalExcludingVAT = TotalSubtotal;
            decimal vatAmount = totalTax;
            decimal totalPayable = total + totalTax;
            decimal paidAmount = totalPayable;

            receipt += AlignText($"Total Excluding VAT: {totalExcludingVAT:F2}", receiptWidth, "left") + "\n";
            receipt += AlignText($"VAT 5% Included: {vatAmount:F2}", receiptWidth, "left") + "\n";
            receipt += AlignText($"Total Payable: {totalPayable:F2}", receiptWidth, "left") + "\n";
            receipt += AlignText($"Paid Amount: {paidAmount:F2}", receiptWidth, "left") + "\n";
            receipt += AlignText($"Payment Method: {payment}", receiptWidth, "left") + "\n\n";

            // Footer
            receipt += AlignText("Keep the bill for exchangeable Items.", receiptWidth, "left") + "\n";
            receipt += AlignText("Exchange within 2 days.", receiptWidth, "left") + "\n";
            receipt += AlignText("No exchange for fresh & frozen foods.", receiptWidth, "left") + "\n";
            receipt += AlignText("Item should be returned in original packaging.", receiptWidth, "left") + "\n";
            receipt += "\n" + AlignText("Thank you for shopping!", receiptWidth, "center") + "\n\n";

            receipt += "\x1D\x56\x42\x03"; // GS V B 3 Cut paper

            RawPrinterHelper.SendStringToPrinter(printerName, receipt);
        }
        private string AlignText(string text, int width, string align = "left", int leftMargin = 2)
        {
            text = text ?? "";

            switch (align.ToLower())
            {
                case "left":
                    return new string(' ', leftMargin) + text.PadRight(width - leftMargin);
                case "right":
                    return text.PadLeft(width - leftMargin) + new string(' ', leftMargin);
                case "center":
                    int padding = (width - text.Length) / 2;
                    return new string(' ', padding) + text;
                default:
                    return new string(' ', leftMargin) + text.PadRight(width - leftMargin);
            }
        }
        private IEnumerable<string> WrapText(string text, int width)
        {
            List<string> lines = new List<string>();

            while (text.Length > width)
            {
                lines.Add(text.Substring(0, width));
                text = text.Substring(width);
            }

            if (text.Length > 0)
                lines.Add(text);

            return lines;
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
                var paymentMethodId = paymentMethod; // "Cash" or "Card"

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

                    PrintReceipt(storeId, cashier, saleItem.ReceiptId, total, totalTax, paymentMethodId);
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
