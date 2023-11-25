using MimeKit;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using Tienda.Data;
using Tienda.Models;
using System.Net.Mail;
using System.Windows.Controls.Primitives;
using Tienda.Interfaces;
using Tienda.Servicios;
using static System.Net.Mime.MediaTypeNames;

namespace Tienda.Controllers
{
    public class HomeController : Controller
    {
        private readonly IReferenciaService _referenciaService;
        private readonly IOrdenService _ordenService;
        private readonly IFactura _facturaService;
        private readonly IProductoService _productoService;
        private readonly IMainService _mainService;

        private readonly DataConexion _dataConexion; // Se crea una instancia de la clase DataConexion
        private readonly Cart _cart; // Se crea una instancia de la clase Cart
        public HomeController() // Constructor de la clase
        {
            _dataConexion = new DataConexion();
            _cart = new Cart();
            _referenciaService = new ReferenciaService(_dataConexion);
            _ordenService = new OrdenService(_dataConexion); // Inicializar _ordenService
            _facturaService = new FacturaService(_dataConexion);
            _productoService = new ProductoService(_dataConexion);
            _mainService = new MainService(_dataConexion);
        }

        [HttpGet] // Acción para mostrar el formulario de pago
        public ActionResult Checkout()
        {
            var cart = Session["cart"] as Cart;
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Carrito");
            }

            var model = new PaymentInfo { Cart = cart };
            return View(model);
        }

        [HttpPost] // Acción para procesar el pago y redirigir a PayU. 
        public ActionResult Checkout(PaymentInfo model)
        {
            StringBuilder productNames = new StringBuilder();
            // Recuperar el carrito de la sesión
            model.Cart = Session["cart"] as Cart;
            // Se prepara los datos para PayU
            var merchantId = "508029";
            var apiKey = "4Vj8eK4rloUd272L48hsrarnUA";
            var referenceCode = _referenciaService.GenerarReferencia();
            var amount = model.Cart.TotalPrice();
            var currency = "COP";
            var buyerEmail = model.Email; // Guardad en base de datos
            var accountId = "512321";
            var tax = 0;
            var taxReturnBase = 0;
            var test = "1";
            var phone = model.Phone; // Guardad en base de datos
            var fullName = $"{model.FirstName} {model.MiddleName} {model.LastName} {model.SecondLastName}";  // Combina FirstName y LastName
            var paymentMethods = "MASTERCARD,PSE,VISA";
            var responseUrl = "https://722a-186-147-92-76.ngrok-free.app/Home/PayUResponse";
            var confirmationUrl = "https://722a-186-147-92-76.ngrok-free.app/Home/Confirmation";
            // Genera la firma
            var formattedAmount = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            var signature = GenerarFirma(apiKey, merchantId, referenceCode, formattedAmount, currency, paymentMethods); // Llama al método para generar la firma

            foreach (var item in model.Cart.Items) // Guardad todos los productos en base de datos 
            {
                if (productNames.Length > 0)
                {
                    productNames.Append(", ");
                }
                productNames.Append(item.Product.Nombre);
            }
            string productDescription = productNames.ToString();
            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int orderId = _ordenService.Orden(model, referenceCode, connection, transaction);
                        _productoService.GuardarProductos(orderId, model.Cart, connection, transaction);

                        // Si todo ha ido bien
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;  // Puedes manejar el error como prefieras o informar al usuario.
                    }
                }
            }
            // Se crea un nuevo modelo para pasar a la vista de PayU
            var payUModel = new PayUModel
            {
                MerchantId = merchantId,
                ApiKey = apiKey,
                ReferenceCode = referenceCode,
                Amount = amount,  // mantener amount como decimal
                Currency = currency,
                BuyerEmail = buyerEmail,
                Signature = signature,
                AccountId = accountId,
                Tax = tax,
                TaxReturnBase = taxReturnBase,
                Test = test,
                ResponseUrl = responseUrl,
                ConfirmationUrl = confirmationUrl,
                Description = productDescription,
                Telephone = phone,
                BuyerfullName = fullName,
                paymentMethods = paymentMethods
            };

            return View("PayUForm", payUModel);
        }




        // Método para generar la firma de la solicitud
        private string GenerarFirma(string apiKey, string merchantId, string referencia, string precio, string currency, string paymentMethods)
        {
            var datos = $"{apiKey}~{merchantId}~{referencia}~{precio}~{currency}~{paymentMethods}";
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(datos);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString().ToLower(); // Convertir a minúsculas si es necesario.
            }
        }


        [HttpPost]
        public ActionResult Confirmation(PayUConfirmation model)
        {
            System.Diagnostics.Debug.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(model)); // para imprimir todo el modelo recibido.
            string apiKey = "4Vj8eK4rloUd272L48hsrarnUA";
            string generatedSignature = CreateSignature(apiKey, model.Account_id, model.Reference_sale, model.Currency, model.State_pol);

            if (model.Response_message_pol == "APPROVED" && model.State_pol == "4")
            {
                var productos = _ordenService.ObtenerProductosPorOrden(model.Reference_sale);
                var datos = _ordenService.Obtenerdatos(model.Reference_sale);
                EnviarEmail(model, productos, datos);  // Enviar correo con el resumen de la compra y el código QR
                GuardarBase(model);  // Guardar en la base de datos
            }
            else
            {
                GuardarBase(model);
            }

            return new EmptyResult();
        }


        public ActionResult PayUResponse(PayUResponse model)
        {
            string apiKey = "4Vj8eK4rloUd272L48hsrarnUA";
            string generatedSignature = CreateSignature2(apiKey, model.MerchantId, model.ReferenceCode, model.Currency, model.TransactionState);

            if (model.Message == "APPROVED" && model.TransactionState == 4)
            {
                model.IsSuccess = true;  // La firma es válida y la transacción está aprobada
                model.Message = "La transacción fue exitosa."; // Puedes configurar un mensaje de éxito aquí si lo deseas
            }
            else
            {
                model.IsSuccess = false;  // La firma no es válida o la transacción no está aprobada
                model.Message = "Hubo un problema con la transacción."; // Puedes configurar un mensaje de error aquí
            }

            return View(model);  // Esta vista simplemente informa al usuario sobre el estado de la transacción
        }



        // Método para crear la firma de confirmación
        private string CreateSignature(string apiKey, int merchantId, string referenceCode, string currency, string transactionState)
        {
            // Aproxima TX_VALUE a un decimal usando el método de redondeo Round half to even
            decimal txValue = _ordenService.GetTotalFromDatabase(referenceCode);

            // Genera la cadena para la firma
            var rawSignature = $"{apiKey}~{merchantId}~{referenceCode}~{txValue}~{currency}~{transactionState}";

            // Calcula el hash MD5
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(rawSignature);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private string CreateSignature2(string apiKey, long merchantId, string referenceCode, string currency, int transactionState)
        {
            // Obtener el valor de la base de datos
            decimal txValue = _ordenService.GetTotalFromDatabase(referenceCode);

            // Genera la cadena para la firma
            string rawSignature = $"{apiKey}~{merchantId}~{referenceCode}~{txValue}~{currency}~{transactionState}";

            // Calcula el hash MD5
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(rawSignature));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }

        private void EnviarEmail(PayUConfirmation model, List<Producto> productos, DatosCliente datos)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Bioparque Ukumarí", "sistemas.rmsoft@gmail.com"));
                message.To.Add(new MailboxAddress(datos.Email, datos.Email)); // usando el email de datos
                message.Subject = "Detalles de tu compra";

                var builder = new BodyBuilder();
                string nombreCompleto = $"{datos.Nombre} {datos.Nombre2} {datos.Apellido} {datos.Apellido2}".Trim();

                // Personalizar el mensaje con HTML
                var htmlContent = "<div style='border: 1px solid black; padding: 10px;'>";

                // Añadir imagen de encabezado
                //htmlContent += "<img src='cid:logoImage' alt='Logo' style='width:100%; height:auto;'/>";
                htmlContent += "<table width='100%' style='border-collapse: collapse;'>";
                htmlContent += "<tr style='border-bottom: 1px solid #000;'>";
                htmlContent += "<td><strong>Estimado cliente</strong></td>";
                htmlContent += $"<td align='right'><strong>{nombreCompleto}</strong></td>";
                htmlContent += "</tr>";
                htmlContent += "<tr style='border-bottom: 1px solid #000;'>";
                htmlContent += "<td><strong>Emisor</strong></td>";
                htmlContent += "<td align='right'><strong>Parque Temático De Flora Y Fauna De Pereira SAS</strong></td>";
                htmlContent += "</tr>";
                htmlContent += "<tr>";
                htmlContent += $"<td><strong>Tipo de Documento:</strong></td>";
                htmlContent += "<td align='right'>Factura de venta</td>";
                htmlContent += "</tr>";
                htmlContent += "<tr>";
                htmlContent += $"<td><strong>Número de la orden:</strong></td>";
                htmlContent += $"<td align='right'>{model.Reference_pol}</td>";
                htmlContent += "</tr>";
                htmlContent += "<tr>";
                htmlContent += $"<td><strong>Fecha de emisión:</strong></td>";
                htmlContent += $"<td align='right'>{model.Transaction_date}</td>";
                htmlContent += "</tr>";
                htmlContent += "<tr style='border-top: 2px solid #000;'>";
                htmlContent += "</table>";

                // Tabla de productos
                htmlContent += "<table width='100%' style='border-collapse: collapse; margin-top: 20px;'>";
                htmlContent += "<tr style='background-color: #f2f2f2;'>";
                htmlContent += "<th style='text-align: center;'>Producto</th>";
                htmlContent += "<th style='text-align: center;'>Cantidad</th>";
                htmlContent += "<th style='text-align: center;'>Valor Unidad</th>";
                htmlContent += "<th style='text-align: center;'>Valor Total</th>";
                htmlContent += "</tr>";

                foreach (var producto in productos)
                {
                    htmlContent += "<tr>";
                    htmlContent += $"<td style='text-align: center;'>{producto.Nombre}</td>";
                    htmlContent += $"<td style='text-align: center;'>{producto.Cantidad}</td>";
                    htmlContent += $"<td style='text-align: center;'>{producto.Precio:C}</td>"; // Formato de moneda para el precio
                    htmlContent += $"<td style='text-align: center;'>{producto.ValorTotal:C}</td>"; // Formato de moneda para el valor total
                    htmlContent += "</tr>";
                }
                htmlContent += "</table>";


                var qrGenerator = new QRCoder.QRCodeGenerator();
                var qrData = qrGenerator.CreateQrCode(model.Reference_pol, QRCoder.QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCoder.QRCode(qrData);

                var qrBitmap = qrCode.GetGraphic(10); // Ajusta el valor '10' si necesitas un tamaño diferente para el QR Code

                var qrMemoryStream = new MemoryStream();
                qrBitmap.Save(qrMemoryStream, ImageFormat.Png);


                // Total
                htmlContent += "<tr style='border-top: 2px solid #000;'>";
                htmlContent += "<td colspan='3' align='right'><strong>Valor Total: </strong></td>";
                htmlContent += $"<td><strong>{datos.Total:C}</strong></td>";
                htmlContent += "</tr>";
                htmlContent += "</table>";

                // Mensaje de agradecimiento y QR
                htmlContent += "<h2 style='text-align:center;'>Gracias por tu compra</h2>";
                htmlContent += "<p style='text-align:center;'><img src='cid:qrCodeImage' alt='QR Code' /></p>";
                htmlContent += "<p style='text-align:center;'>Muestra el código QR en la entrada del Parque para ingresar. </p>";
                htmlContent += "</div>"; // Cierre del div con bordes


                //qrMemoryStream.Position = 0;

                //// Para agregar la imagen del encabezado y el código QR como imágenes incrustadas en el correo
                //var logoPath = System.Web.HttpContext.Current.Server.MapPath("~/Imagenes/Ukumari.jpg"); // Ajusta la ruta según donde tengas la imagen
                //var logoBytes = System.IO.File.ReadAllBytes(logoPath);
                //var logoImage = new MimePart("image", "jpeg")
                //{
                //    Content = new MimeContent(new MemoryStream(logoBytes), ContentEncoding.Base64),
                //    ContentId = "logoImage",
                //    ContentDisposition = new ContentDisposition { FileName = Path.GetFileName(logoPath) },
                //    ContentTransferEncoding = ContentEncoding.Base64
                //};



                // Ahora convertimos el MemoryStream del QR en MimePart
                var qrCodeImage = new MimePart("image", "png")
                {
                    Content = new MimeContent(qrMemoryStream, ContentEncoding.Default),
                    ContentId = "qrCodeImage",
                    FileName = "qrCode.png"
                };

                builder.HtmlBody = htmlContent;

                // Agregar imágenes como incrustadas
                //builder.LinkedResources.Add(logoImage);
                builder.LinkedResources.Add(qrCodeImage);

                message.Body = builder.ToMessageBody();

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, false);
                    client.Authenticate("sistemas.rmsoft@gmail.com", "ektq xifn kjsc mwoy");
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar el correo: {ex.Message}");
            }
        }

        public void GuardarBase(PayUConfirmation model)
        {
            using (var connection = _dataConexion.CreateConnection())
            {
                try
                {
                    connection.Open();

                    int userId = _ordenService.ObtenerUserId(model.Reference_sale, connection);
                    var datosUsuario = _ordenService.ObtenerDatosUsuario(userId, connection);
                    int idFac = _facturaService.InsertarEnTdFac(datosUsuario, model, connection);
                    _productoService.ConsultarYTransferirProductos(userId, idFac, connection);
                    _ordenService.EliminarDeTdOrden(userId, connection);
                    //  EliminarProductosDeTdProduc(userId, connection);

                    connection.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al guardar en td_fac: {ex.Message}");
                }
            }
        }

        //private void EliminarProductosDeTdProduc(int userId, MySqlConnection connection)
        //{
        //    try
        //    {
        //        // Eliminar productos de td_produc
        //        string queryDeleteProducts = "DELETE FROM td_produc WHERE OrderId = @UserId";
        //        using (var commandDeleteProducts = new MySqlCommand(queryDeleteProducts, connection))
        //        {
        //            commandDeleteProducts.Parameters.AddWithValue("@UserId", userId);
        //            commandDeleteProducts.ExecuteNonQuery();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Error al eliminar productos de td_produc: {ex.Message}");
        //    }
        //}

        public JsonResult GetDepartamentos() // Método para obtener los departamentos
        {
            try
            {
                using (var connection = _dataConexion.CreateConnection())
                {
                    connection.Open();
                    var departamentos = new List<string>();
                    using (var command = new MySqlCommand("SELECT DISTINCT CityNdepto FROM xxxxcity", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                departamentos.Add(reader.GetString("CityNdepto"));
                            }
                        }
                    }
                    return Json(departamentos, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener departamentos: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        public JsonResult GetCiudadesPorDepartamento(string departamento) // Método para obtener las ciudades de un departamento
        {
            try
            {
                using (var connection = _dataConexion.CreateConnection())
                {
                    connection.Open();
                    var ciudades = new List<object>(); // Modificado para contener objetos con ciudad y código
                    using (var command = new MySqlCommand("SELECT citynomb, citycodigo FROM xxxxcity WHERE CityNdepto = @departamento", connection))
                    {
                        command.Parameters.AddWithValue("@departamento", departamento);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string ciudadCompleta = reader.GetString("citynomb");
                                string ciudad = ciudadCompleta.Split(',')[0]; // Tomar solo el nombre de la ciudad
                                string codigo = reader.GetString("citycodigo"); // Obtener el código de la ciudad
                                ciudades.Add(new { Ciudad = ciudad, Codigo = codigo });
                            }
                        }
                    }
                    return Json(ciudades, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener ciudades: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }

        }

        [HttpPost] // Accion para agregar un producto al carrito
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
                var product = _mainService.GetProductById(productId);
                if (product == null)
                {
                    return Json(new { success = false, error = "Producto no encontrado" });
                }

                // Recuperar el carrito de la sesión, o crear uno nuevo si no existe.
                var cart = Session["cart"] as Cart;
                if (cart == null)
                {
                    cart = new Cart();
                }

                // Añadir el producto al carrito.
                cart.AddProduct(product, quantity);
                int itemCount = cart.GetTotalItemCount();
                Session["cartItemCount"] = itemCount;
                Session["cart"] = cart;

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost] // Acción para eliminar un producto del carrito
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

        public ActionResult Carrito() // Acción para mostrar el carrito
        {
            var cart = Session["cart"] as Cart;
            if (cart == null)
            {
                cart = new Cart();
                Session["cart"] = cart;
            }
            return View(cart);
        }

        [HttpPost] // Acción para actualizar la cantidad de un producto en el carrito
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

        public ActionResult CategoryNav() // Acción para mostrar las categorías en la barra de navegación
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

        public ActionResult Index(int? categoryId) // Accion para mostrar los productos en la página principal
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
                                Imagen = reader.GetString("td_img"),
                                Detalle = reader.GetString("td_detall")
                            });
                        }
                    }
                }
            }
            ViewBag.ProductCount = products.Count;
            return View(products);
        }

        public ActionResult Details(int id) // Acción para mostrar los detalles de un producto
        {
            Producto product = null;
            List<Producto> relatedProducts;
            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();

                // Modificar la consulta para incluir una cláusula WHERE
                string query = @"
                            SELECT m.*, g.td_nombre AS NombreGrupo 
                            FROM td_main m
                            LEFT JOIN td_grupos g ON m.id_grupo = g.id_grupo
                            WHERE m.id_main = @Id";

                using (var command = new MySqlCommand(query, connection))
                {
                    // Añadir el parámetro ID al comando
                    command.Parameters.AddWithValue("@Id", id);

                    using (var reader = command.ExecuteReader())
                    {
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
                                GrupoNombre = reader.IsDBNull(reader.GetOrdinal("NombreGrupo")) ? null : reader.GetString("NombreGrupo"), // Se Utiliza IsDBNull para comprobar si el campo es NULL
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

        public ActionResult Politica() // Acción para mostrar la política de privacidad
        {

            return View();
        }

        [HttpGet]
        public ActionResult CalcularDigitoVerificacion(string nit)
        {
            try
            {
                int dv = Calcular(nit);
                return Json(new { success = true, digitoVerificacion = dv }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }


        public static int Calcular(string nit)
        {
            int[] pesos = { 71, 67, 59, 53, 47, 43, 41, 37, 29, 23, 19, 17, 13, 7, 3 };
            int suma = 0;
            nit = nit.PadLeft(15, '0');

            for (int i = 0; i < 15; i++)
            {
                suma += (int)char.GetNumericValue(nit[i]) * pesos[i];
            }

            int modulo = suma % 11;
            return (modulo < 2) ? modulo : 11 - modulo;
        }

        [HttpGet]
        public ActionResult GetCartItemCount()
        {
            var cart = Session["cart"] as Cart;
            if (cart != null)
            {
                return Json(new { itemCount = cart.GetTotalItemCount() }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { itemCount = 0 }, JsonRequestBehavior.AllowGet);
        }


    }
}