using CommunityToolkit.Maui.Views;

namespace D365POS.Popups;

public partial class ConfirmationPopup : Popup
{
    private decimal _paymentAmount;
    private decimal _remainingAmount;

    public ConfirmationPopup(decimal paymentAmount = 0, decimal remainingAmount = 0)
    {
        InitializeComponent();

        _paymentAmount = paymentAmount;
        _remainingAmount = remainingAmount;

        PaymentLabel.Text = $"Payment: {_paymentAmount:F2}";
        RemainingLabel.Text = $"Remaining: {_remainingAmount:F2}";
    }

    private async void OnOkClicked(object sender, EventArgs e)
    {
        Close(true);
    }
    
}