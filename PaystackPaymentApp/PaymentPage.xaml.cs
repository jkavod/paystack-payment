using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using Xamarin.Essentials;
using PaystackPaymentApp.Models;
using Paystack.Net.SDK.Models.Charge;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.NetworkInformation;
using Net.Codecrete.QrCodeGenerator;
using SecureStorage = Xamarin.Essentials.SecureStorage;


namespace PaystackPaymentApp
{
    public partial class PaymentPage : ContentPage
    {
        private readonly PaystackService _paystackService;
        private const string PaystackApiKeyKey = "PaystackApiKey";

        public PaymentPage(string selectedPackage, string amount, string details)
        {
            InitializeComponent();

            // Use the asynchronous InitializeAsync method in the constructor
            InitializeAsync(selectedPackage, amount, details);
        }

        public string SelectedPackage
        {
            get => (string)GetValue(SelectedPackageProperty);
            private set => SetValue(SelectedPackageProperty, value);
        }

        // Define a BindableProperty for SelectedPackage
        public static readonly BindableProperty SelectedPackageProperty =
            BindableProperty.Create(nameof(SelectedPackage), typeof(string), typeof(PaymentPage));

        private async void InitializeAsync(string selectedPackage, string amount, string details)
        {
            try
            {
                // Wait for the result of GetPaystackApiKey
                var apiKey = await GetPaystackApiKey();

                // Set SelectedPackage after getting the actual API key value
                SelectedPackage = $"{selectedPackage} - {amount}\nDetails: {details}\nAPI Key: {apiKey}";

                // Initialize PaystackService with the obtained API key
               _paystackService = new PaystackService(apiKey);
            }
            catch (Exception ex)
            {
                // Handle initialization error with a more informative message
                await DisplayAlert("Initialization Error", $"Failed to initialize payment: {ex.Message}", "OK");
            }
        }

        private async Task<string> GetPaystackApiKey()
        {
            // Retrieve the API key securely from the device's secure storage
            return await SecureStorage.GetAsync(PaystackApiKeyKey);
        }

        private async void OnPaymentMethodSelected(object sender, EventArgs e)
        {
            Button selectedButton = (Button)sender;
            string selectedPaymentMethod = selectedButton.CommandParameter.ToString();

            try
            {
                switch (selectedPaymentMethod)
                {
                    case "Card":
                        await HandleCardPayment();
                        break;

                    case "BankTransfer":
                        await HandleBankTransfer();
                        break;

                    case "USSD":
                        await HandleUSSDPayment();
                        break;

                    case "QRCode":
                        await HandleQRCodePayment();
                        break;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Payment Error", ex.Message, "OK");
            }
        }

        private async Task HandleCardPayment()
        {
            try
            {
                // Collect card details securely from the user
                var cardNumberTask = SecureInputPromptAsync("Card Payment", "Enter card number");
                var expiryMonthTask = SecureInputPromptAsync("Card Payment", "Enter expiry month");
                var expiryYearTask = SecureInputPromptAsync("Card Payment", "Enter expiry year");
                var cvvTask = SecureInputPromptAsync("Card Payment", "Enter CVV");
                var emailTask = SecureInputPromptAsync("Card Payment", "Enter email");

                // Ensure all input tasks are complete
                await Task.WhenAll(cardNumberTask, expiryMonthTask, expiryYearTask, cvvTask, emailTask);

                // Retrieve the selected package details from the binding context
                var packageDetails = SelectedPackage.Split('-');
                var amount = decimal.Parse(packageDetails[1].Replace("$", "").Trim());

                // Get the results of the tasks
                var cardNumber = await cardNumberTask;
                var expiryMonth = await expiryMonthTask;
                var expiryYear = await expiryYearTask;
                var cvv = await cvvTask;
                var email = await emailTask;

                // Perform the actual payment using PaystackService
                var chargeModel = new ChargeCardInputModel()
                {
                    amount = amount.ToString(),
                    card = new ChargeCard()
                    {
                        number = cardNumber,
                        expiry_month = expiryMonth,
                        expiry_year = expiryYear,
                        cvv = cvv
                    },
                    email = email
                };

                var reference = await _paystackService.ChargeCard(
                    chargeModel.email,
                    chargeModel.amount,
                    chargeModel.card.cvv,
                    chargeModel.card.expiry_month,
                    chargeModel.card.expiry_year,
                    chargeModel.card.number
                );

                // Display success message with reference
                await DisplayAlert("Payment Success", $"Payment successful!\nReference: {reference}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Payment Error", $"Failed to process card payment: {ex.Message}", "OK");
            }
        }



        private async Task<string> SecureInputPromptAsync(string title, string message)
        {
            try
            {
                // Use Microsoft.Maui.Essentials Secure Storage to securely collect sensitive information
                var secureValue = await SecureStorage.GetAsync(title);
                if (secureValue == null)
                {
                    secureValue = await DisplayPromptAsync(title, message, keyboard: Keyboard.Numeric);
                    await SecureStorage.SetAsync(title, secureValue);
                }

                return secureValue;
            }
            catch (Exception ex)
            {
                // Handle exceptions related to SecureStorage (e.g., permission issues)
                throw new Exception($"Failed to securely collect input: {ex.Message}", ex);
            }
        }


        private async Task HandleBankTransfer()
        {
            try
            {
                // Call your server API to retrieve bank details
                BankDetails bankDetails = await RetrieveBankDetailsFromServer();

                // Display bank details and instructions to the user
                string message = $"Bank Name: {bankDetails.BankName}\nAccount Name: {bankDetails.AccountName}\nAccount Number: {bankDetails.AccountNumber}\nReference: {bankDetails.Reference}";

                bool confirmed = await DisplayAlert("Bank Transfer Instructions", message, "Copy Details", "Cancel");

                if (confirmed)
                {
                    // Optionally, you can copy the bank details to the clipboard
                    var clipboardService = DependencyService.Get<IClipboardService>();
                    clipboardService?.CopyToClipboard(message);

                    // Display a message indicating that the details have been copied
                    await DisplayAlert("Details Copied", "Bank details have been copied to the clipboard.", "OK");
                }
                else
                {
                    // User canceled the operation
                    await DisplayAlert("Bank Transfer", "Bank transfer canceled.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle exception (e.g., network error, server error)
                await DisplayAlert("Error", $"Failed to retrieve bank details: {ex.Message}", "OK");
            }
        }


        private async Task HandleUSSDPayment()
        {
            try
            {
                // Collect necessary information from the user
                var phoneNumber = await DisplayPromptAsync("USSD Payment", "Enter your phone number");
                var amount = await DisplayPromptAsync("USSD Payment", "Enter the amount to pay");

                // Construct the USSD code based on the service provider's requirements
                string ussdCode = ConstructUSSDCode(phoneNumber, amount);

                // Display a confirmation dialog with the USSD code
                bool confirmed = await DisplayAlert("USSD Payment", $"Dial the following USSD code:\n\n{ussdCode}\n\nNote: Standard carrier charges may apply.", "Dial", "Cancel");

                if (confirmed)
                {
                    var paymentResult = await MakeUSSDPaymentAPIRequest(ussdCode, phoneNumber, amount);

                    // Handle the payment result (success, failure, etc.)
                    HandleUSSDPaymentResult(paymentResult);
                }
                else
                {
                    // User canceled the USSD payment
                    await DisplayAlert("USSD Payment", "USSD payment canceled.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during USSD payment processing
                await DisplayAlert("USSD Payment Error", $"Failed to initiate USSD payment: {ex.Message}", "OK");
            }
        }

        private string ConstructUSSDCode(string phoneNumber, string amount)
        {
            // Placeholder: Replace this with the actual USSD code construction logic
            return $"*123*{phoneNumber}*{amount}#";
        }

        private async Task<string> MakeUSSDPaymentAPIRequest(string ussdCode, string phoneNumber, string amount)
        {
            // Placeholder: Replace with the actual server endpoint
            var serverEndpoint = "https://your-api-endpoint.com/ussd/payment";

            var apiRequestModel = new USSDPaymentRequestModel
            {
                USSDCode = ussdCode,
                PhoneNumber = phoneNumber,
                Amount = amount
            };

            using (var httpClient = new HttpClient())
            {
                var requestContent = new StringContent(JsonConvert.SerializeObject(apiRequestModel), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(serverEndpoint, requestContent);

                // Read and parse the response content
                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
        }

        private void HandleUSSDPaymentResult(string paymentResult)
        {
            try
            {
                // Placeholder: Replace with actual logic to parse and interpret the USSD payment result
                bool paymentSuccessful = IsUSSDPaymentSuccessful(paymentResult);

                if (paymentSuccessful)
                {
                    // Display a success message
                    DisplayAlert("USSD Payment Success", "USSD payment was successful.", "OK");
                }
                else
                {
                    // Display an error message
                    DisplayAlert("USSD Payment Error", "USSD payment failed.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during result handling
                DisplayAlert("USSD Result Handling Error", $"Error handling USSD payment result: {ex.Message}", "OK");
            }
        }

        private bool IsUSSDPaymentSuccessful(string paymentResult)
        {
            // Placeholder: Replace with actual logic to check if the USSD payment was successful
            return paymentResult.Contains("Success", StringComparison.OrdinalIgnoreCase);
        }


        private async Task HandleQRCodePayment()
        {
            try
            {
                // Retrieve the selected package details from the binding context
                var packageDetails = SelectedPackage.Split('-');
                var amount = packageDetails[1].Replace("$", "").Trim();

                // Generate a QR code using the PaystackService
                var reference = Guid.NewGuid().ToString();
                var qrCodeUrl = await _paystackService.GenerateQRCodeAsync(amount, reference);

                // Display the QR code to the user
                await Navigation.PushAsync(new QRCodePage(qrCodeUrl));
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during QR code generation
                await DisplayAlert("QR Code Generation Error", $"Failed to generate QR code: {ex.Message}", "OK");
            }
        }

        private async Task<BankDetails> RetrieveBankDetailsFromServer()
        {
            // This is just a placeholder; replace it with the actual implementation

            var serverEndpoint = "YourBankDetailsEndpoint";
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(serverEndpoint);

                // Check if the request was successful
                response.EnsureSuccessStatusCode();

                // Read and parse the response content
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<BankDetails>(responseContent);
            }
        }
    }
}
