using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class PayUResponse
    {

    public bool IsSuccess { get; set; }

    [Range(0, 999999999999)]
    public long MerchantId { get; set; }

    [Range(0, 99)]
    public int TransactionState { get; set; }

    [Range(0.00, 1.00)]
    public decimal Risk { get; set; }

    [MaxLength(64)]
    public string PolResponseCode { get; set; }

    [MaxLength(255)]
    public string ReferenceCode { get; set; }

    [MaxLength(255)]
    public string Reference_pol { get; set; }

    [MaxLength(255)]
    public string Signature { get; set; }

    [MaxLength(255)]
    public string PolPaymentMethod { get; set; }

    public int PolPaymentMethodType { get; set; }

    [Range(0, 99)]
    public int InstallmentsNumber { get; set; }

    public decimal TX_VALUE { get; set; }

    public decimal TX_TAX { get; set; }

    [MaxLength(255)]
    public string BuyerEmail { get; set; }

    public DateTime ProcessingDate { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; }

    [MaxLength(255)]
    public string Cus { get; set; }

    [MaxLength(255)]
    public string PseBank { get; set; }

    [MaxLength(2)]
    public string Lng { get; set; }

    [MaxLength(255)]
    public string Description { get; set; }

    [MaxLength(64)]
    public string LapResponseCode { get; set; }

    [MaxLength(255)]
    public string LapPaymentMethod { get; set; }

    [MaxLength(255)]
    public string LapPaymentMethodType { get; set; }

    [MaxLength(32)]
    public string LapTransactionState { get; set; }

    [MaxLength(255)]
    public string Message { get; set; }

    [MaxLength(255)]
    public string Extra1 { get; set; }

    [MaxLength(255)]
    public string Extra2 { get; set; }

    [MaxLength(255)]
    public string Extra3 { get; set; }

    [MaxLength(12)]
    public string AuthorizationCode { get; set; }

    [MaxLength(255)]
    public string Merchant_address { get; set; }

    [MaxLength(255)]
    public string Merchant_name { get; set; }

    [MaxLength(255)]
    public string Merchant_url { get; set; }

    [MaxLength(2)]
    public string OrderLanguage { get; set; }

    public int? PseCycle { get; set; }

    [MaxLength(255)]
    public string PseReference1 { get; set; }

    [MaxLength(255)]
    public string PseReference2 { get; set; }

    [MaxLength(255)]
        public string PseReference3 { get; set; }
        [MaxLength(20)]
        public string Telephone { get; set; }

        [MaxLength(36)]
        public string TransactionId { get; set; }

        [MaxLength(64)]
        public string TrazabilityCode { get; set; }

        public decimal TX_ADMINISTRATIVE_FEE { get; set; }
        public decimal TX_TAX_ADMINISTRATIVE_FEE { get; set; }
        public decimal TX_TAX_ADMINISTRATIVE_FEE_RETURN_BASE { get; set; }

    }
}