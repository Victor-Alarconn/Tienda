using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class PaymentInfo
    {
       
        public Cart Cart { get; set; } = new Cart();
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CouponCode { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public string CompanyName { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string StreetAddress { get; set; }
        public int? VerificationDigit { get; set; }
        public string SostalCode { get; set; }
        public string Email { get; set; }
        public long Phone { get; set; }
        public int postalCode { get; set; }

    }
}