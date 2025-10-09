namespace D365POS;

public partial class PayCashPage : ContentPage
{
	public PayCashPage()
	{
		InitializeComponent();
	}
    private void OnKeypadButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            var value = button.Text;

            if (PaymentEntry.Text == "0")
                PaymentEntry.Text = string.Empty;

            PaymentEntry.Text += value;
        }
    }

    private void OnBackspaceClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(PaymentEntry.Text))
        {
            PaymentEntry.Text = PaymentEntry.Text[..^1]; 

            if (string.IsNullOrEmpty(PaymentEntry.Text))
                PaymentEntry.Text = "0";
        }
    }

    private async void OnEnterClicked(object sender, EventArgs e)
    {
        string amount = PaymentEntry.Text;
        await DisplayAlert("Payment Entered", $"You entered: {amount}", "OK");
    }
}