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

        // Método que inicializa `rutaColores` antes de que se ejecute cada acción.
        private void InicializarRutaColores()
        {
            rutaColores = Server.MapPath("~/App_Data/colores.json");
        }

        public ActionResult GestionPagina()
        {
            InicializarRutaColores(); // Inicializa `rutaColores` aquí

            // Leer los colores actuales del archivo JSON
            var colores = LeerColores();
            ViewBag.NavbarColor = colores["navbarColor"];
            ViewBag.SliderColor = colores["sliderColor"];
            return View();
        }

        [HttpPost]
        public ActionResult GuardarColores(string navbarColor, string sliderColor)
        {
            InicializarRutaColores(); // Inicializa `rutaColores` aquí

            // Crear un objeto para almacenar los colores seleccionados
            var colores = new Dictionary<string, string>
    {
        { "navbarColor", navbarColor ?? "#007BFF" },
        { "sliderColor", sliderColor ?? "#d6f792" }
    };

            // Guardar los colores en el archivo JSON
            GuardarColores(colores);

            // Volver a establecer los valores en ViewBag para que se muestren correctamente en la vista
            ViewBag.NavbarColor = colores["navbarColor"];
            ViewBag.SliderColor = colores["sliderColor"];

            // Guardar los colores en el archivo JSON
            GuardarColores(colores);

            // Redirigir a la página de gestión de colores
            return View("~/Areas/Admin/Views/Admin/GestionPagina.cshtml");
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
    }
}