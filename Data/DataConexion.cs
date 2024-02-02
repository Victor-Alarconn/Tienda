using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Configuration;
using Tienda.Models;

namespace Tienda.Data
{
    public class DataConexion
    {
        private readonly string _connectionString;

        public DataConexion(string nombreBaseDatos = "MySqlConnectionString")
        {
            _connectionString = ConfigurationManager.ConnectionStrings[nombreBaseDatos].ToString();
        }

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public List<Tamano> CargarTamaños()
        {
            List<Tamano> tamaños = new List<Tamano>();

            using (var connection = CreateConnection())
            {
                connection.Open();
                string query = "SELECT IdTamaño, Descripcion, Precio FROM tamanosDonas";
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var tamano = new Tamano
                            {
                                IdTamano = reader.GetInt32("IdTamaño"),
                                Descripcion = reader.GetString("Descripcion"),
                                Precio = reader.GetDecimal("Precio")
                            };
                            tamaños.Add(tamano);
                        }
                    }
                }
            }

            return tamaños;
        }
    }
}