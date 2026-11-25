using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.Abstracciones.ModelosParaUI.SalidasEstudiantes;

namespace EduNexus.UI.Controllers
{
    public class SalidasEstudiantesController : Controller
    {
        private readonly EduNexusDbContext db =
            new EduNexusDbContext();

        // ========================================
        // VALIDACIÓN DE USUARIO Y PERMISOS
        // ========================================

        private UsuarioDto ObtenerUsuarioActual()
        {
            return Session["UsuarioActual"] as UsuarioDto;
        }

        private bool TienePermiso()
        {
            var usuario = ObtenerUsuarioActual();

            if (usuario == null)
            {
                return false;
            }

            return RolesSistema.Es(
                       usuario, RolesSistema.Administrador)
                   ||
                   RolesSistema.Es(
                       usuario, RolesSistema.Director);
        }

        private ActionResult AccesoDenegado()
        {
            if (ObtenerUsuarioActual() == null)
            {
                return RedirectToAction("Login", "Home");
            }

            TempData["MensajeError"] =
                "No tiene permisos para administrar las salidas de estudiantes.";

            return RedirectToAction("Index", "Home");
        }

        // ========================================
        // RJT-11-002
        // REGISTRAR SALIDA - GET
        // ========================================

        [HttpGet]
        public ActionResult Registrar(
            int? id_seccion,
            string id_estudiante)
        {
            if (!TienePermiso())
            {
                return AccesoDenegado();
            }

            var modelo = new SalidaEstudianteDto
            {
                fk_id_seccion = id_seccion ?? 0,
                fk_id_estudiante = id_estudiante,
                fecha_salida = DateTime.Today,
                hora_salida = new TimeSpan(
                    DateTime.Now.Hour,
                    DateTime.Now.Minute,
                    0),
                estado = "Registrada"
            };

            CargarSecciones(modelo.fk_id_seccion);

            CargarEstudiantes(
                modelo.fk_id_seccion,
                modelo.fk_id_estudiante);

            return View(modelo);
        }

        // ========================================
        // REGISTRAR SALIDA - POST
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registrar(
            SalidaEstudianteDto modelo)
        {
            if (!TienePermiso())
            {
                return AccesoDenegado();
            }

            var usuario = ObtenerUsuarioActual();

            if (modelo == null)
            {
                return RedirectToAction("Registrar");
            }

            // ========================================
            // LIMPIEZA DE CAMPOS
            // ========================================

            modelo.motivo =
                (modelo.motivo ?? "").Trim();

            modelo.nombre_persona_retira =
                (modelo.nombre_persona_retira ?? "").Trim();

            modelo.identificacion_persona_retira =
                (modelo.identificacion_persona_retira ?? "").Trim();

            modelo.parentesco =
                (modelo.parentesco ?? "").Trim();

            modelo.telefono_persona_retira =
                (modelo.telefono_persona_retira ?? "").Trim();

            modelo.observaciones =
                (modelo.observaciones ?? "").Trim();

            // ========================================
            // VALIDACIONES OBLIGATORIAS
            // ========================================

            if (!modelo.confirma_retiro)
            {
                ModelState.AddModelError(
                    "confirma_retiro",
                    "Debe confirmar que verificó la identidad y autorización de la persona que retira al estudiante.");
            }

            if (string.IsNullOrWhiteSpace(modelo.motivo))
            {
                ModelState.AddModelError(
                    "motivo",
                    "Debe indicar el motivo de salida.");
            }

            if (string.IsNullOrWhiteSpace(
                modelo.nombre_persona_retira))
            {
                ModelState.AddModelError(
                    "nombre_persona_retira",
                    "Debe indicar el nombre de la persona que retira.");
            }

            if (string.IsNullOrWhiteSpace(
                modelo.identificacion_persona_retira))
            {
                ModelState.AddModelError(
                    "identificacion_persona_retira",
                    "Debe indicar la identificación de la persona que retira.");
            }

            if (string.IsNullOrWhiteSpace(modelo.parentesco))
            {
                ModelState.AddModelError(
                    "parentesco",
                    "Debe indicar el parentesco.");
            }

            // ========================================
            // VALIDACIÓN DE FECHA Y HORA
            // ========================================

            DateTime fechaHoraSalida = DateTime.Now;

            if (!modelo.fecha_salida.HasValue)
            {
                ModelState.AddModelError(
                    "fecha_salida",
                    "Debe indicar la fecha de salida.");
            }

            if (!modelo.hora_salida.HasValue)
            {
                ModelState.AddModelError(
                    "hora_salida",
                    "Debe indicar la hora de salida.");
            }

            if (modelo.fecha_salida.HasValue &&
                modelo.hora_salida.HasValue)
            {
                if (modelo.hora_salida.Value < TimeSpan.Zero ||
                    modelo.hora_salida.Value >= TimeSpan.FromDays(1))
                {
                    ModelState.AddModelError(
                        "hora_salida",
                        "Debe indicar una hora válida.");
                }
                else
                {
                    fechaHoraSalida =
                        modelo.fecha_salida.Value.Date
                        .Add(modelo.hora_salida.Value);

                    if (fechaHoraSalida > DateTime.Now)
                    {
                        ModelState.AddModelError(
                            "fecha_salida",
                            "No se puede registrar una salida con fecha u hora futura.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return RetornarFormulario(modelo);
            }

            // ========================================
            // REGISTRO DE SALIDA
            // ========================================

            try
            {
                using (var transaccion =
                    db.Database.BeginTransaction(
                        IsolationLevel.Serializable))
                {
                    var estudiante = db.Usuarios
                        .FirstOrDefault(u =>
                            u.id_usuario ==
                                modelo.fk_id_estudiante &&
                            u.estado);

                    if (estudiante == null)
                    {
                        ModelState.AddModelError(
                            "fk_id_estudiante",
                            "El estudiante seleccionado no existe o se encuentra inactivo.");
                    }

                    var seccion = db.Secciones
                        .FirstOrDefault(s =>
                            s.id_seccion ==
                                modelo.fk_id_seccion);

                    if (seccion == null)
                    {
                        ModelState.AddModelError(
                            "fk_id_seccion",
                            "La sección seleccionada no existe.");
                    }

                    bool perteneceSeccion =
                        db.SeccionesEstudiantes.Any(se =>
                            se.fk_id_usuario ==
                                modelo.fk_id_estudiante &&
                            se.fk_id_seccion ==
                                modelo.fk_id_seccion);

                    if (!perteneceSeccion)
                    {
                        ModelState.AddModelError(
                            "fk_id_estudiante",
                            "El estudiante no pertenece a la sección seleccionada.");
                    }

                    if (!ModelState.IsValid)
                    {
                        transaccion.Rollback();

                        return RetornarFormulario(modelo);
                    }

                    // ========================================
                    // PREVENCIÓN DE REGISTROS DUPLICADOS
                    // ========================================

                    DateTime inicioDia =
                        fechaHoraSalida.Date;

                    DateTime finDia =
                        inicioDia.AddDays(1);

                    bool existeSalida =
                        db.SalidasEstudiantes.Any(s =>
                            s.fk_id_estudiante ==
                                modelo.fk_id_estudiante &&
                            s.fecha_salida >= inicioDia &&
                            s.fecha_salida < finDia &&
                            s.estado == "Registrada");

                    if (existeSalida)
                    {
                        ModelState.AddModelError(
                            "",
                            "El estudiante ya tiene una salida registrada para esta fecha.");

                        transaccion.Rollback();

                        return RetornarFormulario(modelo);
                    }

                    // ========================================
                    // CREAR ENTIDAD
                    // ========================================

                    var salida = new SalidaEstudianteEntity
                    {
                        fk_id_estudiante =
                            modelo.fk_id_estudiante,

                        fk_id_seccion =
                            modelo.fk_id_seccion,

                        fecha_salida =
                            fechaHoraSalida,

                        motivo =
                            modelo.motivo,

                        nombre_persona_retira =
                            modelo.nombre_persona_retira,

                        identificacion_persona_retira =
                            modelo.identificacion_persona_retira,

                        parentesco =
                            modelo.parentesco,

                        telefono_persona_retira =
                            modelo.telefono_persona_retira,

                        fk_id_registrado_por =
                            usuario.id_usuario,

                        fecha_registro =
                            DateTime.Now,

                        observaciones =
                            modelo.observaciones,

                        estado =
                            "Registrada"
                    };

                    db.SalidasEstudiantes.Add(salida);

                    db.SaveChanges();

                    transaccion.Commit();
                }

                TempData["Mensaje"] =
                    "La salida del estudiante fue registrada correctamente.";

                return RedirectToAction("Historial");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al registrar salida: {0}", ex);

                ModelState.AddModelError(
                    "",
                    "No fue posible registrar la salida. Intente nuevamente o comuníquese con el administrador del sistema.");

                return RetornarFormulario(modelo);
            }
        }

        // ========================================
        // HISTORIAL DE SALIDAS
        // ========================================

        [HttpGet]
        public ActionResult Historial(
            DateTime? fecha_desde,
            DateTime? fecha_hasta,
            int? id_grado,
            int? id_seccion,
            string id_estudiante)
        {
            if (!TienePermiso())
            {
                return AccesoDenegado();
            }

            if (fecha_desde.HasValue &&
                fecha_hasta.HasValue &&
                fecha_desde.Value.Date >
                    fecha_hasta.Value.Date)
            {
                ModelState.AddModelError(
                    "",
                    "La fecha inicial no puede ser mayor que la fecha final.");
            }

            var consulta =
                from salida in db.SalidasEstudiantes
                join estudiante in db.Usuarios
                    on salida.fk_id_estudiante
                    equals estudiante.id_usuario
                join seccion in db.Secciones
                    on salida.fk_id_seccion
                    equals seccion.id_seccion
                join grado in db.Grados
                    on seccion.fk_id_grado
                    equals grado.id_grado
                join responsable in db.Usuarios
                    on salida.fk_id_registrado_por
                    equals responsable.id_usuario
                    into responsables
                from responsable in
                    responsables.DefaultIfEmpty()
                select new
                {
                    Salida = salida,
                    Estudiante = estudiante,
                    Seccion = seccion,
                    Grado = grado,
                    Responsable = responsable
                };

            // ========================================
            // FILTROS DEL HISTORIAL
            // ========================================

            if (fecha_desde.HasValue)
            {
                DateTime desde =
                    fecha_desde.Value.Date;

                consulta = consulta.Where(x =>
                    x.Salida.fecha_salida >= desde);
            }

            if (fecha_hasta.HasValue)
            {
                DateTime hasta =
                    fecha_hasta.Value.Date;

                consulta = consulta.Where(x =>
                    System.Data.Entity.DbFunctions.TruncateTime(
                        x.Salida.fecha_salida) <= hasta);
            }

            if (id_grado.HasValue &&
                id_grado.Value > 0)
            {
                consulta = consulta.Where(x =>
                    x.Grado.id_grado ==
                        id_grado.Value);
            }

            if (id_seccion.HasValue &&
                id_seccion.Value > 0)
            {
                consulta = consulta.Where(x =>
                    x.Seccion.id_seccion ==
                        id_seccion.Value);
            }

            if (!string.IsNullOrWhiteSpace(id_estudiante))
            {
                consulta = consulta.Where(x =>
                    x.Salida.fk_id_estudiante ==
                        id_estudiante);
            }

            // ========================================
            // CONVERTIR RESULTADOS A DTO
            // ========================================

            var resultados = new List<SalidaEstudianteDto>();

            if (ModelState.IsValid)
            {
                resultados = consulta
                    .OrderByDescending(x =>
                        x.Salida.fecha_salida)
                    .ToList()
                    .Select(x => new SalidaEstudianteDto
                    {
                        id_salida =
                            x.Salida.id_salida,

                        fk_id_estudiante =
                            x.Salida.fk_id_estudiante,

                        identificacion_estudiante =
                            x.Estudiante.identificacion,

                        nombre_estudiante =
                            NombreCompleto(x.Estudiante),

                        fk_id_seccion =
                            x.Salida.fk_id_seccion,

                        nombre_seccion =
                            x.Seccion.nombre,

                        id_grado =
                            x.Grado.id_grado,

                        nombre_grado =
                            x.Grado.nombre,

                        fecha_salida =
                            x.Salida.fecha_salida.Date,

                        hora_salida =
                            x.Salida.fecha_salida.TimeOfDay,

                        motivo =
                            x.Salida.motivo,

                        nombre_persona_retira =
                            x.Salida.nombre_persona_retira,

                        identificacion_persona_retira =
                            x.Salida.identificacion_persona_retira,

                        parentesco =
                            x.Salida.parentesco,

                        telefono_persona_retira =
                            x.Salida.telefono_persona_retira,

                        fk_id_registrado_por =
                            x.Salida.fk_id_registrado_por,

                        nombre_registrado_por =
                            x.Responsable != null
                            ? NombreCompleto(x.Responsable)
                            : "Usuario no disponible",

                        fecha_registro =
                            x.Salida.fecha_registro,

                        observaciones =
                            x.Salida.observaciones,

                        estado =
                            x.Salida.estado
                    })
                    .ToList();
            }

            // ========================================
            // INFORMACIÓN ESTADÍSTICA
            // ========================================

            ViewBag.TotalSalidas =
                resultados.Count;

            ViewBag.TotalEstudiantes =
                resultados
                    .Select(x => x.fk_id_estudiante)
                    .Distinct()
                    .Count();

            ViewBag.SalidasHoy =
                resultados.Count(x =>
                    x.fecha_salida.HasValue &&
                    x.fecha_salida.Value.Date ==
                        DateTime.Today);

            ViewBag.FechaDesde =
                fecha_desde.HasValue
                ? fecha_desde.Value.ToString("yyyy-MM-dd")
                : "";

            ViewBag.FechaHasta =
                fecha_hasta.HasValue
                ? fecha_hasta.Value.ToString("yyyy-MM-dd")
                : "";

            ViewBag.GradoSeleccionado =
                id_grado;

            ViewBag.SeccionSeleccionada =
                id_seccion;

            ViewBag.EstudianteSeleccionado =
                id_estudiante;

            CargarGrados(id_grado);

            CargarSecciones(
                id_seccion ?? 0,
                id_grado);

            CargarEstudiantes(
                id_seccion ?? 0,
                id_estudiante,
                id_grado,
                true);

            return View(resultados);
        }

        // ========================================
        // OBTENER ESTUDIANTES POR SECCIÓN
        // ========================================

        [HttpGet]
        public ActionResult ObtenerEstudiantes(
            int id_seccion)
        {
            if (!TienePermiso())
            {
                return new HttpStatusCodeResult(403);
            }

            if (id_seccion <= 0)
            {
                return Json(
                    new object[0],
                    JsonRequestBehavior.AllowGet);
            }

            var estudiantes =
                (from relacion in db.SeccionesEstudiantes
                 join usuario in db.Usuarios
                    on relacion.fk_id_usuario
                    equals usuario.id_usuario
                 where relacion.fk_id_seccion ==
                       id_seccion &&
                       usuario.estado
                 select usuario)
                .Distinct()
                .ToList()
                .OrderBy(u => u.apellido1)
                .ThenBy(u => u.apellido2)
                .ThenBy(u => u.nombre)
                .Select(u => new
                {
                    id = u.id_usuario,
                    nombre = NombreCompleto(u),
                    identificacion = u.identificacion
                })
                .ToList();

            return Json(
                estudiantes,
                JsonRequestBehavior.AllowGet);
        }

        // ========================================
        // OBTENER SECCIONES POR GRADO
        // ========================================

        [HttpGet]
        public ActionResult ObtenerSecciones(
            int id_grado)
        {
            if (!TienePermiso())
            {
                return new HttpStatusCodeResult(403);
            }

            var consulta = db.Secciones.AsQueryable();

            if (id_grado > 0)
            {
                consulta = consulta.Where(s =>
                    s.fk_id_grado == id_grado);
            }

            var secciones = consulta
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

        // ========================================
        // CARGAR GRADOS
        // ========================================

        private void CargarGrados(
            int? seleccionado = null)
        {
            var grados = db.Grados
                .OrderBy(g => g.nombre)
                .ToList();

            ViewBag.Grados = new SelectList(
                grados,
                "id_grado",
                "nombre",
                seleccionado);
        }

        // ========================================
        // CARGAR SECCIONES
        // ========================================

        private void CargarSecciones(
            int seleccionado = 0,
            int? id_grado = null)
        {
            var consulta =
                from seccion in db.Secciones
                join grado in db.Grados
                    on seccion.fk_id_grado
                    equals grado.id_grado
                select new
                {
                    seccion.id_seccion,
                    seccion.fk_id_grado,
                    NombreSeccion = seccion.nombre,
                    NombreGrado = grado.nombre
                };

            if (id_grado.HasValue &&
                id_grado.Value > 0)
            {
                consulta = consulta.Where(s =>
                    s.fk_id_grado == id_grado.Value);
            }

            var secciones = consulta
                .ToList()
                .OrderBy(s => s.NombreGrado)
                .ThenBy(s => s.NombreSeccion)
                .Select(s => new
                {
                    id = s.id_seccion,

                    nombre =
                        s.NombreGrado +
                        " - " +
                        s.NombreSeccion
                })
                .ToList();

            ViewBag.Secciones = new SelectList(
                secciones,
                "id",
                "nombre",
                seleccionado);
        }

        // ========================================
        // CARGAR ESTUDIANTES
        // ========================================

        private void CargarEstudiantes(
            int id_seccion = 0,
            string seleccionado = null,
            int? id_grado = null,
            bool incluirTodos = false)
        {
            var consulta =
                from relacion in db.SeccionesEstudiantes
                join usuario in db.Usuarios
                    on relacion.fk_id_usuario
                    equals usuario.id_usuario
                join seccion in db.Secciones
                    on relacion.fk_id_seccion
                    equals seccion.id_seccion
                select new
                {
                    Usuario = usuario,
                    Seccion = seccion
                };

            if (!incluirTodos)
            {
                consulta = consulta.Where(x =>
                    x.Usuario.estado);
            }

            if (id_seccion > 0)
            {
                consulta = consulta.Where(x =>
                    x.Seccion.id_seccion ==
                        id_seccion);
            }
            else if (!incluirTodos)
            {
                ViewBag.Estudiantes = new SelectList(
                    new List<SelectListItem>(),
                    "Value",
                    "Text");

                return;
            }

            if (id_grado.HasValue &&
                id_grado.Value > 0)
            {
                consulta = consulta.Where(x =>
                    x.Seccion.fk_id_grado ==
                        id_grado.Value);
            }

            var estudiantes = consulta
                .Select(x => x.Usuario)
                .Distinct()
                .ToList()
                .OrderBy(u => u.apellido1)
                .ThenBy(u => u.apellido2)
                .ThenBy(u => u.nombre)
                .Select(u => new
                {
                    id = u.id_usuario,

                    nombre =
                        NombreCompleto(u) +
                        " - " +
                        u.identificacion
                })
                .ToList();

            ViewBag.Estudiantes = new SelectList(
                estudiantes,
                "id",
                "nombre",
                seleccionado);
        }

        // ========================================
        // RETORNAR FORMULARIO CON ERRORES
        // ========================================

        private ActionResult RetornarFormulario(
            SalidaEstudianteDto modelo)
        {
            try
            {
                CargarSecciones(
                    modelo.fk_id_seccion);

                CargarEstudiantes(
                    modelo.fk_id_seccion,
                    modelo.fk_id_estudiante);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al cargar formulario de salida: {0}", ex);

                ViewBag.Secciones = new SelectList(
                    new List<SelectListItem>(),
                    "Value",
                    "Text");

                ViewBag.Estudiantes = new SelectList(
                    new List<SelectListItem>(),
                    "Value",
                    "Text");

                ModelState.AddModelError(
                    "",
                    "No fue posible cargar los estudiantes y las secciones.");
            }

            return View("Registrar", modelo);
        }

        // ========================================
        // NOMBRE COMPLETO DEL USUARIO
        // ========================================

        private static string NombreCompleto(
            UsuarioEntity usuario)
        {
            if (usuario == null)
            {
                return "Usuario no disponible";
            }

            return string.Join(
                " ",
                new[]
                {
                    usuario.nombre,
                    usuario.apellido1,
                    usuario.apellido2
                }
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x)));
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