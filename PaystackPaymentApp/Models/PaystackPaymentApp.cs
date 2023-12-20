namespace PaystackPaymentApp.Models
{
    public class USSDPaymentRequestModel
    {
        public USSDPaymentRequestModel(string phoneNumber = "", string amount = "", string ussdCode = null)
        {
            PhoneNumber = phoneNumber;
            Amount = amount;
            USSDCode = ussdCode;
        }

        public string PhoneNumber { get; set; }
        public string Amount { get; set; }

        public string USSDCode { get; set; }
    }
}
