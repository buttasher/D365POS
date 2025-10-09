using CommunityToolkit.Maui.Views;
using D365POS.Services;

namespace D365POS.Popups;

public partial class PriceCheckPopup : Popup
{
    private readonly DatabaseService _dbService = new DatabaseService();

    public PriceCheckPopup()
    {
        InitializeComponent();
    }

    private async void OnCheckPriceClicked(object sender, EventArgs e)
    {
        string itemId = ItemIdEntry.Text?.Trim();

        if (string.IsNullOrEmpty(itemId))
        {
            await App.Current.MainPage.DisplayAlert("Error", "Please enter an Item ID", "OK");
            return;
        }

        try
        {
            loaderOverlay.IsVisible = true;
            await Task.Delay(300);

            var allUnits = await _dbService.GetAllProductsUnit();
            var matchingItems = allUnits
                .Where(x => x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            loaderOverlay.IsVisible = false;
            ResultFrame.IsVisible = true;
            ResultListLayout.Children.Clear();

            if (matchingItems.Any())
            {
                foreach (var item in matchingItems)
                {
                    var unitFrame = new Frame
                    {
                        BorderColor = Color.FromArgb("#A66E43"),
                        CornerRadius = 12,
                        Padding = new Thickness(10),
                        HasShadow = false,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 4,
                            Children =
                        {
                            new Label { Text = $"Unit: {item.UnitId}", FontSize = 16 },
                            new Label { Text = $"Price: {item.UnitPrice:C}", FontSize = 16, TextColor = Colors.DarkGreen }
                        }
                        }
                    };
                    ResultListLayout.Children.Add(unitFrame);
                }
            }
            else
            {
                ResultListLayout.Children.Add(new Label
                {
                    Text = $"No record found for Item ID: {itemId}",
                    FontSize = 16,
                    TextColor = Colors.Red
                });
            }

            await PopupFrame.FadeTo(0.9, 100);
            await Task.WhenAll(
            PopupFrame.FadeTo(1, 150),
            AnimatePopupHeight(220, 450, 250) // increased final height
            );

            ResultFrame.Opacity = 0;
            await ResultFrame.FadeTo(1, 250, Easing.CubicIn);
        }
        catch (Exception ex)
        {
            loaderOverlay.IsVisible = false;
            await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task AnimatePopupHeight(double from, double to, uint duration)
    {
        var animation = new Animation(h => PopupFrame.HeightRequest = h, from, to);
        animation.Commit(PopupFrame, "PopupResize", 16, duration, Easing.CubicInOut);
        await Task.Delay((int)duration);
    }
}
