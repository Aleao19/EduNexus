using EduNexus.Abstracciones.ModelosParaUI.Asignaciones;
using EduNexus.Abstracciones.ModelosParaUI.Materias;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
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
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                List<SeccionMateriaEntity> clases = ObtenerClasesVisibles(db, usuario);

                Dictionary<int, string> secciones = HorarioServicio.EtiquetasDeSecciones(db);

                List<int> idsMaterias = clases.Select(c => c.fk_id_materia).Distinct().ToList();
                Dictionary<int, MateriaEntity> materias = db.Materias
                    .Where(m => idsMaterias.Contains(m.id_materia)).ToList()
                    .ToDictionary(m => m.id_materia);

                List<string> idsProfesores = clases.Select(c => c.fk_id_profesor).Distinct().ToList();
                Dictionary<string, UsuarioEntity> profesores = db.Usuarios
                    .Where(u => idsProfesores.Contains(u.id_usuario)).ToList()
                    .ToDictionary(u => u.id_usuario);

                List<MateriasDto> modelo = clases.Select(c =>
                {
                    MateriaEntity materia = materias[c.fk_id_materia];
                    UsuarioEntity profesor;
                    profesores.TryGetValue(c.fk_id_profesor, out profesor);
                    string seccion;
                    secciones.TryGetValue(c.fk_id_seccion, out seccion);

                    return new MateriasDto
                    {
                        id_materia = materia.id_materia,
                        nombre = materia.nombre,
                        profesor = profesor != null ? HorarioServicio.NombreCompleto(profesor) : "",
                        id_seccion = c.fk_id_seccion,
                        seccion = seccion
                    };
                })
                .OrderBy(x => x.nombre).ThenBy(x => x.seccion)
                .ToList();

                return View(modelo);
            }
        }

        // Clases (materia + seccion) del periodo actual que el usuario puede ver
        private static List<SeccionMateriaEntity> ObtenerClasesVisibles(EduNexusDbContext db, UsuarioDto usuario)
        {
            DateTime hoy = DateTime.Today;

            List<int> idsCalendario = db.Calendarios
                .Where(c => c.fecha_inicio <= hoy && c.fecha_fin >= hoy)
                .Select(c => c.id_calendario).ToList();

            List<int> idsSecciones = db.Secciones
                .Where(s => idsCalendario.Contains(s.fk_id_calendario))
                .Select(s => s.id_seccion).ToList();

            IQueryable<SeccionMateriaEntity> consulta = db.SeccionesMaterias
                .Where(sm => idsSecciones.Contains(sm.fk_id_seccion));

            string rol = usuario != null ? usuario.nombreRol : "";
            string idUsuario = usuario != null ? usuario.id_usuario : null;

            if (string.Equals(rol, "Docente", StringComparison.OrdinalIgnoreCase))
            {
                // Solo las clases donde él es el docente de esa sección
                consulta = consulta.Where(sm => sm.fk_id_profesor == idUsuario);
            }
            else if (string.Equals(rol, "Estudiante", StringComparison.OrdinalIgnoreCase))
            {
                // Solo las materias de su seccion
                List<int> susSecciones = db.SeccionesEstudiantes
                    .Where(se => se.fk_id_usuario == idUsuario)
                    .Select(se => se.fk_id_seccion).ToList();

                consulta = consulta.Where(sm => susSecciones.Contains(sm.fk_id_seccion));
            }

            return consulta.ToList();
        }

        // GET: AulaVirtual
        public ActionResult Horario()
        {
            return View();
        }
        // GET: AulaVirtual/Materia/5?seccion=2
        public ActionResult Materia(int? id, int? seccion)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            int? idSeccionClase = seccion;

            using (var db = new EduNexusDbContext())
            {
                SeccionMateriaEntity clase = id.HasValue
                    ? ObtenerClasesVisibles(db, usuario).FirstOrDefault(c =>
                        c.fk_id_materia == id.Value && (!seccion.HasValue || c.fk_id_seccion == seccion.Value))
                    : null;

                if (clase == null)
                {
                    TempData["MensajeError"] = "No tiene acceso a esta materia.";
                    return RedirectToAction("AulaVirtual");
                }

                idSeccionClase = clase.fk_id_seccion;
            }

            ViewBag.IdMateria = id;
            ViewBag.IdSeccion = idSeccionClase;
            ViewBag.EsDocente = usuario != null &&
                string.Equals(usuario.nombreRol, "Docente", StringComparison.OrdinalIgnoreCase);

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
            ViewBag.Estudiantes = ObtenerEstudiantes();

            return View(asignaciones);
        }

        // POST: AulaVirtual/GuardarAsistencia
        [HttpPost]
        public ActionResult GuardarAsistencia(FormCollection collection)
        {
            try
            {
                // TODO: Add insert/update logic here
                // collection["fecha"] trae la fecha general
                // collection["asistencia_1"], collection["asistencia_2"], ... traen el estado de cada estudiante

                return RedirectToAction("AulaVirtual");
            }
            catch
            {
                return RedirectToAction("AulaVirtual");
            }
        }

        private List<string> ObtenerEstudiantes()
        {
            return new List<string>
            {
                "Sofía Castro Mora",
                "Diego Fernández Rojas",
                "Valeria Gómez Vindas",
                "Kevin Araya Solano",
                "Fabiana Chaves Mora"
            };
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
