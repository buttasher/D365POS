namespace D365POS;

public partial class DashboardPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public DashboardPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    private async void OnSaleClicked(object sender, EventArgs e)
    {
        // Resolve SalesPage from DI
        var salesPage = _serviceProvider.GetRequiredService<SalesPage>();
        await Navigation.PushAsync(salesPage);
    }
    private async void OnShowJournalClicked(object sender, EventArgs e)
    {
        var journalPage = _serviceProvider.GetRequiredService<ShowJournalPage>();
        await Navigation.PushAsync(journalPage);
    }
}