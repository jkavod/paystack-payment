using Microsoft.Maui.Controls;

namespace PaystackPaymentApp
{
    public partial class AwaitingConfirmationPage : ContentPage
    {
        public AwaitingConfirmationPage()
        {
            InitializeComponent();
            Content = new StackLayout
            {
                Children = {
                    new Label {
                        Text = "Awaiting Confirmation...",
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        VerticalOptions = LayoutOptions.CenterAndExpand
                    }
                }
            };
        }
    }
}
