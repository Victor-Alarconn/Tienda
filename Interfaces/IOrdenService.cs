using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tienda.Models;

namespace Tienda.Interfaces
{
    public interface IOrdenService
    {
        List<Producto> ObtenerProductosPorOrden(string reference_sale);
        DatosCliente Obtenerdatos(string reference_sale);
        int Orden(PaymentInfo model, string referencia, MySqlConnection connection, MySqlTransaction transaction);
        void EliminarDeTdOrden(int userId, MySqlConnection connection);
        DatosCliente ObtenerDatosUsuario(int userId, MySqlConnection connection);
        int ObtenerUserId(string referenceSale, MySqlConnection connection);
        decimal GetTotalFromDatabase(string referenceCode);
    }
}