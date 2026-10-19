using EduNexus.Abstracciones.ModelosParaUI.Bitacora;
using EduNexus.Abstracciones.ModelosParaUI.Dashboard;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // Dashboard: todos los datos salen de la base de datos (sin datos quemados).
            var model = new DashboardViewModel();

            using (var db = new EduNexusDbContext())
            {
                string idRolEstudiante = db.Roles.Where(r => r.nombre == "Estudiante").Select(r => r.id_rol).FirstOrDefault();
                string idRolDocente = db.Roles.Where(r => r.nombre == "Docente").Select(r => r.id_rol).FirstOrDefault();

                // ---- KPIs ----
                model.TotalEstudiantes = idRolEstudiante == null ? 0 : db.Usuarios.Count(u => u.rol == idRolEstudiante && u.estado);
                model.TotalDocentes = idRolDocente == null ? 0 : db.Usuarios.Count(u => u.rol == idRolDocente && u.estado);
                model.TotalSecciones = db.Secciones.Count();
                model.TotalMaterias = db.Materias.Count();

                var calendarios = db.Calendarios.ToList().ToDictionary(c => c.id_calendario);
                var secciones = db.Secciones.ToList();
                var asignacionesSeccion = db.SeccionesEstudiantes.ToList();

                // ---- Gráfica: matrícula por año (estudiantes asignados a secciones por año lectivo) ----
                var matriculaPorAnio = asignacionesSeccion
                    .Join(secciones, se => se.fk_id_seccion, s => s.id_seccion, (se, s) => new { se.fk_id_usuario, s.fk_id_calendario })
                    .Where(x => calendarios.ContainsKey(x.fk_id_calendario))
                    .GroupBy(x => calendarios[x.fk_id_calendario].fecha_inicio.Year)
                    .Select(g => new { Anio = g.Key, Total = g.Select(x => x.fk_id_usuario).Distinct().Count() })
                    .OrderBy(x => x.Anio)
                    .ToList();
                model.AniosMatricula = matriculaPorAnio.Select(x => x.Anio.ToString()).ToList();
                model.MatriculaPorAnio = matriculaPorAnio.Select(x => x.Total).ToList();

                // ---- Gráfica: estudiantes por sección (secciones del año lectivo más reciente) ----
                int? anioActual = secciones
                    .Where(s => calendarios.ContainsKey(s.fk_id_calendario))
                    .Select(s => (int?)calendarios[s.fk_id_calendario].fecha_inicio.Year)
                    .DefaultIfEmpty(null)
                    .Max();
                var grados = db.Grados.ToList().ToDictionary(g => g.id_grado);
                var seccionesAnio = secciones
                    .Where(s => anioActual == null || (calendarios.ContainsKey(s.fk_id_calendario) && calendarios[s.fk_id_calendario].fecha_inicio.Year == anioActual))
                    .OrderBy(s => grados.ContainsKey(s.fk_id_grado) ? grados[s.fk_id_grado].nivel : 0)
                    .ThenBy(s => s.nombre)
                    .ToList();
                foreach (var s in seccionesAnio)
                {
                    model.NombresSecciones.Add(s.nombre);
                    model.EstudiantesPorSeccion.Add(asignacionesSeccion.Count(se => se.fk_id_seccion == s.id_seccion));
                }

                // ---- Gráfica: docentes por materia (docentes distintos que imparten cada materia) ----
                var docentesPorMateria = db.Materias.ToList()
                    .GroupBy(m => (m.nombre ?? "").Trim())
                    .Select(g => new { Materia = g.Key, Total = g.Where(m => !string.IsNullOrEmpty(m.profesor)).Select(m => m.profesor).Distinct().Count() })
                    .OrderBy(x => x.Materia)
                    .ToList();
                model.NombresMaterias = docentesPorMateria.Select(x => x.Materia).ToList();
                model.DocentesPorMateria = docentesPorMateria.Select(x => x.Total).ToList();

                // ---- Actividad reciente (bitácora) ----
                model.ActividadReciente = ObtenerBitacora(db, 5);

                // ---- Próximas clases de hoy (horarios del día que aún no terminan) ----
                string hoy = NormalizarDia(DateTime.Now.ToString("dddd", new System.Globalization.CultureInfo("es-ES")));
                TimeSpan ahora = DateTime.Now.TimeOfDay;
                var materias = db.Materias.ToList().ToDictionary(m => m.id_materia);
                var usuarios = db.Usuarios.ToList().ToDictionary(u => u.id_usuario);
                var seccionesPorId = secciones.ToDictionary(s => s.id_seccion);

                model.ProximasClases = db.Horarios.ToList()
                    .Where(h => NormalizarDia(h.dia_semana) == hoy && h.hora_fin > ahora)
                    .OrderBy(h => h.hora_inicio)
                    .Take(8)
                    .Select(h =>
                    {
                        MateriaEntity materia = materias.ContainsKey(h.fk_materia) ? materias[h.fk_materia] : null;
                        UsuarioEntity docente = (materia != null && materia.profesor != null && usuarios.ContainsKey(materia.profesor)) ? usuarios[materia.profesor] : null;
                        return new ProximaClaseDto
                        {
                            Materia = materia != null ? materia.nombre : "—",
                            Docente = docente != null ? docente.nombre + " " + docente.apellido1 : "Sin docente",
                            Seccion = seccionesPorId.ContainsKey(h.fk_seccion) ? seccionesPorId[h.fk_seccion].nombre : "—",
                            Hora = DateTime.Today.Add(h.hora_inicio).ToString("HH:mm") + " - " + DateTime.Today.Add(h.hora_fin).ToString("HH:mm"),
                            Dia = DateTime.Now.DayOfWeek
                        };
                    })
                    .ToList();
            }

            return View(model);
        }

        // Lee la bitácora real (más reciente primero) con el nombre del usuario que hizo el cambio.
        private static List<BitacoraDto> ObtenerBitacora(EduNexusDbContext db, int? cantidad)
        {
            IQueryable<BitacoraEntity> consulta = db.Bitacora.OrderByDescending(b => b.fecha).ThenByDescending(b => b.id_evento);
            if (cantidad.HasValue)
            {
                consulta = consulta.Take(cantidad.Value);
            }
            var eventos = consulta.ToList();
            var ids = eventos.Select(b => b.fk_id_usuario).Distinct().ToList();
            var usuarios = db.Usuarios.Where(u => ids.Contains(u.id_usuario)).ToList().ToDictionary(u => u.id_usuario);

            return eventos.Select(b => new BitacoraDto
            {
                id_evento = b.id_evento,
                tipoDeEvento = b.tipo_de_evento,
                descripcionEvento = b.descripcion_evento,
                fecha = b.fecha,
                datosAnteriores = b.datos_anteriores,
                datosPosteriores = b.datos_posteriores,
                Usuario = (b.fk_id_usuario != null && usuarios.ContainsKey(b.fk_id_usuario))
                    ? usuarios[b.fk_id_usuario].nombre + " " + usuarios[b.fk_id_usuario].apellido1
                    : b.fk_id_usuario
            }).ToList();
        }

        private static string NormalizarDia(string dia)
        {
            if (string.IsNullOrWhiteSpace(dia)) return "";
            return dia.Trim().ToLowerInvariant()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");
        }

        [AllowAnonymous]
        public ActionResult ProbarConexion()
        {
            try
            {
                using (var db = new EduNexusDbContext())
                {
                    db.Database.Connection.Open();
                    return Content("Conexión exitosa. Usuarios en la BD: " + db.Usuarios.Count());
                }
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.ToString());
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            // Si ya tiene sesión iniciada, va directo al inicio
            if (Session[AutorizarAdministradorAttribute.ClaveSesion] != null)
            {
                return RedirectToAction("Index");
            }
            return View(new EduNexus.UI.Models.LoginModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(EduNexus.UI.Models.LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var db = new EduNexusDbContext())
            {
                string correo = model.Correo.Trim();
                UsuarioEntity usuario = db.Usuarios.Where(u => u.email == correo).ToList()
                    .FirstOrDefault(u => u.email.Trim().Equals(correo, StringComparison.OrdinalIgnoreCase));

                // Criterio: credenciales inválidas (correo no registrado o contraseña incorrecta).
                if (usuario == null || !ContrasennaHasher.Verificar(model.Contrasenna, usuario.password_hash))
                {
                    ModelState.AddModelError("", "El correo o la contraseña son incorrectos.");
                    return View(model);
                }

                // Criterio RSEG-01-003 #2: "Intento de inicio de sesión de un usuario bloqueado".
                if (!usuario.estado)
                {
                    ModelState.AddModelError("", "Este usuario se encuentra bloqueado o desactivado. Contacte al administrador del sistema.");
                    return View(model);
                }

                RolEntity rol = db.Roles.FirstOrDefault(r => r.id_rol == usuario.rol);
                Session[AutorizarAdministradorAttribute.ClaveSesion] =
                    UsuariosController.AUsuarioDto(usuario, rol != null ? rol.nombre : "");
            }

            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult CerrarSesion()
        {
            Session.Remove(AutorizarAdministradorAttribute.ClaveSesion);
            Session.Abandon();
            return RedirectToAction("Login");
        }

        public ActionResult Bitacora()
        {
            using (var db = new EduNexusDbContext())
            {
                return View(ObtenerBitacora(db, null));
            }
        }
    }
}