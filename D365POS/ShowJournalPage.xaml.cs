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
        DetailsHeaderGrid.Children.Clear();
        DetailsHeaderGrid.ColumnDefinitions.Clear();
        DetailsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());
        DetailsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());
        DetailsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());

        var label1 = new Label { Text = "ITEM ID", FontAttributes = FontAttributes.Bold };
        var label2 = new Label { Text = "QUANTITY", FontAttributes = FontAttributes.Bold };
        var label3 = new Label { Text = "PRICE", FontAttributes = FontAttributes.Bold };

        Grid.SetColumn(label2, 1);
        Grid.SetColumn(label3, 2);

        DetailsHeaderGrid.Children.Add(label1);
        DetailsHeaderGrid.Children.Add(label2);
        DetailsHeaderGrid.Children.Add(label3);
    }

    private void SetPaymentsHeader()
    {
        DetailsHeaderGrid.Children.Clear();
        DetailsHeaderGrid.ColumnDefinitions.Clear();
        DetailsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());
        DetailsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());
        DetailsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());

        var payLabel1 = new Label { Text = "PAYMENT METHOD", FontAttributes = FontAttributes.Bold };
        var payLabel2 = new Label { Text = "CURRENCY", FontAttributes = FontAttributes.Bold };
        var payLabel3 = new Label { Text = "AMOUNT", FontAttributes = FontAttributes.Bold };

        Grid.SetColumn(payLabel2, 1);
        Grid.SetColumn(payLabel3, 2);

        DetailsHeaderGrid.Children.Add(payLabel1);
        DetailsHeaderGrid.Children.Add(payLabel2);
        DetailsHeaderGrid.Children.Add(payLabel3);
    }
    private async Task LoadLinesTab()
    {
        var lines = await _db.GetListAsync<POSRetailTransactionSalesTrans>(x => x.TransactionId == _selectedTransactionId);

        DetailsList.ItemsSource = lines.Select(l => new
        {
            Col1 = l.ItemId,
            Col2 = l.Qty,
            Col3 = l.UnitPrice.ToString("F2")
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
        // Visual states for tabs
        LinesTabColor = linesActive ? Colors.Black : Colors.Gray;
        PaymentsTabColor = linesActive ? Colors.Gray : Colors.Black;
        LinesUnderlineColor = linesActive ? Colors.Black : Colors.Transparent;
        PaymentsUnderlineColor = linesActive ? Colors.Transparent : Colors.Black;
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
