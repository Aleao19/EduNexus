using EduNexus.Abstracciones.ModelosParaUI.Notas;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace EduNexus.UI.Controllers
{
    public class NotasController : Controller
    {
        private readonly EduNexusDbContext db =
            new EduNexusDbContext();

        // ========================================
        // PERIODOS Y TIPOS DE EVALUACION
        // ========================================

        private static readonly string[] Periodos =
        {
            "Primer período",
            "Segundo período",
            "Tercer período"
        };

        private static readonly string[] TiposEvaluacion =
        {
            "Examen",
            "Tarea",
            "Proyecto",
            "Trabajo cotidiano",
            "Trabajo en clase",
            "Exposición",
            "Práctica",
            "Otro"
        };

        // ========================================
        // MODELO INTERNO DE CLASES
        // ========================================

        private class ClaseDocente
        {
            public int id_grado { get; set; }
            public int id_seccion { get; set; }
            public int id_materia { get; set; }
            public int id_calendario { get; set; }
            public string nombre_grado { get; set; }
            public string nombre_seccion { get; set; }
            public string nombre_materia { get; set; }
            public string nombre_calendario { get; set; }
            public DateTime? inicio_periodo { get; set; }
            public DateTime? fin_periodo { get; set; }
        }

        // ========================================
        // VALIDACION DE PERMISOS
        // ========================================

        protected override void OnActionExecuting(
            ActionExecutingContext filterContext)
        {
            var usuario = UsuarioActual();

            if (usuario == null)
            {
                filterContext.Result =
                    RedirectToAction("Login", "Home");

                return;
            }

            if (!RolesSistema.Es(
                usuario, RolesSistema.Docente) ||
                string.IsNullOrWhiteSpace(usuario.id_usuario))
            {
                TempData["MensajeError"] =
                    "Esta funcionalidad es exclusiva de los docentes.";

                filterContext.Result =
                    RedirectToAction("Index", "Home");

                return;
            }

            base.OnActionExecuting(filterContext);
        }

        // ========================================
        // REGISTRAR NOTAS - GET
        // ========================================

        [HttpGet]
        public ActionResult Registrar(
            int? id_grado,
            int? id_seccion,
            int? id_materia,
            string periodo,
            string nombre_evaluacion,
            string tipo_evaluacion,
            DateTime? fecha_evaluacion)
        {
            var modelo = new RegistroNotasDto
            {
                id_grado = id_grado ?? 0,
                fk_id_seccion = id_seccion ?? 0,
                fk_id_materia = id_materia ?? 0,
                fk_id_docente = IdDocente(),
                periodo = periodo,
                nombre_evaluacion = nombre_evaluacion,
                tipo_evaluacion = tipo_evaluacion,
                fecha_evaluacion =
                    fecha_evaluacion ?? DateTime.Today
            };

            try
            {
                List<ClaseDocente> clases =
                    ObtenerClases(true);

                ClaseDocente clase = clases
                    .FirstOrDefault(c =>
                        c.id_grado == modelo.id_grado &&
                        c.id_seccion == modelo.fk_id_seccion &&
                        c.id_materia == modelo.fk_id_materia);

                if (clase != null)
                {
                    AplicarDatosClase(modelo, clase);

                    modelo.Estudiantes =
                        ObtenerEstudiantes(clase.id_seccion);

                    if (!modelo.Estudiantes.Any())
                    {
                        ViewBag.MensajeInformativo =
                            "La sección no tiene estudiantes activos.";
                    }
                }
                else if (modelo.fk_id_seccion > 0 &&
                         modelo.fk_id_materia > 0)
                {
                    ModelState.AddModelError(
                        "",
                        "La clase seleccionada no está asignada al docente.");
                }

                CargarListas(modelo, clases);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error cargando registro de notas: " + ex);

                ModelState.AddModelError(
                    "",
                    "No se pudo cargar la información académica.");

                CargarListas(
                    modelo,
                    new List<ClaseDocente>());
            }

            return View(modelo);
        }

        // ========================================
        // REGISTRAR NOTAS - POST
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registrar(
            RegistroNotasDto modelo)
        {
            if (modelo == null)
            {
                return RedirectToAction("Registrar");
            }

            string idDocente = IdDocente();

            modelo.fk_id_docente = idDocente;

            modelo.nombre_evaluacion =
                LimpiarTexto(modelo.nombre_evaluacion);

            modelo.periodo =
                LimpiarTexto(modelo.periodo);

            modelo.tipo_evaluacion =
                LimpiarTexto(modelo.tipo_evaluacion);

            List<ClaseDocente> clases =
                new List<ClaseDocente>();

            ClaseDocente clase = null;

            try
            {
                clases = ObtenerClases(true);

                clase = clases.FirstOrDefault(c =>
                    c.id_grado == modelo.id_grado &&
                    c.id_seccion == modelo.fk_id_seccion &&
                    c.id_materia == modelo.fk_id_materia);

                // Validar asignacion del docente.

                if (clase == null)
                {
                    ModelState.AddModelError(
                        "",
                        "No tiene autorización para registrar notas " +
                        "en la clase seleccionada.");

                    modelo.Estudiantes =
                        new List<NotaEstudianteDto>();

                    return RetornarFormulario(modelo, clases);
                }

                AplicarDatosClase(modelo, clase);

                ModelState.Remove("fk_id_calendario");
                ModelState.Remove("fk_id_docente");

                ValidarEvaluacion(modelo, clase);

                if (!modelo.confirma_registro)
                {
                    ModelState.AddModelError(
                        "confirma_registro",
                        "Debe confirmar que las calificaciones son correctas.");
                }

                // Validar estudiantes.

                List<NotaEstudianteDto> estudiantesAutorizados =
                    ObtenerEstudiantes(clase.id_seccion);

                if (!estudiantesAutorizados.Any())
                {
                    ModelState.AddModelError(
                        "",
                        "La sección no tiene estudiantes activos.");
                }

                List<NotaEstudianteDto> enviados =
                    modelo.Estudiantes ??
                    new List<NotaEstudianteDto>();

                if (!enviados.Any())
                {
                    ModelState.AddModelError(
                        "",
                        "No se recibieron estudiantes para calificar.");
                }

                if (enviados.Any(e =>
                    e == null ||
                    string.IsNullOrWhiteSpace(
                        e.fk_id_estudiante)))
                {
                    ModelState.AddModelError(
                        "",
                        "Se recibieron estudiantes no válidos.");
                }

                var idsEnviados = enviados
                    .Where(e =>
                        e != null &&
                        !string.IsNullOrWhiteSpace(
                            e.fk_id_estudiante))
                    .Select(e => e.fk_id_estudiante)
                    .ToList();

                if (idsEnviados.Count !=
                    idsEnviados.Distinct().Count())
                {
                    ModelState.AddModelError(
                        "",
                        "Existen estudiantes duplicados en el formulario.");
                }

                var idsAutorizados = estudiantesAutorizados
                    .Select(e => e.fk_id_estudiante)
                    .ToList();

                if (idsEnviados.Any(id =>
                    !idsAutorizados.Contains(id)))
                {
                    ModelState.AddModelError(
                        "",
                        "Uno o más estudiantes no pertenecen " +
                        "a la sección seleccionada.");
                }

                // Validar calificaciones.

                var estudiantesCalificados = enviados
                    .Where(e =>
                        e != null &&
                        e.nota.HasValue)
                    .ToList();

                if (!estudiantesCalificados.Any())
                {
                    ModelState.AddModelError(
                        "",
                        "Debe ingresar al menos una calificación.");
                }

                for (int i = 0; i < enviados.Count; i++)
                {
                    NotaEstudianteDto estudiante = enviados[i];

                    if (estudiante == null)
                    {
                        continue;
                    }

                    estudiante.observaciones =
                        LimpiarTexto(estudiante.observaciones);

                    if (estudiante.nota.HasValue)
                    {
                        decimal nota = estudiante.nota.Value;

                        if (nota < 0 || nota > 100)
                        {
                            ModelState.AddModelError(
                                "Estudiantes[" + i + "].nota",
                                "La nota debe estar entre 0 y 100.");
                        }

                        if (decimal.Round(nota, 2) != nota)
                        {
                            ModelState.AddModelError(
                                "Estudiantes[" + i + "].nota",
                                "La nota admite un máximo de dos decimales.");
                        }
                    }

                    if (!string.IsNullOrEmpty(
                        estudiante.observaciones) &&
                        estudiante.observaciones.Length > 500)
                    {
                        ModelState.AddModelError(
                            "Estudiantes[" + i + "].observaciones",
                            "Las observaciones no pueden superar " +
                            "los 500 caracteres.");
                    }

                    if (!estudiante.nota.HasValue &&
                        !string.IsNullOrEmpty(
                            estudiante.observaciones))
                    {
                        ModelState.AddModelError(
                            "Estudiantes[" + i + "].nota",
                            "Debe ingresar una nota para guardar " +
                            "las observaciones.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    return RetornarFormulario(modelo, clases);
                }

                // Guardar calificaciones.

                using (var transaccion =
                    db.Database.BeginTransaction(
                        IsolationLevel.Serializable))
                {
                    ClaseDocente claseActual =
                        ObtenerClases(true)
                            .FirstOrDefault(c =>
                                c.id_grado == modelo.id_grado &&
                                c.id_seccion == modelo.fk_id_seccion &&
                                c.id_materia == modelo.fk_id_materia &&
                                c.id_calendario ==
                                    modelo.fk_id_calendario);

                    if (claseActual == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "La asignación académica ya no está disponible.");

                        return RetornarFormulario(modelo, clases);
                    }

                    var estudiantesActuales =
                        ObtenerEstudiantes(claseActual.id_seccion)
                            .Select(e => e.fk_id_estudiante)
                            .ToList();

                    if (estudiantesCalificados.Any(e =>
                        !estudiantesActuales.Contains(
                            e.fk_id_estudiante)))
                    {
                        ModelState.AddModelError(
                            "",
                            "La matrícula de la sección cambió. " +
                            "Recargue el formulario.");

                        return RetornarFormulario(modelo, clases);
                    }

                    DateTime fecha =
                        modelo.fecha_evaluacion.Value.Date;

                    DateTime fechaSiguiente =
                        fecha.AddDays(1);

                    string nombreNormalizado =
                        modelo.nombre_evaluacion.ToLowerInvariant();

                    var idsCalificados = estudiantesCalificados
                        .Select(e => e.fk_id_estudiante)
                        .ToList();

                    // Evitar calificaciones duplicadas.

                    List<NotaEntity> existentes = db.Notas
                        .Where(n =>
                            n.fk_id_seccion ==
                                modelo.fk_id_seccion &&

                            n.fk_id_materia ==
                                modelo.fk_id_materia &&

                            n.fk_id_calendario ==
                                modelo.fk_id_calendario &&

                            n.periodo == modelo.periodo &&

                            n.tipo_evaluacion ==
                                modelo.tipo_evaluacion &&

                            n.nombre_evaluacion.ToLower() ==
                                nombreNormalizado &&

                            n.fecha_evaluacion >= fecha &&

                            n.fecha_evaluacion < fechaSiguiente &&

                            idsCalificados.Contains(
                                n.fk_id_estudiante))
                        .ToList();

                    if (existentes.Any())
                    {
                        var idsDuplicados = existentes
                            .Select(n => n.fk_id_estudiante)
                            .Distinct()
                            .ToList();

                        var nombresDuplicados =
                            estudiantesAutorizados
                                .Where(e =>
                                    idsDuplicados.Contains(
                                        e.fk_id_estudiante))
                                .Select(e => e.nombre_estudiante)
                                .ToList();

                        ModelState.AddModelError(
                            "",
                            "Ya existen calificaciones para esta " +
                            "evaluación de los siguientes estudiantes: " +
                            string.Join(", ", nombresDuplicados) +
                            ". No se guardaron cambios.");

                        return RetornarFormulario(modelo, clases);
                    }

                    DateTime ahora = DateTime.Now;

                    foreach (NotaEstudianteDto estudiante
                        in estudiantesCalificados)
                    {
                        var entidad = new NotaEntity
                        {
                            fk_id_estudiante =
                                estudiante.fk_id_estudiante,

                            fk_id_seccion =
                                modelo.fk_id_seccion,

                            fk_id_materia =
                                modelo.fk_id_materia,

                            fk_id_docente =
                                idDocente,

                            fk_id_calendario =
                                modelo.fk_id_calendario,

                            periodo =
                                modelo.periodo,

                            nombre_evaluacion =
                                modelo.nombre_evaluacion,

                            tipo_evaluacion =
                                modelo.tipo_evaluacion,

                            fecha_evaluacion =
                                fecha,

                            nota =
                                estudiante.nota.Value,

                            observaciones =
                                estudiante.observaciones,

                            fecha_registro =
                                ahora,

                            fecha_modificacion =
                                null,

                            fk_id_modificado_por =
                                null,

                            estado =
                                "Registrada"
                        };

                        db.Notas.Add(entidad);
                    }

                    // Registro en bitacora.

                    Auditoria.Registrar(
                        db,
                        "Registrar notas",

                        "Se registraron " +
                        estudiantesCalificados.Count +
                        " calificaciones de " +
                        clase.nombre_materia +
                        " en " +
                        clase.nombre_seccion +
                        ", correspondientes a la evaluación " +
                        modelo.nombre_evaluacion + ".",

                        null,

                        new
                        {
                            seccion = modelo.fk_id_seccion,
                            materia = modelo.fk_id_materia,
                            calendario = modelo.fk_id_calendario,
                            docente = idDocente,
                            periodo = modelo.periodo,
                            evaluacion = modelo.nombre_evaluacion,
                            tipo = modelo.tipo_evaluacion,
                            fecha = fecha.ToString("yyyy-MM-dd"),

                            calificaciones =
                                estudiantesCalificados
                                    .Select(e => new
                                    {
                                        estudiante =
                                            e.fk_id_estudiante,

                                        nota = e.nota.Value
                                    })
                                    .ToList()
                        });

                    db.SaveChanges();

                    transaccion.Commit();
                }

                TempData["Mensaje"] =
                    "Se registraron correctamente " +
                    estudiantesCalificados.Count +
                    " calificaciones.";

                return RedirectToAction(
                    "Historial",
                    new
                    {
                        id_grado = modelo.id_grado,
                        id_seccion = modelo.fk_id_seccion,
                        id_materia = modelo.fk_id_materia,
                        periodo = modelo.periodo
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error registrando calificaciones: " + ex);

                ModelState.AddModelError(
                    "",
                    "No se pudieron guardar las calificaciones. " +
                    "Verifique la información e intente nuevamente.");

                return RetornarFormulario(modelo, clases);
            }
        }

        // ========================================
        // HISTORIAL DE CALIFICACIONES
        // ========================================

        [HttpGet]
        public ActionResult Historial(
            int? id_grado,
            int? id_seccion,
            int? id_materia,
            string periodo,
            string id_estudiante,
            string tipo_evaluacion,
            DateTime? fecha_desde,
            DateTime? fecha_hasta)
        {
            var resultado = new List<NotaDto>();

            var filtros = new RegistroNotasDto
            {
                id_grado = id_grado ?? 0,
                fk_id_seccion = id_seccion ?? 0,
                fk_id_materia = id_materia ?? 0,
                periodo = periodo,
                tipo_evaluacion = tipo_evaluacion
            };

            ViewBag.FechaDesde =
                fecha_desde.HasValue
                    ? fecha_desde.Value.ToString("yyyy-MM-dd")
                    : "";

            ViewBag.FechaHasta =
                fecha_hasta.HasValue
                    ? fecha_hasta.Value.ToString("yyyy-MM-dd")
                    : "";

            ViewBag.EstudianteSeleccionado = id_estudiante;
            ViewBag.GradoSeleccionado = id_grado;
            ViewBag.SeccionSeleccionada = id_seccion;
            ViewBag.MateriaSeleccionada = id_materia;
            ViewBag.PeriodoSeleccionado = periodo;
            ViewBag.TipoSeleccionado = tipo_evaluacion;

            ViewBag.TotalNotas = 0;
            ViewBag.TotalEstudiantes = 0;
            ViewBag.PromedioGeneral = 0m;
            ViewBag.NotaMayor = 0m;
            ViewBag.NotaMenor = 0m;

            ViewBag.Estudiantes = new SelectList(
                new List<NotaEstudianteDto>(),
                "fk_id_estudiante",
                "nombre_estudiante");

            try
            {
                List<ClaseDocente> clases =
                    ObtenerClases(false);

                CargarListas(filtros, clases);

                if (fecha_desde.HasValue &&
                    fecha_hasta.HasValue &&
                    fecha_desde.Value.Date >
                        fecha_hasta.Value.Date)
                {
                    ModelState.AddModelError(
                        "",
                        "La fecha inicial no puede ser posterior " +
                        "a la fecha final.");

                    return View(resultado);
                }

                string idDocente = IdDocente();

                var consulta = db.Notas
                    .Where(n =>
                        n.fk_id_docente == idDocente);

                if (id_grado.HasValue &&
                    id_grado.Value > 0)
                {
                    int grado = id_grado.Value;

                    var seccionesGrado = db.Secciones
                        .Where(s => s.fk_id_grado == grado)
                        .Select(s => s.id_seccion);

                    consulta = consulta.Where(n =>
                        seccionesGrado.Contains(
                            n.fk_id_seccion));
                }

                if (id_seccion.HasValue &&
                    id_seccion.Value > 0)
                {
                    consulta = consulta.Where(n =>
                        n.fk_id_seccion == id_seccion.Value);
                }

                if (id_materia.HasValue &&
                    id_materia.Value > 0)
                {
                    consulta = consulta.Where(n =>
                        n.fk_id_materia == id_materia.Value);
                }

                if (!string.IsNullOrWhiteSpace(periodo))
                {
                    consulta = consulta.Where(n =>
                        n.periodo == periodo);
                }

                if (!string.IsNullOrWhiteSpace(
                    tipo_evaluacion))
                {
                    consulta = consulta.Where(n =>
                        n.tipo_evaluacion == tipo_evaluacion);
                }

                if (fecha_desde.HasValue)
                {
                    DateTime desde = fecha_desde.Value.Date;

                    consulta = consulta.Where(n =>
                        n.fecha_evaluacion >= desde);
                }

                if (fecha_hasta.HasValue)
                {
                    DateTime hasta = fecha_hasta.Value.Date;

                    if (hasta < DateTime.MaxValue.Date)
                    {
                        DateTime limite = hasta.AddDays(1);

                        consulta = consulta.Where(n =>
                            n.fecha_evaluacion < limite);
                    }
                }

                // Cargar filtro de estudiantes.

                var idsEstudiantes = consulta
                    .Select(n => n.fk_id_estudiante)
                    .Distinct()
                    .ToList();

                var estudiantesFiltro = db.Usuarios
                    .Where(u =>
                        idsEstudiantes.Contains(u.id_usuario))
                    .ToList()
                    .Select(u => new NotaEstudianteDto
                    {
                        fk_id_estudiante = u.id_usuario,

                        nombre_estudiante =
                            HorarioServicio.NombreCompleto(u)
                    })
                    .OrderBy(u => u.nombre_estudiante)
                    .ToList();

                ViewBag.Estudiantes = new SelectList(
                    estudiantesFiltro,
                    "fk_id_estudiante",
                    "nombre_estudiante",
                    id_estudiante);

                if (!string.IsNullOrWhiteSpace(id_estudiante))
                {
                    consulta = consulta.Where(n =>
                        n.fk_id_estudiante == id_estudiante);
                }

                List<NotaEntity> notas = consulta
                    .OrderByDescending(n => n.fecha_evaluacion)
                    .ThenByDescending(n => n.id_nota)
                    .ToList();

                resultado = ConvertirNotas(notas);

                ViewBag.TotalNotas = resultado.Count;

                ViewBag.TotalEstudiantes = resultado
                    .Select(n => n.fk_id_estudiante)
                    .Distinct()
                    .Count();

                var calificaciones = resultado
                    .Where(n => n.nota.HasValue)
                    .Select(n => n.nota.Value)
                    .ToList();

                ViewBag.PromedioGeneral =
                    calificaciones.Any()
                        ? Math.Round(
                            calificaciones.Average(), 2)
                        : 0m;

                ViewBag.NotaMayor =
                    calificaciones.Any()
                        ? calificaciones.Max()
                        : 0m;

                ViewBag.NotaMenor =
                    calificaciones.Any()
                        ? calificaciones.Min()
                        : 0m;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error consultando historial de notas: " + ex);

                ModelState.AddModelError(
                    "",
                    "No se pudo consultar el historial de calificaciones.");

                CargarListas(
                    filtros,
                    new List<ClaseDocente>());
            }

            return View(resultado);
        }

        // ========================================
        // MODIFICAR NOTAS - GET
        // ========================================

        [HttpGet]
        public ActionResult Editar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            string idDocente = IdDocente();

            if (string.IsNullOrWhiteSpace(idDocente))
            {
                return new HttpStatusCodeResult(403);
            }

            try
            {
                NotaEntity entidad = db.Notas
                    .FirstOrDefault(n =>
                        n.id_nota == id.Value &&
                        n.fk_id_docente == idDocente);

                if (entidad == null)
                {
                    return HttpNotFound();
                }

                ModificarNotaDto modelo =
                    ConvertirNotaParaModificar(entidad);

                return View(modelo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error cargando modificación de nota: " + ex);

                TempData["MensajeError"] =
                    "No se pudo cargar la calificación.";

                return RedirectToAction("Historial");
            }
        }

        // ========================================
        // MODIFICAR NOTAS - POST
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(
            [Bind(Include =
                "id_nota,nota,observaciones," +
                "motivo_modificacion,version_registro," +
                "confirma_modificacion")]
            ModificarNotaDto modelo)
        {
            if (modelo == null || modelo.id_nota <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            string idDocente = IdDocente();

            if (string.IsNullOrWhiteSpace(idDocente))
            {
                return new HttpStatusCodeResult(403);
            }

            modelo.observaciones =
                LimpiarTexto(modelo.observaciones);

            modelo.motivo_modificacion =
                LimpiarTexto(modelo.motivo_modificacion);

            try
            {
                using (var transaccion =
                    db.Database.BeginTransaction(
                        IsolationLevel.Serializable))
                {
                    // Validar que la nota pertenezca al docente.

                    NotaEntity entidad = db.Notas
                        .FirstOrDefault(n =>
                            n.id_nota == modelo.id_nota &&
                            n.fk_id_docente == idDocente);

                    if (entidad == null)
                    {
                        return HttpNotFound();
                    }

                    ModificarNotaDto datosActuales =
                        ConvertirNotaParaModificar(entidad);

                    // Validar calificacion.

                    if (!modelo.nota.HasValue)
                    {
                        ModelState.AddModelError(
                            "nota",
                            "Debe ingresar la nueva calificación.");
                    }
                    else
                    {
                        decimal notaNueva = modelo.nota.Value;

                        if (notaNueva < 0 || notaNueva > 100)
                        {
                            ModelState.AddModelError(
                                "nota",
                                "La nota debe estar entre 0 y 100.");
                        }

                        if (decimal.Round(notaNueva, 2) != notaNueva)
                        {
                            ModelState.AddModelError(
                                "nota",
                                "La nota admite un máximo de dos decimales.");
                        }
                    }

                    // Validar observaciones.

                    if (!string.IsNullOrEmpty(
                        modelo.observaciones) &&
                        modelo.observaciones.Length > 500)
                    {
                        ModelState.AddModelError(
                            "observaciones",
                            "Las observaciones no pueden superar " +
                            "los 500 caracteres.");
                    }

                    // Validar motivo de modificacion.

                    if (string.IsNullOrWhiteSpace(
                        modelo.motivo_modificacion))
                    {
                        ModelState.AddModelError(
                            "motivo_modificacion",
                            "Debe indicar el motivo de la modificación.");
                    }
                    else if (
                        modelo.motivo_modificacion.Length < 5 ||
                        modelo.motivo_modificacion.Length > 500)
                    {
                        ModelState.AddModelError(
                            "motivo_modificacion",
                            "El motivo debe contener entre " +
                            "5 y 500 caracteres.");
                    }

                    // Validar confirmacion.

                    if (!modelo.confirma_modificacion)
                    {
                        ModelState.AddModelError(
                            "confirma_modificacion",
                            "Debe confirmar que los cambios son correctos.");
                    }

                    // Control de concurrencia.

                    if (string.IsNullOrWhiteSpace(
                        modelo.version_registro) ||
                        !string.Equals(
                            modelo.version_registro,
                            datosActuales.version_registro,
                            StringComparison.Ordinal))
                    {
                        TempData["MensajeError"] =
                            "La calificación fue modificada desde que " +
                            "abrió el formulario. Revise los datos actuales.";

                        return RedirectToAction(
                            "Editar",
                            new { id = entidad.id_nota });
                    }

                    // Validar que existan cambios.

                    if (modelo.nota.HasValue &&
                        modelo.nota.Value == entidad.nota &&
                        string.Equals(
                            LimpiarTexto(entidad.observaciones),
                            modelo.observaciones,
                            StringComparison.Ordinal))
                    {
                        ModelState.AddModelError(
                            "",
                            "No se realizaron cambios en la " +
                            "calificación ni en las observaciones.");
                    }

                    if (!ModelState.IsValid)
                    {
                        datosActuales.nota =
                            modelo.nota;

                        datosActuales.observaciones =
                            modelo.observaciones;

                        datosActuales.motivo_modificacion =
                            modelo.motivo_modificacion;

                        datosActuales.confirma_modificacion =
                            modelo.confirma_modificacion;

                        return View("Editar", datosActuales);
                    }

                    // Conservar datos anteriores.

                    var datosAnteriores = new
                    {
                        id_nota = entidad.id_nota,

                        estudiante =
                            entidad.fk_id_estudiante,

                        seccion =
                            entidad.fk_id_seccion,

                        materia =
                            entidad.fk_id_materia,

                        calendario =
                            entidad.fk_id_calendario,

                        periodo =
                            entidad.periodo,

                        evaluacion =
                            entidad.nombre_evaluacion,

                        nota =
                            entidad.nota,

                        observaciones =
                            entidad.observaciones,

                        fecha_modificacion =
                            entidad.fecha_modificacion,

                        modificado_por =
                            entidad.fk_id_modificado_por,

                        estado =
                            entidad.estado
                    };

                    // Actualizar calificacion.

                    entidad.nota =
                        modelo.nota.Value;

                    entidad.observaciones =
                        modelo.observaciones;

                    entidad.fecha_modificacion =
                        DateTime.Now;

                    entidad.fk_id_modificado_por =
                        idDocente;

                    entidad.estado =
                        "Modificada";

                    // Registrar cambios en bitacora.

                    Auditoria.Registrar(
                        db,

                        "Modificar nota",

                        "Se modificó la calificación del estudiante " +
                        datosActuales.nombre_estudiante +
                        ", correspondiente a la evaluación " +
                        entidad.nombre_evaluacion +
                        " de la materia " +
                        datosActuales.nombre_materia + ".",

                        datosAnteriores,

                        new
                        {
                            id_nota =
                                entidad.id_nota,

                            estudiante =
                                entidad.fk_id_estudiante,

                            seccion =
                                entidad.fk_id_seccion,

                            materia =
                                entidad.fk_id_materia,

                            calendario =
                                entidad.fk_id_calendario,

                            periodo =
                                entidad.periodo,

                            evaluacion =
                                entidad.nombre_evaluacion,

                            nota =
                                entidad.nota,

                            observaciones =
                                entidad.observaciones,

                            motivo_modificacion =
                                modelo.motivo_modificacion,

                            fecha_modificacion =
                                entidad.fecha_modificacion,

                            modificado_por =
                                entidad.fk_id_modificado_por,

                            estado =
                                entidad.estado
                        });

                    db.SaveChanges();

                    transaccion.Commit();

                    TempData["Mensaje"] =
                        "La calificación se modificó correctamente.";

                    return RedirectToAction(
                        "Historial",
                        new
                        {
                            id_grado =
                                datosActuales.id_grado,

                            id_seccion =
                                entidad.fk_id_seccion,

                            id_materia =
                                entidad.fk_id_materia,

                            periodo =
                                entidad.periodo,

                            id_estudiante =
                                entidad.fk_id_estudiante
                        });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error modificando calificación: " + ex);

                TempData["MensajeError"] =
                    "No se pudo confirmar la modificación. " +
                    "Revise la calificación actual e intente nuevamente.";

                return RedirectToAction(
                    "Editar",
                    new { id = modelo.id_nota });
            }
        }

        // ========================================
        // OBTENER SECCIONES POR GRADO
        // ========================================

        [HttpGet]
        public JsonResult ObtenerSecciones(int id_grado)
        {
            var secciones = ObtenerClases(true)
                .Where(c => c.id_grado == id_grado)
                .GroupBy(c => c.id_seccion)
                .Select(g => new
                {
                    id = g.Key,

                    nombre = g.First().nombre_seccion
                })
                .OrderBy(s => s.nombre)
                .ToList();

            return Json(
                secciones,
                JsonRequestBehavior.AllowGet);
        }

        // ========================================
        // OBTENER MATERIAS POR SECCION
        // ========================================

        [HttpGet]
        public JsonResult ObtenerMaterias(int id_seccion)
        {
            var materias = ObtenerClases(true)
                .Where(c => c.id_seccion == id_seccion)
                .GroupBy(c => c.id_materia)
                .Select(g => new
                {
                    id = g.Key,

                    nombre = g.First().nombre_materia,

                    id_calendario =
                        g.First().id_calendario,

                    nombre_calendario =
                        g.First().nombre_calendario
                })
                .OrderBy(m => m.nombre)
                .ToList();

            return Json(
                materias,
                JsonRequestBehavior.AllowGet);
        }

        // ========================================
        // OBTENER ESTUDIANTES POR CLASE
        // ========================================

        [HttpGet]
        public ActionResult ObtenerEstudiantesClase(
            int id_seccion,
            int id_materia)
        {
            ClaseDocente clase = ObtenerClases(true)
                .FirstOrDefault(c =>
                    c.id_seccion == id_seccion &&
                    c.id_materia == id_materia);

            if (clase == null)
            {
                return new HttpStatusCodeResult(
                    403,
                    "No tiene acceso a esta clase.");
            }

            var estudiantes =
                ObtenerEstudiantes(id_seccion)
                    .Select(e => new
                    {
                        id = e.fk_id_estudiante,

                        identificacion =
                            e.identificacion_estudiante,

                        nombre =
                            e.nombre_estudiante
                    })
                    .ToList();

            return Json(
                estudiantes,
                JsonRequestBehavior.AllowGet);
        }

        // ========================================
        // OBTENER CLASES ASIGNADAS AL DOCENTE
        // ========================================

        private List<ClaseDocente> ObtenerClases(
            bool soloVigentes)
        {
            string idDocente = IdDocente();

            DateTime hoy = DateTime.Today;

            var consulta =
                from sm in db.SeccionesMaterias

                join s in db.Secciones
                    on sm.fk_id_seccion equals s.id_seccion

                join g in db.Grados
                    on s.fk_id_grado equals g.id_grado

                join m in db.Materias
                    on sm.fk_id_materia equals m.id_materia

                join c in db.Calendarios
                    on s.fk_id_calendario equals c.id_calendario

                where sm.fk_id_profesor == idDocente

                select new
                {
                    id_grado = g.id_grado,

                    nombre_grado = g.nombre,

                    id_seccion = s.id_seccion,

                    nombre_seccion = s.nombre,

                    id_materia = m.id_materia,

                    nombre_materia = m.nombre,

                    id_calendario = c.id_calendario,

                    inicio = c.fecha_inicio,

                    fin = c.fecha_fin
                };

            if (soloVigentes)
            {
                consulta = consulta.Where(c =>
                    c.inicio <= hoy &&
                    c.fin >= hoy);
            }

            return consulta
                .ToList()
                .Select(c => new ClaseDocente
                {
                    id_grado = c.id_grado,

                    nombre_grado = c.nombre_grado,

                    id_seccion = c.id_seccion,

                    nombre_seccion = c.nombre_seccion,

                    id_materia = c.id_materia,

                    nombre_materia = c.nombre_materia,

                    id_calendario = c.id_calendario,

                    inicio_periodo = c.inicio,

                    fin_periodo = c.fin,

                    nombre_calendario =
                        EtiquetaCalendario(c.inicio, c.fin)
                })
                .OrderBy(c => c.nombre_grado)
                .ThenBy(c => c.nombre_seccion)
                .ThenBy(c => c.nombre_materia)
                .ToList();
        }

        // ========================================
        // OBTENER ESTUDIANTES DE LA SECCION
        // ========================================

        private List<NotaEstudianteDto> ObtenerEstudiantes(
            int idSeccion)
        {
            var idsEstudiantes = db.SeccionesEstudiantes
                .Where(se =>
                    se.fk_id_seccion == idSeccion)
                .Select(se => se.fk_id_usuario)
                .Distinct()
                .ToList();

            return db.Usuarios
                .Where(u =>
                    idsEstudiantes.Contains(u.id_usuario) &&
                    u.estado)
                .ToList()
                .OrderBy(u => u.apellido1)
                .ThenBy(u => u.apellido2)
                .ThenBy(u => u.nombre)
                .Select(u => new NotaEstudianteDto
                {
                    fk_id_estudiante = u.id_usuario,

                    identificacion_estudiante =
                        u.identificacion,

                    nombre_estudiante =
                        HorarioServicio.NombreCompleto(u),

                    nota = null,

                    observaciones = null
                })
                .ToList();
        }

        // ========================================
        // APLICAR DATOS DE LA CLASE
        // ========================================

        private void AplicarDatosClase(
            RegistroNotasDto modelo,
            ClaseDocente clase)
        {
            modelo.id_grado = clase.id_grado;

            modelo.nombre_grado = clase.nombre_grado;

            modelo.fk_id_seccion = clase.id_seccion;

            modelo.nombre_seccion = clase.nombre_seccion;

            modelo.fk_id_materia = clase.id_materia;

            modelo.nombre_materia = clase.nombre_materia;

            modelo.fk_id_calendario = clase.id_calendario;

            modelo.nombre_calendario =
                clase.nombre_calendario;

            modelo.fk_id_docente = IdDocente();

            string idDocente = IdDocente();

            UsuarioEntity docente = db.Usuarios
                .FirstOrDefault(u =>
                    u.id_usuario == idDocente);

            modelo.nombre_docente =
                docente != null
                    ? HorarioServicio.NombreCompleto(docente)
                    : "";
        }

        // ========================================
        // VALIDAR EVALUACION
        // ========================================

        private void ValidarEvaluacion(
            RegistroNotasDto modelo,
            ClaseDocente clase)
        {
            if (!Periodos.Contains(modelo.periodo))
            {
                ModelState.AddModelError(
                    "periodo",
                    "Seleccione un período académico válido.");
            }

            if (!TiposEvaluacion.Contains(
                modelo.tipo_evaluacion))
            {
                ModelState.AddModelError(
                    "tipo_evaluacion",
                    "Seleccione un tipo de evaluación válido.");
            }

            if (string.IsNullOrWhiteSpace(
                modelo.nombre_evaluacion))
            {
                ModelState.AddModelError(
                    "nombre_evaluacion",
                    "Debe ingresar el nombre de la evaluación.");
            }
            else if (modelo.nombre_evaluacion.Length > 150)
            {
                ModelState.AddModelError(
                    "nombre_evaluacion",
                    "El nombre no puede superar los 150 caracteres.");
            }

            if (!modelo.fecha_evaluacion.HasValue)
            {
                ModelState.AddModelError(
                    "fecha_evaluacion",
                    "Debe indicar la fecha de evaluación.");

                return;
            }

            DateTime fecha =
                modelo.fecha_evaluacion.Value.Date;

            if (fecha > DateTime.Today)
            {
                ModelState.AddModelError(
                    "fecha_evaluacion",
                    "No se pueden registrar notas de evaluaciones futuras.");
            }

            if (clase.inicio_periodo.HasValue &&
                fecha < clase.inicio_periodo.Value.Date)
            {
                ModelState.AddModelError(
                    "fecha_evaluacion",
                    "La evaluación es anterior al inicio " +
                    "del calendario académico.");
            }

            if (clase.fin_periodo.HasValue &&
                fecha > clase.fin_periodo.Value.Date)
            {
                ModelState.AddModelError(
                    "fecha_evaluacion",
                    "La evaluación es posterior al cierre " +
                    "del calendario académico.");
            }
        }

        // ========================================
        // CARGAR LISTAS DESPLEGABLES
        // ========================================

        private void CargarListas(
            RegistroNotasDto modelo,
            List<ClaseDocente> clases)
        {
            var grados = clases
                .GroupBy(c => c.id_grado)
                .Select(g => new
                {
                    id = g.Key,

                    nombre = g.First().nombre_grado
                })
                .OrderBy(g => g.nombre)
                .ToList();

            var secciones = clases
                .Where(c =>
                    modelo.id_grado == 0 ||
                    c.id_grado == modelo.id_grado)
                .GroupBy(c => c.id_seccion)
                .Select(g => new
                {
                    id = g.Key,

                    nombre = g.First().nombre_seccion
                })
                .OrderBy(s => s.nombre)
                .ToList();

            var materias = clases
                .Where(c =>
                    (modelo.id_grado == 0 ||
                     c.id_grado == modelo.id_grado) &&

                    (modelo.fk_id_seccion == 0 ||
                     c.id_seccion == modelo.fk_id_seccion))
                .GroupBy(c => c.id_materia)
                .Select(g => new
                {
                    id = g.Key,

                    nombre = g.First().nombre_materia
                })
                .OrderBy(m => m.nombre)
                .ToList();

            var calendarios = clases
                .GroupBy(c => c.id_calendario)
                .Select(g => new
                {
                    id = g.Key,

                    nombre = g.First().nombre_calendario
                })
                .OrderBy(c => c.nombre)
                .ToList();

            ViewBag.Grados = new SelectList(
                grados,
                "id",
                "nombre",
                modelo.id_grado);

            ViewBag.Secciones = new SelectList(
                secciones,
                "id",
                "nombre",
                modelo.fk_id_seccion);

            ViewBag.Materias = new SelectList(
                materias,
                "id",
                "nombre",
                modelo.fk_id_materia);

            ViewBag.Calendarios = new SelectList(
                calendarios,
                "id",
                "nombre",
                modelo.fk_id_calendario);

            ViewBag.Periodos = new SelectList(
                Periodos,
                modelo.periodo);

            ViewBag.TiposEvaluacion = new SelectList(
                TiposEvaluacion,
                modelo.tipo_evaluacion);

            ViewBag.SinClases = !clases.Any();
        }

        // ========================================
        // RETORNAR FORMULARIO CON ERRORES
        // ========================================

        private ActionResult RetornarFormulario(
            RegistroNotasDto modelo,
            List<ClaseDocente> clases)
        {
            ClaseDocente clase = clases
                .FirstOrDefault(c =>
                    c.id_grado == modelo.id_grado &&
                    c.id_seccion == modelo.fk_id_seccion &&
                    c.id_materia == modelo.fk_id_materia);

            try
            {
                if (clase != null)
                {
                    AplicarDatosClase(modelo, clase);

                    var enviados =
                        (modelo.Estudiantes ??
                         new List<NotaEstudianteDto>())
                        .Where(e =>
                            e != null &&
                            !string.IsNullOrWhiteSpace(
                                e.fk_id_estudiante))
                        .GroupBy(e => e.fk_id_estudiante)
                        .ToDictionary(
                            g => g.Key,
                            g => g.First());

                    var estudiantes =
                        ObtenerEstudiantes(clase.id_seccion);

                    foreach (NotaEstudianteDto estudiante
                        in estudiantes)
                    {
                        NotaEstudianteDto enviado;

                        if (enviados.TryGetValue(
                            estudiante.fk_id_estudiante,
                            out enviado))
                        {
                            estudiante.nota = enviado.nota;

                            estudiante.observaciones =
                                enviado.observaciones;
                        }
                    }

                    var errores = ModelState
                        .Where(x =>
                            x.Key.StartsWith("Estudiantes["))
                        .SelectMany(x => x.Value.Errors)
                        .Select(e =>
                            string.IsNullOrWhiteSpace(
                                e.ErrorMessage)
                                ? "Revise las calificaciones ingresadas."
                                : e.ErrorMessage)
                        .Distinct()
                        .ToList();

                    var claves = ModelState.Keys
                        .Where(k =>
                            k.StartsWith("Estudiantes["))
                        .ToList();

                    foreach (string clave in claves)
                    {
                        ModelState.Remove(clave);
                    }

                    foreach (string error in errores)
                    {
                        ModelState.AddModelError("", error);
                    }

                    modelo.Estudiantes = estudiantes;

                    ModelState.Remove("fk_id_calendario");
                }
                else
                {
                    modelo.Estudiantes =
                        new List<NotaEstudianteDto>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error reconstruyendo formulario de notas: " + ex);

                modelo.Estudiantes =
                    new List<NotaEstudianteDto>();

                ModelState.AddModelError(
                    "",
                    "No se pudo recargar la lista de estudiantes.");
            }

            CargarListas(modelo, clases);

            return View("Registrar", modelo);
        }

        // ========================================
        // CONVERTIR NOTAS A DTO
        // ========================================

        private List<NotaDto> ConvertirNotas(
            List<NotaEntity> notas)
        {
            if (!notas.Any())
            {
                return new List<NotaDto>();
            }

            var idsUsuarios = notas
                .Select(n => n.fk_id_estudiante)
                .Union(notas.Select(n => n.fk_id_docente))
                .Union(notas.Select(n => n.fk_id_modificado_por))
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var usuarios = db.Usuarios
                .Where(u =>
                    idsUsuarios.Contains(u.id_usuario))
                .ToList()
                .ToDictionary(u => u.id_usuario);

            var idsSecciones = notas
                .Select(n => n.fk_id_seccion)
                .Distinct()
                .ToList();

            var secciones = db.Secciones
                .Where(s =>
                    idsSecciones.Contains(s.id_seccion))
                .ToList()
                .ToDictionary(s => s.id_seccion);

            var idsGrados = secciones.Values
                .Select(s => s.fk_id_grado)
                .Distinct()
                .ToList();

            var grados = db.Grados
                .Where(g =>
                    idsGrados.Contains(g.id_grado))
                .ToList()
                .ToDictionary(g => g.id_grado);

            var idsMaterias = notas
                .Select(n => n.fk_id_materia)
                .Distinct()
                .ToList();

            var materias = db.Materias
                .Where(m =>
                    idsMaterias.Contains(m.id_materia))
                .ToList()
                .ToDictionary(m => m.id_materia);

            var idsCalendarios = notas
                .Select(n => n.fk_id_calendario)
                .Distinct()
                .ToList();

            var calendarios = db.Calendarios
                .Where(c =>
                    idsCalendarios.Contains(c.id_calendario))
                .ToList()
                .ToDictionary(c => c.id_calendario);

            var resultado = new List<NotaDto>();

            foreach (NotaEntity nota in notas)
            {
                UsuarioEntity estudiante = null;

                UsuarioEntity docente = null;

                SeccionEntity seccion;

                MateriaEntity materia;

                CalendarioEntity calendario;

                if (!string.IsNullOrEmpty(nota.fk_id_estudiante))
                {
                    usuarios.TryGetValue(
                        nota.fk_id_estudiante,
                        out estudiante);
                }

                if (!string.IsNullOrEmpty(nota.fk_id_docente))
                {
                    usuarios.TryGetValue(
                        nota.fk_id_docente,
                        out docente);
                }

                secciones.TryGetValue(
                    nota.fk_id_seccion,
                    out seccion);

                materias.TryGetValue(
                    nota.fk_id_materia,
                    out materia);

                calendarios.TryGetValue(
                    nota.fk_id_calendario,
                    out calendario);

                GradoEntity grado = null;

                if (seccion != null)
                {
                    grados.TryGetValue(
                        seccion.fk_id_grado,
                        out grado);
                }

                UsuarioEntity modificadoPor = null;

                if (!string.IsNullOrEmpty(
                    nota.fk_id_modificado_por))
                {
                    usuarios.TryGetValue(
                        nota.fk_id_modificado_por,
                        out modificadoPor);
                }

                resultado.Add(new NotaDto
                {
                    id_nota = nota.id_nota,

                    fk_id_estudiante =
                        nota.fk_id_estudiante,

                    identificacion_estudiante =
                        estudiante != null
                            ? estudiante.identificacion
                            : "",

                    nombre_estudiante =
                        estudiante != null
                            ? HorarioServicio.NombreCompleto(
                                estudiante)
                            : "Estudiante no disponible",

                    id_grado =
                        grado != null
                            ? grado.id_grado
                            : 0,

                    nombre_grado =
                        grado != null
                            ? grado.nombre
                            : "Grado no disponible",

                    fk_id_seccion =
                        nota.fk_id_seccion,

                    nombre_seccion =
                        seccion != null
                            ? seccion.nombre
                            : "Sección no disponible",

                    fk_id_materia =
                        nota.fk_id_materia,

                    nombre_materia =
                        materia != null
                            ? materia.nombre
                            : "Materia no disponible",

                    fk_id_docente =
                        nota.fk_id_docente,

                    nombre_docente =
                        docente != null
                            ? HorarioServicio.NombreCompleto(
                                docente)
                            : "Docente no disponible",

                    fk_id_calendario =
                        nota.fk_id_calendario,

                    nombre_calendario =
                        calendario != null
                            ? EtiquetaCalendario(
                                calendario.fecha_inicio,
                                calendario.fecha_fin)
                            : "Calendario no disponible",

                    periodo =
                        nota.periodo,

                    nombre_evaluacion =
                        nota.nombre_evaluacion,

                    tipo_evaluacion =
                        nota.tipo_evaluacion,

                    fecha_evaluacion =
                        nota.fecha_evaluacion,

                    nota =
                        nota.nota,

                    observaciones =
                        nota.observaciones,

                    fecha_registro =
                        nota.fecha_registro,

                    fecha_modificacion =
                        nota.fecha_modificacion,

                    fk_id_modificado_por =
                        nota.fk_id_modificado_por,

                    nombre_modificado_por =
                        modificadoPor != null
                            ? HorarioServicio.NombreCompleto(
                                modificadoPor)
                            : null,

                    estado =
                        nota.estado
                });
            }

            return resultado;
        }

        // ========================================
        // CONVERTIR NOTA A DTO DE MODIFICACION
        // ========================================

        private ModificarNotaDto ConvertirNotaParaModificar(
            NotaEntity entidad)
        {
            NotaDto nota = ConvertirNotas(
                new List<NotaEntity>
                {
                    entidad
                }).First();

            return new ModificarNotaDto
            {
                id_nota =
                    entidad.id_nota,

                fk_id_estudiante =
                    nota.fk_id_estudiante,

                identificacion_estudiante =
                    nota.identificacion_estudiante,

                nombre_estudiante =
                    nota.nombre_estudiante,

                id_grado =
                    nota.id_grado,

                nombre_grado =
                    nota.nombre_grado,

                fk_id_seccion =
                    nota.fk_id_seccion,

                nombre_seccion =
                    nota.nombre_seccion,

                fk_id_materia =
                    nota.fk_id_materia,

                nombre_materia =
                    nota.nombre_materia,

                fk_id_docente =
                    nota.fk_id_docente,

                nombre_docente =
                    nota.nombre_docente,

                fk_id_calendario =
                    nota.fk_id_calendario,

                nombre_calendario =
                    nota.nombre_calendario,

                periodo =
                    nota.periodo,

                nombre_evaluacion =
                    nota.nombre_evaluacion,

                tipo_evaluacion =
                    nota.tipo_evaluacion,

                fecha_evaluacion =
                    nota.fecha_evaluacion,

                nota_original =
                    entidad.nota,

                observaciones_originales =
                    entidad.observaciones,

                nota =
                    entidad.nota,

                observaciones =
                    entidad.observaciones,

                motivo_modificacion =
                    null,

                fecha_registro =
                    entidad.fecha_registro,

                fecha_modificacion =
                    entidad.fecha_modificacion,

                fk_id_modificado_por =
                    entidad.fk_id_modificado_por,

                nombre_modificado_por =
                    nota.nombre_modificado_por,

                estado =
                    entidad.estado,

                version_registro =
                    GenerarVersionNota(entidad),

                confirma_modificacion =
                    false
            };
        }

        // ========================================
        // GENERAR VERSION DE LA CALIFICACION
        // ========================================

        private static string GenerarVersionNota(
            NotaEntity entidad)
        {
            var cultura =
                CultureInfo.InvariantCulture;

            string observaciones =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        entidad.observaciones ?? ""));

            string contenido = string.Join("|", new[]
            {
                entidad.id_nota.ToString(cultura),

                entidad.nota.ToString("0.00", cultura),

                observaciones,

                entidad.fecha_registro.Ticks.ToString(cultura),

                entidad.fecha_modificacion.HasValue
                    ? entidad.fecha_modificacion.Value
                        .Ticks.ToString(cultura)
                    : "",

                entidad.fk_id_modificado_por ?? "",

                entidad.estado ?? ""
            });

            using (var sha256 = SHA256.Create())
            {
                byte[] datos =
                    Encoding.UTF8.GetBytes(contenido);

                byte[] hash =
                    sha256.ComputeHash(datos);

                return Convert.ToBase64String(hash);
            }
        }

        // ========================================
        // ETIQUETA DEL CALENDARIO
        // ========================================

        private static string EtiquetaCalendario(
            DateTime? inicio,
            DateTime? fin)
        {
            if (!inicio.HasValue || !fin.HasValue)
            {
                return "Calendario académico";
            }

            return inicio.Value.ToString("dd/MM/yyyy") +
                " - " +
                fin.Value.ToString("dd/MM/yyyy");
        }

        // ========================================
        // LIMPIAR TEXTOS
        // ========================================

        private static string LimpiarTexto(string texto)
        {
            return string.IsNullOrWhiteSpace(texto)
                ? null
                : texto.Trim();
        }

        // ========================================
        // OBTENER USUARIO DE LA SESION
        // ========================================

        private UsuarioDto UsuarioActual()
        {
            return Session[
                AutorizarAdministradorAttribute.ClaveSesion
            ] as UsuarioDto;
        }

        // ========================================
        // OBTENER IDENTIFICADOR DEL DOCENTE
        // ========================================

        private string IdDocente()
        {
            var usuario = UsuarioActual();

            return usuario != null
                ? usuario.id_usuario
                : null;
        }

        // ========================================
        // LIBERAR RECURSOS
        // ========================================

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}