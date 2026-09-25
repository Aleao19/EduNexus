using EduNexus.Abstracciones.ModelosParaUI.Bitacora;
using EduNexus.Abstracciones.ModelosParaUI.Dashboard;
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
            // TODO: reemplazar estos datos mock por llamadas a los
            // repositorios/servicios reales (Estudiantes, Docentes, Secciones, Materias, Bitácora).
            var model = new DashboardViewModel
            {
                TotalEstudiantes = 486,
                TotalDocentes = 32,
                TotalSecciones = 18,
                TotalMaterias = 24,

                VariacionEstudiantes = 4.8,
                VariacionDocentes = 2.1,
                VariacionSecciones = 0,
                VariacionMaterias = -1.5,

                AniosMatricula = new List<string> { "2022", "2023", "2024", "2025", "2026" },
                MatriculaPorAnio = new List<int> { 430, 448, 460, 472, 486 },

                NombresSecciones = new List<string> { "7-A", "7-B", "8-A", "8-B", "9-A" },
                EstudiantesPorSeccion = new List<int> { 32, 30, 35, 28, 31 },

                NombresMaterias = new List<string> { "Matemática", "Español", "Ciencias", "Inglés", "Sociales" },
                DocentesPorMateria = new List<int> { 6, 5, 4, 5, 3 },

                ActividadReciente = new List<BitacoraDto>
                {
                    new BitacoraDto
                    {
                        id_evento = 1,
                        tipoDeEvento = "Creación",
                        descripcionEvento = "Se matriculó un nuevo estudiante en la sección 8-A.",
                        fecha = DateTime.Now.AddMinutes(-25),
                        Usuario = "Pedro"
                    },
                    new BitacoraDto
                    {
                        id_evento = 2,
                        tipoDeEvento = "Actualización",
                        descripcionEvento = "Se actualizó el horario de la materia Ciencias.",
                        fecha = DateTime.Now.AddHours(-3),
                        Usuario = "María"
                    },
                    new BitacoraDto
                    {
                        id_evento = 3,
                        tipoDeEvento = "Asignación",
                        descripcionEvento = "Se asignó un docente a la sección 9-A.",
                        fecha = DateTime.Now.AddHours(-6),
                        Usuario = "Carlos"
                    },
                    new BitacoraDto
                    {
                        id_evento = 4,
                        tipoDeEvento = "Eliminación",
                        descripcionEvento = "Se eliminó un registro duplicado de expediente.",
                        fecha = DateTime.Now.AddDays(-1),
                        Usuario = "Pedro"
                    }
                },

                ProximasClases = new List<ProximaClaseDto>
                {
                    new ProximaClaseDto { Materia = "Matemática", Docente = "Prof. Rojas", Seccion = "8-A", Hora = "08:00 - 09:00", Dia = DateTime.Now.DayOfWeek },
                    new ProximaClaseDto { Materia = "Inglés", Docente = "Prof. Vargas", Seccion = "7-B", Hora = "09:00 - 10:00", Dia = DateTime.Now.DayOfWeek },
                    new ProximaClaseDto { Materia = "Ciencias", Docente = "Prof. Solano", Seccion = "9-A", Hora = "10:15 - 11:15", Dia = DateTime.Now.DayOfWeek }
                }
            };

            return View(model);
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