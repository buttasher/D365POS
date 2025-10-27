using D365POS.Models;
using D365POS.Services;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace D365POS;

public partial class ShowJournalPage : ContentPage
{
    private readonly DatabaseService _db;
    private int _selectedTransactionId;
    private bool _isPaymentsTabActive = false;

    private decimal _subtotal;
    public decimal Subtotal
    {
        get => _subtotal;
        set
        {
            _subtotal = value;
            OnPropertyChanged(nameof(Subtotal));
        }
    }

    private decimal _tax;
    public decimal Tax
    {
        get => _tax;
        set
        {
            _tax = value;
            OnPropertyChanged(nameof(Tax));
        }
    }

    private decimal _total;
    public decimal Total
    {
        get => _total;
        set
        {
            _total = value;
            OnPropertyChanged(nameof(Total));
        }
    }

    public ShowJournalPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
        BindingContext = this;
        SetTabState(linesActive: true);
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

    private async void OnReturnClicked(object sender, EventArgs e)
    {
        try
        {
            if (_selectedTransactionId == 0)
            {
                await DisplayAlert("No Selection", "Please select a transaction first.", "OK");
                return;
            }

            var lines = await _db.GetListAsync<POSRetailTransactionSalesTrans>(
                x => x.TransactionId == _selectedTransactionId);

            if (lines == null || !lines.Any())
            {
                await DisplayAlert("No Lines", "No items found for this transaction.", "OK");
                return;
            }

            await Shell.Current.GoToAsync(nameof(SalesPage), new Dictionary<string, object>
            {
                { "IsReturn", true },
                { "ReturnTransactionId", _selectedTransactionId }
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to initiate return: {ex.Message}", "OK");
        }
    }

    private async Task LoadTotalsAsync()
    {
        try
        {
            var lines = await _db.GetListAsync<POSRetailTransactionSalesTrans>(
                x => x.TransactionId == _selectedTransactionId);

            var taxTrans = await _db.GetListAsync<POSRetailTransactionTaxTrans>(
                x => x.TransactionId == _selectedTransactionId);

            Subtotal = lines.Sum(l => l.NetAmount * l.Qty);
            Tax = taxTrans.Sum(t => t.TaxAmount);
            Total = lines.Sum(l => l.GrossAmount);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load totals: {ex.Message}", "OK");
        }
    }

    private async void OnLinesTabClicked(object sender, TappedEventArgs e)
    {
        _isPaymentsTabActive = false;
        SetTabState(linesActive: true);
        SetLinesHeader();
        if (_selectedTransactionId != 0)
            await LoadLinesTab();
    }

    private async void OnPaymentsTabClicked(object sender, TappedEventArgs e)
    {
        _isPaymentsTabActive = true;
        SetTabState(linesActive: false);
        SetPaymentsHeader();
        if (_selectedTransactionId != 0)
            await LoadPaymentsTab();
    }

    private void SetLinesHeader()
    {
        HeaderCol1.Text = "ITEM ID";
        HeaderCol2.Text = "QUANTITY";
        HeaderCol3.Text = "PRICE";
    }

    private void SetPaymentsHeader()
    {
        HeaderCol1.Text = "PAYMENT METHOD";
        HeaderCol2.Text = "CURRENCY";
        HeaderCol3.Text = "AMOUNT";
    }

    private async Task LoadLinesTab()
    {
        var lines = await _db.GetListAsync<POSRetailTransactionSalesTrans>(x => x.TransactionId == _selectedTransactionId);

        DetailsList.ItemsSource = lines.Select(l => new
        {
            Col1 = l.ItemId,
            Col2 = l.Qty,
            Col3 = l.NetAmount
        }).ToList();
    }

    private async Task LoadPaymentsTab()
    {
        var payments = await _db.GetListAsync<POSRetailTransactionPaymentTrans>(x => x.TransactionId == _selectedTransactionId);

        DetailsList.ItemsSource = payments.Select(p => new
        {
            Col1 = p.PaymentMethod,
            Col2 = p.Currency,
            Col3 = p.PaymentAmount.ToString("N3")
        }).ToList();
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

    // Bindable properties for colors
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
