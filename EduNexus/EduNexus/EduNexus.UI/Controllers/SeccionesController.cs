using EduNexus.Abstracciones.ModelosParaUI.Secciones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static System.Collections.Specialized.BitVector32;

namespace EduNexus.UI.Controllers
{
    public class SeccionesController : Controller
    {
        // GET: Secciones
        public ActionResult ListadoDeSecciones()
        {
            List<SeccionesDto> secciones = ObtenerSecciones();
            return View(secciones);
        }

        // GET: Secciones/Details/5
        public ActionResult DetalleDeSeccion(int? id)
        {
            List<SeccionesDto> secciones = ObtenerSecciones();
            SeccionesDto seccion = secciones.FirstOrDefault(x => x.id_seccion == id);
            return View(seccion);
        }

        // GET: Secciones/Create
        public ActionResult CrearSeccion()
        {
            return View();
        }

        // POST: Secciones/Create
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

        // GET: Secciones/Edit/5
        public ActionResult EditarSeccion(int? id)
        {
            List<SeccionesDto> secciones = ObtenerSecciones();
            SeccionesDto seccion = secciones.FirstOrDefault(x => x.id_seccion == id);
            return View(seccion);
        }

        // POST: Secciones/Edit/5
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

        private List<SeccionesDto> ObtenerSecciones()
        {
            return new List<SeccionesDto>
            {
                new SeccionesDto
                {
                    id_seccion= 1,
                    nombre="1-A",
                    grado= 1,
                    cupo = 30
                },
                new SeccionesDto
                {
                    id_seccion= 2,
                    nombre="1-B",
                    grado= 1,
                    cupo = 30
                },
                new SeccionesDto
                {
                    id_seccion= 3,
                    nombre="2-A",
                    grado= 2,
                    cupo = 30
                },
                new SeccionesDto
                {
                    id_seccion= 4,
                    nombre="2-B",
                    grado= 2,
                    cupo = 30
                },
                new SeccionesDto
                {
                    id_seccion= 5,
                    nombre="3-A",
                    grado= 3,
                    cupo = 30
                },
                new SeccionesDto
                {
                    id_seccion= 6,
                    nombre="4-A",
                    grado= 4,
                    cupo = 30
                },
                new SeccionesDto
                {
                    id_seccion= 7,
                    nombre="5-A",
                    grado= 5,
                    cupo = 30
                },
                new SeccionesDto
                {
                    id_seccion= 8,
                    nombre="6-A",
                    grado= 6,
                    cupo = 30
                },
            };
        }
           
    }
}
