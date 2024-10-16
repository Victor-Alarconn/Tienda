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
using System.Windows.Controls.Primitives;
using Tienda.Interfaces;
using Tienda.Servicios;
using static System.Net.Mime.MediaTypeNames;
using System.Configuration;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Dynamic;

namespace Tienda.Controllers
{
    public class HomeController : Controller
    {
        private readonly IReferenciaService _referenciaService;
        private readonly IOrdenService _ordenService;
        private readonly IFactura _facturaService;
        private readonly IProductoService _productoService;
        private readonly IMainService _mainService;
        private string publicidadPath = ("~/Imagenes/Publicidad");
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

        [HttpGet]
        public JsonResult VerificarPublicidadActiva()
        {
            // Ruta donde se guardan las imágenes y el archivo de fechas
            string path = Server.MapPath(publicidadPath);
            var publicidadFiles = Directory.GetFiles(path);

            foreach (var file in publicidadFiles)
            {
                string fechaFileName = Path.Combine(path, Path.GetFileNameWithoutExtension(file) + "_fecha.txt");

                if (System.IO.File.Exists(fechaFileName))
                {
                    try
                    {
                        // Lee el contenido del archivo de fechas
                        var fechaContenido = System.IO.File.ReadAllText(fechaFileName);
                        var fechas = fechaContenido.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        if (fechas.Length >= 2)
                        {
                            // Extraer fechas de inicio y fin
                            DateTime fechaInicio = DateTime.Parse(fechas[0].Replace("Inicio: ", "").Trim());
                            DateTime fechaFin = DateTime.Parse(fechas[1].Replace("Fin: ", "").Trim());
                            DateTime fechaActual = DateTime.Now;

                            if (fechaActual >= fechaInicio && fechaActual <= fechaFin)
                            {
                                // Si la publicidad está activa, devolver los datos necesarios
                                return Json(new { Activa = true, Publicidad = Path.GetFileName(file), FechaInicio = fechaInicio, FechaFin = fechaFin }, JsonRequestBehavior.AllowGet);
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
                        return Json(new { Activa = false, Error = "Error al procesar la imagen de publicidad: " + ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            // Si no hay publicidad activa
            return Json(new { Activa = false }, JsonRequestBehavior.AllowGet);
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
        public async Task<ActionResult> Checkout(PaymentInfo model)
        {
            StringBuilder productNames = new StringBuilder();
            model.Cart = Session["cart"] as Cart;

            // Datos de la API
            var login = "16af31f43647987414b5ced2164947c5";
            var secretKey = "e76GdAOfhzuytyI9";
            var seed = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK");

            // Generación de `nonce`
            var random = new Random();
            var nonceNumber = random.Next(100000000, 999999999);
            var nonce = Convert.ToBase64String(Encoding.UTF8.GetBytes(nonceNumber.ToString()));

            // Crear `tranKey` con SHA-256
            string tranKey;
            string llave;
            using (var sha256 = SHA256.Create())
            {
                var tranKeyInput = nonceNumber.ToString() + seed + secretKey;
                llave = tranKeyInput.ToString();
                var tranKeyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(tranKeyInput));
                tranKey = Convert.ToBase64String(tranKeyBytes);
            }

            // Configurar datos de la solicitud a la API
            var expiration = DateTime.Now.AddHours(24).ToString("yyyy-MM-ddTHH:mm:ssK");
            var requestBody = new
            {
                buyer = new
                {
                    name = model.FirstName,
                    surname = model.LastName,
                    email = model.Email,
                    document = model.DocumentNumber, // Campo que puede estar en el modelo PaymentInfo
                    documentType = "CC", // Cambiar según corresponda
                    mobile = model.Phone
                },
                payment = new
                {
                    reference = _referenciaService.GenerarReferencia(),
                    description = "Pago de productos en la tienda",
                    amount = new
                    {
                        currency = "COP",
                        total = model.Cart.TotalPrice()
                    }
                },
                expiration = expiration,
                ipAddress = "181.55.25.206",
                returnUrl = "https://8e93-181-55-25-206.ngrok-free.app/Home/PayUResponse",
                userAgent = Request.UserAgent,
                paymentMethod = "MASTERCARD,PSE,VISA,DINERS",
                auth = new
                {
                    login = login,
                    tranKey = tranKey,
                    nonce = nonce,
                    seed = seed
                }
            };

            //using (var connection = _dataConexion.CreateConnection())
            //{
            //    connection.Open();
            //    using (var transaction = connection.BeginTransaction())
            //    {
            //        try
            //        {
            //            int orderId = _ordenService.Orden(model, requestBody.payment.reference, connection, transaction);
            //            _productoService.GuardarProductos(orderId, model.Cart, connection, transaction);

            //            transaction.Commit();
            //        }
            //        catch (Exception)
            //        {
            //            transaction.Rollback();
            //            throw;
            //        }
            //    }
            //}

            // Llamada HTTP a la API (reemplaza la URL con la de tu API)
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://checkout.test.goupagos.com.co");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                string requestData = await content.ReadAsStringAsync();

                HttpResponseMessage response = await client.PostAsync("/api/session", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject<dynamic>(responseData);

                    // Obtén la URL de pago correcta
                    ViewBag.PaymentUrl = result?.processUrl; // Cambia a "processUrl"
                    ViewBag.ReferenceCode = _referenciaService.GenerarReferencia(); // Completa con los datos necesarios
                    ViewBag.Amount = model.Cart.TotalPrice();

                    return View("PayUForm"); // Asegúrate de que sea el nombre correcto de la vista
                }
                else
                {
                    return View("Error"); // Puedes redirigir a una vista de error si la solicitud falla
                }
            }
        }



        public JsonResult ObtenerColores()
        {
            string rutaColores = Server.MapPath("~/App_Data/colores.json");
            if (!System.IO.File.Exists(rutaColores))
            {
                var coloresPorDefecto = new Dictionary<string, string>
        {
            { "navbarColor", "#007BFF" },
            { "sliderColor", "#d6f792" }
        };
                return Json(coloresPorDefecto, JsonRequestBehavior.AllowGet);
            }

            var json = System.IO.File.ReadAllText(rutaColores);
            var colores = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            return Json(colores, JsonRequestBehavior.AllowGet);
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


      
        public ActionResult PayUResponse()
        {
            // Leer el cuerpo de la solicitud
            using (var reader = new System.IO.StreamReader(Request.InputStream))
            {
                var jsonResponse = reader.ReadToEnd();
                // Asegúrate de que jsonResponse no sea nulo
                if (string.IsNullOrEmpty(jsonResponse))
                {
                    return new HttpStatusCodeResult(400, "El contenido de la respuesta está vacío.");
                }

                // Deserializar la respuesta JSON
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);

                // Asegúrate de que 'status' no sea nulo antes de acceder a él
                if (response.TryGetValue("status", out var statusValue))
                {
                    var status = ((Newtonsoft.Json.Linq.JToken)statusValue).ToObject<Dictionary<string, object>>();
                    var internalReference = response["internalReference"];
                    var reference = response["reference"];
                    var signature = response["signature"];

                    // Crear un nuevo modelo para pasar a la vista
                    var model = new PayUResponse
                    {
                        IsSuccess = (string)status["status"] == "APPROVED",
                        Message = (string)status["message"],
                        TransactionId = (string)reference,
                      //  TX_VALUE = "N/A", // Ajusta esto según cómo manejas el monto
                        Description = "N/A" // Ajusta esto según cómo manejas la descripción
                    };

                    // Retornar la vista con el modelo
                    return View(model);
                }
                else
                {
                    return new HttpStatusCodeResult(400, "No se encontró el estado en la respuesta.");
                }
            }
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


        [HttpPost]
        public ActionResult GoUConfirmation(GoUResponse model)
        {
            // Paso 1: Imprimir el modelo recibido para depuración
            System.Diagnostics.Debug.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(model));

            // Paso 2: Validar la firma
            string secretKey = "tuLlaveSecreta"; // Cambia esto por tu clave secreta de GoU
            string generatedSignature = GenerateSignature(model.InternalReference, model.Status.Status, secretKey);

            if (model.Status.Status == "APPROVED" && model.Status.Reason == "00")
            {
                // Aquí puedes agregar la lógica para guardar la transacción o enviar un correo
                var productos = _ordenService.ObtenerProductosPorOrden(model.Reference);
                var datos = _ordenService.Obtenerdatos(model.Reference);
              //  EnviarEmail(model, productos, datos);  // Enviar correo con el resumen de la compra
              //  GuardarBase(model);  // Guardar en la base de datos
            }
            else
            {
              //  GuardarBase(model);  // Guarda el estado de la transacción aunque no sea exitosa
            }

            return new EmptyResult();  // O devolver algún resultado específico si es necesario
        }

        // Método para generar la firma de GoU
        private string GenerateSignature(int internalReference, string status, string secretKey)
        {
            string data = $"{internalReference}{status}{secretKey}";

            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
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
                string nombreCompleto;
                bool usarRazonSocial = false;

                // Comprobar si alguno de los campos de nombre es nulo o vacío
                if (string.IsNullOrEmpty(datos.Nombre) || string.IsNullOrEmpty(datos.Nombre2))
                {
                    usarRazonSocial = true;
                }

                if (usarRazonSocial)
                {
                    // Si alguno de los campos de nombre es nulo o vacío, usa 'datos.Razon'
                    nombreCompleto = datos.Razon;
                }
                else
                {
                    // De lo contrario, construye el nombre completo
                    nombreCompleto = $"{datos.Nombre} {datos.Nombre2} {datos.Apellido} {datos.Apellido2}".Trim();
                }


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
                    client.Connect("smtp.gmail.com", 587, false); // Usar SSL/TLS

                    var username = ConfigurationManager.AppSettings["EmailUsername"];
                    var password = ConfigurationManager.AppSettings["EmailPassword"];

                    client.Authenticate(username, password); // Autenticarse con las credenciales
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

            // Actualizar el contador de elementos en el carrito
            int itemCount = cart.GetTotalItemCount();
            Session["cartItemCount"] = itemCount;

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
                            var producto = new Producto
                            {
                                Id = reader.GetInt32("id_main"),
                                Nombre = reader.IsDBNull(reader.GetOrdinal("td_nombre")) ? null : reader.GetString("td_nombre"),
                                Descripcion = reader.IsDBNull(reader.GetOrdinal("td_descri")) ? null : reader.GetString("td_descri"),
                                Precio = reader.IsDBNull(reader.GetOrdinal("td_precio")) ? 0 : reader.GetDecimal("td_precio"), // Asumiendo que el precio por defecto es 0 si es NULL
                                Imagen = reader.IsDBNull(reader.GetOrdinal("td_img")) ? null : reader.GetString("td_img"),
                                Detalle = reader.IsDBNull(reader.GetOrdinal("td_detall")) ? null : reader.GetString("td_detall")
                            };

                            products.Add(producto);
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
                                Nombre = reader.IsDBNull(reader.GetOrdinal("td_nombre")) ? null : reader.GetString("td_nombre"),
                                Descripcion = reader.IsDBNull(reader.GetOrdinal("td_descri")) ? null : reader.GetString("td_descri"),
                                Precio = reader.IsDBNull(reader.GetOrdinal("td_precio")) ? 0 : reader.GetDecimal("td_precio"), // Suponiendo 0 como valor por defecto para decimal
                                Imagen = reader.IsDBNull(reader.GetOrdinal("td_img")) ? null : reader.GetString("td_img"),
                                Id_Grupo = reader.IsDBNull(reader.GetOrdinal("id_grupo")) ? 0 : reader.GetInt32("id_grupo"), // Suponiendo 0 como valor por defecto para int
                                GrupoNombre = reader.IsDBNull(reader.GetOrdinal("NombreGrupo")) ? null : reader.GetString("NombreGrupo"),
                                Detalle = reader.IsDBNull(reader.GetOrdinal("td_detall")) ? null : reader.GetString("td_detall")
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
                                Nombre = reader.IsDBNull(reader.GetOrdinal("td_nombre")) ? null : reader.GetString("td_nombre"),
                                Descripcion = reader.IsDBNull(reader.GetOrdinal("td_descri")) ? null : reader.GetString("td_descri"),
                                Precio = reader.IsDBNull(reader.GetOrdinal("td_precio")) ? 0 : reader.GetDecimal("td_precio"), // Suponiendo 0 como valor por defecto para decimal
                                Imagen = reader.IsDBNull(reader.GetOrdinal("td_img")) ? null : reader.GetString("td_img")
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