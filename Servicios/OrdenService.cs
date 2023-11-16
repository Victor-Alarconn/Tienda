using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Tienda.Data;
using Tienda.Interfaces;
using Tienda.Models;

namespace Tienda.Servicios
{
    public class OrdenService : IOrdenService
    {
        private readonly DataConexion _dataConexion;

        public OrdenService(DataConexion dataConexion)
        {
            _dataConexion = dataConexion;
        }


        public List<Producto> ObtenerProductosPorOrden(string reference_sale) // Método para obtener los productos por orden
        {
            var productos = new List<Producto>();

            using (var connection = _dataConexion.CreateConnection())
            {
                try
                {
                    connection.Open();

                    // Paso 1: Obtener el Id de la orden usando reference_sale
                    string ordenQuery = "SELECT Id FROM td_orden WHERE refere = @reference_sale";
                    using (var command = new MySqlCommand(ordenQuery, connection))
                    {
                        command.Parameters.AddWithValue("@reference_sale", reference_sale);
                        var orderId = command.ExecuteScalar();

                        if (orderId != null)
                        {
                            // Paso 2: Obtener detalles de productos (incluyendo cantidad y valortotal) usando orderId
                            string producQuery = "SELECT p.ProductId, p.cantidad, p.valortotal, m.td_nombre, m.td_precio FROM td_produc p INNER JOIN td_main m ON p.ProductId = m.id_main WHERE p.OrderId = @orderId";
                            using (var producCommand = new MySqlCommand(producQuery, connection))
                            {
                                producCommand.Parameters.AddWithValue("@orderId", orderId);
                                using (var reader = producCommand.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var producto = new Producto
                                        {
                                            Nombre = reader["td_nombre"].ToString(),
                                            Precio = decimal.Parse(reader["td_precio"].ToString()),
                                            Cantidad = int.Parse(reader["cantidad"].ToString()),
                                            ValorTotal = reader["valortotal"].ToString()
                                        };
                                        productos.Add(producto);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al obtener los productos: {ex.Message}");
                }
            }

            return productos;
        }


        public DatosCliente Obtenerdatos(string reference_sale) // Método para obtener los datos del cliente
        {
            DatosCliente datos = null;
            using (var connection = _dataConexion.CreateConnection())
            {
                try
                {
                    connection.Open();

                    string query = "SELECT nombre,nombre2, apellido, apellido2, email, total FROM td_orden WHERE refere = @reference_sale";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@reference_sale", reference_sale);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                datos = new DatosCliente
                                {
                                    Nombre = reader["nombre"].ToString(),
                                    Nombre2 = reader["nombre2"].ToString(),
                                    Apellido = reader["apellido"].ToString(),
                                    Apellido2 = reader["apellido2"].ToString(),
                                    Email = reader["email"].ToString(),
                                    Total = Convert.ToDecimal(reader["total"])
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al obtener datos del cliente: {ex.Message}");
                    // Si quieres, puedes manejar el error más específicamente aquí. Por ejemplo, puedes decidir lanzar la excepción, registrar el error en un log, etc.
                }
            }
            return datos;
        }

        public int Orden(PaymentInfo model, string referencia, MySqlConnection connection, MySqlTransaction transaction) // Método para guardar los datos de la orden
        {
            // La consulta SQL y la lógica se mantienen, solo cambiamos cómo accedemos a las propiedades
            string query = @"
        INSERT INTO td_orden(email, telef, nombre, nombre2, apellido, apellido2, direc, tipo_doc, numer_doc, nomb_empr, pais, depart, city, 
                             postalnum, td_nit, refere, total, descrip, td_estado, codigo_dcity)
        VALUES (@Email, @Phone, @FullName, @Nombre2, @Apellido, @Apellido2, @Address, @DocumentType, @DocumentNumber, @CompanyName, 
                @Country, @State, @City, @PostalCode, @nit, @ReferenceCode, @Amount, @ProductDescription, @Estado, @CodigoD);
        SELECT LAST_INSERT_ID();";

            using (var command = new MySqlCommand(query, connection, transaction))
            {
                // Asignación de valores a los parámetros de la consulta SQL usando el modelo directamente.
                command.Parameters.AddWithValue("@Email", model.Email);
                command.Parameters.AddWithValue("@Phone", model.Phone);
                command.Parameters.AddWithValue("@FullName", model.FirstName);
                command.Parameters.AddWithValue("@Nombre2", model.MiddleName);
                command.Parameters.AddWithValue("@Apellido", model.LastName);
                command.Parameters.AddWithValue("@Apellido2", model.SecondLastName);
                command.Parameters.AddWithValue("@Address", model.StreetAddress);
                command.Parameters.AddWithValue("@DocumentType", model.DocumentType);
                command.Parameters.AddWithValue("@DocumentNumber", model.DocumentNumber);
                command.Parameters.AddWithValue("@CompanyName", model.CompanyName);
                command.Parameters.AddWithValue("@Country", model.Country);
                command.Parameters.AddWithValue("@State", model.State);
                command.Parameters.AddWithValue("@City", model.City);
                command.Parameters.AddWithValue("@PostalCode", model.postalCode);
                command.Parameters.AddWithValue("@CodigoD", model.CityCode);

                if (model.VerificationDigit.HasValue)
                    command.Parameters.AddWithValue("@nit", model.VerificationDigit);
                else
                    command.Parameters.AddWithValue("@nit", DBNull.Value);

                command.Parameters.AddWithValue("@ReferenceCode", referencia); // Aquí supongo que aún querrás generar una referencia nueva para cada orden.
                command.Parameters.AddWithValue("@Amount", model.Cart.TotalPrice());

                StringBuilder productNames = new StringBuilder();
                foreach (var item in model.Cart.Items)
                {
                    if (productNames.Length > 0)
                    {
                        productNames.Append(", ");
                    }
                    productNames.Append(item.Product.Nombre);
                }
                string productDescription = productNames.ToString();
                command.Parameters.AddWithValue("@ProductDescription", productDescription);
                command.Parameters.AddWithValue("@Estado", 1); // estado de la transacción

                // Ejecuta la consulta SQL y devuelve el ID recién creado.
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public void EliminarDeTdOrden(int userId, MySqlConnection connection)
        {
            try
            {
                string queryDelete = "DELETE FROM td_orden WHERE Id = @Id";
                using (var commandDelete = new MySqlCommand(queryDelete, connection))
                {
                    commandDelete.Parameters.AddWithValue("@Id", userId);
                    commandDelete.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al eliminar de td_orden: {ex.Message}");
            }
        }

        public DatosCliente ObtenerDatosUsuario(int userId, MySqlConnection connection)
        {
            DatosCliente datosUsuario = new DatosCliente();

            try
            {
                string queryUserData = "SELECT email, telef, nombre, nombre2, apellido, apellido2, tipo_doc, numer_doc, td_nit, nomb_empr, depart, city, total, codigo_dcity, postalnum FROM td_orden WHERE Id = @Id";
                using (var commandUserData = new MySqlCommand(queryUserData, connection))
                {
                    commandUserData.Parameters.AddWithValue("@Id", userId);
                    using (var reader = commandUserData.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            datosUsuario.Email = reader["email"].ToString();
                            datosUsuario.Telef = reader["telef"].ToString();
                            datosUsuario.Nombre = reader["nombre"].ToString();
                            datosUsuario.Nombre2 = reader["nombre2"].ToString();
                            datosUsuario.Apellido = reader["apellido"].ToString();
                            datosUsuario.Apellido2 = reader["apellido2"].ToString();
                            datosUsuario.Tipo_doc = reader["tipo_doc"].ToString();
                            datosUsuario.Numer_doc = reader["numer_doc"].ToString();
                            datosUsuario.Td_nit = reader["td_nit"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["td_nit"]);
                            datosUsuario.Nomb_empr = reader["nomb_empr"].ToString();
                            datosUsuario.Depart = reader["depart"].ToString();
                            datosUsuario.City = reader["city"].ToString();
                            datosUsuario.Total = reader.GetDecimal("total");
                            datosUsuario.CodigoCity = reader["codigo_dcity"].ToString();
                            datosUsuario.PostalNum = reader.GetInt32("postalnum");
                        }
                        else
                        {
                            throw new Exception("User_Id no encontrado en td_orden");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener datos del usuario: {ex.Message}");
            }

            return datosUsuario;
        }

        public int ObtenerUserId(string referenceSale, MySqlConnection connection)
        {
            // Consulta para obtener el User_Id
            string queryUserId = "SELECT Id FROM td_orden WHERE refere = @Refere";

            using (var commandUserId = new MySqlCommand(queryUserId, connection))
            {
                commandUserId.Parameters.AddWithValue("@Refere", referenceSale);
                object result = commandUserId.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                {
                    throw new Exception("Reference_sale no encontrado en td_orden");
                }

                return Convert.ToInt32(result);
            }
        }

        public decimal GetTotalFromDatabase(string referenceCode) // Método para obtener el valor total de la orden
        {
            decimal totalValue = 0;

            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();

                string query = "SELECT total FROM td_orden WHERE refere = @ReferenceCode";

                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ReferenceCode", referenceCode);

                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        totalValue = Convert.ToDecimal(result);
                    }
                }

                connection.Close();
            }

            return totalValue;
        }
    }
}