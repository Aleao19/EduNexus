using EduNexus.Abstracciones.ModelosParaUI.Notas;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class ConsultaNotasController : Controller
    {
        private readonly EduNexusDbContext db =
            new EduNexusDbContext();

        // ========================================
        // PERÍODOS ACADÉMICOS
        // ========================================

        private static readonly string[] Periodos =
        {
            "Primer período",
            "Segundo período",
            "Tercer período"
        };

        // ========================================
        // TIPOS DE EVALUACIÓN
        // ========================================

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
        // VALIDACIÓN DE ACCESO
        // ========================================

        protected override void OnActionExecuting(
            ActionExecutingContext filterContext)
        {
            var usuario = UsuarioActual();

            if (usuario == null ||
                string.IsNullOrWhiteSpace(usuario.id_usuario))
            {
                filterContext.Result =
                    RedirectToAction("Login", "Home");

                return;
            }

            bool autorizado =
                RolesSistema.Es(
                    usuario, RolesSistema.Administrador) ||

                RolesSistema.Es(
                    usuario, RolesSistema.Docente) ||

                RolesSistema.Es(
                    usuario, RolesSistema.Estudiante) ||

                RolesSistema.Es(
                    usuario, RolesSistema.Padre);

            if (!autorizado)
            {
                TempData["MensajeError"] =
                    "No tiene permisos para consultar calificaciones.";

                filterContext.Result =
                    RedirectToAction("Index", "Home");

                return;
            }

            // Evitar almacenamiento de información académica
            // privada en la caché del navegador.

            Response.Cache.SetCacheability(
                HttpCacheability.NoCache);

            Response.Cache.SetNoStore();

            base.OnActionExecuting(filterContext);
        }

        // ========================================
        // VISUALIZAR NOTAS
        // ========================================

        [HttpGet]
        public ActionResult Index(
            string id_estudiante,
            int? id_grado,
            int? id_seccion,
            int? id_materia,
            int? id_calendario,
            string periodo,
            string tipo_evaluacion,
            DateTime? fecha_desde,
            DateTime? fecha_hasta)
        {
            var usuario = UsuarioActual();

            var modelo = new ConsultaNotasDto
            {
                id_usuario = usuario.id_usuario,

                nombre_usuario = (
                    usuario.nombre + " " +
                    usuario.apellido1 + " " +
                    usuario.apellido2
                ).Trim(),

                rol_usuario = usuario.nombreRol,

                id_estudiante = LimpiarTexto(
                    id_estudiante),

                id_grado = id_grado,

                id_seccion = id_seccion,

                id_materia = id_materia,

                id_calendario = id_calendario,

                periodo = LimpiarTexto(periodo),

                tipo_evaluacion = LimpiarTexto(
                    tipo_evaluacion),

                fecha_desde = fecha_desde,

                fecha_hasta = fecha_hasta,

                es_administrador = RolesSistema.Es(
                    usuario, RolesSistema.Administrador),

                es_docente = RolesSistema.Es(
                    usuario, RolesSistema.Docente),

                es_estudiante = RolesSistema.Es(
                    usuario, RolesSistema.Estudiante),

                es_encargado = RolesSistema.Es(
                    usuario, RolesSistema.Padre)
            };

            modelo.puede_seleccionar_estudiante =
                !modelo.es_estudiante &&
                (
                    modelo.es_administrador ||
                    modelo.es_docente ||
                    modelo.es_encargado
                );

            // El estudiante solamente puede consultar
            // su propia información.

            if (modelo.es_estudiante)
            {
                modelo.id_estudiante =
                    usuario.id_usuario;
            }

            ViewBag.Resumenes =
                new List<ResumenNotasDto>();

            CargarListas(
                modelo,
                new List<NotaDto>(),
                new List<UsuarioEntity>());

            try
            {
                // ========================================
                // OBTENER NOTAS AUTORIZADAS
                // ========================================

                IQueryable<NotaEntity> consulta =
                    ObtenerConsultaAutorizada();

                List<UsuarioEntity> estudiantes =
                    ObtenerEstudiantesAutorizados();

                // ========================================
                // VALIDAR ESTUDIANTE SELECCIONADO
                // ========================================

                if (!string.IsNullOrWhiteSpace(
                    modelo.id_estudiante))
                {
                    UsuarioEntity estudiante =
                        estudiantes.FirstOrDefault(e =>
                            e.id_usuario ==
                            modelo.id_estudiante);

                    if (estudiante == null)
                    {
                        return new HttpStatusCodeResult(
                            403,
                            "No tiene acceso a este estudiante.");
                    }

                    modelo.nombre_estudiante =
                        HorarioServicio.NombreCompleto(
                            estudiante);

                    modelo.identificacion_estudiante =
                        estudiante.identificacion;
                }

                // ========================================
                // CARGAR FILTROS AUTORIZADOS
                // ========================================

                var notasDisponibles = ConvertirNotas(
                    consulta.ToList());

                CargarListas(
                    modelo,
                    notasDisponibles,
                    estudiantes);

                // ========================================
                // VALIDAR FILTROS
                // ========================================

                ValidarFiltros(modelo);

                if (!ModelState.IsValid)
                {
                    return View(modelo);
                }

                // ========================================
                // FILTRAR POR ESTUDIANTE
                // ========================================

                if (!string.IsNullOrWhiteSpace(
                    modelo.id_estudiante))
                {
                    consulta = consulta.Where(n =>
                        n.fk_id_estudiante ==
                        modelo.id_estudiante);
                }

                // ========================================
                // FILTRAR POR GRADO
                // ========================================

                if (modelo.id_grado.HasValue &&
                    modelo.id_grado.Value > 0)
                {
                    int grado = modelo.id_grado.Value;

                    var seccionesGrado = db.Secciones
                        .Where(s =>
                            s.fk_id_grado == grado)
                        .Select(s => s.id_seccion);

                    consulta = consulta.Where(n =>
                        seccionesGrado.Contains(
                            n.fk_id_seccion));
                }

                // ========================================
                // FILTRAR POR SECCIÓN
                // ========================================

                if (modelo.id_seccion.HasValue &&
                    modelo.id_seccion.Value > 0)
                {
                    int seccion =
                        modelo.id_seccion.Value;

                    consulta = consulta.Where(n =>
                        n.fk_id_seccion == seccion);
                }

                // ========================================
                // FILTRAR POR MATERIA
                // ========================================

                if (modelo.id_materia.HasValue &&
                    modelo.id_materia.Value > 0)
                {
                    int materia =
                        modelo.id_materia.Value;

                    consulta = consulta.Where(n =>
                        n.fk_id_materia == materia);
                }

                // ========================================
                // FILTRAR POR CALENDARIO
                // ========================================

                if (modelo.id_calendario.HasValue &&
                    modelo.id_calendario.Value > 0)
                {
                    int calendario =
                        modelo.id_calendario.Value;

                    consulta = consulta.Where(n =>
                        n.fk_id_calendario ==
                        calendario);
                }

                // ========================================
                // FILTRAR POR PERÍODO
                // ========================================

                if (!string.IsNullOrWhiteSpace(
                    modelo.periodo))
                {
                    consulta = consulta.Where(n =>
                        n.periodo == modelo.periodo);
                }

                // ========================================
                // FILTRAR POR TIPO DE EVALUACIÓN
                // ========================================

                if (!string.IsNullOrWhiteSpace(
                    modelo.tipo_evaluacion))
                {
                    consulta = consulta.Where(n =>
                        n.tipo_evaluacion ==
                        modelo.tipo_evaluacion);
                }

                // ========================================
                // FILTRAR POR FECHA INICIAL
                // ========================================

                if (modelo.fecha_desde.HasValue)
                {
                    DateTime desde =
                        modelo.fecha_desde.Value.Date;

                    consulta = consulta.Where(n =>
                        n.fecha_evaluacion >= desde);
                }

                // ========================================
                // FILTRAR POR FECHA FINAL
                // ========================================

                if (modelo.fecha_hasta.HasValue)
                {
                    DateTime hasta =
                        modelo.fecha_hasta.Value.Date;

                    if (hasta < DateTime.MaxValue.Date)
                    {
                        DateTime limite =
                            hasta.AddDays(1);

                        consulta = consulta.Where(n =>
                            n.fecha_evaluacion < limite);
                    }
                }

                // ========================================
                // OBTENER RESULTADOS
                // ========================================

                List<NotaEntity> notas = consulta
                    .OrderByDescending(n =>
                        n.fecha_evaluacion)
                    .ThenByDescending(n =>
                        n.id_nota)
                    .ToList();

                modelo.Notas =
                    ConvertirNotas(notas);

                // ========================================
                // GENERAR RESUMEN POR MATERIA
                // ========================================

                ViewBag.Resumenes =
                    GenerarResumenes(modelo.Notas);

                if (!modelo.Notas.Any())
                {
                    ViewBag.MensajeInformativo =
                        "No se encontraron calificaciones " +
                        "para los filtros seleccionados.";
                }

                if (modelo.es_encargado &&
                    !estudiantes.Any())
                {
                    ViewBag.MensajeInformativo =
                        "No tiene estudiantes asociados " +
                        "actualmente.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error consultando calificaciones: " +
                    ex);

                modelo.Notas =
                    new List<NotaDto>();

                ViewBag.Resumenes =
                    new List<ResumenNotasDto>();

                CargarListas(
                    modelo,
                    new List<NotaDto>(),
                    new List<UsuarioEntity>());

                ModelState.AddModelError(
                    "",
                    "No se pudieron consultar las " +
                    "calificaciones. Intente nuevamente.");
            }

            return View(modelo);
        }

        // ========================================
        // DETALLE DE CALIFICACIÓN
        // ========================================

        [HttpGet]
        public ActionResult Detalle(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return new HttpStatusCodeResult(
                    400,
                    "Identificador de calificación inválido.");
            }

            try
            {
                // Se aplican nuevamente los permisos.
                // No se consulta directamente por ID
                // sin comprobar la autorización.

                NotaEntity nota =
                    ObtenerConsultaAutorizada()
                        .FirstOrDefault(n =>
                            n.id_nota == id.Value);

                if (nota == null)
                {
                    return HttpNotFound(
                        "La calificación no está disponible.");
                }

                NotaDto modelo = ConvertirNotas(
                    new List<NotaEntity>
                    {
                        nota
                    })
                    .FirstOrDefault();

                if (modelo == null)
                {
                    return HttpNotFound();
                }

                // Obtener las calificaciones de la misma
                // materia, estudiante y período.

                var notasMateria =
                    ObtenerConsultaAutorizada()
                        .Where(n =>

                            n.fk_id_estudiante ==
                                nota.fk_id_estudiante &&

                            n.fk_id_materia ==
                                nota.fk_id_materia &&

                            n.fk_id_seccion ==
                                nota.fk_id_seccion &&

                            n.fk_id_calendario ==
                                nota.fk_id_calendario &&

                            n.periodo ==
                                nota.periodo &&

                            n.fk_id_docente ==
                                nota.fk_id_docente)
                        .OrderByDescending(n =>
                            n.fecha_evaluacion)
                        .ToList();

                var resumenes = GenerarResumenes(
                    ConvertirNotas(notasMateria));

                ViewBag.Resumen =
                    resumenes.FirstOrDefault();

                return View(modelo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error consultando detalle de nota: " +
                    ex);

                TempData["MensajeError"] =
                    "No se pudo consultar el detalle " +
                    "de la calificación.";

                return RedirectToAction("Index");
            }
        }

        // ========================================
        // CONSULTA SEGURA SEGÚN EL ROL
        // ========================================

        private IQueryable<NotaEntity>
            ObtenerConsultaAutorizada()
        {
            var usuario = UsuarioActual();

            IQueryable<NotaEntity> consulta =
                db.Notas.AsNoTracking();

            if (usuario == null ||
                string.IsNullOrWhiteSpace(
                    usuario.id_usuario))
            {
                return consulta.Where(n => false);
            }

            string idUsuario = usuario.id_usuario;

            // ========================================
            // ADMINISTRADOR
            // ========================================

            if (RolesSistema.Es(
                usuario, RolesSistema.Administrador))
            {
                return consulta;
            }

            // ========================================
            // DOCENTE
            // ========================================

            if (RolesSistema.Es(
                usuario, RolesSistema.Docente))
            {
                return consulta.Where(n =>
                    n.fk_id_docente == idUsuario);
            }

            // ========================================
            // ESTUDIANTE
            // ========================================

            if (RolesSistema.Es(
                usuario, RolesSistema.Estudiante))
            {
                return consulta.Where(n =>
                    n.fk_id_estudiante == idUsuario);
            }

            // ========================================
            // PADRE O ENCARGADO
            // ========================================

            if (RolesSistema.Es(
                usuario, RolesSistema.Padre))
            {
                var idsEstudiantes =
                    db.Set<EncargadoEstudianteEntity>()
                        .Where(e =>
                            e.fk_id_encargado ==
                                idUsuario &&

                            e.estado)
                        .Select(e =>
                            e.fk_id_estudiante);

                return consulta.Where(n =>
                    idsEstudiantes.Contains(
                        n.fk_id_estudiante));
            }

            // Si el rol no está autorizado,
            // no se devuelve ninguna calificación.

            return consulta.Where(n => false);
        }

        // ========================================
        // ESTUDIANTES AUTORIZADOS
        // ========================================

        private List<UsuarioEntity>
            ObtenerEstudiantesAutorizados()
        {
            var usuario = UsuarioActual();

            if (usuario == null)
            {
                return new List<UsuarioEntity>();
            }

            string idUsuario =
                usuario.id_usuario;

            IQueryable<string> idsEstudiantes;

            // ========================================
            // ESTUDIANTE
            // ========================================

            if (RolesSistema.Es(
                usuario, RolesSistema.Estudiante))
            {
                return db.Usuarios
                    .AsNoTracking()
                    .Where(u =>
                        u.id_usuario == idUsuario)
                    .ToList();
            }

            // ========================================
            // ADMINISTRADOR
            // ========================================

            if (RolesSistema.Es(
                usuario, RolesSistema.Administrador))
            {
                idsEstudiantes =
                    db.SeccionesEstudiantes
                        .Select(e => e.fk_id_usuario)
                        .Union(
                            db.Notas.Select(n =>
                                n.fk_id_estudiante));
            }

            // ========================================
            // DOCENTE
            // ========================================

            else if (RolesSistema.Es(
                usuario, RolesSistema.Docente))
            {
                idsEstudiantes =
                    db.Notas
                        .Where(n =>
                            n.fk_id_docente ==
                            idUsuario)
                        .Select(n =>
                            n.fk_id_estudiante);
            }

            // ========================================
            // PADRE O ENCARGADO
            // ========================================

            else if (RolesSistema.Es(
                usuario, RolesSistema.Padre))
            {
                idsEstudiantes =
                    db.Set<EncargadoEstudianteEntity>()
                        .Where(e =>
                            e.fk_id_encargado ==
                                idUsuario &&

                            e.estado)
                        .Select(e =>
                            e.fk_id_estudiante);
            }
            else
            {
                return new List<UsuarioEntity>();
            }

            return db.Usuarios
                .AsNoTracking()
                .Where(u =>
                    idsEstudiantes.Contains(
                        u.id_usuario))
                .OrderBy(u => u.apellido1)
                .ThenBy(u => u.apellido2)
                .ThenBy(u => u.nombre)
                .ToList();
        }

        // ========================================
        // VALIDAR FILTROS DE CONSULTA
        // ========================================

        private void ValidarFiltros(
            ConsultaNotasDto modelo)
        {
            if (modelo.fecha_desde.HasValue &&
                modelo.fecha_hasta.HasValue &&
                modelo.fecha_desde.Value.Date >
                    modelo.fecha_hasta.Value.Date)
            {
                ModelState.AddModelError(
                    "",
                    "La fecha inicial no puede ser " +
                    "posterior a la fecha final.");
            }

            if (!string.IsNullOrWhiteSpace(
                modelo.periodo) &&
                !Periodos.Contains(modelo.periodo))
            {
                ModelState.AddModelError(
                    "periodo",
                    "Seleccione un período académico válido.");
            }

            if (!string.IsNullOrWhiteSpace(
                modelo.tipo_evaluacion) &&
                !TiposEvaluacion.Contains(
                    modelo.tipo_evaluacion))
            {
                ModelState.AddModelError(
                    "tipo_evaluacion",
                    "Seleccione un tipo de evaluación válido.");
            }

            if (modelo.id_grado.HasValue &&
                modelo.id_grado.Value < 0)
            {
                ModelState.AddModelError(
                    "id_grado",
                    "El grado seleccionado no es válido.");
            }

            if (modelo.id_seccion.HasValue &&
                modelo.id_seccion.Value < 0)
            {
                ModelState.AddModelError(
                    "id_seccion",
                    "La sección seleccionada no es válida.");
            }

            if (modelo.id_materia.HasValue &&
                modelo.id_materia.Value < 0)
            {
                ModelState.AddModelError(
                    "id_materia",
                    "La materia seleccionada no es válida.");
            }

            if (modelo.id_calendario.HasValue &&
                modelo.id_calendario.Value < 0)
            {
                ModelState.AddModelError(
                    "id_calendario",
                    "El calendario seleccionado no es válido.");
            }
        }

        // ========================================
        // CARGAR LISTAS DESPLEGABLES
        // ========================================

        private void CargarListas(
            ConsultaNotasDto modelo,
            List<NotaDto> notas,
            List<UsuarioEntity> estudiantes)
        {
            var notasEstudiante = notas
                .Where(n =>
                    string.IsNullOrWhiteSpace(
                        modelo.id_estudiante) ||

                    n.fk_id_estudiante ==
                        modelo.id_estudiante)
                .ToList();

            // ========================================
            // LISTADO DE ESTUDIANTES
            // ========================================

            var estudiantesLista = estudiantes
                .Select(e => new
                {
                    id = e.id_usuario,

                    nombre =
                        HorarioServicio.NombreCompleto(e)
                })
                .OrderBy(e => e.nombre)
                .ToList();

            ViewBag.Estudiantes = new SelectList(
                estudiantesLista,
                "id",
                "nombre",
                modelo.id_estudiante);

            // ========================================
            // LISTADO DE GRADOS
            // ========================================

            var grados = notasEstudiante
                .Where(n => n.id_grado > 0)
                .GroupBy(n => n.id_grado)
                .Select(g => new
                {
                    id = g.Key,

                    nombre =
                        g.First().nombre_grado
                })
                .OrderBy(g => g.nombre)
                .ToList();

            ViewBag.Grados = new SelectList(
                grados,
                "id",
                "nombre",
                modelo.id_grado);

            // ========================================
            // LISTADO DE SECCIONES
            // ========================================

            var secciones = notasEstudiante
                .Where(n =>
                    !modelo.id_grado.HasValue ||

                    modelo.id_grado.Value == 0 ||

                    n.id_grado ==
                        modelo.id_grado.Value)
                .GroupBy(n => n.fk_id_seccion)
                .Select(g => new
                {
                    id = g.Key,

                    nombre =
                        g.First().nombre_seccion
                })
                .OrderBy(s => s.nombre)
                .ToList();

            ViewBag.Secciones = new SelectList(
                secciones,
                "id",
                "nombre",
                modelo.id_seccion);

            // ========================================
            // LISTADO DE MATERIAS
            // ========================================

            var materias = notasEstudiante
                .Where(n =>
                    (
                        !modelo.id_grado.HasValue ||

                        modelo.id_grado.Value == 0 ||

                        n.id_grado ==
                            modelo.id_grado.Value
                    )
                    &&
                    (
                        !modelo.id_seccion.HasValue ||

                        modelo.id_seccion.Value == 0 ||

                        n.fk_id_seccion ==
                            modelo.id_seccion.Value
                    ))
                .GroupBy(n => n.fk_id_materia)
                .Select(g => new
                {
                    id = g.Key,

                    nombre =
                        g.First().nombre_materia
                })
                .OrderBy(m => m.nombre)
                .ToList();

            ViewBag.Materias = new SelectList(
                materias,
                "id",
                "nombre",
                modelo.id_materia);

            // ========================================
            // LISTADO DE CALENDARIOS
            // ========================================

            var calendarios = notasEstudiante
                .GroupBy(n => n.fk_id_calendario)
                .Select(g => new
                {
                    id = g.Key,

                    nombre =
                        g.First().nombre_calendario
                })
                .OrderBy(c => c.nombre)
                .ToList();

            ViewBag.Calendarios = new SelectList(
                calendarios,
                "id",
                "nombre",
                modelo.id_calendario);

            // ========================================
            // PERÍODOS Y TIPOS DE EVALUACIÓN
            // ========================================

            ViewBag.Periodos = new SelectList(
                Periodos,
                modelo.periodo);

            ViewBag.TiposEvaluacion =
                new SelectList(
                    TiposEvaluacion,
                    modelo.tipo_evaluacion);

            // ========================================
            // FECHAS SELECCIONADAS
            // ========================================

            ViewBag.FechaDesde =
                modelo.fecha_desde.HasValue
                    ? modelo.fecha_desde.Value
                        .ToString("yyyy-MM-dd")
                    : "";

            ViewBag.FechaHasta =
                modelo.fecha_hasta.HasValue
                    ? modelo.fecha_hasta.Value
                        .ToString("yyyy-MM-dd")
                    : "";
        }

        // ========================================
        // GENERAR RESUMEN DE CALIFICACIONES
        // ========================================

        private List<ResumenNotasDto>
            GenerarResumenes(List<NotaDto> notas)
        {
            if (notas == null || !notas.Any())
            {
                return new List<ResumenNotasDto>();
            }

            return notas
                .GroupBy(n => new
                {
                    n.fk_id_estudiante,

                    n.fk_id_materia,

                    n.fk_id_seccion,

                    n.fk_id_calendario,

                    n.fk_id_docente,

                    n.periodo
                })
                .Select(g =>
                {
                    NotaDto primera = g.First();

                    return new ResumenNotasDto
                    {
                        id_estudiante =
                            primera.fk_id_estudiante,

                        identificacion_estudiante =
                            primera.identificacion_estudiante,

                        nombre_estudiante =
                            primera.nombre_estudiante,

                        id_grado =
                            primera.id_grado,

                        nombre_grado =
                            primera.nombre_grado,

                        id_seccion =
                            primera.fk_id_seccion,

                        nombre_seccion =
                            primera.nombre_seccion,

                        id_materia =
                            primera.fk_id_materia,

                        nombre_materia =
                            primera.nombre_materia,

                        id_calendario =
                            primera.fk_id_calendario,

                        nombre_calendario =
                            primera.nombre_calendario,

                        periodo =
                            primera.periodo,

                        id_docente =
                            primera.fk_id_docente,

                        nombre_docente =
                            primera.nombre_docente,

                        Notas = g
                            .OrderByDescending(n =>
                                n.fecha_evaluacion)
                            .ToList()
                    };
                })
                .OrderBy(r => r.nombre_estudiante)
                .ThenBy(r => r.nombre_materia)
                .ThenBy(r => r.periodo)
                .ToList();
        }

        // ========================================
        // CONVERTIR ENTIDADES A DTO
        // ========================================

        private List<NotaDto> ConvertirNotas(
            List<NotaEntity> notas)
        {
            if (notas == null || !notas.Any())
            {
                return new List<NotaDto>();
            }

            // ========================================
            // CARGAR USUARIOS RELACIONADOS
            // ========================================

            var idsUsuarios = notas
                .Select(n => n.fk_id_estudiante)
                .Union(notas.Select(n =>
                    n.fk_id_docente))
                .Union(notas.Select(n =>
                    n.fk_id_modificado_por))
                .Where(id =>
                    !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var usuarios = db.Usuarios
                .AsNoTracking()
                .Where(u =>
                    idsUsuarios.Contains(
                        u.id_usuario))
                .ToList()
                .ToDictionary(u => u.id_usuario);

            // ========================================
            // CARGAR SECCIONES
            // ========================================

            var idsSecciones = notas
                .Select(n => n.fk_id_seccion)
                .Distinct()
                .ToList();

            var secciones = db.Secciones
                .AsNoTracking()
                .Where(s =>
                    idsSecciones.Contains(
                        s.id_seccion))
                .ToList()
                .ToDictionary(s => s.id_seccion);

            // ========================================
            // CARGAR GRADOS
            // ========================================

            var idsGrados = secciones.Values
                .Select(s => s.fk_id_grado)
                .Distinct()
                .ToList();

            var grados = db.Grados
                .AsNoTracking()
                .Where(g =>
                    idsGrados.Contains(
                        g.id_grado))
                .ToList()
                .ToDictionary(g => g.id_grado);

            // ========================================
            // CARGAR MATERIAS
            // ========================================

            var idsMaterias = notas
                .Select(n => n.fk_id_materia)
                .Distinct()
                .ToList();

            var materias = db.Materias
                .AsNoTracking()
                .Where(m =>
                    idsMaterias.Contains(
                        m.id_materia))
                .ToList()
                .ToDictionary(m => m.id_materia);

            // ========================================
            // CARGAR CALENDARIOS
            // ========================================

            var idsCalendarios = notas
                .Select(n => n.fk_id_calendario)
                .Distinct()
                .ToList();

            var calendarios = db.Calendarios
                .AsNoTracking()
                .Where(c =>
                    idsCalendarios.Contains(
                        c.id_calendario))
                .ToList()
                .ToDictionary(c => c.id_calendario);

            // ========================================
            // CONSTRUIR RESULTADO
            // ========================================

            var resultado = new List<NotaDto>();

            foreach (NotaEntity nota in notas)
            {
                UsuarioEntity estudiante = null;

                UsuarioEntity docente = null;

                UsuarioEntity modificadoPor = null;

                SeccionEntity seccion;

                GradoEntity grado = null;

                MateriaEntity materia;

                CalendarioEntity calendario;

                if (!string.IsNullOrWhiteSpace(
                    nota.fk_id_estudiante))
                {
                    usuarios.TryGetValue(
                        nota.fk_id_estudiante,
                        out estudiante);
                }

                if (!string.IsNullOrWhiteSpace(
                    nota.fk_id_docente))
                {
                    usuarios.TryGetValue(
                        nota.fk_id_docente,
                        out docente);
                }

                if (!string.IsNullOrWhiteSpace(
                    nota.fk_id_modificado_por))
                {
                    usuarios.TryGetValue(
                        nota.fk_id_modificado_por,
                        out modificadoPor);
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

                if (seccion != null)
                {
                    grados.TryGetValue(
                        seccion.fk_id_grado,
                        out grado);
                }

                resultado.Add(new NotaDto
                {
                    id_nota =
                        nota.id_nota,

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
        // ETIQUETA DEL CALENDARIO
        // ========================================

        private static string EtiquetaCalendario(
            DateTime? inicio,
            DateTime? fin)
        {
            if (!inicio.HasValue ||
                !fin.HasValue)
            {
                return "Calendario académico";
            }

            return inicio.Value
                .ToString("dd/MM/yyyy") +
                " - " +
                fin.Value.ToString("dd/MM/yyyy");
        }

        // ========================================
        // LIMPIAR TEXTOS
        // ========================================

        private static string LimpiarTexto(
            string texto)
        {
            return string.IsNullOrWhiteSpace(texto)
                ? null
                : texto.Trim();
        }

        // ========================================
        // OBTENER USUARIO DE LA SESIÓN
        // ========================================

        private UsuarioDto UsuarioActual()
        {
            return Session[
                AutorizarAdministradorAttribute.ClaveSesion
            ] as UsuarioDto;
        }

        // ========================================
        // LIBERAR RECURSOS
        // ========================================

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}