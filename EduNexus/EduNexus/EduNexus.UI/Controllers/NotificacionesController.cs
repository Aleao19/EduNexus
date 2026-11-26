using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Data;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RASI-10-004: Notificaciones de ausencia.
    // RJT-11-001: Enviar justificación.

    public class NotificacionesController : Controller
    {
        // ========================================
        // VALIDAR ACCESO
        // ========================================

        protected override void OnActionExecuting(
            ActionExecutingContext filterContext)
        {
            var usuario = Session[
                AutorizarAdministradorAttribute.ClaveSesion
            ] as UsuarioDto;

            if (usuario == null)
            {
                filterContext.Result =
                    RedirectToAction("Login", "Home");

                return;
            }

            using (var db = new EduNexusDbContext())
            {
                string idUsuario = usuario.id_usuario;

                bool esEncargado = db.EncargadosEstudiantes
                    .Any(e =>
                        e.fk_id_encargado == idUsuario &&
                        e.estado);

                bool usuarioActivo = db.Usuarios
                    .Any(u =>
                        u.id_usuario == idUsuario &&
                        u.estado);

                bool tieneRolEncargado = RolesSistema.Es(
                    usuario,
                    RolesSistema.Padre);

                if (!esEncargado ||
                    !usuarioActivo ||
                    !tieneRolEncargado)
                {
                    TempData["MensajeError"] =
                        "No tiene autorización para acceder " +
                        "a las notificaciones y justificaciones.";

                    filterContext.Result =
                        RedirectToAction("Index", "Home");

                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }

        // ========================================
        // LISTADO DE NOTIFICACIONES
        // ========================================

        [HttpGet]
        public ActionResult Index()
        {
            using (var db = new EduNexusDbContext())
            {
                string idUsuario = ObtenerIdUsuario();

                var notificaciones = db.Notificaciones
                    .Where(n =>
                        n.fk_id_destinatario == idUsuario &&
                        db.EncargadosEstudiantes.Any(e =>
                            e.fk_id_encargado == idUsuario &&
                            e.fk_id_estudiante ==
                                n.fk_id_estudiante &&
                            e.estado))
                    .OrderBy(n => n.leida)
                    .ThenByDescending(n => n.fecha_creacion)
                    .ToList();

                ViewBag.TotalNotificaciones =
                    notificaciones.Count;

                ViewBag.NoLeidas =
                    notificaciones.Count(n => !n.leida);

                return View(notificaciones);
            }
        }

        // ========================================
        // DETALLE DE NOTIFICACIÓN
        // ========================================

        [HttpGet]
        public ActionResult Detalle(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return RedirectToAction("Index");
            }

            using (var db = new EduNexusDbContext())
            {
                string idUsuario = ObtenerIdUsuario();

                var notificacion = db.Notificaciones
                    .FirstOrDefault(n =>
                        n.id_notificacion == id.Value &&
                        n.fk_id_destinatario == idUsuario &&
                        db.EncargadosEstudiantes.Any(e =>
                            e.fk_id_encargado == idUsuario &&
                            e.fk_id_estudiante ==
                                n.fk_id_estudiante &&
                            e.estado));

                if (notificacion == null)
                {
                    return HttpNotFound();
                }

                var asistencia = db.Asistencias
                    .FirstOrDefault(a =>
                        a.id_asistencia ==
                            notificacion.fk_id_asistencia &&
                        a.fk_id_usuario ==
                            notificacion.fk_id_estudiante);

                if (asistencia == null)
                {
                    return HttpNotFound();
                }

                var estudiante = db.Usuarios
                    .FirstOrDefault(u =>
                        u.id_usuario ==
                            notificacion.fk_id_estudiante);

                var materia = db.Materias
                    .FirstOrDefault(m =>
                        m.id_materia ==
                            asistencia.fk_id_materia);

                ViewBag.NombreEstudiante =
                    estudiante != null
                    ? (
                        estudiante.nombre + " " +
                        estudiante.apellido1 + " " +
                        estudiante.apellido2
                      ).Trim()
                    : "Estudiante no disponible";

                ViewBag.NombreMateria =
                    materia != null
                    ? materia.nombre
                    : "Materia no disponible";

                ViewBag.FechaAusencia =
                    asistencia.fecha.ToString("dd/MM/yyyy");

                ViewBag.EstadoAsistencia =
                    asistencia.estado;

                ViewBag.MotivoJustificacion =
                    asistencia.motivo_justificacion;

                // Consultar justificaciones existentes.

                var justificacion = db.JustificacionesAusencia
                    .Where(j =>
                        j.fk_id_asistencia ==
                            asistencia.id_asistencia &&
                        j.fk_id_estudiante ==
                            asistencia.fk_id_usuario &&
                        j.fk_id_encargado == idUsuario)
                    .OrderByDescending(j => j.fecha_envio)
                    .ThenByDescending(j => j.id_justificacion)
                    .FirstOrDefault();

                bool existeJustificacionPendiente =
                    db.JustificacionesAusencia.Any(j =>
                        j.fk_id_asistencia ==
                            asistencia.id_asistencia &&
                        j.estado == "Pendiente");

                ViewBag.TieneJustificacion =
                    justificacion != null;

                ViewBag.ExisteJustificacionPendiente =
                    existeJustificacionPendiente;

                ViewBag.EstadoJustificacion =
                    justificacion != null
                    ? justificacion.estado
                    : null;

                ViewBag.MotivoSolicitud =
                    justificacion != null
                    ? justificacion.motivo
                    : null;

                ViewBag.FechaEnvioJustificacion =
                    justificacion != null
                    ? justificacion.fecha_envio
                        .ToString("dd/MM/yyyy HH:mm")
                    : null;

                ViewBag.ObservacionesDocente =
                    justificacion != null
                    ? justificacion.observaciones
                    : null;

                ViewBag.PuedeJustificar =
                    asistencia.estado ==
                        "Ausencia no justificada" &&
                    !existeJustificacionPendiente;

                return View(notificacion);
            }
        }

        // ========================================
        // RJT-11-001
        // LISTADO DE AUSENCIAS PARA JUSTIFICAR
        // ========================================

        [HttpGet]
        public ActionResult EnviarJustificacion(
            string id_estudiante = null)
        {
            using (var db = new EduNexusDbContext())
            {
                string idUsuario = ObtenerIdUsuario();

                var idsEstudiantes =
                    db.EncargadosEstudiantes
                        .Where(e =>
                            e.fk_id_encargado == idUsuario &&
                            e.estado)
                        .Select(e => e.fk_id_estudiante)
                        .Distinct()
                        .ToList();

                CargarEstudiantes(
                    db,
                    idUsuario,
                    id_estudiante);

                if (!string.IsNullOrWhiteSpace(id_estudiante) &&
                    !idsEstudiantes.Contains(id_estudiante))
                {
                    return new HttpStatusCodeResult(403);
                }

                var consulta = db.Asistencias
                    .Where(a =>
                        idsEstudiantes.Contains(
                            a.fk_id_usuario) &&
                        a.estado ==
                            "Ausencia no justificada" &&
                        !db.JustificacionesAusencia.Any(j =>
                            j.fk_id_asistencia ==
                                a.id_asistencia &&
                            j.estado == "Pendiente"));

                // Filtrar por estudiante.

                if (!string.IsNullOrWhiteSpace(id_estudiante))
                {
                    consulta = consulta.Where(a =>
                        a.fk_id_usuario == id_estudiante);
                }

                var ausencias = consulta
                    .OrderByDescending(a => a.fecha)
                    .ThenByDescending(a => a.id_asistencia)
                    .ToList();

                CargarNombresEstudiantes(
                    db,
                    idsEstudiantes.ToArray());

                CargarNombresMaterias(db);

                ViewBag.TotalAusencias =
                    ausencias.Count;

                ViewBag.EstudianteSeleccionado =
                    id_estudiante;

                return View(ausencias);
            }
        }

        // ========================================
        // ENVIAR JUSTIFICACIÓN DESDE NOTIFICACIÓN
        // RASI-10-004
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EnviarJustificacion(
            int id,
            string motivo)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index");
            }

            string idUsuario = ObtenerIdUsuario();

            motivo = motivo == null
                ? string.Empty
                : motivo.Trim();

            using (var db = new EduNexusDbContext())
            {
                try
                {
                    using (var transaccion =
                        db.Database.BeginTransaction(
                            IsolationLevel.Serializable))
                    {
                        var notificacion = db.Notificaciones
                            .FirstOrDefault(n =>
                                n.id_notificacion == id &&
                                n.fk_id_destinatario ==
                                    idUsuario &&
                                db.EncargadosEstudiantes.Any(e =>
                                    e.fk_id_encargado ==
                                        idUsuario &&
                                    e.fk_id_estudiante ==
                                        n.fk_id_estudiante &&
                                    e.estado));

                        if (notificacion == null)
                        {
                            return HttpNotFound();
                        }

                        var asistencia = db.Asistencias
                            .FirstOrDefault(a =>
                                a.id_asistencia ==
                                    notificacion.fk_id_asistencia &&
                                a.fk_id_usuario ==
                                    notificacion.fk_id_estudiante);

                        if (asistencia == null)
                        {
                            return HttpNotFound();
                        }

                        string error = ValidarJustificacion(
                            db,
                            asistencia,
                            idUsuario,
                            motivo);

                        if (!string.IsNullOrEmpty(error))
                        {
                            TempData["MensajeError"] = error;

                            return RedirectToAction(
                                "Detalle",
                                new { id = id });
                        }

                        RegistrarJustificacion(
                            db,
                            asistencia,
                            idUsuario,
                            motivo);

                        // Marcar notificación como leída.

                        if (!notificacion.leida)
                        {
                            notificacion.leida = true;

                            notificacion.fecha_lectura =
                                DateTime.Now;
                        }

                        db.SaveChanges();

                        transaccion.Commit();
                    }

                    TempData["Mensaje"] =
                        "La justificación fue enviada " +
                        "correctamente y se encuentra " +
                        "pendiente de revisión por el docente.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(
                        "Error al enviar justificación: {0}",
                        ex);

                    TempData["MensajeError"] =
                        "No fue posible enviar la justificación. " +
                        "Intente nuevamente.";
                }

                return RedirectToAction(
                    "Detalle",
                    new { id = id });
            }
        }

        // ========================================
        // RJT-11-001
        // ENVIAR JUSTIFICACIÓN DIRECTAMENTE
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EnviarJustificacionDirecta(
            int id_asistencia,
            string motivo)
        {
            if (id_asistencia <= 0)
            {
                TempData["MensajeError"] =
                    "Debe seleccionar una ausencia válida.";

                return RedirectToAction(
                    "EnviarJustificacion");
            }

            string idUsuario = ObtenerIdUsuario();

            motivo = motivo == null
                ? string.Empty
                : motivo.Trim();

            using (var db = new EduNexusDbContext())
            {
                try
                {
                    using (var transaccion =
                        db.Database.BeginTransaction(
                            IsolationLevel.Serializable))
                    {
                        // Verificar que la ausencia corresponda
                        // a un estudiante asociado al encargado.

                        var asistencia = db.Asistencias
                            .FirstOrDefault(a =>
                                a.id_asistencia ==
                                    id_asistencia &&
                                db.EncargadosEstudiantes.Any(e =>
                                    e.fk_id_encargado ==
                                        idUsuario &&
                                    e.fk_id_estudiante ==
                                        a.fk_id_usuario &&
                                    e.estado));

                        if (asistencia == null)
                        {
                            return HttpNotFound();
                        }

                        string error = ValidarJustificacion(
                            db,
                            asistencia,
                            idUsuario,
                            motivo);

                        if (!string.IsNullOrEmpty(error))
                        {
                            TempData["MensajeError"] = error;

                            return RedirectToAction(
                                "EnviarJustificacion",
                                new
                                {
                                    id_estudiante =
                                        asistencia.fk_id_usuario
                                });
                        }

                        RegistrarJustificacion(
                            db,
                            asistencia,
                            idUsuario,
                            motivo);

                        // Marcar como leídas las notificaciones
                        // relacionadas con esta ausencia.

                        var notificaciones = db.Notificaciones
                            .Where(n =>
                                n.fk_id_asistencia ==
                                    id_asistencia &&
                                n.fk_id_destinatario ==
                                    idUsuario &&
                                n.fk_id_estudiante ==
                                    asistencia.fk_id_usuario &&
                                !n.leida)
                            .ToList();

                        DateTime ahora = DateTime.Now;

                        foreach (var notificacion
                            in notificaciones)
                        {
                            notificacion.leida = true;

                            notificacion.fecha_lectura =
                                ahora;
                        }

                        db.SaveChanges();

                        transaccion.Commit();
                    }

                    TempData["Mensaje"] =
                        "La justificación fue enviada " +
                        "correctamente. Puede consultar " +
                        "su estado en el historial.";

                    return RedirectToAction(
                        "HistorialJustificaciones");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(
                        "Error al enviar justificación directa: {0}",
                        ex);

                    TempData["MensajeError"] =
                        "No fue posible enviar la justificación. " +
                        "Intente nuevamente.";

                    return RedirectToAction(
                        "EnviarJustificacion");
                }
            }
        }

        // ========================================
        // RJT-11-001
        // HISTORIAL DE JUSTIFICACIONES
        // ========================================

        [HttpGet]
        public ActionResult HistorialJustificaciones(
            string estado = null,
            string id_estudiante = null)
        {
            using (var db = new EduNexusDbContext())
            {
                string idUsuario = ObtenerIdUsuario();

                var idsEstudiantes =
                    db.EncargadosEstudiantes
                        .Where(e =>
                            e.fk_id_encargado == idUsuario &&
                            e.estado)
                        .Select(e => e.fk_id_estudiante)
                        .Distinct()
                        .ToList();

                if (!string.IsNullOrWhiteSpace(id_estudiante) &&
                    !idsEstudiantes.Contains(id_estudiante))
                {
                    return new HttpStatusCodeResult(403);
                }

                var consulta = db.JustificacionesAusencia
                    .Where(j =>
                        j.fk_id_encargado == idUsuario &&
                        idsEstudiantes.Contains(
                            j.fk_id_estudiante));

                // Validar filtro por estado.

                string[] estadosPermitidos =
                {
                    "Pendiente",
                    "Aprobada",
                    "Rechazada"
                };

                if (!string.IsNullOrWhiteSpace(estado) &&
                    !estadosPermitidos.Contains(estado))
                {
                    estado = null;
                }

                // Filtrar por estudiante.

                if (!string.IsNullOrWhiteSpace(id_estudiante))
                {
                    consulta = consulta.Where(j =>
                        j.fk_id_estudiante == id_estudiante);
                }

                // Estadísticas antes del filtro de estado.

                ViewBag.TotalJustificaciones =
                    consulta.Count();

                ViewBag.TotalPendientes =
                    consulta.Count(j =>
                        j.estado == "Pendiente");

                ViewBag.TotalAprobadas =
                    consulta.Count(j =>
                        j.estado == "Aprobada");

                ViewBag.TotalRechazadas =
                    consulta.Count(j =>
                        j.estado == "Rechazada");

                // Aplicar filtro de estado.

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    consulta = consulta.Where(j =>
                        j.estado == estado);
                }

                var justificaciones = consulta
                    .OrderByDescending(j => j.fecha_envio)
                    .ThenByDescending(j => j.id_justificacion)
                    .ToList();

                CargarEstudiantes(
                    db,
                    idUsuario,
                    id_estudiante);

                CargarNombresEstudiantes(
                    db,
                    idsEstudiantes.ToArray());

                CargarNombresMaterias(db);

                // Consultar información de las ausencias.

                var idsAsistencias = justificaciones
                    .Select(j => j.fk_id_asistencia)
                    .Distinct()
                    .ToList();

                var asistencias = db.Asistencias
                    .Where(a =>
                        idsAsistencias.Contains(
                            a.id_asistencia) &&
                        idsEstudiantes.Contains(
                            a.fk_id_usuario))
                    .ToList()
                    .ToDictionary(a => a.id_asistencia);

                ViewBag.Asistencias = asistencias;

                ViewBag.EstadoSeleccionado = estado;

                ViewBag.EstudianteSeleccionado =
                    id_estudiante;

                ViewBag.Estados = new SelectList(
                    estadosPermitidos,
                    estado);

                return View(justificaciones);
            }
        }

        // ========================================
        // VALIDAR JUSTIFICACIÓN
        // ========================================

        private string ValidarJustificacion(
            EduNexusDbContext db,
            AsistenciaEntity asistencia,
            string idUsuario,
            string motivo)
        {
            if (asistencia == null)
            {
                return "No se encontró la ausencia.";
            }

            bool perteneceAlEncargado =
                db.EncargadosEstudiantes.Any(e =>
                    e.fk_id_encargado == idUsuario &&
                    e.fk_id_estudiante ==
                        asistencia.fk_id_usuario &&
                    e.estado);

            if (!perteneceAlEncargado)
            {
                return "No tiene autorización para " +
                    "justificar esta ausencia.";
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                return "Debe ingresar el motivo " +
                    "de la ausencia.";
            }

            if (motivo.Length > 500)
            {
                return "El motivo no puede superar " +
                    "los 500 caracteres.";
            }

            if (asistencia.estado !=
                "Ausencia no justificada")
            {
                return "El estado actual de asistencia " +
                    "no permite enviar una justificación.";
            }

            if (asistencia.fecha.Date >
                DateTime.Today)
            {
                return "No se pueden justificar " +
                    "ausencias de fechas futuras.";
            }

            bool existePendiente =
                db.JustificacionesAusencia.Any(j =>
                    j.fk_id_asistencia ==
                        asistencia.id_asistencia &&
                    j.estado == "Pendiente");

            if (existePendiente)
            {
                return "Ya existe una justificación " +
                    "pendiente de revisión para esta ausencia.";
            }

            return null;
        }

        // ========================================
        // REGISTRAR JUSTIFICACIÓN
        // ========================================

        private void RegistrarJustificacion(
            EduNexusDbContext db,
            AsistenciaEntity asistencia,
            string idUsuario,
            string motivo)
        {
            var justificacion =
                new JustificacionAusenciaEntity
                {
                    fk_id_asistencia =
                        asistencia.id_asistencia,

                    fk_id_estudiante =
                        asistencia.fk_id_usuario,

                    fk_id_encargado =
                        idUsuario,

                    motivo = motivo,

                    fecha_envio =
                        DateTime.Now,

                    estado = "Pendiente",

                    fk_id_docente = null,

                    fecha_revision = null,

                    observaciones = null
                };

            db.JustificacionesAusencia
                .Add(justificacion);
        }

        // ========================================
        // CARGAR ESTUDIANTES DEL ENCARGADO
        // ========================================

        private void CargarEstudiantes(
            EduNexusDbContext db,
            string idUsuario,
            string seleccionado = null)
        {
            var idsEstudiantes =
                db.EncargadosEstudiantes
                    .Where(e =>
                        e.fk_id_encargado == idUsuario &&
                        e.estado)
                    .Select(e => e.fk_id_estudiante)
                    .Distinct()
                    .ToList();

            var estudiantes = db.Usuarios
                .Where(u =>
                    idsEstudiantes.Contains(u.id_usuario))
                .ToList()
                .Select(u => new
                {
                    id = u.id_usuario,

                    nombre = (
                        u.nombre + " " +
                        u.apellido1 + " " +
                        u.apellido2
                    ).Trim()
                })
                .OrderBy(u => u.nombre)
                .ToList();

            ViewBag.Estudiantes = new SelectList(
                estudiantes,
                "id",
                "nombre",
                seleccionado);
        }

        // ========================================
        // CARGAR NOMBRES DE ESTUDIANTES
        // ========================================

        private void CargarNombresEstudiantes(
            EduNexusDbContext db,
            string[] idsEstudiantes)
        {
            var estudiantes = db.Usuarios
                .Where(u =>
                    idsEstudiantes.Contains(u.id_usuario))
                .ToList()
                .ToDictionary(
                    u => u.id_usuario,
                    u => (
                        u.nombre + " " +
                        u.apellido1 + " " +
                        u.apellido2
                    ).Trim());

            ViewBag.NombresEstudiantes =
                estudiantes;
        }

        // ========================================
        // CARGAR NOMBRES DE MATERIAS
        // ========================================

        private void CargarNombresMaterias(
            EduNexusDbContext db)
        {
            var materias = db.Materias
                .ToList()
                .ToDictionary(
                    m => m.id_materia,
                    m => m.nombre);

            ViewBag.NombresMaterias =
                materias;
        }

        // ========================================
        // MARCAR NOTIFICACIÓN COMO LEÍDA
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarComoLeida(int id)
        {
            using (var db = new EduNexusDbContext())
            {
                string idUsuario = ObtenerIdUsuario();

                var notificacion = db.Notificaciones
                    .FirstOrDefault(n =>
                        n.id_notificacion == id &&
                        n.fk_id_destinatario == idUsuario &&
                        db.EncargadosEstudiantes.Any(e =>
                            e.fk_id_encargado == idUsuario &&
                            e.fk_id_estudiante ==
                                n.fk_id_estudiante &&
                            e.estado));

                if (notificacion == null)
                {
                    return HttpNotFound();
                }

                if (!notificacion.leida)
                {
                    notificacion.leida = true;

                    notificacion.fecha_lectura =
                        DateTime.Now;

                    db.SaveChanges();
                }

                TempData["Mensaje"] =
                    "La notificación se marcó como leída.";

                return RedirectToAction("Index");
            }
        }

        // ========================================
        // MARCAR TODAS COMO LEÍDAS
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarTodasComoLeidas()
        {
            using (var db = new EduNexusDbContext())
            {
                string idUsuario = ObtenerIdUsuario();

                var notificaciones = db.Notificaciones
                    .Where(n =>
                        n.fk_id_destinatario == idUsuario &&
                        !n.leida &&
                        db.EncargadosEstudiantes.Any(e =>
                            e.fk_id_encargado == idUsuario &&
                            e.fk_id_estudiante ==
                                n.fk_id_estudiante &&
                            e.estado))
                    .ToList();

                DateTime ahora = DateTime.Now;

                foreach (var notificacion in notificaciones)
                {
                    notificacion.leida = true;

                    notificacion.fecha_lectura = ahora;
                }

                db.SaveChanges();

                TempData["Mensaje"] =
                    "Todas las notificaciones se marcaron " +
                    "como leídas.";

                return RedirectToAction("Index");
            }
        }

        // ========================================
        // OBTENER USUARIO DE LA SESIÓN
        // ========================================

        private string ObtenerIdUsuario()
        {
            var usuario = Session[
                AutorizarAdministradorAttribute.ClaveSesion
            ] as UsuarioDto;

            return usuario != null
                ? usuario.id_usuario
                : null;
        }
    }
}