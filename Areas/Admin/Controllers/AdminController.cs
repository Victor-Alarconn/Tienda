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
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace Tienda.Areas.Admin.Controllers
{
    [ValidacionSession]
    public class AdminController : Controller
    {
        private readonly DataConexion _dataConexion; // Se crea una instancia de la clase DataConexion
        private string rutaColores;
        private string imagePath = "~/Imagenes/Carousel";
        private string publicidadPath = ("~/Imagenes/Publicidad");
        private string HorarioPath => Server.MapPath("~/Areas/Descripcion/horarios.txt"); // Ruta horario


        public AdminController()
        {
            _dataConexion = new DataConexion();
        }

        // Método que se ejecuta antes de cada acción
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            InicializarRutaColores();
            var colores = LeerColores(); // Cargar los colores
            ViewBag.NavbarColor = colores["navbarColor"];
            ViewBag.SliderColor = colores["sliderColor"];

            // Verificar los colores
            System.Diagnostics.Debug.WriteLine($"Navbar Color: {ViewBag.NavbarColor}");
            System.Diagnostics.Debug.WriteLine($"Slider Color: {ViewBag.SliderColor}");
        }

        private void InicializarRutaColores()
        {
            rutaColores = Server.MapPath("~/App_Data/colores.json");
        }

        private Dictionary<string, string> LeerColores()
        {
            if (!System.IO.File.Exists(rutaColores))
            {
                var coloresPorDefecto = new Dictionary<string, string>
                {
                    { "navbarColor", "#007BFF" },
                    { "sliderColor", "#d6f792" }
                };
                GuardarColores(coloresPorDefecto);
                return coloresPorDefecto;
            }

            var json = System.IO.File.ReadAllText(rutaColores);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        private void GuardarColores(Dictionary<string, string> colores)
        {
            var json = JsonConvert.SerializeObject(colores, Formatting.Indented);
            System.IO.File.WriteAllText(rutaColores, json);
        }

        [HttpPost]
        public ActionResult GuardarColores(string navbarColor, string sliderColor)
        {
            InicializarRutaColores();

            // Crear un objeto para almacenar los colores seleccionados
            var colores = new Dictionary<string, string>
            {
                { "navbarColor", navbarColor ?? "#007BFF" },
                { "sliderColor", sliderColor ?? "#d6f792" }
            };

            // Guardar los colores en el archivo JSON
            GuardarColores(colores);

            // Redirigir a la página de gestión de colores
            return RedirectToAction("GestionPagina"); // Redirigir a la acción para recargar la vista
        }

        // Acción para cargar imágenes
        [HttpPost]
        public ActionResult UploadImages(HttpPostedFileBase file)
        {
            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    string path = Server.MapPath(imagePath);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    string fileName = Path.GetFileName(file.FileName);
                    string fullPath = Path.Combine(path, fileName);
                    file.SaveAs(fullPath);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar la imagen: " + ex.Message;
                return View("GestionPagina");
            }
        }

        public ActionResult Index()
        {
            var images = Directory.GetFiles(Server.MapPath(imagePath))
                      .Select(Path.GetFileName)
                      .ToList();
            ViewBag.Images = images;

            // Cargar la imagen de publicidad si existe y si la fecha límite no ha pasado
            var publicidadFiles = Directory.GetFiles(Server.MapPath(publicidadPath));
            foreach (var file in publicidadFiles)
            {
                string fechaFileName = Path.Combine(Server.MapPath(publicidadPath), Path.GetFileNameWithoutExtension(file) + "_fecha.txt");

                if (System.IO.File.Exists(fechaFileName))
                {
                    try
                    {
                        var fechaContenido = System.IO.File.ReadAllText(fechaFileName);
                        var fechas = fechaContenido.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        if (fechas.Length >= 2)
                        {
                            DateTime fechaInicio = DateTime.Parse(fechas[0].Replace("Inicio: ", ""));
                            DateTime fechaFin = DateTime.Parse(fechas[1].Replace("Fin: ", ""));
                            DateTime fechaActual = DateTime.Now;

                            if (fechaActual <= fechaFin)
                            {
                                ViewBag.Publicidad = Path.GetFileName(file);
                                ViewBag.PublicidadFechaInicio = fechaInicio;
                                ViewBag.PublicidadFechaFin = fechaFin;
                                break; // Encontró una imagen válida, salir del bucle
                            }
                            else
                            {
                                // Eliminar la imagen y el archivo de fecha si la publicidad ha expirado
                                System.IO.File.Delete(file);
                                System.IO.File.Delete(fechaFileName);
                            }
                        }
                        else
                        {
                            // Manejar el caso en que el archivo de fecha no tiene el formato esperado
                            System.IO.File.Delete(file);
                            System.IO.File.Delete(fechaFileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Manejar posibles excepciones al leer o parsear el archivo de fecha
                        System.IO.File.Delete(file);
                        System.IO.File.Delete(fechaFileName);
                        ViewBag.Error = "Error al procesar la imagen de publicidad: " + ex.Message;
                    }
                }
            }

            return View();
        }

        // Método para subir imágenes de publicidad con fecha y hora límite
        [HttpPost]
        public ActionResult UploadPublicidad(HttpPostedFileBase file, DateTime? publicidadFechaInicio, DateTime? publicidadFechaFin)
        {
            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    // Convertir la ruta virtual a una ruta física
                    string path = Server.MapPath(publicidadPath);

                    // Verificar si la carpeta existe y crearla si no es así
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    // Eliminar la imagen de publicidad anterior si existe
                    var existingPublicidad = Directory.GetFiles(path).FirstOrDefault();
                    if (existingPublicidad != null)
                    {
                        System.IO.File.Delete(existingPublicidad);
                    }

                    // Obtener el nombre del archivo y combinarlo con la ruta
                    string fileName = Path.GetFileName(file.FileName);
                    string fullPath = Path.Combine(path, fileName);

                    // Guardar el archivo de imagen
                    file.SaveAs(fullPath);

                    // Guardar la fecha y hora límite en un archivo de texto
                    if (publicidadFechaFin.HasValue)
                    {
                        string fechaFileName = Path.Combine(path, Path.GetFileNameWithoutExtension(fileName) + "_fecha.txt");
                        string fechaContenido = $"Inicio: {publicidadFechaInicio?.ToString("yyyy-MM-dd HH:mm:ss")}\nFin: {publicidadFechaFin.Value.ToString("yyyy-MM-dd HH:mm:ss")}";
                        System.IO.File.WriteAllText(fechaFileName, fechaContenido);
                    }
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar la imagen de publicidad: " + ex.Message;
                return View("~/Areas/Admin/Views/Admin/Index.cshtml");
            }
        }

        // Acción para eliminar imágenes
        [HttpPost]
        public ActionResult DeleteImage(string fileName)
        {
            try
            {
                string path = Server.MapPath(Path.Combine(imagePath, fileName));
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    ViewBag.Message = "Imagen eliminada exitosamente.";
                }
                else
                {
                    ViewBag.Error = "La imagen no existe.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al eliminar la imagen: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // Método para eliminar imágenes de publicidad
        [HttpPost]
        public ActionResult DeletePublicidad()
        {
            try
            {
                string path = Server.MapPath(publicidadPath);
                var publicidad = Directory.GetFiles(path).FirstOrDefault();
                if (publicidad != null && System.IO.File.Exists(publicidad))
                {
                    System.IO.File.Delete(publicidad);

                    // Eliminar el archivo de fecha asociado
                    string fechaFileName = Path.Combine(path, Path.GetFileNameWithoutExtension(publicidad) + "_fecha.txt");
                    if (System.IO.File.Exists(fechaFileName))
                    {
                        System.IO.File.Delete(fechaFileName);
                    }

                    ViewBag.Message = "Imagen de publicidad eliminada exitosamente.";
                }
                else
                {
                    ViewBag.Error = "La imagen de publicidad no existe.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al eliminar la imagen de publicidad: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: Admin/Admin
        public ActionResult Verificacion()
        {
            return View();
        }

        public ActionResult Pedidos()
        {
            return View();
        }

        public ActionResult GestionPagina()
        {
            return View();
        }

        public ActionResult Ventas()
        {
            return View();
        }

        public ActionResult AgregarProducto()
        {
            var categorias = ObtenerCategorias();

            ViewBag.Categorias = categorias.Select(c => new SelectListItem
            {
                Text = c.Nombre,
                Value = c.Id.ToString()
            }).ToList();

            var producto = new Productos();

            return View(producto);
        }

        [HttpPost]
        public ActionResult AgregarProducto(Productos producto, HttpPostedFileBase ImagenArchivo)
        {
            try
            {
                using (var connection = _dataConexion.CreateConnection())
                {
                    connection.Open();

                    // Manejar la carga de la imagen si es necesario
                    if (ImagenArchivo != null && ImagenArchivo.ContentLength > 0)
                    {
                        var imagePath = Path.Combine(Server.MapPath("~/Archivos"), ImagenArchivo.FileName);
                        ImagenArchivo.SaveAs(imagePath);
                        producto.Imagen = "/Archivos/" + ImagenArchivo.FileName;
                    }


                    string query = "INSERT INTO td_main (td_nombre, td_descri, td_precio, td_img, id_grupo, td_detall, td_exist, td_cantidad) VALUES (@nombre, @descri, @precio, @img, @categoria, @detall, @exist, @cantidad)";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@nombre", producto.Nombre);
                        command.Parameters.AddWithValue("@descri", producto.Descripcion);
                        command.Parameters.AddWithValue("@precio", producto.Precio);
                        command.Parameters.AddWithValue("@img", producto.Imagen);
                        command.Parameters.AddWithValue("@categoria", producto.Categoria);
                        command.Parameters.AddWithValue("@detall", producto.Detalle);
                        command.Parameters.AddWithValue("@exist", producto.Stock ? 1 : 0);
                        command.Parameters.AddWithValue("@cantidad", producto.Cantidad);

                        command.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = "Producto agregado con éxito.";
                TempData.Keep("MensajeExito"); // Mantener TempData para el próximo request
                return RedirectToAction("AgregarProducto");

            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al agregar el producto: " + ex.Message;
                return View(producto);
            }
        }

        public List<Categorias> ObtenerCategorias()
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
            return listaCategorias;
        }

        public ActionResult AgregarCategoria()
        {
            var categorias = new Categorias();
            return View(categorias);
        }

        [HttpPost]
        public ActionResult AgregarCategoria(Categorias categoria)
        {
            try
            {
                using (var connection = _dataConexion.CreateConnection())
                {
                    connection.Open();

                    string query = "INSERT INTO td_grupos (td_nombre) VALUES (@nombre)";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@nombre", categoria.Nombre);

                        command.ExecuteNonQuery();
                    }
                }

                TempData["MensajeExito"] = "Producto agregado con éxito.";
                TempData.Keep("MensajeExito"); // Mantener TempData para el próximo request
                return RedirectToAction("AgregarCategoria");

            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al agregar el producto: " + ex.Message;
                return View(categoria);
            }
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
            // Consulta el total de ventas desde la base de datos
            var totalVentas = ObtenerTotalVentas();
            var totalproductos = ObtenerTotalProductos();
            var totalcategorias = ObtenerTotalCategorias();
            var totalpedidos = ObtenerTotalPedidos();

            // Asignar el total de ventas al ViewBag para que esté disponible en la vista
            ViewBag.TotalVentas = totalVentas;
            ViewBag.TotalProductos = totalproductos;
            ViewBag.TotalCategorias = totalcategorias;
            ViewBag.TotalPedidos = totalpedidos;

            return View();
        }

        // Método que realiza la consulta a la base de datos sobre las ventas 
        private int ObtenerTotalVentas()
        {
            int totalVentas = 0;
            using (var conexion = _dataConexion.CreateConnection())
            {
                string query = "SELECT COUNT(Id_fac) FROM td_fac";
                MySqlCommand comando = new MySqlCommand(query, conexion);
                conexion.Open();

                var resultado = comando.ExecuteScalar();
                totalVentas = (resultado != null) ? Convert.ToInt32(resultado) : 0;
            }

            return totalVentas;
        }


        // Método que realiza la consulta a la base de datos sobre los productos 
        private int ObtenerTotalProductos()
        {
            int totalproductos = 0;
            using (var conexion = _dataConexion.CreateConnection())
            {
                string query = "SELECT COUNT(Id) FROM td_produc";
                MySqlCommand comando = new MySqlCommand(query, conexion);
                conexion.Open();

                var resultado = comando.ExecuteScalar();
                totalproductos = (resultado != null) ? Convert.ToInt32(resultado) : 0;
            }

            return totalproductos;
        }


        // Método que realiza la consulta a la base de datos sobre las categorias 
        private int ObtenerTotalCategorias()
        {
            int totalcategorias = 0;
            using (var conexion = _dataConexion.CreateConnection())
            {
                string query = "SELECT COUNT(id_grupo) FROM td_grupos";
                MySqlCommand comando = new MySqlCommand(query, conexion);
                conexion.Open();

                var resultado = comando.ExecuteScalar();
                totalcategorias = (resultado != null) ? Convert.ToInt32(resultado) : 0;
            }

            return totalcategorias;
        }

        // Método que realiza la consulta a la base de datos sobre los pedidos 
        private int ObtenerTotalPedidos()
        {
            int totalpedidos = 0;
            using (var conexion = _dataConexion.CreateConnection())
            {
                string query = "SELECT COUNT(Id) FROM td_orden";
                MySqlCommand comando = new MySqlCommand(query, conexion);
                conexion.Open();

                var resultado = comando.ExecuteScalar();
                totalpedidos = (resultado != null) ? Convert.ToInt32(resultado) : 0;
            }

            return totalpedidos;
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
                                Nombre = reader.IsDBNull(reader.GetOrdinal("td_nombre")) ? null : reader.GetString("td_nombre"),
                                Descripcion = reader.IsDBNull(reader.GetOrdinal("td_descri")) ? null : reader.GetString("td_descri"),
                                Precio = reader.IsDBNull(reader.GetOrdinal("td_precio")) ? 0 : reader.GetDecimal("td_precio"), // Asumiendo que el precio es 0 si es NULL
                                Imagen = reader.IsDBNull(reader.GetOrdinal("td_img")) ? null : reader.GetString("td_img"),
                                Categoria = reader.IsDBNull(reader.GetOrdinal("id_grupo")) ? 0 : reader.GetInt32("id_grupo"), // Asumiendo un valor por defecto si es NULL
                                Detalle = reader.IsDBNull(reader.GetOrdinal("td_detall")) ? null : reader.GetString("td_detall"),
                                Stock = reader.IsDBNull(reader.GetOrdinal("td_exist")) ? false : reader.GetBoolean("td_exist"), // Asumiendo false si es NULL
                                Cantidad = reader.IsDBNull(reader.GetOrdinal("td_cantidad")) ? 0 : reader.GetInt32("td_cantidad") // Asumiendo un valor por defecto si es NULL
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
                                    Nombre = reader.IsDBNull(reader.GetOrdinal("td_nombre")) ? null : reader.GetString("td_nombre"),
                                    Descripcion = reader.IsDBNull(reader.GetOrdinal("td_descri")) ? null : reader.GetString("td_descri"),
                                    Precio = reader.IsDBNull(reader.GetOrdinal("td_precio")) ? 0 : reader.GetDecimal("td_precio"), // Suponiendo 0 como valor por defecto
                                    Imagen = reader.IsDBNull(reader.GetOrdinal("td_img")) ? null : reader.GetString("td_img"),
                                    Categoria = reader.IsDBNull(reader.GetOrdinal("id_grupo")) ? 0 : reader.GetInt32("id_grupo"), // Suponiendo 0 como valor por defecto
                                    Detalle = reader.IsDBNull(reader.GetOrdinal("td_detall")) ? null : reader.GetString("td_detall"),
                                    Stock = reader.IsDBNull(reader.GetOrdinal("td_exist")) ? false : reader.GetBoolean("td_exist"), // Suponiendo false como valor por defecto
                                    Cantidad = reader.IsDBNull(reader.GetOrdinal("td_cantidad")) ? 0 : reader.GetInt32("td_cantidad") // Suponiendo 0 como valor por defecto
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
                        var imagePath = Path.Combine(Server.MapPath("~/Archivos"), imagenSubida.FileName);
                        imagenSubida.SaveAs(imagePath);
                        producto.Imagen = "/Archivos/" + imagenSubida.FileName;
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
