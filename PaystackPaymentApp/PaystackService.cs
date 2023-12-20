using Paystack.Net.SDK;
using Paystack.Net.SDK.Models.Charge;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PaystackPaymentApp
{
    public class PaystackService
    {
        private readonly string _apiKey;
        private readonly HttpClient _client;

        // Constructor for initializing with Apikey
        public PaystackService(string apiKey)
        {
            _apiKey = apiKey;
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://api.paystack.co/")
            };
        }

        public async Task<ChargeResponseModel> ChargeCard(string amount, string cvv,
            string expiry_month, string expiry_year, string number, string display_name = null, string value = null,
            string variable_name = null)
        {
            var model = new ChargeCardInputModel()
            {
                amount = amount,
                card = new ChargeCard()
                {
                    cvv = cvv,
                    expiry_month = expiry_month,
                    expiry_year = expiry_year,
                    number = number
                },
                //email = email,
                // pin = pin
            };

            if (!string.IsNullOrWhiteSpace(display_name))
            {
                var metadata = new ChargeCardMetadata()
                {
                    custom_fields = new List<Custom_Field>()
                    {
                        new Custom_Field()
                        {
                            display_name = display_name,
                            value = value,
                            variable_name = variable_name
                        }
                    }
                };

                model.metadata = metadata;
            }

            var jsonObj = JsonConvert.SerializeObject(model);
            var content = new StringContent(jsonObj, Encoding.UTF8, "application/json");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _client.PostAsync("charge", content);
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ChargeResponseModel>(json);
        }

        public async Task<string> GenerateQRCodeAsync(string amount, string reference)
        {
            try
            {
                var requestContent = new StringContent(JsonConvert.SerializeObject(new
                {
                    amount,
                    reference,
                    // Add any other required parameters
                }), Encoding.UTF8, "application/json");

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                    var response = await httpClient.PostAsync("https://api.paystack.co/qr/initiate", requestContent);

                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<PaystackQRResponse>(responseContent);

                    return responseData.QRCodeURL;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating QR Code: {ex.Message}");
                throw;
            }
        }

        internal Task ChargeCard(string email, decimal amount, string cvv, string expiryMonth, string expiryYear, string cardNumber)
        {
            throw new NotImplementedException();
        }
    }

    public class PaystackQRResponse
    {
        public string QRCodeURL { get; set; }
    }
}
