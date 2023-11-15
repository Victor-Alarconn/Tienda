using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Web;
using Tienda.Data;
using Tienda.Interfaces;

namespace Tienda.Servicios
{
    public class ReferenciaService : IReferenciaService
    {
        private readonly DataConexion _dataConexion;

        public ReferenciaService(DataConexion dataConexion)
        {
            _dataConexion = dataConexion;
        }

        public string GenerarReferencia()
        {
            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();

                // Consulta para obtener la referencia actual
                string queryObtenerReferencia = "SELECT ID FROM referencia LIMIT 1;";

                string referenciaActual;
                using (var commandObtener = new MySqlCommand(queryObtenerReferencia, connection))
                {
                    object result = commandObtener.ExecuteScalar();
                    referenciaActual = result == null || result == DBNull.Value ? "00001" : result.ToString();
                }

                // Eliminar la referencia actual
                string queryEliminarReferencia = "DELETE FROM referencia;";
                using (var commandEliminar = new MySqlCommand(queryEliminarReferencia, connection))
                {
                    commandEliminar.ExecuteNonQuery();
                }

                // Calcular la siguiente referencia
                int numeroReferencia = Convert.ToInt32(referenciaActual);
                numeroReferencia++;
                string nuevaReferencia = numeroReferencia.ToString("D5");

                // Insertar la nueva referencia
                string queryInsertarReferencia = "INSERT INTO referencia (ID) VALUES (@nuevaReferencia);";
                using (var commandInsertar = new MySqlCommand(queryInsertarReferencia, connection))
                {
                    commandInsertar.Parameters.AddWithValue("@nuevaReferencia", nuevaReferencia);
                    commandInsertar.ExecuteNonQuery();
                }

                return referenciaActual;
            }
        }

    }
}