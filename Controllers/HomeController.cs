﻿using MimeKit;
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

namespace Tienda.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataConexion _dataConexion; // Se crea una instancia de la clase DataConexion
        private readonly Cart _cart; // Se crea una instancia de la clase Cart
        public HomeController() // Constructor de la clase
        {
            _dataConexion = new DataConexion();
            _cart = new Cart();
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
            var referenceCode = GenerarReferencia();
            var amount = model.Cart.TotalPrice();
            var currency = "COP";
            var buyerEmail = model.Email; // Guardad en base de datos
            var accountId = "512321";
            var tax = 0;
            var taxReturnBase = 0;
            var test = "1";
            var phone = model.Phone; // Guardad en base de datos
            var fullName = $"{model.FirstName} {model.LastName}";  // Combina FirstName y LastName
            var address = model.StreetAddress; // Guardad en base de datos
            var document = model.DocumentType; // Guardad en base de datos
            var nit = model.VerificationDigit;
            var documentNumber = model.DocumentNumber; // Guardad en base de datos
            var company = model.CompanyName; // Guardad en base de datos
            var country = model.Country; // Guardad en base de datos
            var state = model.State; // Guardad en base de datos
            var city = model.City; // Guardad en base de datos
            var PostalCode = model.postalCode; // Guardad en base de datos
            var paymentMethods = "MASTERCARD,PSE,VISA";
            var responseUrl = "https://dc72-181-59-112-133.ngrok-free.app/Home/PayUResponse";
            var confirmationUrl = "https://dc72-181-59-112-133.ngrok-free.app/Home/Confirmation";
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
                        int userId = UsuarioInfo(buyerEmail, phone, fullName, address, document, documentNumber, company, country, state, city, PostalCode, nit, connection, transaction);
                        int orderId = Orden(userId, referenceCode, amount, productDescription, connection, transaction);
                        GuardarProductos(orderId, model.Cart, connection, transaction);

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
            string generatedSignature = CreateSignature(apiKey, model.Account_id, model.Reference_sale, model.Value, model.Currency, model.State_pol);
            var productos = ObtenerProductosPorOrden(model.Reference_sale);
            EnviarEmail(model, productos);  // Enviar correo con el resumen de la compra y el código QR
            if (generatedSignature.Equals(model.Sign, StringComparison.OrdinalIgnoreCase))
            {
                // La firma es válida
               // GuardarBase(model);  // Guardar en la base de datos
               // 
            }
            else
            {
                // La firma no es válida. 
            }

            return new EmptyResult();
        }


        public ActionResult PayUResponse(PayUResponse model)
        {
            string apiKey = "4Vj8eK4rloUd272L48hsrarnUA";
            string generatedSignature = CreateSignature2(apiKey, model.MerchantId, model.ReferenceCode, model.TX_VALUE, model.Currency, model.TransactionState);

            if (generatedSignature.Equals(model.Signature, StringComparison.OrdinalIgnoreCase))
            {
                model.IsSuccess = true;  // La firma es válida
                model.Message = "La transacción fue exitosa."; // Puedes configurar un mensaje de éxito aquí si lo deseas
            }
            else
            {
                model.IsSuccess = false;  // La firma no es válida
                model.Message = "Hubo un problema con la firma de la transacción."; // Puedes configurar un mensaje de error aquí
            }

            return View(model);  // Esta vista simplemente informa al usuario sobre el estado de la transacción
        }



        // Método para crear la firma de confirmación
        private string CreateSignature(string apiKey, int merchantId, string referenceCode, decimal txValue, string currency, string transactionState)
        {
            // Aproxima TX_VALUE a un decimal usando el método de redondeo Round half to even
            decimal newValue = Math.Round(txValue, 1, MidpointRounding.ToEven);

            // Genera la cadena para la firma
            string rawSignature = $"{apiKey}~{merchantId}~{referenceCode}~{newValue}~{currency}~{transactionState}";

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

        private string CreateSignature2(string apiKey, long merchantId, string referenceCode, decimal txValue, string currency, int transactionState)
        {
            // Aproxima TX_VALUE a un decimal usando el método de redondeo Round half to even
            decimal newValue = Math.Round(txValue, 1, MidpointRounding.ToEven);

            // Genera la cadena para la firma
            string rawSignature = $"{apiKey}~{merchantId}~{referenceCode}~{newValue.ToString("F1")}~{currency}~{transactionState}";

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


        private void EnviarEmail(PayUConfirmation model, List<Producto> productos)
        {
            try
            {

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("RmSoft", "victor.alarcon@utp.edu.co"));
                message.To.Add(new MailboxAddress(model.Email_buyer, "alarcongt4@gmail.com"));
                message.Subject = "Detalles de tu compra";

                var builder = new BodyBuilder();

                // Personalizar el mensaje con HTML
                var htmlContent = $"<div style='border: 1px solid black; padding: 5px;'>"; // Div con bordes
                htmlContent += $"<h2 style='text-align:center;'>Estimado(a) cliente - {model.Cc_holder}</h2>";
                htmlContent += $"<p style='text-align:center;'>Gracias por tu compra.</p>";
                htmlContent += "<h1 style='color:blue;text-align:center;'>Ha recibido una factura</h1>";
                htmlContent += "<h2 style='text-align:center;'>RESUMEN DEL DOCUMENTO</h2>";
                htmlContent += "<p style='text-align:center;'>Emisor: RM Soft Casa De Software SAS</p>";
                htmlContent += $"<p style='text-align:center;'>Tipo de Documento: Factura de venta</p>";
                htmlContent += $"<p style='text-align:center;'>Número de documento: {model.Reference_pol}</p>";
                htmlContent += $"<p style='text-align:center;'>Fecha de emisión: {model.Transaction_date}</p>";
                htmlContent += "<h3 style='text-align:center;'>Productos</h3>";
                htmlContent += "<ul style='text-align:center;'>";
                foreach (var producto in productos)
                {
                    htmlContent += $"<li>{producto.Nombre} - {producto.Precio}</li>";
                }
                htmlContent += $"</ul><p style='text-align:center;'><strong>Total:</strong> {model.Value} {model.Currency}</p>";

                // Generar el código QR
                var qrWriter = new ZXing.BarcodeWriterPixelData
                {
                    Format = ZXing.BarcodeFormat.QR_CODE,
                    Options = new ZXing.QrCode.QrCodeEncodingOptions
                    {
                        Height = 300,
                        Width = 300,
                        Margin = 2,
                        ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.H,
                        CharacterSet = "UTF-8"
                    }
                };

                var pixelData = qrWriter.Write(model.State_pol);
                var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);

                for (int y = 0; y < pixelData.Height; y++)
                {
                    for (int x = 0; x < pixelData.Width; x++)
                    {
                        int pixel = pixelData.Pixels[y * pixelData.Width + x];
                        Color color = Color.FromArgb(255, (pixel >> 16) & 255, (pixel >> 8) & 255, pixel & 255);
                        bitmap.SetPixel(x, y, color);
                    }
                }

                var qrMemoryStream = new MemoryStream();
                bitmap.Save(qrMemoryStream, ImageFormat.Png);

                // Añadir el QR directamente en el cuerpo del correo
                var attachment = builder.Attachments.Add("codigoQR.png", qrMemoryStream.ToArray(), new ContentType("image", "png"));
                attachment.ContentId = "codigoQR"; // Aquí estamos especificando el Content-ID para el adjunto
                htmlContent += $"<p style='text-align:center;'><img src=\"cid:{attachment.ContentId}\" alt=\"Código QR\" /></p>"; // Aquí referenciamos ese Content-ID
                htmlContent += "</div>"; // Cierre del div con bordes

                builder.HtmlBody = htmlContent;

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, false);
                    client.Authenticate("victor.alarcon@utp.edu.co", "rlhx qtvu ewxo uliq");
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar el correo: {ex.Message}");
            }
        }





        private List<Producto> ObtenerProductosPorOrden(string reference_sale)
        {
            var productos = new List<Producto>();

            using (var connection = _dataConexion.CreateConnection())
            {
                try
                {
                    connection.Open();

                    // Paso 1: Obtener el Id de la orden usando reference_sale
                    string ordenQuery = "SELECT Id FROM td_orden WHERE refere = @reference_sale";
                    using (var command = new MySqlCommand(ordenQuery, connection))
                    {
                        command.Parameters.AddWithValue("@reference_sale", reference_sale);
                        var orderId = command.ExecuteScalar();

                        if (orderId != null)
                        {
                            // Paso 2: Obtener ProductIds usando orderId
                            string producQuery = "SELECT ProductId FROM td_produc WHERE OrderId = @orderId";
                            using (var producCommand = new MySqlCommand(producQuery, connection))
                            {
                                producCommand.Parameters.AddWithValue("@orderId", orderId);
                                using (var reader = producCommand.ExecuteReader())
                                {
                                    var productIds = new List<string>();
                                    while (reader.Read())
                                    {
                                        productIds.Add(reader["ProductId"].ToString());
                                    }

                                    reader.Close(); // Cierra el reader después de usarlo

                                    // Ahora puedes ejecutar la siguiente consulta fuera del bloque anterior
                                    foreach (var productId in productIds)
                                    {
                                        // Paso 3: Obtener detalles del producto usando productId
                                        string mainQuery = "SELECT * FROM td_main WHERE id_main = @productId";
                                        using (var mainCommand = new MySqlCommand(mainQuery, connection))
                                        {
                                            mainCommand.Parameters.AddWithValue("@productId", productId);
                                            using (var mainReader = mainCommand.ExecuteReader())
                                            {
                                                if (mainReader.Read())
                                                {
                                                    var producto = new Producto
                                                    {
                                                        Nombre = mainReader["td_nombre"].ToString(),
                                                        Precio = decimal.Parse(mainReader["td_precio"].ToString())
                                                    };
                                                    productos.Add(producto);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Aquí puedes manejar el error, por ejemplo, registrar el error en un log, mostrar un mensaje, etc.
                    Console.WriteLine($"Error al obtener los productos: {ex.Message}");
                    // O también puedes decidir relanzar la excepción si es necesario
                    // throw;
                }
            }

            return productos;
        }





        private int UsuarioInfo(string buyerEmail, long phone, string fullName, string address, string document, string documentNumber, string company, string country, string state, string city, int PostalCode, int? nit, MySqlConnection connection, MySqlTransaction transaction)
        {
            // Definir la consulta SQL para insertar la información del usuario en la base de datos.
            string query = @"INSERT INTO td_user(email, telef, nombre, direc, tipo_doc, numer_doc, nomb_empr, pais, depart, city, postalnum, td_nit) 
                     VALUES (@Email, @Phone, @FullName, @Address, @DocumentType, @DocumentNumber, @CompanyName, @Country, @State, @City, @PostalCode, @nit);
                     SELECT LAST_INSERT_ID();";

            using (var command = new MySqlCommand(query, connection, transaction))
            {
                // Asignar valores a los parámetros de la consulta SQL.
                command.Parameters.AddWithValue("@Email", buyerEmail);
                command.Parameters.AddWithValue("@Phone", phone);
                command.Parameters.AddWithValue("@FullName", fullName);
                command.Parameters.AddWithValue("@Address", address);
                command.Parameters.AddWithValue("@DocumentType", document);
                command.Parameters.AddWithValue("@DocumentNumber", documentNumber);
                command.Parameters.AddWithValue("@CompanyName", company);
                command.Parameters.AddWithValue("@Country", country);
                command.Parameters.AddWithValue("@State", state);
                command.Parameters.AddWithValue("@City", city);
                command.Parameters.AddWithValue("@PostalCode", PostalCode);
                // Verificar si nit es nulo o no
                if (nit.HasValue)
                    command.Parameters.AddWithValue("@nit", nit);
                else
                    command.Parameters.AddWithValue("@nit", DBNull.Value);

                // Ejecutar la consulta SQL.
                return Convert.ToInt32(command.ExecuteScalar()); // Devuelve el ID del usuario recién creado.
            }
        }


        private int Orden(int userId, string referenceCode, decimal amount, string productDescription, MySqlConnection connection, MySqlTransaction transaction)
        {
            // Ahora, además de los otros campos, también insertamos el estado en la consulta SQL.
            string orderInsertQuery = @"INSERT INTO td_orden(User_Id, refere, total, descrip, td_estado)
                    VALUES (@UserId, @ReferenceCode, @Amount, @ProductDescription, @Estado);
                    SELECT LAST_INSERT_ID();"; // Devuelve el ID recién creado.

            using (var command = new MySqlCommand(orderInsertQuery, connection, transaction))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@ReferenceCode", referenceCode);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@ProductDescription", productDescription);
                command.Parameters.AddWithValue("@Estado", 1); // estado de la transacción

                return Convert.ToInt32(command.ExecuteScalar()); // Devuelve el ID de la orden recién creada.
            }
        }

        private string GenerarReferencia()
        {
            using (var connection = _dataConexion.CreateConnection())
            {
                connection.Open();

                // Consulta para obtener el último referenceCode
                string query = @"SELECT refere FROM td_orden ORDER BY Id DESC LIMIT 1;";

                using (var command = new MySqlCommand(query, connection))
                {
                    object result = command.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        // Si no hay registros, retorna "00001"
                        return "00001";
                    }
                    else
                    {
                        // Extrae el número del último referenceCode
                        string lastReference = result.ToString();
                        string lastNumber = lastReference.Split('-').Last(); // Suponiendo que el formato es "TestPayU-00001"

                        // Convierte ese número a int y súmale 1
                        int nextNumber = Convert.ToInt32(lastNumber) + 1;

                        // Retorna el número formateado a 5 dígitos
                        return nextNumber.ToString("D5");
                    }
                }
            }
        }



        private void GuardarProductos(int orderId, Cart cart, MySqlConnection connection, MySqlTransaction transaction)
        {

            foreach (var item in cart.Items)
                {
                 string orderProductInsertQuery = "INSERT INTO td_produc(OrderId, ProductId, cantidad) VALUES (@OrderId, @ProductId, @Quantity)";

                using (var command = new MySqlCommand(orderProductInsertQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@ProductId", item.Product.Id);  
                    command.Parameters.AddWithValue("@Quantity", item.Quantity); 
                    command.ExecuteNonQuery();
                }
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
                }

                // Añadir el producto al carrito.
                cart.AddProduct(product, quantity);

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


        private Producto GetProductById(int productId) // Método para obtener un producto por su ID
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
                                Imagen = reader.GetString("td_img")
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

        public ActionResult About() // aun sin implementar
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact() // aun sin implementar
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Politica() // Acción para mostrar la política de privacidad
        {

            return View();
        }
       
    }
}