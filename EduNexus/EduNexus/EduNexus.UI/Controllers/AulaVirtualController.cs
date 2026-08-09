using EduNexus.Abstracciones.ModelosParaUI.Asignaciones;
using EduNexus.Abstracciones.ModelosParaUI.Materias;
using EduNexus.Abstracciones.ModelosParaUI.Secciones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class AulaVirtualController : Controller
    {
        // GET: AulaVirtual
        public ActionResult AulaVirtual()
        {
            List<MateriasDto> materia = new List<MateriasDto>()
            {
                new MateriasDto
                {
                    id_materia = 1,
                    nombre = "Matemáticas",
                    profesor = "Ana Maria"
                },
                new MateriasDto
                {
                    id_materia = 2,
                    nombre = "Español",
                    profesor = "Juan Carlos"
                },
                new MateriasDto
                {
                    id_materia = 3,
                    nombre = "Ciencias",
                    profesor = "María Fernanda"
                },  
                new MateriasDto
                {
                    id_materia = 4,
                    nombre = "Estudios Sociales",
                    profesor = "Carlos Eduardo"
                },
                new MateriasDto
                {
                    id_materia = 5,
                    nombre = "Inglés",
                    profesor = "Laura Sofía"
                }
            };

                       

            return View(materia);
        }

        // GET: AulaVirtual
        public ActionResult Horario()
        {
            return View();
        }

        // GET: AulaVirtual/Details/5
        public ActionResult Materia(int? id)
        {
            List<AsignacionesDto> asignaciones = new List<AsignacionesDto>()
            {
                new AsignacionesDto
                {
                    id_asignacion = 1,
                    nombre = "Tarea 1",
                    descripcion = "Lectura de un texto y respuesta a preguntas",
                    porcentaje_nota = 10,
                    calificacion = 85,
                    estado = "Entregado",
                    fechaDeApertura = DateTime.Now,
                    fechaDeCierre = DateTime.Now.AddDays(7),
                    fechaDeEntrega = DateTime.Now.AddDays(7)
                },
                new AsignacionesDto
                {
                    id_asignacion = 2,
                    nombre = "Tarea 2",
                    descripcion = "Redactar un ensayo sobre la historia",
                    porcentaje_nota = 15,
                    calificacion = 90,
                    estado = "Entregado",
                    fechaDeApertura = DateTime.Now,
                    fechaDeCierre = DateTime.Now.AddDays(7),
                    fechaDeEntrega = DateTime.Now.AddDays(7)
                },
                new AsignacionesDto
                {
                    id_asignacion = 3,
                    nombre = "Tarea 3",
                    descripcion = "Realizar un experimento y presentar los resultados",
                    porcentaje_nota = 20,
                    calificacion = 0,
                    estado = "Pendiente",
                    fechaDeApertura = DateTime.Now,
                    fechaDeCierre = DateTime.Now.AddDays(7),
                    fechaDeEntrega = DateTime.Now.AddDays(7)
                }
            };
            return View(asignaciones);
        }

        // GET: AulaVirtual/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AulaVirtual/Create
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

        // GET: AulaVirtual/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: AulaVirtual/Edit/5
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

        // GET: AulaVirtual/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AulaVirtual/Delete/5
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
