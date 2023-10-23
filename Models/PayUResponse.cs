using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class PayUResponse
    {
        public string TransactionId { get; set; }
        public string TransactionStatus { get; set; }
        public string Amount { get; set; }
        public string Description { get; set; }
        public bool IsSuccess { get; set; }
        public string MerchantId { get; set; }
        public string ReferenceCode { get; set; }
        public string TX_VALUE { get; set; }
        public string Currency { get; set; }
        public string TransactionState { get; set; }
        public string Signature { get; set; }

    }
}