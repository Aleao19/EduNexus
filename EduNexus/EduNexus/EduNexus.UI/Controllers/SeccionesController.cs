using EduNexus.Abstracciones.ModelosParaUI.Grados;
using EduNexus.Abstracciones.ModelosParaUI.Secciones;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RCONF-04-002 Como administrador quiero crear las secciones con nombre del grado, nombre de sección y año para asignar a los estudiantes
    // RGRU-06-003 Como administrador quiero consultar la cantidad de estudiantes por grupo para controlar los cupos disponibles
    // Tablas: secciones (fk_id_grado, fk_id_calendario), grados, calendario, secciones_estudiantes, bitacora
    [AutorizarAdministrador]
    public class SeccionesController : Controller
    {
        private const string EventoModificacion = "Modificación de sección";

        // GET: Secciones
        public ActionResult ListadoDeSecciones()
        {
            using (var db = new EduNexusDbContext())
            {
                return View(ObtenerSecciones(db, null, null, null));
            }
        }

        // GET: Secciones/CuposPorGrupo?grado=1&anio=2026
        public ActionResult CuposPorGrupo(int? grado, int? anio)
        {
            using (var db = new EduNexusDbContext())
            {
                List<SeccionesDto> todas = ObtenerSecciones(db, null, null, null);

                ViewBag.Grados = ObtenerGrados(db);
                ViewBag.Anios = todas.Where(x => x.anio.HasValue).Select(x => x.anio.Value)
                                     .Distinct().OrderByDescending(x => x).ToList();
                ViewBag.GradoSeleccionado = grado;
                ViewBag.AnioSeleccionado = anio;

                // Escenarios 1 y 2: cada grupo muestra estudiantes asignados y cupos disponibles
                // (un grupo sin estudiantes muestra 0 y el cupo completo disponible).
                // Escenario 3: filtrado por nivel educativo (grado) o periodo lectivo (año).
                var filtradas = todas
                    .Where(x => !grado.HasValue || x.grado == grado.Value)
                    .Where(x => !anio.HasValue || x.anio == anio.Value)
                    .ToList();
                return View(filtradas);
            }
        }

        // GET: Secciones/DetalleDeSeccion/5
        public ActionResult DetalleDeSeccion(int? id)
        {
            SeccionesDto seccion = BuscarSeccion(id, true);
            if (seccion == null)
            {
                return HttpNotFound();
            }
            return View(seccion);
        }

        // GET: Secciones/CrearSeccion
        public ActionResult CrearSeccion()
        {
            using (var db = new EduNexusDbContext())
            {
                ViewBag.Grados = ObtenerGrados(db);
            }
            return View();
        }

        // POST: Secciones/CrearSeccion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearSeccion(SeccionesDto seccion)
        {
            using (var db = new EduNexusDbContext())
            {
                ViewBag.Grados = ObtenerGrados(db);

                // Escenario 3: creación con datos obligatorios incompletos
                if (!ModelState.IsValid)
                {
                    return View(seccion);
                }

                string nombreGrado = seccion.nombre_grado.Trim();
                string nombre = seccion.nombre.Trim();

                // Escenario 4: creación de sección para un grado inexistente
                GradoEntity grado = db.Grados.ToList().FirstOrDefault(x =>
                    string.Equals(x.nombre.Trim(), nombreGrado, StringComparison.OrdinalIgnoreCase));
                if (grado == null)
                {
                    ModelState.AddModelError("nombre_grado", "El grado no existe en el sistema. Primero debe crear el grado.");
                    return View(seccion);
                }

                // El año lectivo debe existir en el calendario
                CalendarioEntity calendario = BuscarCalendario(db, seccion.anio.Value);
                if (calendario == null)
                {
                    ModelState.AddModelError("anio", "No hay un periodo lectivo registrado en el calendario para el año " + seccion.anio.Value + ".");
                    return View(seccion);
                }

                // Escenario 2: creación de sección duplicada (mismo nombre, grado y año)
                if (ExisteSeccion(db, nombre, grado.id_grado, seccion.anio.Value, 0))
                {
                    ModelState.AddModelError("", "Ya existe esa sección para ese grado y año.");
                    return View(seccion);
                }

                // Escenario 1: creación exitosa de sección
                db.Secciones.Add(new SeccionEntity
                {
                    nombre = nombre,
                    fk_id_grado = grado.id_grado,
                    fk_id_calendario = calendario.id_calendario,
                    cupo = seccion.cupo
                });
                db.SaveChanges();
            }

            TempData["Mensaje"] = "La sección se registró correctamente y está disponible para asignar estudiantes.";
            return RedirectToAction("ListadoDeSecciones");
        }

        // GET: Secciones/EditarSeccion/5
        public ActionResult EditarSeccion(int? id)
        {
            SeccionesDto seccion = BuscarSeccion(id, false);
            if (seccion == null)
            {
                return HttpNotFound();
            }
            return View(seccion);
        }

        // POST: Secciones/EditarSeccion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarSeccion(SeccionesDto seccion)
        {
            using (var db = new EduNexusDbContext())
            {
                SeccionEntity existente = db.Secciones.FirstOrDefault(x => x.id_seccion == seccion.id_seccion);
                if (existente == null)
                {
                    return HttpNotFound();
                }

                // El grado no se modifica desde esta pantalla
                seccion.grado = existente.fk_id_grado;
                GradoEntity grado = db.Grados.FirstOrDefault(g => g.id_grado == existente.fk_id_grado);
                seccion.nombre_grado = grado != null ? grado.nombre : "";
                ModelState.Remove("nombre_grado");

                if (!ModelState.IsValid)
                {
                    return View(seccion);
                }

                CalendarioEntity calendario = BuscarCalendario(db, seccion.anio.Value);
                if (calendario == null)
                {
                    ModelState.AddModelError("anio", "No hay un periodo lectivo registrado en el calendario para el año " + seccion.anio.Value + ".");
                    return View(seccion);
                }

                string nombre = seccion.nombre.Trim();
                if (ExisteSeccion(db, nombre, existente.fk_id_grado, seccion.anio.Value, existente.id_seccion))
                {
                    ModelState.AddModelError("", "Ya existe esa sección para ese grado y año.");
                    return View(seccion);
                }

                CalendarioEntity calendarioAnterior = db.Calendarios.FirstOrDefault(c => c.id_calendario == existente.fk_id_calendario);
                var antes = new
                {
                    existente.nombre,
                    anio = calendarioAnterior != null ? calendarioAnterior.fecha_inicio.Year : (int?)null,
                    existente.cupo
                };

                existente.nombre = nombre;
                existente.fk_id_calendario = calendario.id_calendario;
                existente.cupo = seccion.cupo;

                // Escenario 5: edición exitosa, se registra la fecha y el usuario que realizó la modificación (bitácora)
                var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;
                if (usuario != null)
                {
                    db.Bitacora.Add(new BitacoraEntity
                    {
                        tipo_de_evento = EventoModificacion,
                        descripcion_evento = DescripcionEvento(existente.id_seccion) + " " + nombre,
                        fecha = DateTime.Now,
                        datos_anteriores = JsonConvert.SerializeObject(antes),
                        datos_posteriores = JsonConvert.SerializeObject(new { nombre, anio = seccion.anio.Value, cupo = seccion.cupo }),
                        fk_id_usuario = usuario.id_usuario
                    });
                }
                db.SaveChanges();
            }

            TempData["Mensaje"] = "La sección se actualizó correctamente.";
            return RedirectToAction("ListadoDeSecciones");
        }

        // GET: Secciones/EliminarSeccion/5
        public ActionResult EliminarSeccion(int? id)
        {
            SeccionesDto seccion = BuscarSeccion(id, false);
            if (seccion == null)
            {
                return HttpNotFound();
            }
            return View(seccion);
        }

        // POST: Secciones/EliminarSeccion/5
        [HttpPost, ActionName("EliminarSeccion")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarEliminarSeccion(int id)
        {
            using (var db = new EduNexusDbContext())
            {
                SeccionEntity seccion = db.Secciones.FirstOrDefault(x => x.id_seccion == id);
                if (seccion == null)
                {
                    return HttpNotFound();
                }

                // Escenario 7: intento de eliminar sección con estudiantes asignados
                if (db.SeccionesEstudiantes.Any(x => x.fk_id_seccion == id))
                {
                    TempData["Error"] = "No se puede eliminar la sección porque tiene estudiantes asignados.";
                    return RedirectToAction("ListadoDeSecciones");
                }

                // La sección tampoco se puede borrar si otros módulos ya la usan (horarios o asignaciones)
                int usos = db.Database.SqlQuery<int>(
                    "SELECT (SELECT COUNT(*) FROM horarios WHERE fk_seccion = @p0) + (SELECT COUNT(*) FROM asignaciones WHERE fk_id_seccion = @p0)",
                    id).First();
                if (usos > 0)
                {
                    TempData["Error"] = "No se puede eliminar la sección porque tiene horarios o asignaciones asociadas.";
                    return RedirectToAction("ListadoDeSecciones");
                }

                // Escenario 6: eliminación de sección sin estudiantes
                db.Secciones.Remove(seccion);
                db.SaveChanges();
            }

            TempData["Mensaje"] = "La sección se eliminó correctamente.";
            return RedirectToAction("ListadoDeSecciones");
        }

        private static string DescripcionEvento(int idSeccion)
        {
            return "Sección #" + idSeccion + ":";
        }

        // El año lectivo corresponde al periodo del calendario que inicia en ese año
        private static CalendarioEntity BuscarCalendario(EduNexusDbContext db, int anio)
        {
            return db.Calendarios.ToList()
                .Where(c => c.fecha_inicio.Year == anio)
                .OrderBy(c => c.fecha_inicio)
                .FirstOrDefault();
        }

        private static bool ExisteSeccion(EduNexusDbContext db, string nombre, int idGrado, int anio, int idExcluido)
        {
            var anioPorCalendario = db.Calendarios.ToList().ToDictionary(c => c.id_calendario, c => c.fecha_inicio.Year);
            return db.Secciones
                .Where(x => x.id_seccion != idExcluido && x.fk_id_grado == idGrado)
                .ToList()
                .Any(x => anioPorCalendario.ContainsKey(x.fk_id_calendario)
                       && anioPorCalendario[x.fk_id_calendario] == anio
                       && string.Equals(x.nombre.Trim(), nombre, StringComparison.OrdinalIgnoreCase));
        }

        private static List<GradosDto> ObtenerGrados(EduNexusDbContext db)
        {
            return db.Grados
                .OrderBy(x => x.nivel)
                .ToList()
                .Select(x => new GradosDto { id_grado = x.id_grado, grado = x.nivel, descripcion = x.nombre })
                .ToList();
        }

        private static SeccionesDto BuscarSeccion(int? id, bool incluirAuditoria)
        {
            if (id == null)
            {
                return null;
            }
            using (var db = new EduNexusDbContext())
            {
                SeccionesDto seccion = ObtenerSecciones(db, null, null, id.Value).FirstOrDefault();
                if (seccion != null && incluirAuditoria)
                {
                    // Última modificación registrada en la bitácora
                    string prefijo = DescripcionEvento(seccion.id_seccion);
                    var ultima = (from b in db.Bitacora
                                  join u in db.Usuarios on b.fk_id_usuario equals u.id_usuario into usuariosEvento
                                  from u in usuariosEvento.DefaultIfEmpty()
                                  where b.tipo_de_evento == EventoModificacion && b.descripcion_evento.StartsWith(prefijo)
                                  orderby b.fecha descending
                                  select new { b.fecha, u.nombre, u.apellido1, u.email })
                                 .FirstOrDefault();
                    if (ultima != null)
                    {
                        seccion.fecha_modificacion = ultima.fecha;
                        seccion.usuario_modificacion = ((ultima.nombre ?? "") + " " + (ultima.apellido1 ?? "")).Trim()
                                                       + (ultima.email != null ? " (" + ultima.email + ")" : "");
                    }
                }
                return seccion;
            }
        }

        private static List<SeccionesDto> ObtenerSecciones(EduNexusDbContext db, int? grado, int? anio, int? idSeccion)
        {
            var consulta = db.Secciones.AsQueryable();
            if (idSeccion.HasValue)
            {
                consulta = consulta.Where(s => s.id_seccion == idSeccion.Value);
            }
            if (grado.HasValue)
            {
                consulta = consulta.Where(s => s.fk_id_grado == grado.Value);
            }

            var filas = (from s in consulta
                         join g in db.Grados on s.fk_id_grado equals g.id_grado into gradosSeccion
                         from g in gradosSeccion.DefaultIfEmpty()
                         join c in db.Calendarios on s.fk_id_calendario equals c.id_calendario into calendariosSeccion
                         from c in calendariosSeccion.DefaultIfEmpty()
                         select new
                         {
                             s,
                             nombreGrado = g != null ? g.nombre : "",
                             nivel = g != null ? g.nivel : 0,
                             inicioPeriodo = c != null ? (DateTime?)c.fecha_inicio : null,
                             estudiantes = db.SeccionesEstudiantes.Count(e => e.fk_id_seccion == s.id_seccion)
                         })
                        .ToList();

            return filas
                .Select(x => new
                {
                    x.nivel,
                    dto = new SeccionesDto
                {
                    id_seccion = x.s.id_seccion,
                    nombre = x.s.nombre,
                    grado = x.s.fk_id_grado,
                    nombre_grado = x.nombreGrado,
                    id_calendario = x.s.fk_id_calendario,
                    anio = x.inicioPeriodo.HasValue ? x.inicioPeriodo.Value.Year : (int?)null,
                    cupo = x.s.cupo,
                    estudiantes_matriculados = x.estudiantes
                }
                })
                .Where(x => !anio.HasValue || x.dto.anio == anio.Value)
                .OrderByDescending(x => x.dto.anio)
                .ThenBy(x => x.nivel)
                .ThenBy(x => x.dto.nombre)
                .Select(x => x.dto)
                .ToList();
        }
    }
}
