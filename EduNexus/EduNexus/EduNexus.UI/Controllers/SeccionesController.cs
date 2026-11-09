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

        // GET: Secciones/AsignarEstudiantes/5
        [PermitirCoordinador]
        public ActionResult AsignarEstudiantes(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(400);
            }

            using (var db = new EduNexusDbContext())
            {
                var seccion = ObtenerSecciones(db, null, null, id.Value).FirstOrDefault();

                if (seccion == null)
                {
                    return HttpNotFound();
                }

                CargarDatosAsignacion(db, id.Value);
                return View(seccion);
            }
        }

        // POST: Secciones/AsignarEstudiantes
        [PermitirCoordinador]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarEstudiantes(int id, string idUsuario)
        {
            using (var db = new EduNexusDbContext())
            {
                var seccion = db.Secciones.FirstOrDefault(s => s.id_seccion == id);

                if (seccion == null)
                {
                    return HttpNotFound();
                }

                var rolEstudiante = db.Roles.ToList().FirstOrDefault(r =>
                    !string.IsNullOrWhiteSpace(r.nombre) &&
                    (r.nombre.Trim().Equals("Estudiante", StringComparison.OrdinalIgnoreCase) ||
                     r.nombre.Trim().Equals("Estudiantes", StringComparison.OrdinalIgnoreCase)));

                if (rolEstudiante == null)
                {
                    TempData["Error"] = "No se encontró el rol Estudiante. Verifique el nombre del rol registrado.";
                    return RedirectToAction("AsignarEstudiantes", new { id = id });
                }

                var estudiante = db.Usuarios.FirstOrDefault(u =>
                    u.id_usuario == idUsuario &&
                    u.estado &&
                    u.rol == rolEstudiante.id_rol);

                if (estudiante == null)
                {
                    TempData["Error"] = "Seleccione un estudiante válido y activo.";
                    return RedirectToAction("AsignarEstudiantes", new { id = id });
                }

                bool yaAsignado = db.SeccionesEstudiantes.Any(e =>
                    e.fk_id_seccion == id && e.fk_id_usuario == idUsuario);

                if (yaAsignado)
                {
                    TempData["Error"] = "Este estudiante ya está asignado a esta sección.";
                    return RedirectToAction("AsignarEstudiantes", new { id = id });
                }

                // Un estudiante solo puede pertenecer a una sección por periodo lectivo
                int idCalendario = seccion.fk_id_calendario;
                int? otraSeccion = (from e in db.SeccionesEstudiantes
                                    join s in db.Secciones on e.fk_id_seccion equals s.id_seccion
                                    where e.fk_id_usuario == idUsuario && s.fk_id_calendario == idCalendario
                                    select (int?)s.id_seccion).FirstOrDefault();

                if (otraSeccion.HasValue)
                {
                    var etiquetasOtra = HorarioServicio.EtiquetasDeSecciones(db);
                    TempData["Error"] = "El estudiante ya está asignado a la sección "
                        + (etiquetasOtra.ContainsKey(otraSeccion.Value) ? etiquetasOtra[otraSeccion.Value] : otraSeccion.Value.ToString())
                        + " en este periodo lectivo. Use la opción \"Mover\" desde esa sección.";
                    return RedirectToAction("AsignarEstudiantes", new { id = id });
                }

                int cantidadActual = db.SeccionesEstudiantes.Count(e => e.fk_id_seccion == id);

                if (cantidadActual >= seccion.cupo)
                {
                    TempData["Error"] = "No se puede asignar al estudiante porque la sección ya alcanzó su cupo máximo.";
                    return RedirectToAction("AsignarEstudiantes", new { id = id });
                }

                db.SeccionesEstudiantes.Add(new SeccionEstudianteEntity
                {
                    fk_id_seccion = id,
                    fk_id_usuario = idUsuario,
                    fecha_asignacion = DateTime.Now
                });

                var etiquetas = HorarioServicio.EtiquetasDeSecciones(db);
                string etiqueta = etiquetas.ContainsKey(id) ? etiquetas[id] : seccion.nombre;
                Auditoria.Registrar(db, "Asignación de estudiante a sección",
                    "Estudiante " + HorarioServicio.NombreCompleto(estudiante) + " (" + estudiante.identificacion + ") asignado a " + etiqueta + ".",
                    null, new { id_seccion = id, seccion = etiqueta, id_usuario = idUsuario });

                db.SaveChanges();
            }

            TempData["Mensaje"] = "El estudiante se asignó correctamente a la sección.";
            return RedirectToAction("AsignarEstudiantes", new { id = id });
        }
        // POST: Secciones/MoverEstudiante
        [PermitirCoordinador]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MoverEstudiante(int id, string idUsuario, int? idSeccionDestino)
        {
            if (!idSeccionDestino.HasValue)
            {
                TempData["Error"] = "Seleccione la sección destino.";
                return RedirectToAction("AsignarEstudiantes", new { id = id });
            }

            using (var db = new EduNexusDbContext())
            {
                var origen = db.Secciones.FirstOrDefault(s => s.id_seccion == id);
                var destino = db.Secciones.FirstOrDefault(s => s.id_seccion == idSeccionDestino.Value);

                if (origen == null || destino == null)
                {
                    return HttpNotFound();
                }

                if (destino.id_seccion == origen.id_seccion)
                {
                    TempData["Error"] = "El estudiante ya está en esa sección.";
                    return RedirectToAction("AsignarEstudiantes", new { id = id });
                }

                if (destino.fk_id_grado != origen.fk_id_grado || destino.fk_id_calendario != origen.fk_id_calendario)
                {
                    TempData["Error"] = "Solo se puede mover al estudiante a una sección del mismo grado y año lectivo.";
                    return RedirectToAction("AsignarEstudiantes", new { id = id });
                }

                var asignacion = db.SeccionesEstudiantes.FirstOrDefault(e =>
                    e.fk_id_seccion == id && e.fk_id_usuario == idUsuario);

                if (asignacion == null)
                {
                    TempData["Error"] = "El estudiante no está asignado a esta sección.";
                    return RedirectToAction("AsignarEstudiantes", new { id = id });
                }

                if (db.SeccionesEstudiantes.Any(e => e.fk_id_seccion == destino.id_seccion && e.fk_id_usuario == idUsuario))
                {
                    TempData["Error"] = "El estudiante ya está asignado a la sección destino.";
                    return RedirectToAction("AsignarEstudiantes", new { id = id });
                }

                if (!db.Horarios.Any(h => h.fk_seccion == destino.id_seccion))
                {
                    TempData["Error"] = "La sección destino no tiene horario configurado.";
                    return RedirectToAction("AsignarEstudiantes", new { id = id });
                }

                if (db.SeccionesEstudiantes.Count(e => e.fk_id_seccion == destino.id_seccion) >= destino.cupo)
                {
                    TempData["Error"] = "La sección destino ya alcanzó su cupo máximo.";
                    return RedirectToAction("AsignarEstudiantes", new { id = id });
                }

                var etiquetas = HorarioServicio.EtiquetasDeSecciones(db);
                string etiquetaOrigen = etiquetas.ContainsKey(origen.id_seccion) ? etiquetas[origen.id_seccion] : origen.nombre;
                string etiquetaDestino = etiquetas.ContainsKey(destino.id_seccion) ? etiquetas[destino.id_seccion] : destino.nombre;

                // Se reutiliza el mismo registro: solo cambia la sección y la fecha de asignación
                asignacion.fk_id_seccion = destino.id_seccion;
                asignacion.fecha_asignacion = DateTime.Now;

                Auditoria.Registrar(db, "Cambio de sección de estudiante",
                    "Estudiante " + idUsuario + " movido a " + etiquetaDestino,
                    new { id_seccion = origen.id_seccion, seccion = etiquetaOrigen },
                    new { id_seccion = destino.id_seccion, seccion = etiquetaDestino });

                db.SaveChanges();

                TempData["Mensaje"] = "El estudiante se movió a " + etiquetaDestino + ".";
            }

            return RedirectToAction("AsignarEstudiantes", new { id = id });
        }

        private void CargarDatosAsignacion(EduNexusDbContext db, int idSeccion)
        {
            var rolEstudiante = db.Roles.ToList().FirstOrDefault(r =>
                !string.IsNullOrWhiteSpace(r.nombre) &&
                (r.nombre.Trim().Equals("Estudiante", StringComparison.OrdinalIgnoreCase) ||
                 r.nombre.Trim().Equals("Estudiantes", StringComparison.OrdinalIgnoreCase)));

            var estudiantes = new List<object>();
            var seccionActual = db.Secciones.FirstOrDefault(s => s.id_seccion == idSeccion);

            if (rolEstudiante != null)
            {
                // Se excluyen los estudiantes que ya tienen sección en el mismo periodo lectivo
                int idCalendario = seccionActual != null ? seccionActual.fk_id_calendario : 0;
                List<string> yaAsignados = (from e in db.SeccionesEstudiantes
                                            join s in db.Secciones on e.fk_id_seccion equals s.id_seccion
                                            where s.fk_id_calendario == idCalendario
                                            select e.fk_id_usuario).Distinct().ToList();
                var asignadosSet = new HashSet<string>(yaAsignados);

                estudiantes = db.Usuarios
                    .Where(u => u.estado && u.rol == rolEstudiante.id_rol)
                    .OrderBy(u => u.nombre).ThenBy(u => u.apellido1)
                    .ToList()
                    .Where(u => !asignadosSet.Contains(u.id_usuario))
                    .Select(u => (object)new
                    {
                        id_usuario = u.id_usuario,
                        nombreCompleto = ((u.nombre ?? "") + " " +
                                          (u.apellido1 ?? "") + " " +
                                          (u.apellido2 ?? "")).Trim() +
                                          " - " + (u.identificacion ?? "")
                    })
                    .ToList();
            }

            ViewBag.EstudiantesDisponibles = new SelectList(
                estudiantes,
                "id_usuario",
                "nombreCompleto");

            ViewBag.ErrorRolEstudiante = rolEstudiante == null;

            ViewBag.EstudiantesAsignados = (
                from e in db.SeccionesEstudiantes
                join u in db.Usuarios on e.fk_id_usuario equals u.id_usuario
                where e.fk_id_seccion == idSeccion
                orderby u.nombre, u.apellido1
                select new EstudianteSeccionVm
                {
                    id_usuario = u.id_usuario,
                    nombreCompleto = ((u.nombre ?? "") + " " +
                                      (u.apellido1 ?? "") + " " +
                                      (u.apellido2 ?? "")).Trim(),
                    identificacion = u.identificacion,
                    fecha_asignacion = e.fecha_asignacion
                }).ToList();

            var etiquetasSecciones = HorarioServicio.EtiquetasDeSecciones(db);

            // Secciones a las que se puede mover un estudiante: mismo grado y mismo año lectivo.
            // Para permitir cualquier sección, quita la condición del .Where.
            ViewBag.SeccionesDestino = seccionActual == null
                ? new List<SeccionDestinoVm>()
                : ObtenerSecciones(db, seccionActual.fk_id_grado, null, null)
                    .Where(s => s.id_seccion != idSeccion && s.id_calendario == seccionActual.fk_id_calendario)
                    .Select(s => new SeccionDestinoVm
                    {
                        id_seccion = s.id_seccion,
                        texto = etiquetasSecciones.ContainsKey(s.id_seccion) ? etiquetasSecciones[s.id_seccion] : s.nombre_grado + " - " + s.nombre,
                        disponibles = Math.Max(0, s.cupo - s.estudiantes_matriculados)
                    }).ToList();

            ViewBag.CupoMaximo = seccionActual != null ? seccionActual.cupo : 0;
            ViewBag.CantidadAsignada = db.SeccionesEstudiantes.Count(e =>
                e.fk_id_seccion == idSeccion);
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
                Auditoria.Registrar(db, "Crear sección",
                    "Se registró la sección " + nombre + " de " + grado.nombre + " (" + seccion.anio.Value + ").",
                    null, new { nombre, grado = grado.nombre, anio = seccion.anio.Value, cupo = seccion.cupo });
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

                // El cupo no puede quedar por debajo de los estudiantes ya asignados
                int asignados = db.SeccionesEstudiantes.Count(e => e.fk_id_seccion == existente.id_seccion);
                seccion.estudiantes_matriculados = asignados;
                if (seccion.cupo < asignados)
                {
                    ModelState.AddModelError("cupo", "El cupo no puede ser menor a los " + asignados + " estudiantes ya asignados a la sección.");
                    return View(seccion);
                }

                // Con estudiantes asignados no se cambia el periodo lectivo (podrían quedar en dos secciones del mismo periodo)
                if (calendario.id_calendario != existente.fk_id_calendario && asignados > 0)
                {
                    ModelState.AddModelError("anio", "No se puede cambiar el año lectivo de una sección que ya tiene estudiantes asignados.");
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
                Auditoria.Registrar(db, EventoModificacion,
                    DescripcionEvento(existente.id_seccion) + " " + nombre,
                    antes, new { nombre, anio = seccion.anio.Value, cupo = seccion.cupo });
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
                    "SELECT (SELECT COUNT(*) FROM horarios WHERE fk_seccion = @p0) + (SELECT COUNT(*) FROM asignaciones WHERE fk_id_seccion = @p0) + (SELECT COUNT(*) FROM asistencia WHERE fk_id_seccion = @p0)",
                    id).First();
                if (usos > 0)
                {
                    TempData["Error"] = "No se puede eliminar la sección porque tiene horarios, asignaciones o registros de asistencia asociados.";
                    return RedirectToAction("ListadoDeSecciones");
                }

                // Las materias asignadas a la sección (secciones_materias) se quitan junto con ella
                var materiasDeSeccion = db.SeccionesMaterias.Where(sm => sm.fk_id_seccion == id).ToList();

                // Escenario 6: eliminación de sección sin estudiantes
                var etiquetas = HorarioServicio.EtiquetasDeSecciones(db);
                string etiqueta = etiquetas.ContainsKey(id) ? etiquetas[id] : seccion.nombre;
                db.SeccionesMaterias.RemoveRange(materiasDeSeccion);
                db.Secciones.Remove(seccion);
                Auditoria.Registrar(db, "Eliminar sección", "Se eliminó la sección " + etiqueta + ".",
                    new { seccion.id_seccion, seccion.nombre, seccion.fk_id_grado, seccion.fk_id_calendario, seccion.cupo,
                          materias = materiasDeSeccion.Select(m => m.fk_id_materia).ToList() }, null);
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
