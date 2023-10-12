using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class ProductDetailsViewModel
    {
        public Producto Product { get; set; }
        public List<Producto> RelatedProducts { get; set; }
    }

}