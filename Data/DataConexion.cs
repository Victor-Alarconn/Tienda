using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Configuration;

namespace Tienda.Data
{
    public class DataConexion
    {
        private readonly string _connectionString;

        public DataConexion()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MySqlConnectionString"].ToString();
        }

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}