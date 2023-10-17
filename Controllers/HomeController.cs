using MySqlConnector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tienda.Data;
using Tienda.Models;




namespace Tienda.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataConexion _dataConexion;
        private readonly Cart _cart;
        public HomeController()
        {
            _dataConexion = new DataConexion();
            _cart = new Cart();
        }

        public ActionResult Checkout()
        {
            var cart = Session["cart"] as Cart;
            if (cart == null || !cart.Items.Any())
            {
                // Si el carrito está vacío, redirige de vuelta a la vista del carrito o muestra un mensaje de error.
                return RedirectToAction("Carrito");
            }

            return View(cart);
        }


        [HttpPost]
        public ActionResult AddToCart()
        {
            try
            {
                // Leer el cuerpo de la solicitud como una cadena
                System.IO.Stream bodyStream = Request.InputStream;
                System.Text.Encoding encoding = Request.ContentEncoding;
                System.IO.StreamReader reader = new System.IO.StreamReader(bodyStream, encoding);

                char[] readBuffer = new char[256];
                int count = reader.Read(readBuffer, 0, 256);

                string requestBodyData = string.Empty;
                while (count > 0)
                {
                    string outputData = new string(readBuffer, 0, count);
                    requestBodyData += outputData;
                    count = reader.Read(readBuffer, 0, 256);
                }
                reader.Close();
                bodyStream.Close();

                // Parsear la cadena JSON en un objeto JObject
                JObject data = JObject.Parse(requestBodyData);
                var productId = data["productId"].ToObject<int>();
                var quantity = data.ContainsKey("quantity") ? data["quantity"].ToObject<int>() : 1;

                // Asumiendo que tienes una forma de obtener un producto por su ID
                var product = GetProductById(productId);
                if (product == null)
                {
                    return Json(new { success = false, error = "Producto no encontrado" });
                }

                // Recuperar el carrito de la sesión, o crear uno nuevo si no existe.
                var cart = Session["cart"] as Cart;
                if (cart == null)
                {
                    cart = new Cart();
                    Session["cart"] = cart;
                }

                // Añadir el producto al carrito.
                cart.AddProduct(product, quantity);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult RemoveFromCart(int productId)
        {
            // Obtener el carrito de la sesión
            var cart = Session["cart"] as Cart;
            if (cart == null)
            {
                return HttpNotFound();  // Retorna un error si no hay carrito en la sesión
            }

            // Buscar el item en el carrito
            var itemToRemove = cart.Items.SingleOrDefault(item => item.Product.Id == productId);
            if (itemToRemove == null)
            {
                return HttpNotFound();  // Retorna un error si el producto no se encuentra en el carrito
            }

            // Eliminar el item del carrito
            cart.Items.Remove(itemToRemove);

            // Guardar el carrito actualizado en la sesión
            Session["cart"] = cart;

            // Guardar mensaje de éxito en TempData
            TempData["SuccessMessage"] = $"Producto \"{itemToRemove.Product.Nombre}\" eliminado. <a href='#' class='undo-link'>¿Deshacer?</a>";

            return RedirectToAction("Carrito");  // Retorna una respuesta exitosa si el producto fue eliminado correctamente
        }


        public ActionResult Carrito()
        {
            var cart = Session["cart"] as Cart;
            if (cart == null)
            {
                cart = new Cart();
                Session["cart"] = cart;
            }
            return View(cart);
        }

        [HttpPost]
        public JsonResult UpdateQuantity(int productId, int quantity)
        {
            var cart = Session["cart"] as Cart;
            if (cart == null)
            {
                return Json(new { success = false });
            }

            var itemToUpdate = cart.Items.SingleOrDefault(item => item.Product.Id == productId);
            if (itemToUpdate == null)
            {
                return Json(new { success = false });
            }

            itemToUpdate.Quantity = quantity;  // Actualiza la cantidad
            Session["cart"] = cart;  // Guarda el carrito actualizado en la sesión

            return Json(new { success = true });
        }


        private Producto GetProductById(int productId)
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
                                Imagen = reader.GetString("td_img")
                            };
                        }
                    }
                }
            }
            // Si no se encuentra el producto, devolver null.
            return null;
        }

        public ActionResult CategoryNav()
        {
            var categories = new List<Categoria>();
            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand("SELECT * FROM td_grupos", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new Categoria
                        {
                            Id = reader.GetInt32("id_grupo"),
                            Nombre = reader.GetString("td_nombre")
                        });
                    }
                }
            }
            return PartialView("_CategoryNav", categories);
        }

        public ActionResult Index(int? categoryId)
        {
            var products = new List<Producto>();
            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();

                // Definir la consulta básica.
                string query = "SELECT * FROM td_main";

                // Si categoria tiene un valor, modificar la consulta para filtrar por categoría.
                if (categoryId.HasValue)
                {
                    query += " WHERE id_grupo = @CategoryId";
                }

                using (var command = new MySqlCommand(query, connection))
                {
                    //  categoria tiene un valor, añadir el parámetro a la consulta.
                    if (categoryId.HasValue)
                    {
                        command.Parameters.AddWithValue("@CategoryId", categoryId.Value);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Producto
                            {
                                Id = reader.GetInt32("id_main"),
                                Nombre = reader.GetString("td_nombre"),
                                Descripcion = reader.GetString("td_descri"),
                                Precio = reader.GetDecimal("td_precio"),
                                Imagen = reader.GetString("td_img")
                            });
                        }
                    }
                }
            }
            ViewBag.ProductCount = products.Count;
            return View(products);
        }

        public ActionResult Details(int id)
        {
            Producto product = null;
            List<Producto> relatedProducts;
            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();

                // Modificar la consulta para incluir una cláusula WHERE
                string query = "SELECT * FROM td_main WHERE id_main = @Id";
                using (var command = new MySqlCommand(query, connection))
                {
                    // Añadir el parámetro ID al comando
                    command.Parameters.AddWithValue("@Id", id);

                    using (var reader = command.ExecuteReader())
                    {
                        // Usar if en lugar de while ya que sólo estás buscando un único producto
                        if (reader.Read())
                        {
                            product = new Producto
                            {
                                Id = reader.GetInt32("id_main"),
                                Nombre = reader.GetString("td_nombre"),
                                Descripcion = reader.GetString("td_descri"),
                                Precio = reader.GetDecimal("td_precio"),
                                Imagen = reader.GetString("td_img"),
                                Id_Grupo = reader.GetInt32("id_grupo"),
                                Detalle = reader.GetString("td_detall")
                            };
                        }
                    }
                }
                // Obtén el id_grupo del producto actual
                var groupId = product.Id_Grupo;
                string relatedQuery = "SELECT * FROM td_main WHERE id_grupo = @IdGrupo AND id_main != @ProductId";
                using (var command = new MySqlCommand(relatedQuery, connection))
                {
                    command.Parameters.AddWithValue("@IdGrupo", groupId);
                    command.Parameters.AddWithValue("@ProductId", id);

                    using (var reader = command.ExecuteReader())
                    {
                        relatedProducts = new List<Producto>();
                        while (reader.Read())
                        {
                            relatedProducts.Add(new Producto
                            {
                                Id = reader.GetInt32("id_main"),
                                Nombre = reader.GetString("td_nombre"),
                                Descripcion = reader.GetString("td_descri"),
                                Precio = reader.GetDecimal("td_precio"),
                                Imagen = reader.GetString("td_img")
                            });
                        }
                    }
                }
            }

            if (product == null)
            {
                // Manejar el caso en que no se encontró el producto, por ejemplo, redirigir a una página de error
                return RedirectToAction("Error", "Home");
            }
            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            return View(viewModel);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Politica()
        {

            return View();
        }

    }
}