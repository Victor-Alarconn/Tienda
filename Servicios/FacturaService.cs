using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tienda.Data;
using Tienda.Interfaces;
using Tienda.Models;

namespace Tienda.Servicios
{
    public class FacturaService : IFactura
    {
        private readonly DataConexion _dataConexion;

        public FacturaService(DataConexion dataConexion)
        {
            _dataConexion = dataConexion;
        }

        public int InsertarEnTdFac(DatosCliente datosUsuario, PayUConfirmation model, MySqlConnection connection)
        {
            try
            {
                string queryInsert = @"
            INSERT INTO td_fac (codigo_R, estado, referencia, valortotal, fecha_trans, email_buyer, descrip, telefono, nombre, nombre2, apellido, apellido2, id_transa, depart, ciudad, tipo_doc, nit, nombre_empr,numer_doc, codigo_dcity, postalnum, td_razon, td_legal)
            VALUES (@Codigo_R, @Estado, @Referencia, @Valortotal, @Fecha_trans, @Email_buyer, @Descrip, @Telefono, @Nombre_C, @Nombre2, @Apellido, @Apellido2, @Id_transa, @Depart, @Ciudad, @Tipo_doc, @Nit, @Nombre_empr,@NumeroD, @CodigoD, @Postal, @Razon, @Legal);
            SELECT LAST_INSERT_ID();";

                using (var commandInsert = new MySqlCommand(queryInsert, connection))
                {
                    commandInsert.Parameters.AddWithValue("@Codigo_R", model.Sign);
                    commandInsert.Parameters.AddWithValue("@Estado", model.State_pol);
                    commandInsert.Parameters.AddWithValue("@Referencia", model.Reference_sale);
                    commandInsert.Parameters.AddWithValue("@Valortotal", datosUsuario.Total);
                    commandInsert.Parameters.AddWithValue("@Fecha_trans", model.Operation_date);
                    commandInsert.Parameters.AddWithValue("@Email_buyer", datosUsuario.Email);
                    commandInsert.Parameters.AddWithValue("@Descrip", model.Description);
                    commandInsert.Parameters.AddWithValue("@Telefono", datosUsuario.Telef);
                    commandInsert.Parameters.AddWithValue("@Nombre_C", datosUsuario.Nombre);
                    commandInsert.Parameters.AddWithValue("@Nombre2", datosUsuario.Nombre2);
                    commandInsert.Parameters.AddWithValue("@Apellido", datosUsuario.Apellido);
                    commandInsert.Parameters.AddWithValue("@Apellido2", datosUsuario.Apellido2);
                    commandInsert.Parameters.AddWithValue("@Id_transa", model.Reference_pol);
                    commandInsert.Parameters.AddWithValue("@Depart", datosUsuario.Depart);
                    commandInsert.Parameters.AddWithValue("@Ciudad", datosUsuario.City);
                    commandInsert.Parameters.AddWithValue("@Tipo_doc", datosUsuario.Tipo_doc);
                    commandInsert.Parameters.AddWithValue("@Nit", datosUsuario.Td_nit);
                    commandInsert.Parameters.AddWithValue("@Nombre_empr", datosUsuario.Nomb_empr);
                    commandInsert.Parameters.AddWithValue("@NumeroD", datosUsuario.Numer_doc);
                    commandInsert.Parameters.AddWithValue("@CodigoD", datosUsuario.CodigoCity);
                    commandInsert.Parameters.AddWithValue("@Postal", datosUsuario.PostalNum);
                    commandInsert.Parameters.AddWithValue("@Razon", datosUsuario.Razon);
                    commandInsert.Parameters.AddWithValue("@Legal", datosUsuario.Legal);

                    return Convert.ToInt32(commandInsert.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al insertar en td_fac: {ex.Message}");
            }
        }
    }
}