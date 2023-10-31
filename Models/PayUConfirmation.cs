using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class PayUConfirmation
    {
        public int Account_id { get; set; }
        public decimal Additional_value { get; set; }
        public decimal Administrative_fee { get; set; }
        public decimal Administrative_fee_base { get; set; }
        public decimal Administrative_fee_tax { get; set; }
        public string Airline_code { get; set; }
        public string AntifraudMerchantId { get; set; }
        public int Attempts { get; set; }
        public string Authorization_code { get; set; }
        public string Bank_id { get; set; }
        public string Bank_referenced_code { get; set; }
        public string Bank_referenced_name { get; set; }
        public string Billing_address { get; set; }
        public string Billing_city { get; set; }
        public string Billing_country { get; set; }
        public string CardType { get; set; }
        public string Cc_holder { get; set; }
        public string Cc_number { get; set; }
        public decimal Commision_pol { get; set; }
        public string Currency { get; set; }
        public string Cus { get; set; }
        public int Customer_number { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string Email_buyer { get; set; }
        public string Error_code_bank { get; set; }
        public string Error_message_bank { get; set; }
        public decimal Exchange_rate { get; set; }
        public string Extra1 { get; set; }
        public string Extra2 { get; set; }
        public string Franchise { get; set; }
        public int Installments_number { get; set; }
        public string Ip { get; set; }
        public string Merchant_id { get; set; }
        public string Nickname_buyer { get; set; }
        public string Nickname_seller { get; set; }
        public string Office_phone { get; set; }
        public DateTime Operation_date { get; set; }
        public int Payment_method { get; set; }
        public int Payment_method_id { get; set; }
        public string Payment_method_name { get; set; }
        public int Payment_method_type { get; set; }
        public string Payment_request_state { get; set; }
        public string Phone { get; set; }
        public string Pse_bank { get; set; }
        public string PseReference1 { get; set; }
        public string PseReference2 { get; set; }
        public string PseReference3 { get; set; }
        public string Reference_pol { get; set; }
        public string Reference_sale { get; set; }
        public string Response_code_pol { get; set; }
        public string Response_message_pol { get; set; }
        public decimal Risk { get; set; }
        public string Shipping_address { get; set; }
        public string Shipping_city { get; set; }
        public string Shipping_country { get; set; }
        public string Sign { get; set; }
        public string State_pol { get; set; }
        public decimal Tax { get; set; }
        public bool Test { get; set; }
        public string Transaction_bank_id { get; set; }
        public DateTime Transaction_date { get; set; }
        public string Transaction_id { get; set; }
        public string Transaction_type { get; set; }
        [JsonProperty("value")]
        public decimal Value { get; set; }
   
        public IEnumerable<Producto> Producto { get; internal set; }
        public List<Producto> Productos { get; set; }
    }
}