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

                List<string> idsProfesores = materias.Values.Select(m => m.profesor).Distinct().ToList();
                Dictionary<string, UsuarioEntity> profesores = db.Usuarios
                    .Where(u => idsProfesores.Contains(u.id_usuario)).ToList()
                    .ToDictionary(u => u.id_usuario);

                List<MateriaSeccionDto> modelo = asignaciones.Select(a =>
                {
                    MateriaEntity materia = materias[a.fk_id_materia];
                    UsuarioEntity profesor;
                    profesores.TryGetValue(materia.profesor, out profesor);
                    string etiqueta;
                    etiquetas.TryGetValue(a.fk_id_seccion, out etiqueta);

                    return new MateriaSeccionDto
                    {
                        id_seccion_materia = a.id_seccion_materia,
                        id_seccion = a.fk_id_seccion,
                        seccion = etiqueta,
                        id_materia = materia.id_materia,
                        materia = materia.nombre,
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

        public ActionResult AsignarMateria()
        {
            using (var db = new EduNexusDbContext())
            {
                CargarListas(db);
            }
            return View(new AsignarMateriaDto());
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
                    fecha_asignacion = DateTime.Now
                });
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

                // Escenario 3: se elimina la asociación
                db.SeccionesMaterias.Remove(asignacion);
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
        }

        // Etiqueta de cada seccion ejemplo "Tercer Grado - 3-A (2026)"
        private static Dictionary<int, string> EtiquetasDeSecciones(EduNexusDbContext db)
        {
            Dictionary<int, string> grados = db.Grados.ToList().ToDictionary(g => g.id_grado, g => g.nombre);
            Dictionary<int, int> anios = db.Calendarios.ToList().ToDictionary(c => c.id_calendario, c => c.fecha_inicio.Year);

            return db.Secciones.ToList().ToDictionary(
                s => s.id_seccion,
                s => (grados.ContainsKey(s.fk_id_grado) ? grados[s.fk_id_grado] : "Grado")
                     + " - " + s.nombre
                     + (anios.ContainsKey(s.fk_id_calendario) ? " (" + anios[s.fk_id_calendario] + ")" : ""));
        }

        private static string NombreCompleto(UsuarioEntity u)
        {
            return string.Join(" ", new[] { u.nombre, u.apellido1, u.apellido2 }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return "";
            }
            return string.Join(" ", texto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        }
    }
}