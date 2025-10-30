using CommunityToolkit.Maui.Views;
using D365POS.Services;
using Microsoft.Maui.Controls;
using D365POS.Popups;

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
        loaderOverlay.IsVisible = true;
        activityIndicator.IsRunning = true;
        await Task.Delay(100);

        try
        {
            var salesPage = _serviceProvider.GetRequiredService<SalesPage>();
            await Navigation.PushAsync(salesPage); 
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to navigate to Sales Page: {ex.Message}", "OK");
        }
        finally
        {
            loaderOverlay.IsVisible = false;
            activityIndicator.IsRunning = false; 
        }
    }
    private async void OnShowJournalClicked(object sender, EventArgs e)
    {
        loaderOverlay.IsVisible = true;
        activityIndicator.IsRunning = true;
        await Task.Delay(100);

        try
        {
            var journalPage = _serviceProvider.GetRequiredService<ShowJournalPage>();
            await Navigation.PushAsync(journalPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to navigate to Journal Page: {ex.Message}", "OK");
        }
        finally
        {
            loaderOverlay.IsVisible = false;
            activityIndicator.IsRunning = false;
        }
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
    private async void OnAddPrinterNameClicked(object sender, EventArgs e)
    {
        string currentPrinter = Preferences.Get("PrinterName", "");
        string result = await DisplayPromptAsync("Add Printer", "Enter your printer name:", initialValue: currentPrinter);

        if (!string.IsNullOrWhiteSpace(result))
        {
            Preferences.Set("PrinterName", result);
            await DisplayAlert("Success", $"Printer name '{result}' saved successfully.", "OK");
        }
    }
}