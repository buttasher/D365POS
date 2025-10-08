using D365POS.Services;
using Microsoft.Maui.Controls;
using System;

namespace D365POS
{
    public partial class SignInPage : ContentPage
    {
        public SignInPage()
        {
            InitializeComponent();
            StartClock();
        }

        private void StartClock()
        {
            Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                lblTime.Text = DateTime.Now.ToString("hh:mm tt");  
                lblDate.Text = DateTime.Now.ToString("MM/dd/yyyy");
                return true; 
            });
        }
        private async void OnSignInClicked(object sender, EventArgs e)
        {
            loaderOverlay.IsVisible = true;
            activityIndicator.IsRunning = true;
            SignInButton.IsEnabled = false;

            var userId = UserIdEntry.Text;
            var password = PasswordEntry.Text;
            var userService = new UserService();
            var storeService = new GetStoreService();
            var productSyncService = new ProductSyncService();

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            try
            {
                var result = await userService.SignInAsync(userId, password, cts.Token);

                if (result != null && result.Status?.Equals("Success", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Preferences.Set("UserId", userId);

                    var storeResult = await storeService.GetStoreAsync(userId, result.CompanyList?.FirstOrDefault());
                    if (storeResult != null && storeResult.Status?.Equals("Success", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var store = storeResult.Warehouse?.FirstOrDefault();
                        Preferences.Set("Store", store);
                        Preferences.Set("Company", result.CompanyList?.FirstOrDefault());

                        
                        await productSyncService.SyncProductsAsync(result.CompanyList?.FirstOrDefault(), store);
                    }

                    // Navigate to Dashboard
                    await Shell.Current.GoToAsync(nameof(DashboardPage));
                }
                else
                {
                    await DisplayAlert("Sign In Failed", result?.Message ?? "Unknown error", "OK");
                }
            }
            catch (OperationCanceledException)
            {
                await DisplayAlert("Timeout", "Server is not responding. Please try again.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                loaderOverlay.IsVisible = false;
                activityIndicator.IsRunning = false;
                SignInButton.IsEnabled = true;
            }
        }

    }
}
