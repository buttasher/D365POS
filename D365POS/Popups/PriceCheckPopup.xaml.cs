using CommunityToolkit.Maui.Views;
using D365POS.Services;
using D365POS.Models;

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
        string barcode = ItemIdEntry.Text?.Trim();

        if (string.IsNullOrEmpty(barcode))
        {
            await App.Current.MainPage.DisplayAlert("Error", "Please enter or scan a barcode.", "OK");
            return;
        }

        try
        {
            loaderOverlay.IsVisible = true;
            await Task.Delay(300);

            // 1️⃣ Find product by Barcode
            var allProducts = await _dbService.GetAllProducts();
            var product = allProducts.FirstOrDefault(p =>
                p.ItemBarCode.Equals(barcode, StringComparison.OrdinalIgnoreCase));

            loaderOverlay.IsVisible = false;
            ResultFrame.IsVisible = true;
            ResultListLayout.Children.Clear();

            if (product == null)
            {
                ResultListLayout.Children.Add(new Label
                {
                    Text = $"No product found for barcode: {barcode}",
                    FontSize = 16,
                    TextColor = Colors.Red
                });
                return;
            }

            // 2️⃣ Get matching ItemId and find prices
            var allUnits = await _dbService.GetAllProductsUnit();
            var matchingUnits = allUnits
                .Where(x => x.ItemId.Equals(product.ItemId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // 3️⃣ Display results
            if (matchingUnits.Any())
            {
                foreach (var unit in matchingUnits)
                {
                    var frame = new Frame
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
                                new Label { Text = $"Item: {product.Description}", FontSize = 16, FontAttributes = FontAttributes.Bold },
                                new Label { Text = $"UOM: {unit.UnitId}", FontSize = 16 },
                                new Label { Text = $"Price: {unit.UnitPrice:N3}", FontSize = 16, TextColor = Colors.DarkGreen }
                            }
                        }
                    };

                    ResultListLayout.Children.Add(frame);
                }
            }
            else
            {
                ResultListLayout.Children.Add(new Label
                {
                    Text = $"No price found for Item ID: {product.ItemId}",
                    FontSize = 16,
                    TextColor = Colors.Red
                });
            }
        }
        catch (Exception ex)
        {
            loaderOverlay.IsVisible = false;
            await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
