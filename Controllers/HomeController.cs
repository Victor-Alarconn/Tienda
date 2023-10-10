using MySqlConnector;
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

        public HomeController()
        {
            _dataConexion = new DataConexion();
        }

        public ActionResult Categorias()
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

            return View(categories);
        }


        public ActionResult Index()
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

            return View(categories);
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
    }
}