using EduNexus.Abstracciones.ModelosParaUI.Asistencia;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RASI-10-005
    // Reportes mensuales de asistencia.

    public class ReportesAsistenciaController : Controller
    {
        // ========================================
        // VALIDAR ACCESO DEL ADMINISTRADOR
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
                RolesSistema.Administrador))
            {
                TempData["MensajeError"] =
                    "Esta funcionalidad es exclusiva " +
                    "de los administradores.";

                filterContext.Result =
                    RedirectToAction("Index", "Home");

                return;
            }

            base.OnActionExecuting(filterContext);
        }

        // ========================================
        // REPORTE MENSUAL DE ASISTENCIA
        // ========================================

        [HttpGet]
        public ActionResult Index(
            int? anio,
            int? mes,
            int? id_grado,
            int? id_seccion,
            bool consultar = false)
        {
            var modelo = new ReporteAsistenciaDto
            {
                anio = anio ?? DateTime.Today.Year,

                mes = mes ?? DateTime.Today.Month,

                id_grado = id_grado,

                id_seccion = id_seccion,

                consultado = false
            };

            using (var db = new EduNexusDbContext())
            {
                // Cargar filtros del reporte.

                CargarFiltros(db, modelo);

                // Validar año.

                if (modelo.anio < 2000 ||
                    modelo.anio > DateTime.Today.Year)
                {
                    modelo.mensajeError =
                        "Seleccione un año válido.";

                    return View(modelo);
                }

                // Validar mes.

                if (modelo.mes < 1 ||
                    modelo.mes > 12)
                {
                    modelo.mensajeError =
                        "Seleccione un mes válido.";

                    return View(modelo);
                }

                // Evitar consultar meses futuros.

                if (modelo.anio == DateTime.Today.Year &&
                    modelo.mes > DateTime.Today.Month)
                {
                    modelo.mensajeError =
                        "No se pueden consultar meses futuros.";

                    return View(modelo);
                }

                // Establecer rango mensual.

                modelo.fecha_inicio = new DateTime(
                    modelo.anio,
                    modelo.mes,
                    1);

                modelo.fecha_fin =
                    modelo.fecha_inicio
                        .AddMonths(1)
                        .AddDays(-1);

                modelo.nombre_mes =
                    CultureInfo
                        .GetCultureInfo("es-CR")
                        .DateTimeFormat
                        .GetMonthName(modelo.mes);

                // Mostrar únicamente los filtros
                // cuando todavía no se consulta.

                if (!consultar)
                {
                    return View(modelo);
                }

                // Validar grado.

                if (!modelo.id_grado.HasValue ||
                    modelo.id_grado.Value <= 0)
                {
                    modelo.mensajeError =
                        "Debe seleccionar un grado.";

                    return View(modelo);
                }

                // Validar sección.

                if (!modelo.id_seccion.HasValue ||
                    modelo.id_seccion.Value <= 0)
                {
                    modelo.mensajeError =
                        "Debe seleccionar una sección.";

                    return View(modelo);
                }

                int idGrado = modelo.id_grado.Value;

                int idSeccion = modelo.id_seccion.Value;

                var grado = db.Grados
                    .FirstOrDefault(g =>
                        g.id_grado == idGrado);

                if (grado == null)
                {
                    modelo.mensajeError =
                        "El grado seleccionado no existe.";

                    return View(modelo);
                }

                var seccion = db.Secciones
                    .FirstOrDefault(s =>
                        s.id_seccion == idSeccion &&
                        s.fk_id_grado == idGrado);

                if (seccion == null)
                {
                    modelo.mensajeError =
                        "La sección seleccionada no pertenece " +
                        "al grado indicado.";

                    return View(modelo);
                }

                modelo.nombre_grado = grado.nombre;

                modelo.nombre_seccion = seccion.nombre;

                // Generar reporte.

                GenerarReporte(db, modelo);

                return View(modelo);
            }
        }

        // ========================================
        // CARGAR FILTROS
        // ========================================

        private void CargarFiltros(
            EduNexusDbContext db,
            ReporteAsistenciaDto modelo)
        {
            // AÑOS DISPONIBLES.

            var aniosRegistrados = db.Asistencias
                .Select(a => a.fecha.Year)
                .Distinct()
                .ToList();

            aniosRegistrados.Add(
                DateTime.Today.Year);

            var anios = aniosRegistrados
                .Where(a =>
                    a >= 2000 &&
                    a <= DateTime.Today.Year)
                .Distinct()
                .OrderByDescending(a => a)
                .Select(a => new
                {
                    valor = a,

                    texto = a.ToString()
                })
                .ToList();

            ViewBag.Anios = new SelectList(
                anios,
                "valor",
                "texto",
                modelo.anio);

            // MESES DEL AÑO.

            var cultura =
                CultureInfo.GetCultureInfo("es-CR");

            var meses = Enumerable
                .Range(1, 12)
                .Select(m => new
                {
                    valor = m,

                    texto = cultura.TextInfo.ToTitleCase(
                        cultura.DateTimeFormat
                            .GetMonthName(m))
                })
                .ToList();

            ViewBag.Meses = new SelectList(
                meses,
                "valor",
                "texto",
                modelo.mes);

            // GRADOS.

            var grados = db.Grados
                .OrderBy(g => g.id_grado)
                .ToList();

            ViewBag.Grados = new SelectList(
                grados,
                "id_grado",
                "nombre",
                modelo.id_grado);

            // SECCIONES SEGÚN EL GRADO Y AÑO.

            var consultaSecciones =
                db.Secciones.AsQueryable();

            if (modelo.id_grado.HasValue &&
                modelo.id_grado.Value > 0)
            {
                int idGrado =
                    modelo.id_grado.Value;

                consultaSecciones =
                    consultaSecciones.Where(s =>
                        s.fk_id_grado == idGrado);
            }
            else
            {
                consultaSecciones =
                    consultaSecciones.Where(s => false);
            }

            if (modelo.anio >= 2000 &&
                modelo.anio <= DateTime.Today.Year)
            {
                DateTime inicioAnio =
                    new DateTime(modelo.anio, 1, 1);

                DateTime finAnio =
                    new DateTime(modelo.anio, 12, 31);

                consultaSecciones =
                    consultaSecciones.Where(s =>
                        db.Calendarios.Any(c =>
                            c.id_calendario ==
                                s.fk_id_calendario &&
                            c.fecha_inicio <= finAnio &&
                            c.fecha_fin >= inicioAnio));
            }

            var secciones = consultaSecciones
                .OrderBy(s => s.nombre)
                .ToList();

            ViewBag.Secciones = new SelectList(
                secciones,
                "id_seccion",
                "nombre",
                modelo.id_seccion);
        }

        // ========================================
        // GENERAR REPORTE MENSUAL
        // ========================================

        private void GenerarReporte(
            EduNexusDbContext db,
            ReporteAsistenciaDto modelo)
        {
            int idSeccion =
                modelo.id_seccion.Value;

            DateTime fechaInicio =
                modelo.fecha_inicio;

            DateTime fechaFinExclusiva =
                modelo.fecha_inicio.AddMonths(1);

            // Obtener registros del mes seleccionado.

            List<AsistenciaEntity> registros =
                db.Asistencias
                    .Where(a =>
                        a.fk_id_seccion == idSeccion &&
                        a.fecha >= fechaInicio &&
                        a.fecha < fechaFinExclusiva)
                    .ToList();

            // Obtener estudiantes asignados
            // actualmente a la sección.

            List<string> idsEstudiantes =
                db.SeccionesEstudiantes
                    .Where(se =>
                        se.fk_id_seccion == idSeccion)
                    .Select(se => se.fk_id_usuario)
                    .ToList();

            // Incluir estudiantes con registros
            // históricos durante el mes.

            idsEstudiantes = idsEstudiantes
                .Union(registros.Select(a =>
                    a.fk_id_usuario))
                .Where(id =>
                    !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            // Obtener información de estudiantes.

            var usuarios = db.Usuarios
                .Where(u =>
                    idsEstudiantes.Contains(u.id_usuario))
                .ToList()
                .ToDictionary(u => u.id_usuario);

            // Agrupar registros por estudiante.

            var registrosPorEstudiante = registros
                .Where(a =>
                    !string.IsNullOrEmpty(a.fk_id_usuario))
                .ToLookup(a => a.fk_id_usuario);

            modelo.estudiantes = idsEstudiantes
                .Select(id =>
                {
                    UsuarioEntity estudiante;

                    usuarios.TryGetValue(
                        id,
                        out estudiante);

                    var asistencias =
                        registrosPorEstudiante[id];

                    return new ReporteAsistenciaEstudianteDto
                    {
                        id_usuario = id,

                        identificacion =
                            estudiante != null
                            ? estudiante.identificacion
                            : "",

                        nombre =
                            estudiante != null
                            ? HorarioServicio.NombreCompleto(
                                estudiante)
                            : "Estudiante no disponible",

                        presentes = asistencias.Count(a =>
                            a.estado == "Presente"),

                        tardias = asistencias.Count(a =>
                            a.estado == "Tardía"),

                        ausencias_justificadas =
                            asistencias.Count(a =>
                                a.estado ==
                                AsistenciaFormDto.EstadoJustificado),

                        ausencias_no_justificadas =
                            asistencias.Count(a =>
                                a.estado ==
                                "Ausencia no justificada")
                    };
                })
                .OrderBy(e => e.nombre)
                .ToList();

            // ========================================
            // TOTALES GENERALES DE LA SECCIÓN
            // ========================================

            modelo.total_estudiantes =
                modelo.estudiantes.Count;

            modelo.total_presentes =
                modelo.estudiantes.Sum(e =>
                    e.presentes);

            modelo.total_tardias =
                modelo.estudiantes.Sum(e =>
                    e.tardias);

            modelo.total_ausencias_justificadas =
                modelo.estudiantes.Sum(e =>
                    e.ausencias_justificadas);

            modelo.total_ausencias_no_justificadas =
                modelo.estudiantes.Sum(e =>
                    e.ausencias_no_justificadas);

            modelo.consultado = true;
        }

        // ========================================
        // OBTENER SECCIONES SEGÚN EL GRADO
        // ========================================

        [HttpGet]
        public JsonResult ObtenerSecciones(
            int id_grado,
            int anio)
        {
            if (id_grado <= 0 ||
                anio < 2000 ||
                anio > DateTime.Today.Year)
            {
                return Json(
                    new object[0],
                    JsonRequestBehavior.AllowGet);
            }

            using (var db = new EduNexusDbContext())
            {
                DateTime inicioAnio =
                    new DateTime(anio, 1, 1);

                DateTime finAnio =
                    new DateTime(anio, 12, 31);

                var secciones = db.Secciones
                    .Where(s =>
                        s.fk_id_grado == id_grado &&

                        db.Calendarios.Any(c =>
                            c.id_calendario ==
                                s.fk_id_calendario &&
                            c.fecha_inicio <= finAnio &&
                            c.fecha_fin >= inicioAnio))
                    .OrderBy(s => s.nombre)
                    .Select(s => new
                    {
                        id = s.id_seccion,

                        nombre = s.nombre
                    })
                    .ToList();

                return Json(
                    secciones,
                    JsonRequestBehavior.AllowGet);
            }
        }
    }
}