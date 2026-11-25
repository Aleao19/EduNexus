using EduNexus.Abstracciones.ModelosParaUI.Asistencia;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Data;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RASI-10-004
    // Revisión de justificaciones de ausencia.

    public class JustificacionesController : Controller
    {
        // ========================================
        // MODELO PARA LA VISTA
        // ========================================

        public class JustificacionVista
        {
            public int id_justificacion { get; set; }

            public string estudiante { get; set; }

            public string encargado { get; set; }

            public string materia { get; set; }

            public string seccion { get; set; }

            public DateTime fecha_ausencia { get; set; }

            public DateTime fecha_envio { get; set; }

            public string motivo { get; set; }

            public string estado { get; set; }

            public string estado_asistencia { get; set; }

            public string observaciones { get; set; }

            public DateTime? fecha_revision { get; set; }
        }

        // ========================================
        // VALIDAR ACCESO DEL DOCENTE
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

            if (!RolesSistema.Es(
                usuario,
                RolesSistema.Docente))
            {
                TempData["MensajeError"] =
                    "Esta funcionalidad es exclusiva " +
                    "de los docentes.";

                filterContext.Result =
                    RedirectToAction("Index", "Home");

                return;
            }

            using (var db = new EduNexusDbContext())
            {
                string idDocente = usuario.id_usuario;

                bool activo = db.Usuarios.Any(u =>
                    u.id_usuario == idDocente &&
                    u.estado);

                if (!activo)
                {
                    filterContext.Result =
                        RedirectToAction("Login", "Home");

                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }

        // ========================================
        // LISTADO DE JUSTIFICACIONES
        // ========================================

        [HttpGet]
        public ActionResult Index(string estado)
        {
            using (var db = new EduNexusDbContext())
            {
                string idDocente = ObtenerIdDocente();

                var consulta =
                    from j in db.JustificacionesAusencia

                    join a in db.Asistencias
                        on j.fk_id_asistencia
                        equals a.id_asistencia

                    join estudiante in db.Usuarios
                        on j.fk_id_estudiante
                        equals estudiante.id_usuario

                    join encargado in db.Usuarios
                        on j.fk_id_encargado
                        equals encargado.id_usuario

                    join materia in db.Materias
                        on a.fk_id_materia
                        equals materia.id_materia

                    join seccion in db.Secciones
                        on a.fk_id_seccion
                        equals seccion.id_seccion

                    where
                        j.fk_id_estudiante ==
                            a.fk_id_usuario &&

                        db.SeccionesMaterias.Any(sm =>
                            sm.fk_id_profesor == idDocente &&
                            sm.fk_id_seccion ==
                                a.fk_id_seccion &&
                            sm.fk_id_materia ==
                                a.fk_id_materia)

                    select new JustificacionVista
                    {
                        id_justificacion =
                            j.id_justificacion,

                        estudiante =
                            estudiante.nombre + " " +
                            estudiante.apellido1 + " " +
                            (estudiante.apellido2 ?? ""),

                        encargado =
                            encargado.nombre + " " +
                            encargado.apellido1 + " " +
                            (encargado.apellido2 ?? ""),

                        materia = materia.nombre,

                        seccion = seccion.nombre,

                        fecha_ausencia = a.fecha,

                        fecha_envio = j.fecha_envio,

                        motivo = j.motivo,

                        estado = j.estado,

                        estado_asistencia = a.estado,

                        observaciones = j.observaciones,

                        fecha_revision = j.fecha_revision
                    };

                var solicitudes = consulta
                    .OrderByDescending(j =>
                        j.estado == "Pendiente")
                    .ThenByDescending(j => j.fecha_envio)
                    .ToList();

                ViewBag.Total = solicitudes.Count;

                ViewBag.Pendientes = solicitudes.Count(
                    j => j.estado == "Pendiente");

                ViewBag.Aprobadas = solicitudes.Count(
                    j => j.estado == "Aprobada");

                ViewBag.Rechazadas = solicitudes.Count(
                    j => j.estado == "Rechazada");

                // Filtrar por estado.

                if (estado == "Pendiente" ||
                    estado == "Aprobada" ||
                    estado == "Rechazada")
                {
                    solicitudes = solicitudes
                        .Where(j => j.estado == estado)
                        .ToList();
                }
                else
                {
                    estado = "Todas";
                }

                ViewBag.EstadoSeleccionado = estado;

                return View(solicitudes);
            }
        }

        // ========================================
        // APROBAR JUSTIFICACIÓN
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Aprobar(
            int id,
            string observaciones)
        {
            return ProcesarRevision(
                id,
                true,
                observaciones);
        }

        // ========================================
        // RECHAZAR JUSTIFICACIÓN
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Rechazar(
            int id,
            string observaciones)
        {
            return ProcesarRevision(
                id,
                false,
                observaciones);
        }

        // ========================================
        // PROCESAR REVISIÓN
        // ========================================

        private ActionResult ProcesarRevision(
            int id,
            bool aprobar,
            string observaciones)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index");
            }

            string idDocente = ObtenerIdDocente();

            observaciones =
                string.IsNullOrWhiteSpace(observaciones)
                ? null
                : observaciones.Trim();

            // Validar las observaciones.

            if (observaciones != null &&
                observaciones.Length > 500)
            {
                TempData["MensajeError"] =
                    "Las observaciones no pueden superar " +
                    "los 500 caracteres.";

                return RedirectToAction("Index");
            }

            // Para rechazar se requiere una explicación.

            if (!aprobar &&
                string.IsNullOrWhiteSpace(observaciones))
            {
                TempData["MensajeError"] =
                    "Debe indicar el motivo por el cual " +
                    "rechaza la justificación.";

                return RedirectToAction("Index");
            }

            using (var db = new EduNexusDbContext())
            {
                try
                {
                    using (var transaccion =
                        db.Database.BeginTransaction(
                            IsolationLevel.Serializable))
                    {
                        var justificacion =
                            db.JustificacionesAusencia
                                .FirstOrDefault(j =>
                                    j.id_justificacion == id);

                        if (justificacion == null)
                        {
                            return HttpNotFound();
                        }

                        var asistencia = db.Asistencias
                            .FirstOrDefault(a =>
                                a.id_asistencia ==
                                    justificacion.fk_id_asistencia &&
                                a.fk_id_usuario ==
                                    justificacion.fk_id_estudiante);

                        if (asistencia == null)
                        {
                            return HttpNotFound();
                        }

                        // Validar que la clase pertenezca
                        // al docente de la sesión.

                        bool claseAsignada =
                            db.SeccionesMaterias.Any(sm =>
                                sm.fk_id_profesor ==
                                    idDocente &&
                                sm.fk_id_seccion ==
                                    asistencia.fk_id_seccion &&
                                sm.fk_id_materia ==
                                    asistencia.fk_id_materia);

                        if (!claseAsignada)
                        {
                            return HttpNotFound();
                        }

                        // Evitar revisar dos veces
                        // la misma solicitud.

                        if (justificacion.estado != "Pendiente")
                        {
                            TempData["MensajeError"] =
                                "Esta solicitud ya fue revisada.";

                            return RedirectToAction("Index");
                        }

                        // Validar el estado de asistencia
                        // antes de aprobar la solicitud.

                        if (aprobar &&
                            asistencia.estado !=
                                "Ausencia no justificada")
                        {
                            TempData["MensajeError"] =
                                "El estado actual de asistencia " +
                                "no permite aprobar esta solicitud.";

                            return RedirectToAction("Index");
                        }

                        if (aprobar &&
                            (string.IsNullOrWhiteSpace(
                                justificacion.motivo) ||
                             justificacion.motivo.Length >
                                AsistenciaFormDto.MaximoMotivo))
                        {
                            TempData["MensajeError"] =
                                "El motivo de la justificación " +
                                "no es válido.";

                            return RedirectToAction("Index");
                        }

                        DateTime ahora = DateTime.Now;

                        // Guardar información anterior
                        // para la bitácora.

                        var antes = new
                        {
                            id_justificacion =
                                justificacion.id_justificacion,

                            estado_solicitud =
                                justificacion.estado,

                            estado_asistencia =
                                asistencia.estado,

                            motivo_justificacion =
                                asistencia.motivo_justificacion
                        };

                        // Actualizar solicitud.

                        justificacion.estado =
                            aprobar
                            ? "Aprobada"
                            : "Rechazada";

                        justificacion.fk_id_docente =
                            idDocente;

                        justificacion.fecha_revision =
                            ahora;

                        justificacion.observaciones =
                            observaciones;

                        // ========================================
                        // ACTUALIZAR ASISTENCIA
                        // ========================================

                        if (aprobar)
                        {
                            asistencia.estado =
                                AsistenciaFormDto
                                    .EstadoJustificado;

                            asistencia.motivo_justificacion =
                                justificacion.motivo;

                            asistencia.fk_id_justificado_por =
                                idDocente;

                            asistencia.fecha_justificacion =
                                ahora;
                        }

                        // ========================================
                        // REGISTRAR EN BITÁCORA
                        // ========================================

                        Auditoria.Registrar(
                            db,

                            aprobar
                                ? "Aprobar justificación"
                                : "Rechazar justificación",

                            (aprobar
                                ? "Se aprobó"
                                : "Se rechazó")
                            + " la solicitud de justificación "
                            + justificacion.id_justificacion
                            + " correspondiente a la ausencia "
                            + "del "
                            + asistencia.fecha.ToString(
                                "dd/MM/yyyy")
                            + ".",

                            antes,

                            new
                            {
                                id_justificacion =
                                    justificacion.id_justificacion,

                                estado_solicitud =
                                    justificacion.estado,

                                estado_asistencia =
                                    asistencia.estado,

                                motivo_justificacion =
                                    asistencia.motivo_justificacion,

                                docente = idDocente,

                                observaciones =
                                    observaciones,

                                fecha_revision =
                                    ahora.ToString(
                                        "yyyy-MM-dd HH:mm:ss")
                            });

                        // Guardar todos los cambios.

                        db.SaveChanges();

                        transaccion.Commit();
                    }

                    TempData["Mensaje"] =
                        aprobar
                        ? "La justificación fue aprobada. " +
                          "La asistencia se actualizó correctamente."
                        : "La justificación fue rechazada.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(
                        "Error al revisar justificación: {0}",
                        ex);

                    TempData["MensajeError"] =
                        "No fue posible procesar la solicitud. " +
                        "Intente nuevamente.";
                }

                return RedirectToAction("Index");
            }
        }

        // ========================================
        // OBTENER DOCENTE DE LA SESIÓN
        // ========================================

        private string ObtenerIdDocente()
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