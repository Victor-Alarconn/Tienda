using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Windows.Media.Media3D;
using Tienda.Data;
using Tienda.Areas.Admin.Models;
using Tienda.Areas.Admin.Permisos;
using System.IO;

namespace Tienda.Areas.Admin.Controllers
{
    [ValidacionSession]
    public class AdminController : Controller
    {
        private readonly DataConexion _dataConexion; // Se crea una instancia de la clase DataConexion

        public AdminController()
        {
            _dataConexion = new DataConexion();
        }

        // GET: Admin/Admin
        public ActionResult Verificacion()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Verificacion(string clave, string pw)
        {
            Usuario cantidadFilas = new Usuario();
            string query = "SELECT nroprint FROM xxxxciao WHERE clave = @clave AND pw = @pw";

            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@clave", clave);
                cmd.Parameters.AddWithValue("@pw", pw);
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    cantidadFilas.nroprint = Convert.ToInt32(result);
                }
            }

            if (cantidadFilas.nroprint == 13)
            {
                // usuario válido
                Session["Usuario"] = cantidadFilas;
                // Consultar el nivel de acceso del usuario en la base de datos
                string queryNivel = "SELECT nivel FROM xxxxciao WHERE clave = @clave";
                using (var connection = _dataConexion.CreateConnection())
                {
                    MySqlCommand cmdNivel = new MySqlCommand(queryNivel, connection);
                    cmdNivel.Parameters.AddWithValue("@clave", clave);
                    connection.Open();
                    int nivelAcceso = Convert.ToInt32(cmdNivel.ExecuteScalar().ToString());
                    Session["NivelAcceso"] = nivelAcceso;
                }
                return RedirectToAction("Inicio", "Admin");
            }
            else
            {
                ViewData["Mensaje"] = "Usuario/Contraseña incorrectas";
                return View();
            }
        }


        public ActionResult CerrarSesion()
        {
            Session["Usuario"] = null;
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Inicio()
        {
            return View();
        }
        public ActionResult Categorias()
        {
            List<Categorias> listaCategorias = new List<Categorias>();
            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();
                string query = "SELECT id_grupo, td_nombre FROM td_grupos";
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var categoria = new Categorias
                            {
                                Id = reader.GetInt32("id_grupo"),
                                Nombre = reader.GetString("td_nombre")
                            };
                            listaCategorias.Add(categoria);
                        }
                    }
                }
            }
            return View(listaCategorias);
        }

        public ActionResult Productos()
        {
            List<Productos> listaProductos = new List<Productos>();
            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();
                string query = "SELECT id_main, td_nombre, td_descri, td_precio, td_img, id_grupo, td_detall, td_exist, td_cantidad FROM td_main";
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var producto = new Productos
                            {
                                Id = reader.GetInt32("id_main"),
                                Nombre = reader.GetString("td_nombre"),
                                Descripcion = reader.GetString("td_descri"),
                                Precio = reader.GetDecimal("td_precio"),
                                Imagen = reader.GetString("td_img"),
                                Categoria = reader.GetInt32("id_grupo"),
                                Detalle = reader.GetString("td_detall"),
                                Stock = reader.GetBoolean("td_exist"),
                                Cantidad = reader.GetInt32("td_cantidad")
                            };
                            listaProductos.Add(producto);
                        }
                    }
                }
            }
            return View(listaProductos);
        }

        public ActionResult EditarProducto(int id)
        {
            Productos producto = null;

            try
            {
                using (var connection = _dataConexion.CreateConnection())
                {
                    connection.Open();
                    string query = "SELECT id_main, td_nombre, td_descri, td_precio, td_img, id_grupo, td_detall, td_exist, td_cantidad FROM td_main WHERE id_main = @id";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                producto = new Productos
                                {
                                    Id = reader.GetInt32("id_main"),
                                    Nombre = reader.GetString("td_nombre"),
                                    Descripcion = reader.GetString("td_descri"),
                                    Precio = reader.GetDecimal("td_precio"),
                                    Imagen = reader.GetString("td_img"),
                                    Categoria = reader.GetInt32("id_grupo"),
                                    Detalle = reader.GetString("td_detall"),
                                    Stock = reader.GetBoolean("td_exist"),
                                    Cantidad = reader.GetInt32("td_cantidad")
                                };
                            }
                        }
                    }
                }

                if (producto == null)
                {
                    return HttpNotFound(); // O manejar como prefieras si el producto no se encuentra
                }

                return View(producto);
            }
            catch (Exception ex)
            {
                return View(ex); // Una vista genérica de error o redirigir como sea adecuado
            }
        }

        [HttpPost]
        public ActionResult ActualizarProducto(Productos producto, HttpPostedFileBase imagenSubida)
        {
            try
            {
                using (var connection = _dataConexion.CreateConnection())
                {
                    connection.Open();

                    if (imagenSubida != null && imagenSubida.ContentLength > 0)
                    {
                        // Guardar la imagen en el servidor o en un almacenamiento de archivos
                        var path = Path.Combine(Server.MapPath("~/Archivos"), imagenSubida.FileName);
                        imagenSubida.SaveAs(path);

                        // Actualiza la propiedad Imagen del producto con la nueva ruta de la imagen
                        producto.Imagen = "~/Archivos" + imagenSubida.FileName;
                    }
                    // Si 'producto.Imagen' es una URL, no es necesario hacer nada 

                    // Preparar la consulta SQL para actualizar los datos del producto
                    string query = "UPDATE td_main SET td_nombre = @nombre, td_descri = @descripcion, td_precio = @precio, td_img = @imagen, id_grupo = @categoria, td_detall = @detalle, td_exist = @stock, td_cantidad = @cantidad WHERE id_main = @id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        // Asignar valores a los parámetros
                        command.Parameters.AddWithValue("@nombre", producto.Nombre);
                        command.Parameters.AddWithValue("@descripcion", producto.Descripcion);
                        command.Parameters.AddWithValue("@precio", producto.Precio);
                        command.Parameters.AddWithValue("@imagen", producto.Imagen);
                        command.Parameters.AddWithValue("@categoria", producto.Categoria);
                        command.Parameters.AddWithValue("@detalle", producto.Detalle);
                        command.Parameters.AddWithValue("@stock", producto.Stock);
                        command.Parameters.AddWithValue("@cantidad", producto.Cantidad);
                        command.Parameters.AddWithValue("@id", producto.Id);

                        // Ejecutar la consulta
                        command.ExecuteNonQuery();
                    }
                    TempData["MensajeExito"] = "Producto actualizado correctamente.";
                    return RedirectToAction("Productos"); // Redirige a la lista de productos después de actualizar
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al actualizar el producto: " + ex.Message;
                return RedirectToAction("Productos");
            }
        }


        public ActionResult EditarCategoria(int id)
        {
            Categorias categoria = null;

            try
            {
                using (var connection = _dataConexion.CreateConnection())
                {
                    connection.Open();
                    string query = "SELECT id_grupo, td_nombre FROM td_grupos WHERE id_grupo = @id";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                categoria = new Categorias
                                {
                                    Id = reader.GetInt32("id_grupo"),
                                    Nombre = reader.GetString("td_nombre"),
                                };
                            }
                        }
                    }
                }
                TempData["MensajeExito"] = "Categoria actualizada correctamente.";
                return View(categoria);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al actualizar la categoria: " + ex.Message;
                return RedirectToAction("Categorias");
            }
        }

        [HttpPost]
        public ActionResult ActualizarCategoria(Categorias categoria)
        {
            try
            {
                using (var connection = _dataConexion.CreateConnection())
                {
                    connection.Open();

                    // Preparar la consulta SQL para actualizar los datos del producto
                    string query = "UPDATE td_grupos SET td_nombre = @nombre WHERE id_grupo = @id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        // Asignar valores a los parámetros
                        command.Parameters.AddWithValue("@nombre", categoria.Nombre);
                        command.Parameters.AddWithValue("@id", categoria.Id); // Asumiendo que 'categoria.Id' contiene el ID de la categoría

                        // Ejecutar la consulta
                        command.ExecuteNonQuery();
                    }

                    return RedirectToAction("Categorias"); // Redirige a la lista de productos después de actualizar
                }
            }
            catch (Exception ex)
            {
                // Manejar la excepción
                return View(ex);
            } 
        }


        [HttpPost]
        public ActionResult BorrarProducto(int id)
        {
            try
            {
                // Lógica para borrar el producto de la base de datos
                using (var connection = _dataConexion.CreateConnection())
                {
                    connection.Open();
                    var command = new MySqlCommand("DELETE FROM td_main WHERE id_main = @id", connection);
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Producto borrado con éxito" });
            }
            catch (Exception ex)
            {
                // Manejar excepciones
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult BorrarCategoria(int id)
        {
            try
            {
                // Lógica para borrar el producto de la base de datos
                using (var connection = _dataConexion.CreateConnection())
                {
                    connection.Open();
                    var command = new MySqlCommand("DELETE FROM td_grupos WHERE id_grupo = @id", connection);
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Producto borrado con éxito" });
            }
            catch (Exception ex)
            {
                // Manejar excepciones
                return Json(new { success = false, error = ex.Message });
            }
        }

    }
}
