using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Areas.Admin.Models
{
    public class Usuario
    {
        public string obra { get; set; }
        public string clave { get; set; }
        public string nombre { get; set; }
        public string pasabordo { get; set; }
        public string cedula { get; set; }
        public string direccion { get; set; }
        public string ciudad { get; set; }
        public string cargo { get; set; }
        public string telefono { get; set; }
        public string celular { get; set; }
        public string email { get; set; }
        public DateTime facceso { get; set; }
        public int status { get; set; }
        public int nivel { get; set; }
        public int fectrl { get; set; }
        public int activo { get; set; }
        public int impresion { get; set; }
        public DateTime fingreso { get; set; }
        public DateTime fnacto { get; set; }
        public DateTime fretiro { get; set; }
        public string datos { get; set; }
        public int nroprint { get; set; }
        public string punto { get; set; }
        public string terminal { get; set; }
        public string sucursal { get; set; }
        public string bodega1 { get; set; }
        public string bodega2 { get; set; }
        public string bodega3 { get; set; }
        public string bodega4 { get; set; }
        public string bodega5 { get; set; }
        public string bodega6 { get; set; }
        public string bodega7 { get; set; }
        public string pw { get; set; }
        public string ciao_vend { get; set; }
    }
}