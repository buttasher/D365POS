using CommunityToolkit.Maui.Views;
using D365POS.Popups;
using Microsoft.Maui.Controls;
using System;
using System.Data; // Needed for DataTable.Compute

namespace D365POS
{
    [QueryProperty(nameof(AmountDue), "AmountDue")]
    public partial class PayCashPage : ContentPage
    {
        private decimal _amountDue;

        public string AmountDue
        {
            set
            {
                if (decimal.TryParse(value, out var result))
                {
                    _amountDue = result;
                    AmountDueLabel.Text = _amountDue.ToString("N3"); // update UI
                }
            }
        }

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

                // Handle + or - press for instant calculation
                if (value == "+" || value == "-")
                {
                    if (decimal.TryParse(PaymentEntry.Text, out decimal currentValue))
                    {
                        // Store last entered value if needed later (optional)
                    }

                    // If entry already contains + or -, split and calculate
                    if (PaymentEntry.Text.Contains("+") || PaymentEntry.Text.Contains("-"))
                    {
                        try
                        {
                            var resultObj = new System.Data.DataTable().Compute(PaymentEntry.Text, "");
                            PaymentEntry.Text = Convert.ToDecimal(resultObj).ToString("N0");
                        }
                        catch
                        {
                            PaymentEntry.Text = "0";
                        }
                    }

                    // Append + or - for next entry
                    PaymentEntry.Text += value;
                    return;
                }

                // Only one decimal per number
                if (value == "." && PaymentEntry.Text.EndsWith("."))
                    return;

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
            try
            {
                if (string.IsNullOrWhiteSpace(PaymentEntry.Text))
                {
                    await DisplayAlert("Error", "Please enter an amount.", "OK");
                    return;
                }

                // Compute + or - operations before validating
                decimal payment;
                try
                {
                    var resultObj = new System.Data.DataTable().Compute(PaymentEntry.Text, "");
                    payment = Convert.ToDecimal(resultObj);
                }
                catch
                {
                    await DisplayAlert("Error", "Invalid amount entered.", "OK");
                    return;
                }

                if (payment < _amountDue)
                {
                    await DisplayAlert("Error", "Entered amount cannot be smaller than amount due.", "OK");
                    return;
                }

                decimal newAmountDue = payment - _amountDue;

                var navParams = new Dictionary<string, object>
                {
                    { "PaymentAmount", payment },
                    { "NewAmountDue", newAmountDue }
                };

                await Shell.Current.GoToAsync("..", navParams);

                var popup = new ConfirmationPopup();
                await this.ShowPopupAsync(popup);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Something went wrong: {ex.Message}", "OK");
            }
        }

        private async void OnDenominationClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                string text = button.Text.Replace(",", "");
                if (decimal.TryParse(text, out decimal payment))
                {
                    // Check if payment is smaller than amount due
                    if (payment < _amountDue)
                    {
                        await DisplayAlert("Error", "Selected amount cannot be smaller than amount due.", "OK");
                        return; // stop further execution
                    }

                    decimal newAmountDue = payment - _amountDue;

                    var navParams = new Dictionary<string, object>
                    {
                        { "PaymentAmount", payment },
                        { "NewAmountDue", newAmountDue }
                    };

                    await Shell.Current.GoToAsync("..", navParams);

                    var popup = new ConfirmationPopup();
                    await this.ShowPopupAsync(popup);
                }
            }
        }
    }
}
