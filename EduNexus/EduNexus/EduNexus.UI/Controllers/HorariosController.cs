using EduNexus.Abstracciones.ModelosParaUI.Horarios;
using EduNexus.Abstracciones.ModelosParaUI.Materias;
using EduNexus.Abstracciones.ModelosParaUI.Secciones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class HorariosController : Controller
    {
        // GET: Horarios/ListadoDeHorarios/5 (5 = id_seccion)
        public ActionResult ListadoDeHorarios(int? id)
        {
            List<HorariosDto> horarios = ObtenerHorarios().Where(x => x.seccion == id).ToList();
            ViewBag.IdSeccion = id;
            ViewBag.NombreSeccion = ObtenerSecciones().FirstOrDefault(x => x.id_seccion == id)?.nombre;
            ViewBag.Materias = ObtenerMaterias();
            return View(horarios);
        }

        // GET: Horarios/VisualizarHorario/5 (5 = id_seccion)
        public ActionResult VisualizarHorario(int? id)
        {
            List<HorariosDto> horarios = ObtenerHorarios().Where(x => x.seccion == id).ToList();
            ViewBag.IdSeccion = id;
            ViewBag.NombreSeccion = ObtenerSecciones().FirstOrDefault(x => x.id_seccion == id)?.nombre;
            ViewBag.Materias = ObtenerMaterias();
            return View(horarios);
        }

        // GET: Horarios/CrearHorario/5 (5 = id_seccion)
        public ActionResult CrearHorario(int? id)
        {
            ViewBag.NombreSeccion = ObtenerSecciones().FirstOrDefault(x => x.id_seccion == id)?.nombre;
            ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
            ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
            return View(new HorariosDto { seccion = id ?? 0 });
        }

        // POST: Horarios/Create
        [HttpPost]
        public ActionResult Create(HorariosDto horario, FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("ListadoDeHorarios", new { id = horario.seccion });
            }
            catch
            {
                ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                return View("CrearHorario", horario);
            }
        }

        // GET: Horarios/EditarHorario/5 (5 = id_horario)
        public ActionResult EditarHorario(int? id)
        {
            HorariosDto horario = ObtenerHorarios().FirstOrDefault(x => x.id_horario == id);
            ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
            ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
            return View(horario);
        }

        // POST: Horarios/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, HorariosDto horario, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("ListadoDeHorarios", new { id = horario.seccion });
            }
            catch
            {
                ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                return View("EditarHorario", horario);
            }
        }

        private List<HorariosDto> ObtenerHorarios()
        {
            return new List<HorariosDto>
            {
                new HorariosDto 
                { id_horario = 1, 
                    dia_semana = "Lunes", 
                    hora_inicio = DateTime.Today.AddHours(7), 
                    hora_fin = DateTime.Today.AddHours(8), 
                    materia = 1, seccion = 1 },
                new HorariosDto 
                { id_horario = 2, 
                    dia_semana = "Martes", 
                    hora_inicio = DateTime.Today.AddHours(8), 
                    hora_fin = DateTime.Today.AddHours(9), 
                    materia = 2, 
                    seccion = 1 
                },
                new HorariosDto 
                { id_horario = 3, 
                    dia_semana = "Miércoles", 
                    hora_inicio = DateTime.Today.AddHours(9), 
                    hora_fin = DateTime.Today.AddHours(10), 
                    materia = 3, 
                    seccion = 1 
                },
                new HorariosDto 
                { id_horario = 4, 
                    dia_semana = "Lunes", 
                    hora_inicio = DateTime.Today.AddHours(7), 
                    hora_fin = DateTime.Today.AddHours(8), 
                    materia = 1, 
                    seccion = 2 
                },
            };
        }

        private List<MateriasDto> ObtenerMaterias()
        {
            return new List<MateriasDto>
            {
                new MateriasDto 
                { id_materia = 1, 
                    nombre = "Matemáticas", 
                    profesor = "Ana Rodríguez" 
                },
                new MateriasDto 
                { id_materia = 2, 
                    nombre = "Español", 
                    profesor = "Carlos Jiménez" 
                },
                new MateriasDto 
                { id_materia = 3,
                    nombre = "Ciencias", 
                    profesor = "María Solano" 
                },
                new MateriasDto 
                { id_materia = 4, 
                    nombre = "Estudios Sociales", 
                    profesor = "Luis Vargas" 
                },
            };
        }

        private List<SeccionesDto> ObtenerSecciones()
        {
            return new List<SeccionesDto>
            {
                new SeccionesDto 
                { id_seccion = 1, 
                    nombre = "1-A", 
                    grado = 1, 
                    cupo = 30 
                },
                new SeccionesDto 
                { id_seccion = 2, 
                    nombre = "1-B", 
                    grado = 1, 
                    cupo = 30 
                },
                new SeccionesDto 
                { id_seccion = 3, 
                    nombre = "2-A", 
                    grado = 2, 
                    cupo = 30 
                },
            };

        }
    }
}