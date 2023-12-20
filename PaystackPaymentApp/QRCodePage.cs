using Microsoft.Maui.Controls;

namespace PaystackPaymentApp
{
    public class QRCodePage : ContentPage
    {
        public QRCodePage(string qrCodeUrl)
        {
            // Create a label to display the QR code URL
            var label = new Label
            {
                Text = qrCodeUrl,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                VerticalOptions = LayoutOptions.CenterAndExpand
            };

            // Add the label to the content of the page
            Content = new StackLayout
            {
                Children = { label }
            };
        }
    }
}
