using Microsoft.Maui.Controls;

namespace PaystackPaymentApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnPackageSelected(object sender, EventArgs e)
        {
            Button selectedButton = (Button)sender;
            string selectedPackage = selectedButton.CommandParameter.ToString();
            string amount = "";
            string details = "";

            // Set amount and details based on the selected package
            switch (selectedPackage)
            {
                case "Starter":
                    amount = "$19.99";
                    details = "Basic features included.";
                    break;

                case "Premium":
                    amount = "$39.99";
                    details = "More advanced features included.";
                    break;

                case "Advance":
                    amount = "$59.99";
                    details = "Full access to all features.";
                    break;
            }

            // Pass the selected package, amount, and details to the PaymentPage
            await Navigation.PushAsync(new PaymentPage(selectedPackage, amount, details));
        }
    }
}
