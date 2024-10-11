using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Tienda.Areas.Admin.Controllers
{
    public class GestionPaginaController : Controller
    {
        // Directorio donde se almacenan las imágenes del carrusel
        private string imagePath = "~/Imagenes/Carousel";

        // Directorio donde se almacenará la imagen del logo
        private string logoPath = "~/Imagenes/Logo";

        private string publicidadPath = ("~/Imagenes/Publicidad");

        // Ruta relativa al archivo donde se almacenará la descripción
        private static readonly string DescripcionPath = System.Web.HttpContext.Current.Server.MapPath("~/Areas/Descripcion/descripcion.txt");

        // Ruta horario
        private string HorarioPath => Server.MapPath("~/Areas/Descripcion/horarios.txt");

        // Ruta para las redes sociales
        private string RedesSocialesPath => Server.MapPath("~/Areas/Descripcion/redes-sociales.txt");

        // Ruta para guardar el color de la barra de navegación
        private string ColorNavbarPath => Server.MapPath("~/Areas/Descripcion/color-navbar.txt");

        // Ruta para guardar el color del slider
        private string ColorSliderPath => Server.MapPath("~/Areas/Descripcion/color-slider.txt");

        // GET: Admin/GestionPagina
        public ActionResult Index()
        {
            var images = Directory.GetFiles(Server.MapPath(imagePath))
                      .Select(Path.GetFileName)
                      .ToList();
            ViewBag.Images = images;

            // Cargar la imagen del logo si existe
            var logo = Directory.GetFiles(Server.MapPath(logoPath))
                      .Select(Path.GetFileName)
                      .FirstOrDefault();
            ViewBag.Logo = logo;

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

            return View("~/Areas/Admin/Views/Admin/Index.cshtml");
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
                return View("~/Areas/Admin/Views/Admin/Index.cshtml");
            }
        }

        // Acción para cargar el logo
        [HttpPost]
        public ActionResult UploadLogo(HttpPostedFileBase file)
        {
            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    string path = Server.MapPath(logoPath);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    // Eliminar el logo anterior si existe
                    var existingLogo = Directory.GetFiles(path).FirstOrDefault();
                    if (existingLogo != null)
                    {
                        System.IO.File.Delete(existingLogo);
                    }

                    string fileName = Path.GetFileName(file.FileName);
                    string fullPath = Path.Combine(path, fileName);
                    file.SaveAs(fullPath);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar el logo: " + ex.Message;
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

        // Acción para eliminar el logo
        [HttpPost]
        public ActionResult DeleteLogo()
        {
            try
            {
                string path = Server.MapPath(logoPath);
                var logo = Directory.GetFiles(path).FirstOrDefault();
                if (logo != null && System.IO.File.Exists(logo))
                {
                    System.IO.File.Delete(logo);
                    ViewBag.Message = "Logo eliminado exitosamente.";
                }
                else
                {
                    ViewBag.Error = "El logo no existe.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al eliminar el logo: " + ex.Message;
            }

            return RedirectToAction("Index");
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


        // GET: GestionPagina/GestionDescripcion
        public ActionResult GestionDescripcion()
        {
            // Leer la descripción desde el archivo o usar un valor predeterminado si no existe
            ViewBag.Descripcion = System.IO.File.Exists(DescripcionPath)
                ? System.IO.File.ReadAllText(DescripcionPath)
                : "Descripción predeterminada";

            // Leer el horario desde el archivo o usar un valor predeterminado si no existe
            ViewBag.Horario = System.IO.File.Exists(HorarioPath)
                ? System.IO.File.ReadAllText(HorarioPath)
                : "Horario predeterminado";

            return View("~/Areas/Admin/Views/Admin/GestionDescripcion.cshtml");
        }

        // POST: GestionPagina/GuardarDescripcion
        [HttpPost]
        public ActionResult GuardarDescripcion(string nuevaDescripcion)
        {
            try
            {
                // Crear el archivo y escribir la nueva descripción
                System.IO.File.WriteAllText(DescripcionPath, nuevaDescripcion);
                TempData["Message"] = "Descripción guardada exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al guardar la descripción: " + ex.Message;
            }

            // Volver a cargar la vista con la nueva descripción
            return RedirectToAction("GestionDescripcion");
        }

        // POST: GestionPagina/GuardarHorario
        [HttpPost]
        public ActionResult GuardarHorario(string nuevoHorario)
        {
            try
            {
                // Escribir el nuevo horario en el archivo
                System.IO.File.WriteAllText(HorarioPath, nuevoHorario);
                TempData["Message"] = "Horario guardado exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al guardar el horario: " + ex.Message;
            }

            return RedirectToAction("GestionDescripcion");
        }

        // GET: GestionPagina/GestionRedesSociales
        public ActionResult GestionRedesSociales()
        {
            var urls = new Dictionary<string, string>();
            if (System.IO.File.Exists(RedesSocialesPath))
            {
                var lines = System.IO.File.ReadAllLines(RedesSocialesPath);
                if (lines.Length >= 2)
                {
                    urls["FacebookUrl"] = lines[0];
                    urls["InstagramUrl"] = lines[1];
                }
            }
            else
            {
                urls["FacebookUrl"] = "https://www.facebook.com/mini.dulce.bocado/";
                urls["InstagramUrl"] = "https://www.instagram.com/minidulcebocado/";
            }

            ViewBag.FacebookUrl = urls["FacebookUrl"];
            ViewBag.InstagramUrl = urls["InstagramUrl"];

            return View("~/Areas/Admin/Views/Admin/GestionRedesSociales.cshtml");
        }


        // GET: GestionPagina/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: GestionPagina/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: GestionPagina/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: GestionPagina/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: GestionPagina/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: GestionPagina/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: GestionPagina/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
