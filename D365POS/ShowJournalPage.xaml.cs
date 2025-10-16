using D365POS.Models;
using D365POS.Services;

namespace D365POS;

public partial class ShowJournalPage : ContentPage
{
    private readonly DatabaseService _db;
    private int _selectedTransactionId;
    private bool _isPaymentsTabActive = false;

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

        if (_isPaymentsTabActive)
            await LoadPaymentsTab();
        else
            await LoadLinesTab();
    }

    private async void OnLinesTabClicked(object sender, TappedEventArgs e)
    {
        _isPaymentsTabActive = false;
        SetTabState(linesActive: true);
        SetLinesHeader();
        await LoadLinesTab();
        if (_selectedTransactionId != 0)
        {
            await LoadLinesTab();
        }    
            
    }

    private async void OnPaymentsTabClicked(object sender, TappedEventArgs e)
    {
        _isPaymentsTabActive = true;
        SetTabState(linesActive: false);
        SetPaymentsHeader();

        if (_selectedTransactionId != 0)
        {
            await LoadPaymentsTab();
        }    
         
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
            Col3 = l.GrossAmount.ToString("F2")
        }).ToList();
    }

    private async Task LoadPaymentsTab()
    {
        var payments = await _db.GetListAsync<POSRetailTransactionPaymentTrans>(x => x.TransactionId == _selectedTransactionId);

        DetailsList.ItemsSource = payments.Select(p => new
        {
            Col1 = p.PaymentMethod,
            Col2 = p.Currency,
            Col3 = p.PaymentAmount.ToString("F2")
        }).ToList();
    }

    private void SetTabState(bool linesActive)
    {
        // Define your active tab color
        Color activeColor = Color.FromArgb("#A66E43");

        // Visual states for tabs
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
}
