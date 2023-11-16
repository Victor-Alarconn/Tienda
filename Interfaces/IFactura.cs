using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tienda.Models;

namespace Tienda.Interfaces
{
    public interface IFactura
    {
        int InsertarEnTdFac(DatosCliente datosUsuario, PayUConfirmation model, MySqlConnection connection);
    }
}
