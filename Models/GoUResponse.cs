using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class GoUResponse
    {
        public StatusModel Status { get; set; }
        public int InternalReference { get; set; }
        public string Reference { get; set; }
        public string Signature { get; set; }
    }

    public class StatusModel
    {
        public string Status { get; set; }
        public string Reason { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
    }

}