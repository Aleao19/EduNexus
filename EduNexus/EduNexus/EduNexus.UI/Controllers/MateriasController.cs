using EduNexus.Abstracciones.ModelosParaUI.Materias;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class MateriasController : Controller
    {
        // GET: Materias
        public ActionResult ListadoDeMaterias()
        {
            List<MateriasDto> materias = ObtenerMaterias();
            return View(materias);
        }

        // GET: Materias/Create
        public ActionResult CrearMateria()
        {
            return View();
        }

        // POST: Materias/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("ListadoDeMaterias");
            }
            catch
            {
                return View();
            }
        }

        // GET: Materias/Edit/5
        public ActionResult EditarMateria(int? id)
        {
            List<MateriasDto> materias = ObtenerMaterias();
            MateriasDto materia = materias.FirstOrDefault(x => x.id_materia == id);
            return View(materia);
        }

        // POST: Materias/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("ListadoDeMaterias");
            }
            catch
            {
                return View();
            }
        }

        private List<MateriasDto> ObtenerMaterias()
        {
            return new List<MateriasDto>
            {
                new MateriasDto 
                {   id_materia = 1, 
                    nombre = "Matematicas", 
                    profesor = "Ana Rodriguez" 
                },
                new MateriasDto 
                {   id_materia = 2, 
                    nombre = "Español", 
                    profesor = "Carlos Jimenez" 
                },
                new MateriasDto 
                {   id_materia = 3,
                    nombre = "Ciencias", 
                    profesor = "Maria Solano" 
                },
                new MateriasDto 
                {   id_materia = 4, 
                    nombre = "Estudios Sociales", 
                    profesor = "Luis Vargas" 
                },
            };
        }
    }
}