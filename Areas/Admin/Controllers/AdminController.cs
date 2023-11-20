using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Windows.Media.Media3D;
using Tienda.Areas.Admin.Data;
using Tienda.Areas.Admin.Models;

namespace Tienda.Areas.Admin.Controllers
{
    public class AdminController : Controller
    {
        private readonly DataConexion _dataConexion; // Se crea una instancia de la clase DataConexion

        public AdminController()
        {
            _dataConexion = new DataConexion();
        }

        // GET: Admin/Admin
        public ActionResult Verificacion()
        {
            return View();
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
            return View();
        }
    }
}
