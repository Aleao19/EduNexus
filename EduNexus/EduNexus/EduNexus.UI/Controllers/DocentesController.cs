using EduNexus.Abstracciones.ModelosParaUI.AsignacionesDocentes;
using EduNexus.Abstracciones.ModelosParaUI.Docentes;
using EduNexus.Abstracciones.ModelosParaUI.Materias;
using EduNexus.Abstracciones.ModelosParaUI.Secciones;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class DocentesController : Controller
    {
        // GET: Docentes
        public ActionResult ListadoDeDocentes()
        {
            List<DocentesDto> docentes = ObtenerDocentes();
            return View(docentes);
        }

        // GET: Docentes/Create
        public ActionResult CrearDocente()
        {
            return View();
        }

        // POST: Docentes/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("ListadoDeDocentes");
            }
            catch
            {
                return View();
            }
        }

        // GET: Docentes/Edit/5
        public ActionResult EditarDocente(int? id)
        {
            List<DocentesDto> docentes = ObtenerDocentes();
            DocentesDto docente = docentes.FirstOrDefault(x => x.id_docente == id);
            return View(docente);
        }

        // POST: Docentes/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("ListadoDeDocentes");
            }
            catch
            {
                return View();
            }
        }

        // GET: Docentes/CargaAcademicaDocente/5
        public ActionResult CargaAcademicaDocente(int? id)
        {
            List<AsignacionDocenteDto> asignaciones = ObtenerAsignaciones().Where(x => x.id_docente == id).ToList();
            ViewBag.IdDocente = id;
            ViewBag.NombreDocente = ObtenerDocentes().FirstOrDefault(x => x.id_docente == id)?.nombre;
            ViewBag.Materias = ObtenerMaterias();
            ViewBag.Secciones = ObtenerSecciones();
            return View(asignaciones);
        }

        // GET: Docentes/AsignarDocente/5  
        public ActionResult AsignarDocente(int? id)
        {
            ViewBag.NombreDocente = ObtenerDocentes().FirstOrDefault(x => x.id_docente == id)?.nombre;
            ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
            ViewBag.Secciones = new SelectList(ObtenerSecciones(), "id_seccion", "nombre");
            return View(new AsignacionDocenteDto { id_docente = id ?? 0 });
        }

        // POST: Docentes/AsignarDocente/5
        [HttpPost]
        public ActionResult AsignarDocente(AsignacionDocenteDto asignacion, FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("CargaAcademicaDocente", new { id = asignacion.id_docente });
            }
            catch
            {
                ViewBag.NombreDocente = ObtenerDocentes().FirstOrDefault(x => x.id_docente == asignacion.id_docente)?.nombre;
                ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                ViewBag.Secciones = new SelectList(ObtenerSecciones(), "id_seccion", "nombre");
                return View(asignacion);
            }
        }

        private List<DocentesDto> ObtenerDocentes()
        {
            return new List<DocentesDto>
            {
                new DocentesDto 
                { id_docente = 1, 
                    identificacion = "1-1111-1111", 
                    nombre = "Ana", 
                    apellido1 = "Rodríguez", 
                    apellido2 = "Mora", 
                    email = "ana.rodriguez@mep.go.cr", 
                    telefono = "8888-1111", 
                    estado = "Activo" 
                },
                new DocentesDto 
                { id_docente = 2, 
                    identificacion = "2-2222-2222", 
                    nombre = "Carlos", 
                    apellido1 = "Jiménez", 
                    apellido2 = "Solís", 
                    email = "carlos.jimenez@mep.go.cr", 
                    telefono = "8888-2222", 
                    estado = "Activo" 
                },
                new DocentesDto 
                { id_docente = 3, 
                    identificacion = "3-3333-3333", 
                    nombre = "María", 
                    apellido1 = "Solano", 
                    apellido2 = "Araya", 
                    email = "maria.solano@mep.go.cr", 
                    telefono = "8888-3333", 
                    estado = "Activo" 
                },
                new DocentesDto 
                { id_docente = 4, 
                    identificacion = "4-4444-4444", 
                    nombre = "Luis", 
                    apellido1 = "Vargas", 
                    apellido2 = "Chaves", 
                    email = "luis.vargas@mep.go.cr", 
                    telefono = "8888-4444", 
                    estado = "Inactivo" 
                },
            };
        }

        private List<AsignacionDocenteDto> ObtenerAsignaciones()
        {
            return new List<AsignacionDocenteDto>
            {
                new AsignacionDocenteDto 
                { id_asignacion_docente = 1, 
                    id_docente = 1, 
                    id_materia = 1, 
                    id_seccion = 1 
                },
                new AsignacionDocenteDto 
                { id_asignacion_docente = 2, 
                    id_docente = 2, 
                    id_materia = 2, 
                    id_seccion = 2 
                },
                new AsignacionDocenteDto 
                { id_asignacion_docente = 3, 
                    id_docente = 3, 
                    id_materia = 3, 
                    id_seccion = 1 
                },
                new AsignacionDocenteDto
                { id_asignacion_docente = 4,
                    id_docente = 4,
                    id_materia = 4,
                    id_seccion = 1
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