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
    public class ProductoService : IProductoService
    {
        private readonly DataConexion _dataConexion;

        public ProductoService(DataConexion dataConexion)
        {
            _dataConexion = dataConexion;
        }

        public void ConsultarYTransferirProductos(int userId, int idFac, MySqlConnection connection)
        {
            try
            {
                // Consulta de productos en td_produc
                string querySelectProducts = "SELECT ProductId, cantidad, td_valor, valortotal, td_detalle FROM td_produc WHERE OrderId = @UserId";
                List<Producto> productos = new List<Producto>();

                using (var commandSelectProducts = new MySqlCommand(querySelectProducts, connection))
                {
                    commandSelectProducts.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = commandSelectProducts.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Producto producto = new Producto
                            {
                                Id = reader.GetInt32("ProductId"),
                                Cantidad = reader.GetInt32("cantidad"),
                                Precio2 = reader.GetString("td_valor"),
                                ValorTotal = reader.GetString("valortotal"),
                                Detalle = reader.GetString("td_detalle")
                            };
                            productos.Add(producto);
                        }
                    }
                }

                // Transferencia de productos a td_producx
                foreach (var producto in productos)
                {
                    string queryInsertProductx = "INSERT INTO td_producx (OrderId, ProductId, cantidad, td_valor, valortotal, td_detalle) VALUES (@OrdenId, @ProductId, @Cantidad, @ValorUnitario, @ValorTotal, @Detalle)";
                    using (var commandInsertProductx = new MySqlCommand(queryInsertProductx, connection))
                    {
                        commandInsertProductx.Parameters.AddWithValue("@OrdenId", idFac);
                        commandInsertProductx.Parameters.AddWithValue("@ProductId", producto.Id);
                        commandInsertProductx.Parameters.AddWithValue("@Cantidad", producto.Cantidad);
                        commandInsertProductx.Parameters.AddWithValue("@ValorUnitario", producto.Precio2);
                        commandInsertProductx.Parameters.AddWithValue("@ValorTotal", producto.ValorTotal);
                        commandInsertProductx.Parameters.AddWithValue("@Detalle", producto.Detalle);

                        commandInsertProductx.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al consultar y transferir productos: {ex.Message}");
            }
        }

        public void GuardarProductos(int orderId, Cart cart, MySqlConnection connection, MySqlTransaction transaction)
        {

            foreach (var item in cart.Items)
            {
                decimal valor = item.Product.Precio;
                int quantity = item.Quantity;
                decimal total = valor * quantity;

                string orderProductInsertQuery = "INSERT INTO td_produc(OrderId, ProductId, cantidad, td_valor, valortotal, td_detalle) VALUES (@OrderId, @ProductId, @Quantity, @Valor, @Total, @Detalle)";

                using (var command = new MySqlCommand(orderProductInsertQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@ProductId", item.Product.Id);
                    command.Parameters.AddWithValue("@Quantity", item.Quantity);
                    command.Parameters.AddWithValue("@Valor", item.Product.Precio);
                    command.Parameters.AddWithValue("@Total", total);
                    command.Parameters.AddWithValue("@Detalle", item.Product.Detalle);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}