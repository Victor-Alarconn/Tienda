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
        // Directorio donde se almacenan las imágenes
        private string imagePath = "~/Imagenes/Carousel";

        // Ruta relativa al archivo donde se almacenará la descripción
         private static readonly string DescripcionPath = System.Web.HttpContext.Current.Server.MapPath("~/Areas/Descripcion/descripcion.txt");

        // Ruta horario
        private string HorarioPath => Server.MapPath("~/Areas/Descripcion/horarios.txt");

        // Ruta para las redes sociales
        private string RedesSocialesPath => Server.MapPath("~/Areas/Descripcion/redes-sociales.txt");

        // GET: Admin/GestionPagina
        public ActionResult Index()
        {
            var images = Directory.GetFiles(Server.MapPath(imagePath))
                      .Select(Path.GetFileName)
                      .ToList();
            ViewBag.Images = images;


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

            // Ajusta la ruta a la vista según la ubicación real
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

            // Volver a cargar la vista de gestión
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

        // POST: GestionPagina/GuardarRedesSociales
        [HttpPost]
        public ActionResult GuardarRedesSociales(string facebookUrl, string instagramUrl)
        {
            try
            {
                var urls = new[] { facebookUrl, instagramUrl };
                System.IO.File.WriteAllLines(RedesSocialesPath, urls);
                TempData["Message"] = "URLs de redes sociales guardadas exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al guardar las URLs: " + ex.Message;
            }

            return RedirectToAction("GestionRedesSociales");
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
