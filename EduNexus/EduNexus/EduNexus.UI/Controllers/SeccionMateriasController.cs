using EduNexus.Abstracciones.ModelosParaUI.Materias;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RMAT-05-002 Como administrador quiero configurar las materias que se impartirán en cada sección
    // Tablas: secciones_materias, materia, secciones, grados, calendario, asignaciones (notas), asistencia
    [AutorizarAdministrador]
    public class SeccionMateriasController : Controller
    {
        public ActionResult ListadoDeAsignaciones(int? seccion)
        {
            using (var db = new EduNexusDbContext())
            {
                Dictionary<int, string> etiquetas = EtiquetasDeSecciones(db);

                IQueryable<SeccionMateriaEntity> consulta = db.SeccionesMaterias;
                if (seccion.HasValue)
                {
                    int idSeccion = seccion.Value;
                    consulta = consulta.Where(x => x.fk_id_seccion == idSeccion);
                }
                List<SeccionMateriaEntity> asignaciones = consulta.ToList();

                List<int> idsMaterias = asignaciones.Select(a => a.fk_id_materia).Distinct().ToList();
                Dictionary<int, MateriaEntity> materias = db.Materias
                    .Where(m => idsMaterias.Contains(m.id_materia)).ToList()
                    .ToDictionary(m => m.id_materia);

                List<string> idsProfesores = asignaciones.Select(a => a.fk_id_profesor).Where(x => x != null).Distinct().ToList();
                Dictionary<string, UsuarioEntity> profesores = db.Usuarios
                    .Where(u => idsProfesores.Contains(u.id_usuario)).ToList()
                    .ToDictionary(u => u.id_usuario);

                List<MateriaSeccionDto> modelo = asignaciones.Select(a =>
                {
                    MateriaEntity materia;
                    materias.TryGetValue(a.fk_id_materia, out materia);
                    UsuarioEntity profesor = null;
                    if (a.fk_id_profesor != null)
                    {
                        profesores.TryGetValue(a.fk_id_profesor, out profesor);
                    }
                    string etiqueta;
                    etiquetas.TryGetValue(a.fk_id_seccion, out etiqueta);

                    return new MateriaSeccionDto
                    {
                        id_seccion_materia = a.id_seccion_materia,
                        id_seccion = a.fk_id_seccion,
                        seccion = etiqueta,
                        id_materia = a.fk_id_materia,
                        materia = materia != null ? materia.nombre : "",
                        profesor = profesor != null ? NombreCompleto(profesor) : ""
                    };
                })
                .OrderBy(x => x.seccion)
                .ThenBy(x => x.materia)
                .ToList();

                ViewBag.Secciones = new SelectList(etiquetas.OrderBy(x => x.Value).ToList(), "Key", "Value", seccion);
                return View(modelo);
            }
        }

        // GET: SeccionMaterias/AsignarMateria?seccion=5 (la sección es opcional y solo precarga el campo)
        public ActionResult AsignarMateria(int? seccion)
        {
            var modelo = new AsignarMateriaDto();
            using (var db = new EduNexusDbContext())
            {
                CargarListas(db);
                string etiqueta;
                if (seccion.HasValue && EtiquetasDeSecciones(db).TryGetValue(seccion.Value, out etiqueta))
                {
                    modelo.seccion = etiqueta;
                }
            }
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarMateria(AsignarMateriaDto modelo)
        {
            using (var db = new EduNexusDbContext())
            {
                CargarListas(db);

                if (!ModelState.IsValid)
                {
                    return View(modelo);
                }

                // Escenario 5: asignación a una sección inexistente
                string buscada = Normalizar(modelo.seccion);
                int idSeccion = EtiquetasDeSecciones(db)
                    .Where(e => Normalizar(e.Value) == buscada)
                    .Select(e => e.Key)
                    .FirstOrDefault();
                if (idSeccion == 0)
                {
                    ModelState.AddModelError("seccion", "La sección debe estar registrada en el sistema.");
                    return View(modelo);
                }

                int idMateria = modelo.id_materia.Value;
                if (!db.Materias.Any(m => m.id_materia == idMateria))
                {
                    ModelState.AddModelError("id_materia", "La materia seleccionada no está registrada en el sistema.");
                    return View(modelo);
                }
                UsuarioEntity docente = HorarioServicio.ObtenerDocentes(db)
                    .FirstOrDefault(u => u.id_usuario == modelo.id_docente);
                if (docente == null)
                {
                    ModelState.AddModelError("id_docente", "El docente debe estar registrado en el sistema.");
                    return View(modelo);
                }
                if (!docente.estado)
                {
                    ModelState.AddModelError("id_docente", "El docente seleccionado está inactivo.");
                    return View(modelo);
                }

                // Escenario 2: la materia ya está asignada a esa sección
                if (db.SeccionesMaterias.Any(x => x.fk_id_seccion == idSeccion && x.fk_id_materia == idMateria))
                {
                    ModelState.AddModelError("", "La materia ya está asignada a esa sección.");
                    return View(modelo);
                }

                // Escenario 1: asignación exitosa
                db.SeccionesMaterias.Add(new SeccionMateriaEntity
                {
                    fk_id_seccion = idSeccion,
                    fk_id_materia = idMateria,
                    fecha_asignacion = DateTime.Now,
                    fk_id_profesor = docente.id_usuario
                });

                string nombreMateria = db.Materias.Where(m => m.id_materia == idMateria).Select(m => m.nombre).FirstOrDefault();
                string etiquetaSeccion = EtiquetasDeSecciones(db)[idSeccion];
                Auditoria.Registrar(db, "Asignación de materia a sección",
                    "Materia " + nombreMateria + " asignada a " + etiquetaSeccion + " con el docente " + NombreCompleto(docente) + ".",
                    null, new { id_seccion = idSeccion, seccion = etiquetaSeccion, id_materia = idMateria, materia = nombreMateria, id_profesor = docente.id_usuario });
                db.SaveChanges();

                TempData["Mensaje"] = "La materia se asignó correctamente a la sección.";
                return RedirectToAction("ListadoDeAsignaciones", new { seccion = idSeccion });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoverMateria(int id, int? seccion)
        {
            using (var db = new EduNexusDbContext())
            {
                SeccionMateriaEntity asignacion = db.SeccionesMaterias.FirstOrDefault(x => x.id_seccion_materia == id);
                if (asignacion == null)
                {
                    return HttpNotFound();
                }

                // Escenario 4: la materia tiene notas o asistencia registradas en esa sección
                int registros = db.Database.SqlQuery<int>(
                    "SELECT (SELECT COUNT(*) FROM asignaciones WHERE fk_id_seccion = @p0 AND fk_id_materia = @p1) " +
                    "+ (SELECT COUNT(*) FROM asistencia WHERE fk_id_seccion = @p0 AND fk_id_materia = @p1)",
                    asignacion.fk_id_seccion, asignacion.fk_id_materia).First();

                if (registros > 0)
                {
                    TempData["Error"] = "La materia tiene registros activos (notas o asistencia) en esta sección y no puede ser removida.";
                    return RedirectToAction("ListadoDeAsignaciones", new { seccion });
                }

                // La materia tampoco se puede remover si ya tiene horario en la sección
                int idSeccion = asignacion.fk_id_seccion;
                int idMateria = asignacion.fk_id_materia;
                if (db.Horarios.Any(h => h.fk_seccion == idSeccion && h.fk_materia == idMateria))
                {
                    TempData["Error"] = "La materia tiene horarios registrados en esta sección. Elimine o modifique primero esos horarios.";
                    return RedirectToAction("ListadoDeAsignaciones", new { seccion });
                }

                // Escenario 3: se elimina la asociación
                string nombreMateria = db.Materias.Where(m => m.id_materia == idMateria).Select(m => m.nombre).FirstOrDefault();
                string etiquetaSeccion;
                EtiquetasDeSecciones(db).TryGetValue(idSeccion, out etiquetaSeccion);
                db.SeccionesMaterias.Remove(asignacion);
                Auditoria.Registrar(db, "Remover materia de sección",
                    "Materia " + nombreMateria + " removida de " + etiquetaSeccion + ".",
                    new { id_seccion = idSeccion, seccion = etiquetaSeccion, id_materia = idMateria, materia = nombreMateria, id_profesor = asignacion.fk_id_profesor }, null);
                db.SaveChanges();
            }

            TempData["Mensaje"] = "La materia se removió de la sección correctamente.";
            return RedirectToAction("ListadoDeAsignaciones", new { seccion });
        }

        private void CargarListas(EduNexusDbContext db)
        {
            ViewBag.Materias = db.Materias.OrderBy(m => m.nombre).ToList()
                .Select(m => new SelectListItem { Value = m.id_materia.ToString(), Text = m.id_materia + " - " + m.nombre })
                .ToList();

            ViewBag.EtiquetasSecciones = EtiquetasDeSecciones(db).Values.OrderBy(x => x).ToList();
            ViewBag.Docentes = new SelectList(
                HorarioServicio.ObtenerDocentes(db)
                    .Where(u => u.estado)
                    .Select(u => new { u.id_usuario, nombre = HorarioServicio.NombreCompleto(u) }).ToList(),
                "id_usuario", "nombre");
        }

        private static Dictionary<int, string> EtiquetasDeSecciones(EduNexusDbContext db)
        {
            return HorarioServicio.EtiquetasDeSecciones(db);
        }

        private static string NombreCompleto(UsuarioEntity u)
        {
            return HorarioServicio.NombreCompleto(u);
        }

        private static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return "";
            }
            return HorarioServicio.SinTildes(string.Join(" ", texto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
        }
    }
}