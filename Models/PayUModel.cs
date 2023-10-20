using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class PayUModel
    {
        public string MerchantId { get; set; }
        public string AccountId { get; set; }
        public string Description { get; set; }
        public string ApiKey { get; set; }
        public string ReferenceCode { get; set; }
        public decimal Amount { get; set; }
        public decimal Tax { get; set; }
        public decimal TaxReturnBase { get; set; }
        public string Test { get; set; }
        public string Currency { get; set; }
        public string BuyerEmail { get; set; }
        public string Signature { get; set; }
        public string ResponseUrl { get; set; }
        public string ConfirmationUrl { get; set; }
        public int Telephone { get; set; }
        public string BuyerfullName { get; set; }
        public string PaymentMethod { get; set; }
    }
}