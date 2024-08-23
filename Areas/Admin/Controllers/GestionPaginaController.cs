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

        // POST: GestionPagina/GuardarColorSlider
        [HttpPost]
        public JsonResult GuardarColorSlider(string color)
        {
            try
            {
                // Crear el directorio si no existe
                string directory = Path.GetDirectoryName(ColorSliderPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Escribir el color en el archivo
                System.IO.File.WriteAllText(ColorSliderPath, color);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: GestionPagina/ObtenerColorNavbar
        [HttpGet]
        public JsonResult ObtenerColorNavbar()
        {
            string color = "#000000"; // Color negro por defecto

            try
            {
                if (System.IO.File.Exists(ColorNavbarPath))
                {
                    color = System.IO.File.ReadAllText(ColorNavbarPath);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { color = color }, JsonRequestBehavior.AllowGet);
        }

        // GET: GestionPagina/ObtenerColorSlider
        [HttpGet]
        public JsonResult ObtenerColorSlider()
        {
            string color = "#000000"; // Color negro por defecto

            try
            {
                if (System.IO.File.Exists(ColorSliderPath))
                {
                    color = System.IO.File.ReadAllText(ColorSliderPath);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { color = color }, JsonRequestBehavior.AllowGet);
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
