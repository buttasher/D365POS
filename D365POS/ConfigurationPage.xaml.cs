using Microsoft.Maui.Storage;

namespace D365POS;

public partial class ConfigurationPage : ContentPage
{
    public ConfigurationPage()
    {
        InitializeComponent();
        LoadExistingConfig();
    }

    private void LoadExistingConfig()
    {
        EnvironmentEntry.Text = Preferences.Get("Resource", "");
        TenantEntry.Text = Preferences.Get("TenantId", "");
        ClientIdEntry.Text = Preferences.Get("ClientId", "");
        ClientSecretEntry.Text = Preferences.Get("ClientSecret", "");
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        Preferences.Set("Resource", EnvironmentEntry.Text.Trim());
        Preferences.Set("TenantId", TenantEntry.Text.Trim());
        Preferences.Set("ClientId", ClientIdEntry.Text.Trim());
        Preferences.Set("ClientSecret", ClientSecretEntry.Text.Trim());

        await DisplayAlert("Saved", "Configuration saved successfully.", "OK");
        await Navigation.PopAsync();
    }
}
