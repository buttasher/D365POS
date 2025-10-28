using D365POS.Models;
using D365POS.Services;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Collections.ObjectModel;

namespace D365POS;

public partial class ShowJournalPage : ContentPage
{
    private readonly DatabaseService _db;
    private int _selectedTransactionId;
    private bool _isPaymentsTabActive = false;
    private List<POSRetailTransactionSalesTrans> _selectedLines = new();


    private decimal _subtotal;
    public decimal Subtotal
    {
        get => _subtotal;
        set { _subtotal = value; OnPropertyChanged(nameof(Subtotal)); }
    }

    private decimal _tax;
    public decimal Tax
    {
        get => _tax;
        set { _tax = value; OnPropertyChanged(nameof(Tax)); }
    }

    private decimal _total;
    public decimal Total
    {
        get => _total;
        set { _total = value; OnPropertyChanged(nameof(Total)); }
    }

    public ShowJournalPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
        BindingContext = this;
        SetTabState(linesActive: true);

        // Initialize selection handling
        DetailsList.SelectionMode = SelectionMode.Multiple;
        DetailsList.SelectionChanged += OnLineSelected;
        SetLinesTemplate();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadJournalList();
    }

    private async Task LoadJournalList()
    {
        var journals = await _db.GetAllTransactions();
        JournalList.ItemsSource = journals;
    }

    private async void OnJournalSelected(object sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection.FirstOrDefault() as POSRetailTransactionTable;
        if (selected == null) return;

        _selectedTransactionId = selected.TransactionId;
        await LoadTotalsAsync();

        if (_isPaymentsTabActive)
            await LoadPaymentsTab();
        else
            await LoadLinesTab();
    }

    //Updated Selection Handler
    private void OnLineSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_isPaymentsTabActive || DetailsList.SelectionMode == SelectionMode.None)
            return;

        _selectedLines = e.CurrentSelection.Cast<POSRetailTransactionSalesTrans>().ToList();
    }

    private async void OnReturnClicked(object sender, EventArgs e)
    {
        try
        {
            if (_selectedTransactionId == 0)
            {
                await DisplayAlert("No Selection", "Please select a transaction first.", "OK");
                return;
            }

            if (_isPaymentsTabActive)
            {
                await DisplayAlert("Invalid Action", "Please switch to Lines tab to select items to return.", "OK");
                return;
            }

            if (_selectedLines == null || !_selectedLines.Any())
            {
                await DisplayAlert("No Line Selected", "Please select at least one line to return.", "OK");
                return;
            }

            var transaction = await _db.GetAsync<POSRetailTransactionTable>(x => x.TransactionId == _selectedTransactionId);

            if (transaction == null)
            {
                await DisplayAlert("Error", "Transaction not found.", "OK");
                return;
            }

            if (transaction.TransactionType == POSRetailTransactionTable.TransactionTypeEnum.Return)
            {
                await DisplayAlert("Invalid Operation", "You cannot return a transaction of type RETURN.", "OK");
                return;
            }

            // Pass only selected lines to return page
            await Shell.Current.GoToAsync(nameof(SalesPage), new Dictionary<string, object>
            {
                { "IsReturn", true },
                { "ReturnTransactionId", _selectedTransactionId },
                { "ReturnLines", _selectedLines }
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to initiate return: {ex.Message}", "OK");
        }
    }
    private void SetLinesHeader()
    {
        HeaderCol1.Text = "Description";
        HeaderCol2.Text = "Quantity";
        HeaderCol3.Text = "Price";
    }

    private void SetPaymentsHeader()
    {
        HeaderCol1.Text = "Payment Method";
        HeaderCol2.Text = "Currency";
        HeaderCol3.Text = "Amount";
    }
    private void SetLinesTemplate()
    {
        DetailsList.ItemTemplate = new DataTemplate(() =>
        {
            var grid = new Grid
            {
                Padding = 10,
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
            };

            var lblDesc = new Label();
            lblDesc.SetBinding(Label.TextProperty, "ItemDescription");
            grid.Add(lblDesc, 0, 0);

            var lblQty = new Label { HorizontalTextAlignment = TextAlignment.Center };
            lblQty.SetBinding(Label.TextProperty, new Binding("Qty", stringFormat: "{0:N3}"));
            grid.Add(lblQty, 1, 0);

            var lblPrice = new Label { HorizontalTextAlignment = TextAlignment.End };
            lblPrice.SetBinding(Label.TextProperty, new Binding("UnitPrice", stringFormat: "{0:N3}"));
            grid.Add(lblPrice, 2, 0);

            return grid;
        });
    }

    private void SetPaymentsTemplate()
    {
        DetailsList.ItemTemplate = new DataTemplate(() =>
        {
            var grid = new Grid
            {
                Padding = 10,
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
            };

            var lblMethod = new Label();
            lblMethod.SetBinding(Label.TextProperty, "Col1");
            grid.Add(lblMethod, 0, 0);

            var lblCurrency = new Label { HorizontalTextAlignment = TextAlignment.Center };
            lblCurrency.SetBinding(Label.TextProperty, "Col2");
            grid.Add(lblCurrency, 1, 0);

            var lblAmount = new Label { HorizontalTextAlignment = TextAlignment.End };
            lblAmount.SetBinding(Label.TextProperty, "Col3");
            grid.Add(lblAmount, 2, 0);

            return grid;
        });
    }

    private async void OnLinesTabClicked(object sender, TappedEventArgs e)
    {
        _isPaymentsTabActive = false;
        SetTabState(linesActive: true);
        SetLinesHeader();
        SetLinesTemplate();
        if (_selectedTransactionId != 0)
            await LoadLinesTab();
        DetailsList.SelectionMode = SelectionMode.Multiple;
    }

    private async void OnPaymentsTabClicked(object sender, TappedEventArgs e)
    {
        _isPaymentsTabActive = true;
        SetTabState(linesActive: false);
        SetPaymentsHeader();
        SetPaymentsTemplate();
        if (_selectedTransactionId != 0)
            await LoadPaymentsTab();
        DetailsList.SelectionMode = SelectionMode.None;
    }

    private async Task LoadTotalsAsync()
    {
        try
        {
            var lines = await _db.GetListAsync<POSRetailTransactionSalesTrans>(x => x.TransactionId == _selectedTransactionId);
            var taxTrans = await _db.GetListAsync<POSRetailTransactionTaxTrans>(x => x.TransactionId == _selectedTransactionId);

            Subtotal = lines.Sum(l => l.NetAmount * l.Qty);
            Tax = taxTrans.Sum(t => t.TaxAmount);
            Total = lines.Sum(l => l.GrossAmount);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load totals: {ex.Message}", "OK");
        }
    }

   

    // Updated LoadLinesTab for binding selection
    private async Task LoadLinesTab()
    {
        var lines = await _db.GetListAsync<POSRetailTransactionSalesTrans>(x => x.TransactionId == _selectedTransactionId);
        var products = await _db.GetAllProducts();

        foreach (var line in lines)
        {
            var product = products.FirstOrDefault(p => p.ItemId == line.ItemId);
            line.ItemDescription = product?.Description ?? "N/A";
            line.UnitPrice = line.UnitPrice;
        }

        DetailsList.ItemsSource = lines;
    }

    private async Task LoadPaymentsTab()
    {
        var payments = await _db.GetListAsync<POSRetailTransactionPaymentTrans>(x => x.TransactionId == _selectedTransactionId);

        var paymentDisplay = payments.Select(p => new
        {
            Col1 = p.PaymentMethod,
            Col2 = p.Currency,
            Col3 = p.PaymentAmount.ToString("N3")
        }).ToList();

        DetailsList.ItemsSource = paymentDisplay;
    }

    private void SetTabState(bool linesActive)
    {
        Color activeColor = Color.FromArgb("#A66E43");

        LinesTabColor = linesActive ? activeColor : Colors.Gray;
        PaymentsTabColor = linesActive ? Colors.Gray : activeColor;
        LinesUnderlineColor = linesActive ? activeColor : Colors.Transparent;
        PaymentsUnderlineColor = linesActive ? Colors.Transparent : activeColor;

        OnPropertyChanged(nameof(LinesTabColor));
        OnPropertyChanged(nameof(PaymentsTabColor));
        OnPropertyChanged(nameof(LinesUnderlineColor));
        OnPropertyChanged(nameof(PaymentsUnderlineColor));
    }

    public Color LinesTabColor { get; set; }
    public Color PaymentsTabColor { get; set; }
    public Color LinesUnderlineColor { get; set; }
    public Color PaymentsUnderlineColor { get; set; }

    // ------------------ PRINT RECEIPT FUNCTIONALITY ------------------

    private async void OnPrintReceiptClicked(object sender, EventArgs e)
    {
        if (_selectedTransactionId == 0)
        {
            await DisplayAlert("No Selection", "Please select a transaction first.", "OK");
            return;
        }

        var lines = await _db.GetListAsync<POSRetailTransactionSalesTrans>(x => x.TransactionId == _selectedTransactionId);
        var taxLines = await _db.GetListAsync<POSRetailTransactionTaxTrans>(x => x.TransactionId == _selectedTransactionId);
        var payments = await _db.GetListAsync<POSRetailTransactionPaymentTrans>(x => x.TransactionId == _selectedTransactionId);

        if (lines == null || !lines.Any())
        {
            await DisplayAlert("No Lines", "No items found for this transaction.", "OK");
            return;
        }

        decimal subtotal = lines.Sum(l => l.NetAmount * l.Qty);
        decimal tax = taxLines.Sum(t => t.TaxAmount);
        decimal total = lines.Sum(l => l.GrossAmount);
        decimal totalWithoutVAT = subtotal;
        string paymentMethod = payments.FirstOrDefault()?.PaymentMethod ?? "Cash";


        string receiptId = $"INV-{_selectedTransactionId}";
        var storeId = Preferences.Get("Store", string.Empty);
        var cashier = Preferences.Get("UserId", string.Empty);

        string receiptContent = BuildReceiptText(lines, subtotal, tax, total, paymentMethod, receiptId, storeId, cashier, totalWithoutVAT);
        string pdfPath = GenerateReceiptPDF(receiptId, receiptContent);

        try
        {
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(pdfPath)
            });
        }
        catch { }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(5000);
                if (File.Exists(pdfPath))
                    File.Delete(pdfPath);
            }
            catch { }
        });
    }

    private string BuildReceiptText(
        IEnumerable<POSRetailTransactionSalesTrans> lines,
        decimal subtotal,
        decimal tax,
        decimal total,
        string paymentMethod,
        string receiptId,
        string storeId,
        string cashier,
        decimal totalWithoutVAT)
    {
        string receipt = "";

        receipt += "\x1B\x40"; // ESC @ Initialize printer
        receipt += AlignText("Tax Invoice", 48, "center") + "\n";
        receipt += AlignText("YASIR DOURI FOODSTUFF TRADING COMPANY L.L.C", 48, "center") + "\n";
        receipt += AlignText("Oman", 48, "center") + "\n\n";

        receipt += AlignText($"Receipt No: {receiptId}", 48, "left") + "\n";
        receipt += AlignText($"Store Id: {storeId}", 48, "left") + "\n";
        receipt += AlignText($"Cashier Id: {cashier}", 48, "left") + "\n";
        receipt += AlignText($"Date: {DateTime.Now:dd-MM-yyyy}", 48, "right") + "\n";
        receipt += AlignText($"Time: {DateTime.Now:hh:mm tt}", 48, "right") + "\n\n";

        receipt += AlignText("Item", 20, "left") +
                   AlignText("Qty", 10, "center") +
                   AlignText("Price", 10, "center") +
                   AlignText("Amt", 10, "right") + "\n";
        receipt += "-----------------------------------------------\n";

        foreach (var l in lines)
        {
            var wrappedNameLines = WrapText(l.ItemId, 20).ToList();
            for (int i = 0; i < wrappedNameLines.Count; i++)
            {
                string itemName = AlignText(wrappedNameLines[i], 20, "left");
                string qty = i == 0 ? AlignText(l.Qty.ToString(), 10, "center") : AlignText("", 10, "center");
                string price = i == 0 ? AlignText(l.NetAmount.ToString("F2"), 10, "center") : AlignText("", 10, "center");
                string amt = i == 0 ? AlignText(l.GrossAmount.ToString("F2"), 10, "right") : AlignText("", 10, "right");

                receipt += $"{itemName}{qty}{price}{amt}\n";
            }
        }

        receipt += "-----------------------------------------------\n";

        receipt += AlignText($"Total Excluding VAT: {totalWithoutVAT:F3}", 48, "left") + "\n";
        receipt += AlignText($"VAT 5% Included: {tax:F3}", 48, "left") + "\n";
        receipt += AlignText($"Total Payable: {total:F3}", 48, "left") + "\n";
        receipt += AlignText($"Payment Method: {paymentMethod}", 48, "left") + "\n\n";

        receipt += AlignText("Keep the bill for exchangeable Items.", 48, "left") + "\n";
        receipt += AlignText("Exchange within 2 days.", 48, "left") + "\n";
        receipt += AlignText("No exchange for fresh & frozen foods.", 48, "left") + "\n";
        receipt += AlignText("Item should be returned in original packaging.", 48, "left") + "\n";
        receipt += "\n" + AlignText("Thank you for shopping!", 48, "center") + "\n\n";

        receipt += "\x1D\x56\x42\x03"; // GS V B 3 Cut paper

        return receipt;
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

    private string GenerateReceiptPDF(string receiptId, string content)
    {
        string folderPath = FileSystem.AppDataDirectory;
        string filePath = Path.Combine(folderPath, $"Receipt_{receiptId}.pdf");

        using (PdfDocument document = new PdfDocument())
        {
            var page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Courier New", 10, XFontStyle.Regular);

            double y = 20;
            double lineHeight = 14;

            foreach (string line in content.Split('\n'))
            {
                gfx.DrawString(line, font, XBrushes.Black, new XRect(20, y, page.Width - 40, lineHeight), XStringFormats.TopLeft);
                y += lineHeight;

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

}
