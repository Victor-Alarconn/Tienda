using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Tienda.Areas.Admin.Data
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