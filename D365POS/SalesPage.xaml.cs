using CommunityToolkit.Maui.Views;
using D365POS.Helpers;
using D365POS.Models;
using D365POS.Popups;
using D365POS.Services;
using System.Collections.ObjectModel;
using System.Data;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;


namespace D365POS
{
    [QueryProperty(nameof(PaymentAmount), "PaymentAmount")]
    [QueryProperty(nameof(NewAmountDue), "NewAmountDue")]
    [QueryProperty(nameof(IsReturn), "IsReturn")]
    [QueryProperty(nameof(ReturnTransactionId), "ReturnTransactionId")]
    [QueryProperty(nameof(ReturnLines), "ReturnLines")]
    public partial class SalesPage : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DatabaseService _db;
        private List<StoreProducts> _allProducts;
        private List<StoreProductsUnit> _allUnits = new();
        private readonly RecordSalesService _recordSalesService = new RecordSalesService();
        public int LinesCount => AddedProducts?.Count ?? 0;
        public ObservableCollection<StoreProducts> FilteredProducts { get; set; }
        public ObservableCollection<StoreProducts> AddedProducts { get; set; }
        private StoreProducts _selectedProduct;
        private CancellationTokenSource _searchDelayCts;
        public bool IsReturn { get; set; } = false;
        public int ReturnTransactionId { get; set; } = 0;
        public List<POSRetailTransactionSalesTrans> ReturnLines { get; set; }

        private bool _returnLinesInitialized = false;

   
        private decimal _paymentAmount;
        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                _paymentAmount = value;
                PaymentLabel.Text = value.ToString("F3");

            }
        }
        private decimal _newAmountDue;
        public decimal NewAmountDue
        {
            get => _newAmountDue;
            set
            {
                _newAmountDue = value;
                AmountDueLabel.Text = value.ToString("F3");
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


            if (IsReturn && ReturnTransactionId != 0 && !_returnLinesInitialized)
            {
                await InitializeReturnLines();
                _returnLinesInitialized = true; // mark as initialized
            }
        }
        private async Task InitializeReturnLines()
        {
            try
            {
                if (IsReturn && ReturnLines != null)
                {
                    foreach (var line in ReturnLines)
                    {
                        var product = _allProducts.FirstOrDefault(p =>
                        p.ItemId == line.ItemId && p.UnitId == line.UnitId);

                        if (product != null)
                        {
                            AddProductToTable(product, -line.Qty); // Add as negative qty
                        }
                       
                    }
                    return;
                }

                var lines = await _db.GetListAsync<POSRetailTransactionSalesTrans>(
                    x => x.TransactionId == ReturnTransactionId
                );

                if (lines == null || !lines.Any())
                    return;

                foreach (var line in lines)
                {
                    var product = _allProducts.FirstOrDefault(p =>
                        p.ItemId == line.ItemId && p.UnitId == line.UnitId);

                    if (product != null)
                    {
                        AddProductToTable(product, -line.Qty);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load return lines: {ex.Message}", "OK");
            }
        }
        private async void OnSearchProductlicked(object sender, EventArgs e)
        {
            loaderOverlay.IsVisible = true;
            activityIndicator.IsRunning = true;
            await Task.Delay(100);

            try
            {
                // ✅ Pass callback so selected product comes back here
                await Navigation.PushAsync(new SearchProductsPage(_db, OnProductSelectedFromSearch));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to navigate to Search Page: {ex.Message}", "OK");
            }
            finally
            {
                loaderOverlay.IsVisible = false;
                activityIndicator.IsRunning = false;
            }
        }
        private void OnProductSelectedFromSearch(StoreProducts selectedProduct)
        {
            if (selectedProduct == null)
                return;

            // Default quantity = 1
            AddProductToTable(selectedProduct);
        }
        private void OnProductSelectedFromList(object sender, SelectionChangedEventArgs e)
        {
            _selectedProduct = e.CurrentSelection.FirstOrDefault() as StoreProducts;
        }
        private void RecalculateTotals()
        {
            var activeProducts = AddedProducts.Where(p => !p.IsVoid);

            TotalTax = activeProducts.Sum(p => p.TaxAmount);
            TotalSubtotal = activeProducts.Sum(p => p.Subtotal);
        }
        // Auto-detect barcode scanner input
        private async void BarcodeEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue))
                return;

            string company = Preferences.Get("Company", string.Empty);

            // Detect fast barcode paste (scanners input entire code at once)
            if (e.NewTextValue.Length >= 8 &&
                e.NewTextValue.Length - (e.OldTextValue?.Length ?? 0) > 5)
            {
                var barcode = e.NewTextValue.Trim();
                await HandleBarcodeAsync(barcode);
            }
        }

        // Manual entry (user presses Enter)
        private async void OnBarcodeCompleted(object sender, EventArgs e)
        {
            string barcode = BarcodeEntry.Text?.Trim();
            string company = Preferences.Get("Company", string.Empty);
            await HandleBarcodeAsync(barcode);
        }

        // Main logic used by both scanner & manual typing
        private async Task HandleBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return;

            try
            {
                // 1️⃣ Try direct product match first
                var directProduct = _allProducts.FirstOrDefault(p =>
                    p.ItemBarCode == barcode ||
                    p.ItemId == barcode);

                if (directProduct != null)
                {
                    AddProductToTable(directProduct);
                    return;
                }

                // 2Try decoding via mask logic
                var allMasks = await _db.GetAllMasksAsync();
                var matchedMask = allMasks.FirstOrDefault(m =>
                    barcode.StartsWith(m.Prefix.ToString()) && barcode.Length == m.Length);

                if (matchedMask == null)
                {
                    await DisplayAlert("Not Found", $"No product or mask match for barcode: {barcode}", "OK");
                    return;
                }

                int prefix = matchedMask.Prefix;
                int productLength = matchedMask.Length;
                string prefixStr = prefix.ToString();

               
                var maskLines = await _db.GetAllMasksSegmentAsync(x => x.BarcodeMasksId == matchedMask.BarcodeMasksId);
                var productSegment = maskLines.FirstOrDefault(x =>
                    x.Type.Equals("Product", StringComparison.OrdinalIgnoreCase));

                var priceSegment = maskLines.FirstOrDefault(x =>
                    x.Type.Equals("Price", StringComparison.OrdinalIgnoreCase));

                if (productSegment != null)
                {
                    int productSegmentLength = productSegment.Length;
                    string pluCode = barcode.Substring(prefixStr.Length, productSegmentLength);

                    var product = await _db.GetProductByPLUAsync(pluCode);

                    if (product != null)
                    {
                        var unitInfo = _allUnits.FirstOrDefault(u => u.ItemId == product.ItemId && u.UnitId == product.UnitId);
                        decimal unitPrice = unitInfo?.UnitPrice ?? 0;
                        decimal quantity = 1;

                        if (priceSegment != null)
                        {
                            int priceStartIndex = prefixStr.Length + productSegmentLength;
                            int priceLength = priceSegment.Length;

                            string priceStr = barcode.Substring(priceStartIndex, priceLength);

                            // Handle decimal places
                            int decimalCount = 0;

                            // Case 1: if Decimals is numeric (e.g., 2)
                            if (priceSegment.Decimals is decimal decValue)
                            {
                                decimalCount = (int)decValue;
                            }
                            // Apply decimal placement if we have decimal info
                            if (decimalCount > 0)
                            {
                                if (priceStr.Length > decimalCount)
                                {
                                    int integerPartLength = priceStr.Length - decimalCount;
                                    priceStr = priceStr.Insert(integerPartLength, ".");
                                }
                                else
                                {
                                    priceStr = "0." + priceStr.PadLeft(decimalCount, '0');
                                }
                            }

                            // Parse and calculate quantity
                            if (decimal.TryParse(priceStr, out decimal totalPrice) && unitPrice > 0)
                            {
                                quantity = totalPrice / unitPrice;
                            }
                        }

                        AddProductToTable(product, quantity);
                    }
                }
                else
                {
                    await DisplayAlert("Invalid Mask", $"No product segment defined for mask {matchedMask.BarcodeMasksId}.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                // Always reset entry after processing
                BarcodeEntry.Text = string.Empty;
                BarcodeEntry.Focus();
            }
        }
        private void AddProductToTable(StoreProducts product, decimal quantity = 1)
        {
            // Check if already exists (ignore void)
            var existing = AddedProducts.FirstOrDefault(p => p.ItemId == product.ItemId && p.UnitId == product.UnitId && !p.IsVoid);

            // Get unit info
            var unit = _allUnits.FirstOrDefault(u => u.ItemId == product.ItemId && u.UnitId == product.UnitId);
            decimal unitPrice = unit?.UnitPrice ?? 0;
            decimal priceIncludeTax = unit?.PriceIncludeTax ?? 0;

            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.UnitPrice = unitPrice;
                existing.PriceIncludeTax = priceIncludeTax;
            }
            else
            {
                product.Quantity = quantity;
                product.UnitPrice = unitPrice;
                product.PriceIncludeTax = priceIncludeTax;
                product.IsVoid = false;
                AddedProducts.Add(product);
            }

            RecalculateTotals();
        }

        private async void OnPayCashClicked(object sender, EventArgs e)
        {
            var totalAmount = AddedProducts.Sum(p => p.Total);
            await Shell.Current.GoToAsync($"{nameof(PayCashPage)}?AmountDue={TotalSubtotal}");
        }
        private async void OnPayQuickCashClicked(object sender, EventArgs e)
        {
            await RecordSaleAsync("Cash");
        }
        private async void OnSetButtonClicked(object sender, EventArgs e)
        {
            if (ProductsList.SelectedItem is not StoreProducts selectedProduct)
            {
                await DisplayAlert("No Selection", "Please select a product first.", "OK");
                return;
            }

            try
            {
                var popup = new SetQuantityPopup(selectedProduct);
                var result = await this.ShowPopupAsync(popup);

                if (result is bool success && success)
                {
                    // Refresh collection to reflect updated quantity and total
                    ProductsList.ItemsSource = null;
                    ProductsList.ItemsSource = AddedProducts;

                    // Update totals
                    RecalculateTotals();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open popup: {ex.Message}", "OK");
            }
            
        }

        private void PrintReceipt(string storeId, string cashier, string receiptId, decimal total, decimal totalTax, string payment, decimal totalWithoutVAT)
        {
            string printerName = "EPSON TM-T82 Receipt";
            string receipt = "";

            receipt += "\x1B\x40"; // ESC @ Initialize printer
            receipt += AlignText("Tax Invoice", 48, "center") + "\n";
            receipt += AlignText("YASIR DOURI FOODSTUFF TRADING COMPANY L.L.C", 48, "center") + "\n";
            receipt += AlignText("Oman", 48, "center") + "\n\n";

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

            var activeProducts = AddedProducts.Where(p => !p.IsVoid).ToList();
            // Items
            foreach (var p in activeProducts)
            {
                decimal quantity = Math.Round(p.Quantity, 3);
                decimal totalamt = Math.Round(p.Total, 3);
                var wrappedNameLines = WrapText(p.Description, itemWidth).ToList();
                for (int i = 0; i < wrappedNameLines.Count; i++)
                {
                    string itemName = AlignText(wrappedNameLines[i], itemWidth, "left");
                    string qty = i == 0 ? AlignText(quantity.ToString(), qtyWidth, "center") : AlignText("", qtyWidth, "center");
                    string price = i == 0 ? AlignText(p.UnitPrice.ToString("F2"), priceWidth, "center") : AlignText("", priceWidth, "center");
                    string amt = i == 0 ? AlignText(totalamt.ToString("F2"), amtWidth, "right") : AlignText("", amtWidth, "right");

                    receipt += $"{itemName}{qty}{price}{amt}\n";
                }
            }

            receipt += "-----------------------------------------------\n";

            // Totals
            decimal totalExcludingVAT = totalWithoutVAT;
            decimal vatAmount = totalTax;
            decimal totalPayable = total;
            decimal paidAmount = totalPayable;

            receipt += AlignText($"Total Excluding VAT: {totalExcludingVAT:F3}", receiptWidth, "left") + "\n";
            receipt += AlignText($"VAT 5% Included: {vatAmount:F3}", receiptWidth, "left") + "\n";
            receipt += AlignText($"Total Payable: {totalPayable:F3}", receiptWidth, "left") + "\n";
            receipt += AlignText($"Paid Amount: {paidAmount:F3}", receiptWidth, "left") + "\n";
            receipt += AlignText($"Payment Method: {payment}", receiptWidth, "left") + "\n\n";

            // Footer
            receipt += AlignText("Keep the bill for exchangeable Items.", receiptWidth, "left") + "\n";
            receipt += AlignText("Exchange within 2 days.", receiptWidth, "left") + "\n";
            receipt += AlignText("No exchange for fresh & frozen foods.", receiptWidth, "left") + "\n";
            receipt += AlignText("Item should be returned in original packaging.", receiptWidth, "left") + "\n";
            receipt += "\n" + AlignText("Thank you for shopping!", receiptWidth, "center") + "\n\n";

            receipt += "\x1D\x56\x42\x03"; // GS V B 3 Cut paper

          
            string pdfPath = GenerateReceiptPDF(receiptId, receipt);

            RawPrinterHelper.SendStringToPrinter(printerName, receipt);

            try
            {
                Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(pdfPath)
                });
            }
            catch { }
            // 🔹 Step 4: Delete the PDF automatically after short delay
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(5000); // wait 5 seconds before deleting
                    if (File.Exists(pdfPath))
                        File.Delete(pdfPath);
                }
                catch { }
            });
        }
        private string GenerateReceiptPDF(string receiptId, string content)
        {
            string folderPath = FileSystem.AppDataDirectory;
            string filePath = Path.Combine(folderPath, $"Receipt_{receiptId}.pdf");

            using (PdfDocument document = new PdfDocument())
            {
                var page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Courier New", 10, XFontStyle.Regular);

                string logoPath = Path.Combine(FileSystem.AppDataDirectory, "logo.png");

                double y = 20;
                double lineHeight = 14;

                foreach (string line in content.Split('\n'))
                {
                    gfx.DrawString(line, font, XBrushes.Black, new XRect(20, y, page.Width - 40, lineHeight), XStringFormats.TopLeft);
                    y += lineHeight;

                    // Add new page if too long
                    if (y > page.Height - 40)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        y = 20;
                    }
                }

                using (var stream = File.Create(filePath))
                {
                    document.Save(stream, false);
                }
            }

            return filePath;
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
        private async void OnVoidProductClicked(object sender, EventArgs e)
        {
            if (ProductsList.SelectedItem is not StoreProducts selectedProduct)
            {
                await DisplayAlert("No Selection", "Please select a product first.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Confirm", $"Are you sure you want to void {selectedProduct.Description}?", "Yes", "No");
            if (!confirm)
                return;

            try
            {
                var storeId = Preferences.Get("Store", string.Empty);
                var company = Preferences.Get("Company", string.Empty);
                var cashier = Preferences.Get("UserId", string.Empty);

                // Create a void transaction header for this line
                var header = new POSRetailTransactionTable
                {
                    StoreId = storeId,
                    TerminalId = "POS1",
                    ShiftId = "Morning",
                    ShiftStaffId = cashier,
                    ReceiptId = $"VOID-{DateTime.Now:yyyyMMddHHmmss}",
                    BusinessDate = DateTime.Now,
                    Currency = "AED",
                    Total = selectedProduct.Total,
                    TransactionType = POSRetailTransactionTable.TransactionTypeEnum.Void
                };

                await _db.CreateTransactionTable(header);

                // Save only the selected product line (optional: keep zero amounts)
                var line = new POSRetailTransactionSalesTrans
                {
                    TransactionId = header.TransactionId,
                    LineNum = 1,
                    ItemId = selectedProduct.ItemId,
                    Qty = selectedProduct.Quantity,
                    UnitId = selectedProduct.UnitId,
                    UnitPrice = selectedProduct.UnitPrice,
                    NetAmount = 0,
                    TaxAmount = 0,
                    GrossAmount = 0,
                    DiscAmount = 0,
                    DiscAmountWithoutTax = 0
                };
                await _db.CreateTransactionSalesTrans(line);

                // Mark as void
                selectedProduct.IsVoid = true;

                // Subtract this product from totals instead of resetting all
                TotalSubtotal -= selectedProduct.Subtotal;
                TotalTax -= selectedProduct.TaxAmount;

                // Refresh the CollectionView
                ProductsList.ItemsSource = null;
                ProductsList.ItemsSource = AddedProducts;

                await DisplayAlert("Success", $"{selectedProduct.Description} has been voided successfully!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to void product: {ex.Message}", "OK");
            }
        }
        private async void OnVoidTransactionClicked(object sender, EventArgs e)
        {

            bool confirm = await DisplayAlert("Confirm", "Are you sure you want to void this transaction?", "Yes", "No");
            if (!confirm)
                return;

            try
            {
                var storeId = Preferences.Get("Store", string.Empty);
                var company = Preferences.Get("Company", string.Empty);
                var cashier = Preferences.Get("UserId", string.Empty);

                // Create void transaction header
                var header = new POSRetailTransactionTable
                {
                    StoreId = storeId,
                    TerminalId = "POS1",
                    ShiftId = "Morning",
                    ShiftStaffId = cashier,
                    ReceiptId = $"VOID-{DateTime.Now:yyyyMMddHHmmss}",
                    BusinessDate = DateTime.Now,
                    Currency = "AED",
                    Total = AddedProducts.Sum(p => p.Total),
                    TransactionType = POSRetailTransactionTable.TransactionTypeEnum.Void
                };

                await _db.CreateTransactionTable(header);

                // Optionally, also store line items if you want record trail
                int lineNum = 1;
                foreach (var p in AddedProducts)
                {
                    var line = new POSRetailTransactionSalesTrans
                    {
                        TransactionId = header.TransactionId,
                        LineNum = lineNum++,
                        ItemId = p.ItemId,
                        Qty = p.Quantity,
                        UnitId = p.UnitId,
                        UnitPrice = p.UnitPrice,
                        NetAmount = 0,
                        TaxAmount = 0,
                        GrossAmount = 0,
                        DiscAmount = 0,
                        DiscAmountWithoutTax = 0
                    };
                    await _db.CreateTransactionSalesTrans(line);
                }

                await DisplayAlert("Success", "Transaction has been voided successfully!", "OK");

                // Reset UI
                AddedProducts.Clear();
                ProductsList.ItemsSource = null;
                ProductsList.ItemsSource = AddedProducts;
                PaymentLabel.Text = "0.0000";
                AmountDueLabel.Text = "0.0000";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to void transaction: {ex.Message}", "OK");
            }
           
        }

        private async Task RecordSaleAsync(string paymentMethod)
        {
            loaderOverlay.IsVisible = true;
            activityIndicator.IsRunning = true;

            // Filter only non-void products
            var activeProducts = AddedProducts.Where(p => !p.IsVoid).ToList();

            if (!activeProducts.Any())
            {
                await DisplayAlert("No items", "There are no items to record.", "OK");
                return;
            }

            try
            {
                var storeId = Preferences.Get("Store", string.Empty);
                var company = Preferences.Get("Company", string.Empty);
                var cashier = Preferences.Get("UserId", string.Empty);
                var paymentMethodId = paymentMethod; // "Cash" or "Card"

                decimal totalAmount = 0m;
                decimal totalTax = 0m;
                decimal totalExcludingVAT = 0m;

                decimal unitPrice = activeProducts.Sum(p => Math.Round(p.Total, 3));

                // Calculate totals considering tax
                foreach (var p in activeProducts)
                {
                    decimal taxAmount, netAmount, total;

                    if (p.PriceIncludeTax > 0) // Price already includes tax
                    {
                        taxAmount = Math.Round(p.Total - (p.Total / (1 + p.TaxFactor)), 3);
                        netAmount = Math.Round(p.Total / (1 + p.TaxFactor), 3);
                        total = Math.Round(p.Total, 3);
                    }
                    else // Price does not include tax
                    {
                        taxAmount = Math.Round(p.Total * p.TaxFactor, 3);
                        netAmount = Math.Round(p.Total, 3);
                        total = Math.Round(p.Total + taxAmount, 3);
                    }
                    totalExcludingVAT = Math.Round(totalExcludingVAT + netAmount, 3);
                    totalAmount = Math.Round(totalAmount + total, 3);
                    totalTax = Math.Round(totalTax + taxAmount, 3);
                }
                // Final rounding for totals before using them
                totalAmount = Math.Round(totalAmount, 3);
                totalTax = Math.Round(totalTax, 3);
                totalExcludingVAT = Math.Round(totalExcludingVAT, 3);

                // Create sale DTO using only active products
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
                            PaymentAmount = Math.Round(totalAmount,3)
                        }
                    },
                    Taxes = new List<RecordSalesService.TaxDto>
                    {
                        new RecordSalesService.TaxDto
                        {
                            TaxName = "VAT",
                            TaxRate = Math.Round((double)(activeProducts.FirstOrDefault()?.TaxFactor ?? 0.05m), 3),
                            TaxValue =  Math.Round(totalTax,3)
                        }
                    },
                    Items = activeProducts.Select(p => new RecordSalesService.ItemDto
                    {
                        ItemId = p.ItemId,
                        UnitId = p.UnitId,
                        UnitPrice = Math.Round(p.Total, 3),
                        Qty = Math.Round(p.Quantity, 3),
                        LineAmount = Math.Round(p.Total, 3),
                        TaxAmount = Math.Round(p.TaxAmount, 3),
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
                    // Save header
                    var header = new POSRetailTransactionTable
                    {
                        StoreId = storeId,
                        TerminalId = saleItem.TerminalId,
                        ShiftId = saleItem.ShiftId,
                        ShiftStaffId = cashier,
                        ReceiptId = saleItem.ReceiptId,
                        BusinessDate = DateTime.Now,
                        Currency = saleItem.Payments.FirstOrDefault()?.Currency ?? "AED",
                        Total = Math.Round(totalAmount, 3),
                        TransactionType = IsReturn ? POSRetailTransactionTable.TransactionTypeEnum.Return: POSRetailTransactionTable.TransactionTypeEnum.Sale
                    };

                    await _db.CreateTransactionTable(header);

                    // Save each line (only active products)
                    int lineNum = 1;
                    foreach (var p in activeProducts)
                    {
                        var line = new POSRetailTransactionSalesTrans
                        {
                            TransactionId = header.TransactionId,
                            LineNum = lineNum++,
                            ItemId = p.ItemId,
                            Qty = Math.Round(p.Quantity, 3),
                            UnitId = p.UnitId,
                            UnitPrice = Math.Round(p.Total, 3),
                            NetAmount = Math.Round(p.Total, 3),
                            TaxAmount = Math.Round(p.TaxAmount, 3),
                            GrossAmount = Math.Round(p.Subtotal, 3),
                            DiscAmount = 0,
                            DiscAmountWithoutTax = 0
                        };
                        await _db.CreateTransactionSalesTrans(line);
                    }

                    // Save payment
                    var payment = new POSRetailTransactionPaymentTrans
                    {
                        TransactionId = header.TransactionId,
                        PaymentDateTime = DateTime.Now,
                        PaymentMethod = paymentMethod,
                        PaymentType = "",
                        Currency = saleItem.Payments.FirstOrDefault()?.Currency ?? "AED",
                        PaymentAmount = Math.Round(totalAmount, 3)
                    };
                    await _db.CreateTransactionPaymentTrans(payment);

                    // Save tax
                    var tax = new POSRetailTransactionTaxTrans
                    {
                        TransactionId = header.TransactionId,
                        TaxName = saleItem.Taxes.FirstOrDefault()?.TaxName ?? "VAT",
                        TaxRate = Math.Round(saleItem.Taxes.FirstOrDefault()?.TaxRate ?? 0.05, 3),
                        TaxAmount = Math.Round(totalTax, 3)
                    };
                    await _db.CreateTransactionTaxTrans(tax);

                    await DisplayAlert("Success", $"Sale recorded successfully via {paymentMethod}!", "OK");

                    // Print receipt
                    PrintReceipt(storeId, cashier, saleItem.ReceiptId, totalAmount, totalTax, paymentMethodId, totalExcludingVAT);

                    // Reset UI
                    AddedProducts.Clear();
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
                activityIndicator.IsRunning = false;
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
