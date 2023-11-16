using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tienda.Models;

namespace Tienda.Interfaces
{
    public interface IProductoService
    {
        void ConsultarYTransferirProductos(int userId, int idFac, MySqlConnection connection);
         void GuardarProductos(int orderId, Cart cart, MySqlConnection connection, MySqlTransaction transaction);
    }
}
