using EduNexus.Abstracciones.ModelosParaUI.ExpedienteAcademico;
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
    // REXP-02-007 Consultar el historial académico completo del estudiante durante todos los años lectivos.
    // REXP-02-008 Registrar el tipo de adecuación y su descripción (clasificar casos de adecuación).
    // REXP-02-009 Registrar a los estudiantes que requieren algún tipo de ayuda y gestionar los apoyos.
    // Los estudiantes son los usuarios con rol "Estudiante".
    [AutorizarAdministrador]
    public class ExpedienteAcademicoController : Controller
    {
        private const string Activa = "Activa";
        private const string Finalizada = "Finalizada";
        private const string EventoClasificacionAdecuacion = "Clasificación de adecuación";
        private const string EventoClasificacionAyuda = "Clasificación de ayuda";
        private const string EventoReclasificacionAyuda = "Reclasificación de ayuda";

        // =========================================================
        // Estudiantes y expediente
        // =========================================================

        // GET: ExpedienteAcademico/ListadoDeEstudiantes
        public ActionResult ListadoDeEstudiantes()
        {
            using (var db = new EduNexusDbContext())
            {
                RolEntity rolEstudiante = BuscarRolEstudiante(db);
                ViewBag.SinRolEstudiante = rolEstudiante == null;
                if (rolEstudiante == null)
                {
                    return View(new List<EstudianteResumenDto>());
                }

                List<UsuarioEntity> usuarios = db.Usuarios
                    .Where(u => u.rol == rolEstudiante.id_rol)
                    .OrderBy(u => u.nombre).ThenBy(u => u.apellido1)
                    .ToList();

                var adecuaciones = db.AdecuacionesEstudiantes.Where(a => a.estado == Activa)
                    .GroupBy(a => a.fk_id_usuario).Select(g => new { g.Key, n = g.Count() }).ToList()
                    .ToDictionary(x => x.Key, x => x.n);
                var ayudas = db.AyudasEstudiantes.Where(a => a.estado == Activa)
                    .GroupBy(a => a.fk_id_usuario).Select(g => new { g.Key, n = g.Count() }).ToList()
                    .ToDictionary(x => x.Key, x => x.n);
                Dictionary<string, string> secciones = SeccionActualPorEstudiante(db);

                var lista = usuarios.Select(u => new EstudianteResumenDto
                {
                    id_usuario = u.id_usuario,
                    identificacion = u.identificacion,
                    nombre_completo = NombreCompleto(u),
                    correo = u.email,
                    estado = u.estado ? "Activo" : "Inactivo",
                    seccion_actual = secciones.ContainsKey(u.id_usuario) ? secciones[u.id_usuario] : "Sin sección",
                    adecuaciones_activas = adecuaciones.ContainsKey(u.id_usuario) ? adecuaciones[u.id_usuario] : 0,
                    ayudas_activas = ayudas.ContainsKey(u.id_usuario) ? ayudas[u.id_usuario] : 0
                }).ToList();

                return View(lista);
            }
        }

        // GET: ExpedienteAcademico/Expediente/{id}
        public ActionResult Expediente(string id)
        {
            using (var db = new EduNexusDbContext())
            {
                UsuarioEntity estudiante = BuscarEstudiante(db, id);
                if (estudiante == null)
                {
                    return HttpNotFound();
                }

                ViewBag.Estudiante = AResumen(db, estudiante);
                ViewBag.Adecuaciones = CasosDeAdecuacion(db, estudiante.id_usuario);
                ViewBag.Ayudas = CasosDeAyuda(db, estudiante.id_usuario);
                return View();
            }
        }

        // =========================================================
        // REXP-02-007 Historial académico
        // =========================================================

        // GET: ExpedienteAcademico/HistorialAcademico/{id}?anio=2026
        public ActionResult HistorialAcademico(string id, int? anio)
        {
            using (var db = new EduNexusDbContext())
            {
                UsuarioEntity estudiante = BuscarEstudiante(db, id);
                if (estudiante == null)
                {
                    return HttpNotFound();
                }

                List<HistorialAnioDto> historial = ObtenerHistorial(db, estudiante.id_usuario);

                ViewBag.Estudiante = AResumen(db, estudiante);
                // Escenario 2: estudiante sin historial previo
                ViewBag.SinHistorial = !historial.Any();
                ViewBag.Anios = historial.Select(h => h.anio).Distinct().OrderByDescending(a => a).ToList();
                ViewBag.AnioSeleccionado = anio;

                // Escenario 3: filtrado del historial por año lectivo
                if (anio.HasValue)
                {
                    historial = historial.Where(h => h.anio == anio.Value).ToList();
                }

                // Escenario 1: progreso organizado por año lectivo
                return View(historial);
            }
        }

        // =========================================================
        // REXP-02-008 Tipos de adecuación (catálogo)
        // =========================================================

        // GET: ExpedienteAcademico/TiposDeAdecuacion
        public ActionResult TiposDeAdecuacion()
        {
            using (var db = new EduNexusDbContext())
            {
                var activos = db.AdecuacionesEstudiantes
                    .Where(a => a.estado == Activa && a.fk_id_adecuacion != null)
                    .GroupBy(a => a.fk_id_adecuacion.Value).Select(g => new { g.Key, n = g.Count() }).ToList()
                    .ToDictionary(x => x.Key, x => x.n);

                var tipos = db.Adecuaciones.OrderBy(a => a.nombre).ToList()
                    .Select(a => new TipoAdecuacionDto
                    {
                        id_adecuacion = a.id_adecuacion,
                        nombre = a.nombre,
                        descripcion = a.descripcion,
                        casos_activos = activos.ContainsKey(a.id_adecuacion) ? activos[a.id_adecuacion] : 0
                    }).ToList();
                return View(tipos);
            }
        }

        // GET: ExpedienteAcademico/CrearTipoAdecuacion
        public ActionResult CrearTipoAdecuacion()
        {
            return View(new TipoAdecuacionDto());
        }

        // POST: ExpedienteAcademico/CrearTipoAdecuacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearTipoAdecuacion(TipoAdecuacionDto modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            using (var db = new EduNexusDbContext())
            {
                string nombre = modelo.nombre.Trim();
                bool existe = db.Adecuaciones.Select(a => a.nombre).ToList()
                    .Any(n => string.Equals(n.Trim(), nombre, StringComparison.OrdinalIgnoreCase));
                if (existe)
                {
                    ModelState.AddModelError("nombre", "Ya existe un tipo de adecuación con ese nombre.");
                    return View(modelo);
                }

                db.Adecuaciones.Add(new AdecuacionEntity
                {
                    nombre = nombre,
                    descripcion = string.IsNullOrWhiteSpace(modelo.descripcion) ? null : modelo.descripcion.Trim()
                });
                db.SaveChanges();
            }

            TempData["Mensaje"] = "El tipo de adecuación se registró correctamente.";
            return RedirectToAction("TiposDeAdecuacion");
        }

        // =========================================================
        // REXP-02-008 Casos de adecuación del estudiante
        // =========================================================

        // GET: ExpedienteAcademico/RegistrarAdecuacion/{id del estudiante}
        public ActionResult RegistrarAdecuacion(string id)
        {
            using (var db = new EduNexusDbContext())
            {
                UsuarioEntity estudiante = BuscarEstudiante(db, id);
                if (estudiante == null)
                {
                    return HttpNotFound();
                }
                CargarTiposAdecuacion(db, null);
                return View(new CasoApoyoDto { id_usuario = estudiante.id_usuario, estudiante = NombreCompleto(estudiante) });
            }
        }

        // POST: ExpedienteAcademico/RegistrarAdecuacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarAdecuacion(CasoApoyoDto modelo)
        {
            using (var db = new EduNexusDbContext())
            {
                UsuarioEntity estudiante = BuscarEstudiante(db, modelo.id_usuario);
                if (estudiante == null)
                {
                    return HttpNotFound();
                }
                modelo.estudiante = NombreCompleto(estudiante);

                // Escenario 2: el estudiante ya tiene una adecuación activa del mismo tipo
                if (modelo.id_tipo.HasValue && TieneAdecuacionActiva(db, estudiante.id_usuario, modelo.id_tipo.Value, 0))
                {
                    ModelState.AddModelError("id_tipo", "El estudiante ya tiene asignada una adecuación activa de este tipo. No se puede duplicar.");
                }

                if (!ModelState.IsValid)
                {
                    CargarTiposAdecuacion(db, modelo.id_tipo);
                    return View(modelo);
                }

                // Si no se elige tipo, el caso queda registrado "sin clasificar"
                db.AdecuacionesEstudiantes.Add(new AdecuacionEstudianteEntity
                {
                    fk_id_usuario = estudiante.id_usuario,
                    fk_id_adecuacion = modelo.id_tipo,
                    descripcion = modelo.descripcion.Trim(),
                    estado = Activa,
                    fecha_registro = DateTime.Now
                });
                db.SaveChanges();
            }

            TempData["Mensaje"] = modelo.id_tipo.HasValue
                ? "La adecuación se registró y quedó reflejada en el expediente del estudiante."
                : "El caso de adecuación se registró sin clasificar. Puede clasificarlo desde el expediente.";
            return RedirectToAction("Expediente", new { id = modelo.id_usuario });
        }

        // GET: ExpedienteAcademico/ClasificarAdecuacion/{id del caso}
        public ActionResult ClasificarAdecuacion(int? id)
        {
            using (var db = new EduNexusDbContext())
            {
                CasoApoyoDto caso = CasosDeAdecuacion(db, null, id).FirstOrDefault();
                if (caso == null)
                {
                    return HttpNotFound();
                }
                CargarTiposAdecuacion(db, caso.id_tipo);
                return View(caso);
            }
        }

        // POST: ExpedienteAcademico/ClasificarAdecuacion/{id del caso}
        // Escenario 1: registro exitoso del tipo de adecuación de un caso sin clasificar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClasificarAdecuacion(int id, int? id_tipo)
        {
            using (var db = new EduNexusDbContext())
            {
                AdecuacionEstudianteEntity caso = db.AdecuacionesEstudiantes.FirstOrDefault(a => a.id_adecuacion_estudiante == id);
                if (caso == null)
                {
                    return HttpNotFound();
                }

                CasoApoyoDto vista = CasosDeAdecuacion(db, null, id).First();
                if (!id_tipo.HasValue)
                {
                    ModelState.AddModelError("id_tipo", "Seleccione un tipo de adecuación.");
                }
                // Escenario 2: intento de asignar una adecuación duplicada
                else if (TieneAdecuacionActiva(db, caso.fk_id_usuario, id_tipo.Value, caso.id_adecuacion_estudiante))
                {
                    ModelState.AddModelError("id_tipo", "El estudiante ya tiene asignada una adecuación activa de este tipo. No se puede duplicar.");
                }

                if (!ModelState.IsValid)
                {
                    CargarTiposAdecuacion(db, id_tipo);
                    return View(vista);
                }

                string anterior = NombreTipoAdecuacion(db, caso.fk_id_adecuacion);
                caso.fk_id_adecuacion = id_tipo;
                RegistrarBitacora(db, EventoClasificacionAdecuacion, "Adecuación #" + caso.id_adecuacion_estudiante + ":",
                    anterior, NombreTipoAdecuacion(db, id_tipo));
                db.SaveChanges();

                TempData["Mensaje"] = "La adecuación se clasificó y quedó reflejada en el expediente del estudiante.";
                return RedirectToAction("Expediente", new { id = caso.fk_id_usuario });
            }
        }

        // POST: ExpedienteAcademico/FinalizarAdecuacion/{id del caso}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FinalizarAdecuacion(int id)
        {
            using (var db = new EduNexusDbContext())
            {
                AdecuacionEstudianteEntity caso = db.AdecuacionesEstudiantes.FirstOrDefault(a => a.id_adecuacion_estudiante == id);
                if (caso == null)
                {
                    return HttpNotFound();
                }
                caso.estado = Finalizada;
                db.SaveChanges();
                TempData["Mensaje"] = "La adecuación se marcó como finalizada.";
                return RedirectToAction("Expediente", new { id = caso.fk_id_usuario });
            }
        }

        // =========================================================
        // REXP-02-009 Casos de ayuda del estudiante
        // =========================================================

        // GET: ExpedienteAcademico/RegistrarAyuda/{id del estudiante}
        public ActionResult RegistrarAyuda(string id)
        {
            using (var db = new EduNexusDbContext())
            {
                UsuarioEntity estudiante = BuscarEstudiante(db, id);
                if (estudiante == null)
                {
                    return HttpNotFound();
                }
                CargarTiposAyuda(db, null);
                return View(new CasoApoyoDto { id_usuario = estudiante.id_usuario, estudiante = NombreCompleto(estudiante) });
            }
        }

        // POST: ExpedienteAcademico/RegistrarAyuda
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarAyuda(CasoApoyoDto modelo)
        {
            using (var db = new EduNexusDbContext())
            {
                UsuarioEntity estudiante = BuscarEstudiante(db, modelo.id_usuario);
                if (estudiante == null)
                {
                    return HttpNotFound();
                }
                modelo.estudiante = NombreCompleto(estudiante);

                // Escenario 2: intento de asignar un beneficio duplicado
                if (modelo.id_tipo.HasValue && TieneAyudaActiva(db, estudiante.id_usuario, modelo.id_tipo.Value, 0))
                {
                    ModelState.AddModelError("id_tipo", "Posible duplicación: el estudiante ya tiene un beneficio activo de este tipo. El registro fue rechazado.");
                }

                if (!ModelState.IsValid)
                {
                    CargarTiposAyuda(db, modelo.id_tipo);
                    return View(modelo);
                }

                db.AyudasEstudiantes.Add(new AyudaEstudianteEntity
                {
                    fk_id_usuario = estudiante.id_usuario,
                    fk_id_tipo_ayuda = modelo.id_tipo,
                    descripcion = modelo.descripcion.Trim(),
                    estado = Activa,
                    fecha_registro = DateTime.Now
                });
                db.SaveChanges();
            }

            TempData["Mensaje"] = modelo.id_tipo.HasValue
                ? "La ayuda se registró y quedó reflejada en el expediente del estudiante."
                : "El caso de ayuda se registró sin clasificar. Puede clasificarlo desde el expediente.";
            return RedirectToAction("Expediente", new { id = modelo.id_usuario });
        }

        // GET: ExpedienteAcademico/ClasificarAyuda/{id del caso}
        public ActionResult ClasificarAyuda(int? id)
        {
            using (var db = new EduNexusDbContext())
            {
                CasoApoyoDto caso = CasosDeAyuda(db, null, id).FirstOrDefault();
                if (caso == null)
                {
                    return HttpNotFound();
                }
                CargarTiposAyuda(db, caso.id_tipo);
                ViewBag.Cambios = CambiosDeAyuda(db, caso.id_caso);
                return View(caso);
            }
        }

        // POST: ExpedienteAcademico/ClasificarAyuda/{id del caso}
        // Escenario 1: clasificación de un caso sin clasificar
        // Escenario 3: reclasificación de un caso existente (se conserva el registro del cambio)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClasificarAyuda(int id, int? id_tipo)
        {
            using (var db = new EduNexusDbContext())
            {
                AyudaEstudianteEntity caso = db.AyudasEstudiantes.FirstOrDefault(a => a.id_ayuda_estudiante == id);
                if (caso == null)
                {
                    return HttpNotFound();
                }

                CasoApoyoDto vista = CasosDeAyuda(db, null, id).First();
                if (!id_tipo.HasValue)
                {
                    ModelState.AddModelError("id_tipo", "Seleccione un tipo de ayuda.");
                }
                else if (caso.fk_id_tipo_ayuda == id_tipo)
                {
                    ModelState.AddModelError("id_tipo", "El caso ya tiene asignado ese tipo de ayuda.");
                }
                // Escenario 2: el estudiante ya tiene otro beneficio activo igual
                else if (TieneAyudaActiva(db, caso.fk_id_usuario, id_tipo.Value, caso.id_ayuda_estudiante))
                {
                    ModelState.AddModelError("id_tipo", "Posible duplicación: el estudiante ya tiene un beneficio activo de este tipo. El cambio fue rechazado.");
                }

                if (!ModelState.IsValid)
                {
                    CargarTiposAyuda(db, id_tipo);
                    ViewBag.Cambios = CambiosDeAyuda(db, id);
                    return View(vista);
                }

                bool esReclasificacion = caso.fk_id_tipo_ayuda.HasValue;
                string anterior = NombreTipoAyuda(db, caso.fk_id_tipo_ayuda);
                caso.fk_id_tipo_ayuda = id_tipo;
                RegistrarBitacora(db, esReclasificacion ? EventoReclasificacionAyuda : EventoClasificacionAyuda,
                    "Ayuda #" + caso.id_ayuda_estudiante + ":", anterior, NombreTipoAyuda(db, id_tipo));
                db.SaveChanges();

                TempData["Mensaje"] = esReclasificacion
                    ? "El tipo de ayuda se actualizó y el cambio quedó registrado."
                    : "La ayuda se clasificó y quedó reflejada en el expediente del estudiante.";
                return RedirectToAction("Expediente", new { id = caso.fk_id_usuario });
            }
        }

        // POST: ExpedienteAcademico/FinalizarAyuda/{id del caso}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FinalizarAyuda(int id)
        {
            using (var db = new EduNexusDbContext())
            {
                AyudaEstudianteEntity caso = db.AyudasEstudiantes.FirstOrDefault(a => a.id_ayuda_estudiante == id);
                if (caso == null)
                {
                    return HttpNotFound();
                }
                caso.estado = Finalizada;
                db.SaveChanges();
                TempData["Mensaje"] = "La ayuda se marcó como finalizada.";
                return RedirectToAction("Expediente", new { id = caso.fk_id_usuario });
            }
        }

        // =========================================================
        // Métodos auxiliares
        // =========================================================

        private static RolEntity BuscarRolEstudiante(EduNexusDbContext db)
        {
            return db.Roles.ToList().FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.nombre) &&
                (r.nombre.Trim().Equals("Estudiante", StringComparison.OrdinalIgnoreCase) ||
                 r.nombre.Trim().Equals("Estudiantes", StringComparison.OrdinalIgnoreCase)));
        }

        private static UsuarioEntity BuscarEstudiante(EduNexusDbContext db, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }
            RolEntity rol = BuscarRolEstudiante(db);
            if (rol == null)
            {
                return null;
            }
            return db.Usuarios.FirstOrDefault(u => u.id_usuario == id && u.rol == rol.id_rol);
        }

        private static string NombreCompleto(UsuarioEntity u)
        {
            return ((u.nombre ?? "") + " " + (u.apellido1 ?? "") + " " + (u.apellido2 ?? "")).Trim();
        }

        private static EstudianteResumenDto AResumen(EduNexusDbContext db, UsuarioEntity u)
        {
            Dictionary<string, string> secciones = SeccionActualPorEstudiante(db, u.id_usuario);
            return new EstudianteResumenDto
            {
                id_usuario = u.id_usuario,
                identificacion = u.identificacion,
                nombre_completo = NombreCompleto(u),
                correo = u.email,
                estado = u.estado ? "Activo" : "Inactivo",
                seccion_actual = secciones.ContainsKey(u.id_usuario) ? secciones[u.id_usuario] : "Sin sección",
                adecuaciones_activas = db.AdecuacionesEstudiantes.Count(a => a.fk_id_usuario == u.id_usuario && a.estado == Activa),
                ayudas_activas = db.AyudasEstudiantes.Count(a => a.fk_id_usuario == u.id_usuario && a.estado == Activa)
            };
        }

        // Sección del año lectivo más reciente de cada estudiante: "Primer Grado 1-A (2026)"
        private static Dictionary<string, string> SeccionActualPorEstudiante(EduNexusDbContext db, string idUsuario = null)
        {
            var consulta = from se in db.SeccionesEstudiantes
                           join s in db.Secciones on se.fk_id_seccion equals s.id_seccion
                           join g in db.Grados on s.fk_id_grado equals g.id_grado into gs
                           from g in gs.DefaultIfEmpty()
                           join c in db.Calendarios on s.fk_id_calendario equals c.id_calendario into cs
                           from c in cs.DefaultIfEmpty()
                           select new
                           {
                               se.fk_id_usuario,
                               s.nombre,
                               grado = g != null ? g.nombre : "",
                               inicio = c != null ? (DateTime?)c.fecha_inicio : null
                           };
            if (idUsuario != null)
            {
                consulta = consulta.Where(x => x.fk_id_usuario == idUsuario);
            }

            return consulta.ToList()
                .GroupBy(x => x.fk_id_usuario)
                .ToDictionary(gr => gr.Key, gr =>
                {
                    var ultima = gr.OrderByDescending(x => x.inicio).First();
                    return (ultima.grado + " " + ultima.nombre).Trim()
                           + (ultima.inicio.HasValue ? " (" + ultima.inicio.Value.Year + ")" : "");
                });
        }

        // REXP-02-007: secciones del estudiante por año lectivo con sus calificaciones
        private static List<HistorialAnioDto> ObtenerHistorial(EduNexusDbContext db, string idUsuario)
        {
            List<int> idsSecciones = db.SeccionesEstudiantes.Where(e => e.fk_id_usuario == idUsuario)
                .Select(e => e.fk_id_seccion)
                .ToList()
                .Union(db.Asignaciones.Where(a => a.fk_id_usuario == idUsuario).Select(a => a.fk_id_seccion).ToList())
                .Distinct()
                .ToList();

            if (!idsSecciones.Any())
            {
                return new List<HistorialAnioDto>();
            }

            var secciones = (from s in db.Secciones
                             where idsSecciones.Contains(s.id_seccion)
                             join g in db.Grados on s.fk_id_grado equals g.id_grado into gs
                             from g in gs.DefaultIfEmpty()
                             join c in db.Calendarios on s.fk_id_calendario equals c.id_calendario into cs
                             from c in cs.DefaultIfEmpty()
                             select new
                             {
                                 s.id_seccion,
                                 s.nombre,
                                 grado = g != null ? g.nombre : "",
                                 periodo = c != null ? c.nombre : "",
                                 inicio = c != null ? (DateTime?)c.fecha_inicio : null
                             }).ToList();

            var notas = (from a in db.Asignaciones
                         where a.fk_id_usuario == idUsuario
                         join m in db.Materias on a.fk_id_materia equals (int?)m.id_materia into ms
                         from m in ms.DefaultIfEmpty()
                         select new
                         {
                             a.fk_id_seccion,
                             materia = m != null ? m.nombre : "Sin materia",
                             a.nombre,
                             a.porcentaje_nota,
                             a.calificacion,
                             a.estado
                         }).ToList();

            return secciones
                .Select(s =>
                {
                    var calificaciones = notas.Where(n => n.fk_id_seccion == s.id_seccion)
                        .OrderBy(n => n.materia).ThenBy(n => n.nombre)
                        .Select(n => new CalificacionDto
                        {
                            materia = n.materia,
                            evaluacion = n.nombre,
                            porcentaje = n.porcentaje_nota,
                            calificacion = n.calificacion,
                            estado = n.estado
                        }).ToList();
                    return new HistorialAnioDto
                    {
                        anio = s.inicio.HasValue ? s.inicio.Value.Year : 0,
                        periodo = s.periodo,
                        grado = s.grado,
                        seccion = s.nombre,
                        calificaciones = calificaciones,
                        promedio = calificaciones.Any() ? (decimal?)Math.Round(calificaciones.Average(c => c.calificacion), 2) : null
                    };
                })
                .OrderByDescending(h => h.anio)
                .ToList();
        }

        private static bool TieneAdecuacionActiva(EduNexusDbContext db, string idUsuario, int idTipo, int idExcluido)
        {
            return db.AdecuacionesEstudiantes.Any(a => a.fk_id_usuario == idUsuario && a.fk_id_adecuacion == idTipo
                                                       && a.estado == Activa && a.id_adecuacion_estudiante != idExcluido);
        }

        private static bool TieneAyudaActiva(EduNexusDbContext db, string idUsuario, int idTipo, int idExcluido)
        {
            return db.AyudasEstudiantes.Any(a => a.fk_id_usuario == idUsuario && a.fk_id_tipo_ayuda == idTipo
                                                 && a.estado == Activa && a.id_ayuda_estudiante != idExcluido);
        }

        private static string NombreTipoAdecuacion(EduNexusDbContext db, int? id)
        {
            if (!id.HasValue) return "Sin clasificar";
            AdecuacionEntity t = db.Adecuaciones.FirstOrDefault(a => a.id_adecuacion == id.Value);
            return t != null ? t.nombre : "Sin clasificar";
        }

        private static string NombreTipoAyuda(EduNexusDbContext db, int? id)
        {
            if (!id.HasValue) return "Sin clasificar";
            TipoAyudaEntity t = db.TiposAyuda.FirstOrDefault(a => a.id_tipo_ayuda == id.Value);
            return t != null ? t.nombre : "Sin clasificar";
        }

        private static List<CasoApoyoDto> CasosDeAdecuacion(EduNexusDbContext db, string idUsuario, int? idCaso = null)
        {
            var consulta = db.AdecuacionesEstudiantes.AsQueryable();
            if (idUsuario != null) consulta = consulta.Where(a => a.fk_id_usuario == idUsuario);
            if (idCaso.HasValue) consulta = consulta.Where(a => a.id_adecuacion_estudiante == idCaso.Value);

            return (from a in consulta
                    join t in db.Adecuaciones on a.fk_id_adecuacion equals (int?)t.id_adecuacion into ts
                    from t in ts.DefaultIfEmpty()
                    join u in db.Usuarios on a.fk_id_usuario equals u.id_usuario
                    orderby a.estado, a.fecha_registro descending
                    select new { a, tipo = t != null ? t.nombre : null, u }).ToList()
                .Select(x => new CasoApoyoDto
                {
                    id_caso = x.a.id_adecuacion_estudiante,
                    id_usuario = x.a.fk_id_usuario,
                    estudiante = NombreCompleto(x.u),
                    id_tipo = x.a.fk_id_adecuacion,
                    tipo = x.tipo ?? "Sin clasificar",
                    descripcion = x.a.descripcion,
                    estado = x.a.estado,
                    fecha_registro = x.a.fecha_registro
                }).ToList();
        }

        private static List<CasoApoyoDto> CasosDeAyuda(EduNexusDbContext db, string idUsuario, int? idCaso = null)
        {
            var consulta = db.AyudasEstudiantes.AsQueryable();
            if (idUsuario != null) consulta = consulta.Where(a => a.fk_id_usuario == idUsuario);
            if (idCaso.HasValue) consulta = consulta.Where(a => a.id_ayuda_estudiante == idCaso.Value);

            return (from a in consulta
                    join t in db.TiposAyuda on a.fk_id_tipo_ayuda equals (int?)t.id_tipo_ayuda into ts
                    from t in ts.DefaultIfEmpty()
                    join u in db.Usuarios on a.fk_id_usuario equals u.id_usuario
                    orderby a.estado, a.fecha_registro descending
                    select new { a, tipo = t != null ? t.nombre : null, u }).ToList()
                .Select(x => new CasoApoyoDto
                {
                    id_caso = x.a.id_ayuda_estudiante,
                    id_usuario = x.a.fk_id_usuario,
                    estudiante = NombreCompleto(x.u),
                    id_tipo = x.a.fk_id_tipo_ayuda,
                    tipo = x.tipo ?? "Sin clasificar",
                    descripcion = x.a.descripcion,
                    estado = x.a.estado,
                    fecha_registro = x.a.fecha_registro
                }).ToList();
        }

        // REXP-02-009 Escenario 3: registro de cambios de clasificación guardado en la bitácora
        private static List<CambioClasificacionDto> CambiosDeAyuda(EduNexusDbContext db, int idCaso)
        {
            string prefijo = "Ayuda #" + idCaso + ":";
            return (from b in db.Bitacora
                    where (b.tipo_de_evento == EventoClasificacionAyuda || b.tipo_de_evento == EventoReclasificacionAyuda)
                          && b.descripcion_evento.StartsWith(prefijo)
                    join u in db.Usuarios on b.fk_id_usuario equals u.id_usuario into us
                    from u in us.DefaultIfEmpty()
                    orderby b.fecha descending
                    select new { b.fecha, b.datos_anteriores, b.datos_posteriores, u.nombre, u.apellido1 }).ToList()
                .Select(x => new CambioClasificacionDto
                {
                    fecha = x.fecha,
                    usuario = ((x.nombre ?? "") + " " + (x.apellido1 ?? "")).Trim(),
                    tipo_anterior = LeerTipo(x.datos_anteriores),
                    tipo_nuevo = LeerTipo(x.datos_posteriores)
                }).ToList();
        }

        private static string LeerTipo(string json)
        {
            try
            {
                var datos = JsonConvert.DeserializeAnonymousType(json ?? "{}", new { tipo = "" });
                return datos != null && datos.tipo != null ? datos.tipo : "";
            }
            catch (JsonException)
            {
                return "";
            }
        }

        private void RegistrarBitacora(EduNexusDbContext db, string evento, string prefijo, string tipoAnterior, string tipoNuevo)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;
            if (usuario == null)
            {
                return;
            }
            db.Bitacora.Add(new BitacoraEntity
            {
                tipo_de_evento = evento,
                descripcion_evento = prefijo + " " + tipoAnterior + " -> " + tipoNuevo,
                fecha = DateTime.Now,
                datos_anteriores = JsonConvert.SerializeObject(new { tipo = tipoAnterior }),
                datos_posteriores = JsonConvert.SerializeObject(new { tipo = tipoNuevo }),
                fk_id_usuario = usuario.id_usuario
            });
        }

        private void CargarTiposAdecuacion(EduNexusDbContext db, int? seleccionado)
        {
            ViewBag.Tipos = new SelectList(db.Adecuaciones.OrderBy(a => a.nombre).ToList(), "id_adecuacion", "nombre", seleccionado);
        }

        private void CargarTiposAyuda(EduNexusDbContext db, int? seleccionado)
        {
            ViewBag.Tipos = new SelectList(db.TiposAyuda.OrderBy(a => a.nombre).ToList(), "id_tipo_ayuda", "nombre", seleccionado);
        }
    }
}
