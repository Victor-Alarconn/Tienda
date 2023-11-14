using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public string Precio2 { get; set; }
        public string Imagen { get; set; }
        public int Id_Grupo { get; set; }
        public string Detalle { get; set; }
        public string GrupoNombre { get; set; }
        public int Cantidad { get; set; }
        public string ValorTotal { get; set; }
    }
}
