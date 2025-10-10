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
                    AmountDueLabel.Text = _amountDue.ToString("N2"); // update UI
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

                // Prevent duplicate operators
                if ("+-*/".Contains(value))
                {
                    if (PaymentEntry.Text.EndsWith("+") || PaymentEntry.Text.EndsWith("-") ||
                        PaymentEntry.Text.EndsWith("*") || PaymentEntry.Text.EndsWith("/"))
                        return; // skip duplicate operator
                }

                // Only one decimal per number group
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
                string expression = PaymentEntry.Text;

                if (string.IsNullOrWhiteSpace(expression))
                {
                    await DisplayAlert("Error", "Please enter an amount.", "OK");
                    return;
                }

                // Evaluate the arithmetic expression (like calculator)
                var resultObj = new DataTable().Compute(expression, "");
                decimal result = Convert.ToDecimal(resultObj);

                // Show result in the entry field too
                PaymentEntry.Text = result.ToString("N2");
            }
            catch
            {
                await DisplayAlert("Error", "Invalid input. Please enter a valid amount or expression.", "OK");
                PaymentEntry.Text = "0";
            }
        }
        private async void OnDenominationClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                string text = button.Text.Replace(",", "");
                if (decimal.TryParse(text, out decimal payment))
                {
                  
                    decimal newAmountDue = payment - _amountDue;
                    if (newAmountDue < 0)
                    {
                        newAmountDue = 0;
                    } 
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
