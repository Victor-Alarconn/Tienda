using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Tienda.Areas.Admin.Controllers
{
    public class GestionPaginaController : Controller
    {
        private string rutaColores;

        public GestionPaginaController()
        {
            // Inicializar la ruta del archivo JSON en el constructor
            rutaColores = Server.MapPath("~/App_Data/colores.json");
        }

        // GET: Admin/GestionPagina
        public ActionResult Index()
        {
            // Leer los colores actuales del archivo JSON
            var colores = LeerColores();
            ViewBag.NavbarColor = colores["navbarColor"];
            ViewBag.SliderColor = colores["sliderColor"];
            return View();
        }

        [HttpPost]
        public ActionResult GuardarColores(string navbarColor, string sliderColor)
        {
            // Crear un objeto para almacenar los colores
            var colores = new
            {
                navbarColor = navbarColor ?? "#007BFF", // Color por defecto si es null
                sliderColor = sliderColor ?? "#d6f792"  // Color por defecto si es null
            };

            // Guardar los colores en el archivo JSON
            GuardarColores(colores);

            // Redirigir a la página de administración con los colores actualizados
            return RedirectToAction("Index");
        }

        private Dictionary<string, string> LeerColores()
        {
            // Verificar si el archivo JSON existe
            if (!System.IO.File.Exists(rutaColores))
            {
                // Si no existe, crear un archivo JSON con colores por defecto
                var coloresPorDefecto = new Dictionary<string, string>
                {
                    { "navbarColor", "#007BFF" }, // Color por defecto
                    { "sliderColor", "#d6f792" }  // Color por defecto
                };
                GuardarColores(coloresPorDefecto);
            }

            // Leer el archivo JSON
            var json = System.IO.File.ReadAllText(rutaColores);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        private void GuardarColores(object colores)
        {
            // Convertir el objeto a JSON y guardarlo en el archivo
            var json = JsonConvert.SerializeObject(colores, Formatting.Indented);
            System.IO.File.WriteAllText(rutaColores, json);
        }
    }
}