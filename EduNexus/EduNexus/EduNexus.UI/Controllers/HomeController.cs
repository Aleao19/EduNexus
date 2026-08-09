using EduNexus.Abstracciones.ModelosParaUI.Bitacora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Web;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Login()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Bitacora()
        {
            List<BitacoraDto> bitacoras = new List<BitacoraDto>()
            {
                new BitacoraDto
                {
                    id_evento = 1,
                    tipoDeEvento = "Creación",
                    descripcionEvento = "Se creó un nuevo registro.",
                    fecha = DateTime.Now,
                    datosAnteriores = null,
                    datosPosteriores = "{ \"nombre\": \"Juan\", \"edad\": 30 }",
                    Usuario = "Pedro"
                },
                new BitacoraDto
                {
                    id_evento = 2,
                    tipoDeEvento = "Actualización",
                    descripcionEvento = "Se actualizó un registro existente.",
                    fecha = DateTime.Now,
                    datosAnteriores = "{ \"nombre\": \"Juan\", \"edad\": 30 }",
                    datosPosteriores = "{ \"nombre\": \"Juan\", \"edad\": 31 }",
                    Usuario = "Pedro"
                },
                new BitacoraDto
                {
                    id_evento = 3,
                    tipoDeEvento = "Eliminación",
                    descripcionEvento = "Se eliminó un registro.",
                    fecha = DateTime.Now,
                    datosAnteriores = "{ \"nombre\": \"Juan\", \"edad\": 31 }",
                    datosPosteriores = null,
                    Usuario = "Pedro"
                }
            };
            

            return View(bitacoras);
        }
    }
}