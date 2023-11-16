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
    public class MainService : IMainService
    {
        private readonly DataConexion _dataConexion;

        public MainService(DataConexion dataConexion)
        {
            _dataConexion = dataConexion;
        }

        public Producto GetProductById(int productId) // Método para obtener un producto por su ID
        {
            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();

                // Definir la consulta para obtener un producto específico por su ID.
                string query = "SELECT * FROM td_main WHERE id_main = @ProductId";

                using (var command = new MySqlCommand(query, connection))
                {
                    // Añadir el parámetro a la consulta.
                    command.Parameters.AddWithValue("@ProductId", productId);

                    using (var reader = command.ExecuteReader())
                    {
                        // Si se encuentra el producto, devolverlo.
                        if (reader.Read())
                        {
                            return new Producto
                            {
                                Id = reader.GetInt32("id_main"),
                                Nombre = reader.GetString("td_nombre"),
                                Descripcion = reader.GetString("td_descri"),
                                Precio = reader.GetDecimal("td_precio"),
                                Detalle = reader.GetString("td_detall"),
                                Imagen = reader.GetString("td_img")
                            };
                        }
                    }
                }
            }
            // Si no se encuentra el producto, devolver null.
            return null;
        }
    }
}