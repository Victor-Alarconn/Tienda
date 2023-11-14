using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class DatosCliente
    {
        public string Nombre { get; set; }
        public string Nombre2 { get; set; }
        public string Apellido { get; set; }
        public string Apellido2 { get; set; }
        public string Email { get; set; }
        public int? Td_nit { get; set; }
        public decimal Total { get; set; }
        public string Telef { get; set; }
        public string Tipo_doc { get; set; }
        public string Numer_doc { get; set; }
        public string Nomb_empr { get; set; }
        public string Depart { get; set; }
        public string City { get; set; }
        public string CodigoCity { get; set; }
        public int PostalNum { get; set; }

    }
}