using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Areas.Admin.Models
{
    public class Productos
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public string Imagen { get; set; }
        public int Categoria { get; set; }
        public bool Stock { get; set; }
        public int Cantidad { get; set; }
        public string Detalle { get; set; }
    }
}