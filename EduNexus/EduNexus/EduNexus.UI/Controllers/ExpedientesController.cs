using EduNexus.Abstracciones.ModelosParaUI.ExpedienteAcademico;
using EduNexus.Abstracciones.ModelosParaUI.Expedientes;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // REXP-03-001 / REXP-03-003 / REXP-03-004 / REXP-03-006
    // Expediente estudiantil: tabla expediente_estudiantes ligada a la
    // prematrícula (datos personales) y al usuario estudiante (correo).
    // Expediente unificado del estudiante (estudiantil + académico):
    //   listado:    Expedientes            (Expedientes/Index)
    //   expediente: Expedientes/Expediente/{id_usuario}?tab=datos|medica|documentos|historial|apoyos
    [AutorizarAdministrador]
    public class ExpedientesController : Controller
    {
        private const string EstadoAprobada = "Aprobada";

        private readonly EduNexusDbContext db = new EduNexusDbContext();

        // Campos del DTO que vienen de la prematrícula o que no se editan:
        // no se validan al crear el expediente.
        private static readonly string[] CamposNoEditablesAlCrear =
        {
            "id_expediente", "id_usuario", "identificacion", "nombre",
            "apellido1", "apellido2", "genero", "grado", "nombre_grado",
            "telefono", "fecha_nacimiento", "tiene_adecuacion",
            "tiene_discapacidad", "direccion"
        };

        private static readonly string[] CamposEditables =
        {
            "id_expediente", "nombre", "apellido1", "apellido2", "genero",
            "grupo_sanguineo", "alergias", "correo", "contactoEmergencia"
        };

        private static readonly string[] Pestanas =
        {
            "datos", "medica", "documentos", "historial", "apoyos"
        };

        // =========================================
        // LISTADO ÚNICO DE ESTUDIANTES (rol Estudiante)
        // con su expediente, sección actual y apoyos activos
        // =========================================

        // GET: Expedientes
        public ActionResult Index()
        {
            string idRol = ValidacionExpediente.IdRolEstudiante(db);
            ViewBag.SinRolEstudiante = idRol == null;
            ViewBag.ExpedientesSinEstudiante = 0;

            if (idRol == null)
            {
                return View("ListadoDeExpedientes", new List<EstudianteResumenDto>());
            }

            List<UsuarioEntity> usuarios = db.Usuarios
                .Where(u => u.rol == idRol)
                .OrderBy(u => u.nombre).ThenBy(u => u.apellido1)
                .ToList();

            Dictionary<string, int> adecuaciones = db.AdecuacionesEstudiantes
                .Where(a => a.estado == "Activa" && a.fk_id_usuario != null)
                .GroupBy(a => a.fk_id_usuario).Select(g => new { g.Key, n = g.Count() }).ToList()
                .ToDictionary(x => x.Key, x => x.n);

            Dictionary<string, int> ayudas = db.AyudasEstudiantes
                .Where(a => a.estado == "Activa" && a.fk_id_usuario != null)
                .GroupBy(a => a.fk_id_usuario).Select(g => new { g.Key, n = g.Count() }).ToList()
                .ToDictionary(x => x.Key, x => x.n);

            Dictionary<int, int> documentos = db.DocumentosExpediente
                .GroupBy(d => d.fk_id_expediente).Select(g => new { g.Key, n = g.Count() }).ToList()
                .ToDictionary(x => x.Key, x => x.n);

            Dictionary<string, string> secciones = ExpedienteAcademicoController.SeccionActualPorEstudiante(db);

            Dictionary<int, string> grados = db.Grados.ToList()
                .ToDictionary(g => g.id_grado, g => g.nombre);

            List<ExpedienteEntity> expedientes = db.Expedientes.ToList();
            List<PrematriculaEntity> prematriculas = db.Prematriculas.ToList();

            // Las cédulas se comparan normalizadas (datos viejos sin guiones).
            Func<string, string> clave = c => Cedula.Normalizar(c) ?? (c ?? "").Trim();

            Dictionary<string, List<PrematriculaEntity>> prematriculasPorCedula = prematriculas
                .Where(p => !string.IsNullOrWhiteSpace(p.identificacion))
                .GroupBy(p => clave(p.identificacion))
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<int, PrematriculaEntity> prematriculaPorId = prematriculas
                .GroupBy(p => p.id_prematricula)
                .ToDictionary(g => g.Key, g => g.First());

            Dictionary<string, ExpedienteEntity> expedientePorUsuario = expedientes
                .Where(e => !string.IsNullOrEmpty(e.fk_id_usuario))
                .GroupBy(e => e.fk_id_usuario)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.id_expediente).First());

            // Expedientes viejos sin usuario: se emparejan por la cédula de su matrícula.
            Dictionary<string, ExpedienteEntity> expedienteSinUsuarioPorCedula = expedientes
                .Where(e => string.IsNullOrEmpty(e.fk_id_usuario) && prematriculaPorId.ContainsKey(e.fk_id_matricula))
                .GroupBy(e => clave(prematriculaPorId[e.fk_id_matricula].identificacion))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.id_expediente).First());

            List<EstudianteResumenDto> lista = usuarios.Select(u =>
            {
                string cedula = clave(u.identificacion);

                List<PrematriculaEntity> suyas = cedula.Length > 0 && prematriculasPorCedula.ContainsKey(cedula)
                    ? prematriculasPorCedula[cedula]
                    : new List<PrematriculaEntity>();

                ExpedienteEntity expediente = expedientePorUsuario.ContainsKey(u.id_usuario)
                    ? expedientePorUsuario[u.id_usuario]
                    : (cedula.Length > 0 && expedienteSinUsuarioPorCedula.ContainsKey(cedula)
                        ? expedienteSinUsuarioPorCedula[cedula]
                        : null);

                PrematriculaEntity aprobada = UltimaAprobada(suyas);

                return new EstudianteResumenDto
                {
                    id_usuario = u.id_usuario,
                    identificacion = Cedula.Normalizar(u.identificacion) ?? u.identificacion,
                    nombre_completo = ((u.nombre ?? "") + " " + (u.apellido1 ?? "") + " " + (u.apellido2 ?? "")).Trim(),
                    iniciales = ValidacionExpediente.Iniciales(u.nombre, u.apellido1),
                    correo = u.email,
                    estado = u.estado ? "Activo" : "Inactivo",
                    seccion_actual = secciones.ContainsKey(u.id_usuario)
                        ? secciones[u.id_usuario]
                        : GradoDePrematricula(aprobada, grados),
                    adecuaciones_activas = adecuaciones.ContainsKey(u.id_usuario) ? adecuaciones[u.id_usuario] : 0,
                    ayudas_activas = ayudas.ContainsKey(u.id_usuario) ? ayudas[u.id_usuario] : 0,
                    id_expediente = expediente != null ? (int?)expediente.id_expediente : null,
                    total_documentos = expediente != null && documentos.ContainsKey(expediente.id_expediente)
                        ? documentos[expediente.id_expediente]
                        : 0,
                    id_matricula_aprobada = aprobada != null ? (int?)aprobada.id_prematricula : null
                };
            }).ToList();

            var expedientesListados = new HashSet<int>(lista
                .Where(x => x.id_expediente.HasValue)
                .Select(x => x.id_expediente.Value));

            ViewBag.ExpedientesSinEstudiante = expedientes.Count(e => !expedientesListados.Contains(e.id_expediente));

            return View("ListadoDeExpedientes", lista);
        }

        // =========================================
        // EXPEDIENTE UNIFICADO DEL ESTUDIANTE
        // GET: Expedientes/Expediente/{id_usuario}?tab=documentos&anio=2026
        // =========================================

        public ActionResult Expediente(string id, string tab, int? anio)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index");
            }

            UsuarioEntity usuario = ExpedienteAcademicoController.BuscarEstudiante(db, id);

            if (usuario == null)
            {
                return HttpNotFound();
            }

            var modelo = new ExpedienteEstudianteDto
            {
                pestana = Pestanas.Contains((tab ?? "").Trim().ToLowerInvariant())
                    ? tab.Trim().ToLowerInvariant()
                    : "datos",
                estudiante = ExpedienteAcademicoController.AResumen(db, usuario)
            };

            List<PrematriculaEntity> prematriculas = ValidacionExpediente.PrematriculasDe(db, usuario.identificacion);
            PrematriculaEntity aprobada = UltimaAprobada(prematriculas);
            modelo.estudiante.id_matricula_aprobada = aprobada != null ? (int?)aprobada.id_prematricula : null;

            // Expediente estudiantil: datos médicos, contacto y documentos
            ExpedienteEntity expediente = ValidacionExpediente.BuscarExpedienteDeUsuario(db, usuario);

            if (expediente != null)
            {
                modelo.expediente = Consultar(expediente.id_expediente).FirstOrDefault();
                modelo.estudiante.id_expediente = expediente.id_expediente;
                modelo.documentos = DocumentosExpedienteController.ConsultarDocumentos(db, expediente.id_expediente);
                modelo.estudiante.total_documentos = modelo.documentos.Count;

                if (modelo.expediente != null && !string.IsNullOrEmpty(modelo.expediente.identificacion))
                {
                    modelo.datos_personales = modelo.expediente;
                }
            }

            // Sin expediente (o sin su prematrícula): datos de la última prematrícula del estudiante
            if (modelo.datos_personales == null)
            {
                PrematriculaEntity ultima = aprobada ?? prematriculas
                    .OrderByDescending(p => p.fecha_prematricula)
                    .FirstOrDefault();

                if (ultima != null)
                {
                    var datos = new ExpedienteDto
                    {
                        id_matricula = ultima.id_prematricula,
                        correo = modelo.expediente != null && !string.IsNullOrWhiteSpace(modelo.expediente.correo)
                            ? modelo.expediente.correo
                            : usuario.email
                    };
                    CompletarDatosPersonales(datos, ultima);
                    modelo.datos_personales = datos;
                }
            }

            if (modelo.estudiante.seccion_actual == "Sin sección" && aprobada != null)
            {
                Dictionary<int, string> grados = db.Grados.ToList()
                    .ToDictionary(g => g.id_grado, g => g.nombre);
                modelo.estudiante.seccion_actual = GradoDePrematricula(aprobada, grados);
            }

            // Historial académico (REXP-02-007)
            List<HistorialAnioDto> historial = ExpedienteAcademicoController.ObtenerHistorial(db, usuario.id_usuario);
            modelo.sin_historial = !historial.Any();
            modelo.anios = historial.Select(h => h.anio).Distinct().OrderByDescending(a => a).ToList();
            modelo.anio_seleccionado = anio;
            modelo.historial = anio.HasValue
                ? historial.Where(h => h.anio == anio.Value).ToList()
                : historial;

            // Adecuaciones y ayudas (REXP-02-008 / REXP-02-009)
            modelo.adecuaciones = ExpedienteAcademicoController.CasosDeAdecuacion(db, usuario.id_usuario);
            modelo.ayudas = ExpedienteAcademicoController.CasosDeAyuda(db, usuario.id_usuario);

            return View("DetalleDeExpediente", modelo);
        }

        // =========================================
        // RUTAS VIEJAS: redirigen al expediente unificado
        // =========================================

        // GET: Expedientes/ListadoDeExpedientes
        public ActionResult ListadoDeExpedientes()
        {
            return RedirectToAction("Index");
        }

        // GET: Expedientes/DetalleDeExpediente/5
        public ActionResult DetalleDeExpediente(int? id, string tab)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            if (!db.Expedientes.Any(e => e.id_expediente == id.Value))
            {
                return HttpNotFound();
            }

            string idUsuario = ValidacionExpediente.IdUsuarioDeExpediente(db, id.Value);

            if (idUsuario == null)
            {
                TempData["Error"] = "El expediente #" + id.Value + " no está ligado a un usuario con rol Estudiante.";
            }

            return AlExpediente(idUsuario, string.IsNullOrWhiteSpace(tab) ? "datos" : tab);
        }

        private ActionResult AlExpediente(string idUsuario, string tab)
        {
            if (string.IsNullOrEmpty(idUsuario))
            {
                return RedirectToAction("Index");
            }

            return RedirectToAction("Expediente", new { id = idUsuario, tab = tab });
        }

        private static PrematriculaEntity UltimaAprobada(IEnumerable<PrematriculaEntity> prematriculas)
        {
            return prematriculas
                .Where(p => p.estado == EstadoAprobada)
                .OrderByDescending(p => p.fecha_prematricula)
                .FirstOrDefault();
        }

        // Si el estudiante aún no está en una sección, se muestra el grado de su prematrícula aprobada.
        private static string GradoDePrematricula(PrematriculaEntity aprobada, Dictionary<int, string> grados)
        {
            if (aprobada == null || !grados.ContainsKey(aprobada.fk_id_grado))
            {
                return "Sin sección";
            }

            return grados[aprobada.fk_id_grado] + " (sin sección asignada)";
        }

        // =========================================
        // REXP-03-001
        // CREAR EXPEDIENTE ESTUDIANTIL
        // =========================================

        [HttpGet]
        public ActionResult CrearExpediente(int? id_matricula)
        {
            var modelo = new ExpedienteDto();

            if (id_matricula.HasValue && id_matricula.Value > 0)
            {
                modelo.id_matricula = id_matricula.Value;

                PrematriculaEntity prematricula = db.Prematriculas
                    .FirstOrDefault(p => p.id_prematricula == modelo.id_matricula);

                CompletarDatosPersonales(modelo, prematricula);

                if (prematricula != null)
                {
                    UsuarioEntity usuario = ValidacionExpediente
                        .BuscarUsuarioEstudiante(db, prematricula.identificacion);

                    if (usuario != null)
                    {
                        modelo.correo = usuario.email;
                    }

                    modelo.contactoEmergencia = prematricula.contactoEmergencia;
                }
            }

            CargarPrematriculas(modelo.id_matricula);

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearExpediente(
            [Bind(Include =
                "id_matricula,correo,grupo_sanguineo," +
                "alergias,contactoEmergencia")]
            ExpedienteDto expediente)
        {
            if (expediente == null)
            {
                return new HttpStatusCodeResult(400);
            }

            // Los datos personales salen de la prematrícula: no se piden ni se validan aquí.
            foreach (string campo in CamposNoEditablesAlCrear)
            {
                ModelState.Remove(campo);
            }

            ValidarDatosDelExpediente(expediente);

            PrematriculaEntity prematricula = expediente.id_matricula > 0
                ? db.Prematriculas.FirstOrDefault(p => p.id_prematricula == expediente.id_matricula)
                : null;

            if (prematricula == null)
            {
                ModelState.AddModelError("id_matricula", "Debe seleccionar una matrícula válida.");
            }
            else if (prematricula.estado != EstadoAprobada)
            {
                ModelState.AddModelError("id_matricula", "Solo se puede crear el expediente de una matrícula aprobada.");
            }

            if (!ModelState.IsValid)
            {
                return VistaCrear(expediente, prematricula);
            }

            try
            {
                ExpedienteEntity entidad;
                string idUsuarioCreado;

                using (var transaccion = db.Database.BeginTransaction())
                {
                    // Bloquea las prematrículas del estudiante para que dos
                    // solicitudes simultáneas no creen dos expedientes.
                    db.Database.ExecuteSqlCommand(
                        "SELECT id_matricula FROM prematricula WHERE identificacion_estudiante = @p0 FOR UPDATE",
                        prematricula.identificacion);

                    UsuarioEntity usuario = ValidacionExpediente
                        .BuscarUsuarioEstudiante(db, prematricula.identificacion);

                    List<string> errores = ValidacionExpediente.ValidarRelacion(
                        db,
                        expediente.id_matricula,
                        usuario != null ? usuario.id_usuario : null);

                    if (errores.Any())
                    {
                        transaccion.Rollback();

                        foreach (string error in errores)
                        {
                            ModelState.AddModelError("", error);
                        }

                        return VistaCrear(expediente, prematricula);
                    }

                    if (string.IsNullOrWhiteSpace(expediente.correo))
                    {
                        expediente.correo = usuario.email;
                    }

                    idUsuarioCreado = usuario.id_usuario;
                    entidad = ExpedienteMapper.ConvertirAEntidad(expediente, usuario.id_usuario);
                    db.Expedientes.Add(entidad);

                    Auditoria.Registrar(
                        db,
                        "Crear expediente",
                        "Se creó el expediente del estudiante " + prematricula.identificacion + ".",
                        null,
                        new
                        {
                            matricula = entidad.fk_id_matricula,
                            usuario = entidad.fk_id_usuario,
                            identificacion = prematricula.identificacion,
                            entidad.correo,
                            entidad.grupo_sanguineo,
                            entidad.alergias,
                            contacto_emergencia = entidad.contactoEmergencia
                        });

                    db.SaveChanges();
                    transaccion.Commit();
                }

                TempData["Mensaje"] = "El expediente se creó correctamente.";
                return AlExpediente(idUsuarioCreado, "datos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Error creando expediente: " + ex);
                ModelState.AddModelError("", "No fue posible guardar el expediente. Intente de nuevo.");
                return VistaCrear(expediente, prematricula);
            }
        }

        private ActionResult VistaCrear(ExpedienteDto expediente, PrematriculaEntity prematricula)
        {
            CompletarDatosPersonales(expediente, prematricula);
            CargarPrematriculas(expediente.id_matricula);
            return View("CrearExpediente", expediente);
        }

        // =========================================
        // REXP-03-006
        // MODIFICAR EXPEDIENTE ESTUDIANTIL
        // =========================================

        // GET: Expedientes/EditarExpediente/5
        [HttpGet]
        public ActionResult EditarExpediente(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            ExpedienteDto expediente = Consultar(id.Value).FirstOrDefault();

            if (expediente == null)
            {
                return HttpNotFound();
            }

            expediente.id_usuario = ValidacionExpediente.IdUsuarioDeExpediente(db, id.Value);

            if (string.IsNullOrEmpty(expediente.identificacion))
            {
                TempData["Error"] = "La prematrícula ligada a este expediente no existe; no se pueden editar los datos personales.";
                return AlExpediente(expediente.id_usuario, "medica");
            }

            return View(expediente);
        }

        // POST: Expedientes/EditarExpediente
        // La identificación no se edita. Los datos personales se guardan en la
        // prematrícula (todas las del estudiante) y en su usuario; los datos médicos,
        // el correo y el contacto de emergencia se guardan en el expediente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarExpediente(
            [Bind(Include =
                "id_expediente,nombre,apellido1,apellido2,genero," +
                "grupo_sanguineo,alergias,correo,contactoEmergencia")]
            ExpedienteDto modelo)
        {
            if (modelo == null || modelo.id_expediente <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            ExpedienteEntity entidad = db.Expedientes
                .FirstOrDefault(e => e.id_expediente == modelo.id_expediente);

            if (entidad == null)
            {
                return HttpNotFound();
            }

            PrematriculaEntity prematricula = db.Prematriculas
                .FirstOrDefault(p => p.id_prematricula == entidad.fk_id_matricula);

            string idUsuarioExpediente = ValidacionExpediente.IdUsuarioDeExpediente(db, entidad.id_expediente);

            if (prematricula == null)
            {
                TempData["Error"] = "La prematrícula ligada a este expediente no existe; no se pueden editar los datos personales.";
                return AlExpediente(idUsuarioExpediente, "medica");
            }

            // La identificación (cédula) no es modificable: no está en el Bind del POST,
            // siempre se toma de la base de datos y no se escribe en ninguna tabla.
            modelo.identificacion = prematricula.identificacion;
            modelo.id_matricula = entidad.fk_id_matricula;
            modelo.id_usuario = idUsuarioExpediente;

            foreach (string campo in ModelState.Keys.ToList())
            {
                if (!CamposEditables.Contains(campo))
                {
                    ModelState.Remove(campo);
                }
            }

            // Género: mismas opciones que la prematrícula
            if (!string.IsNullOrWhiteSpace(modelo.genero))
            {
                string genero = ValidacionExpediente.Generos.FirstOrDefault(g =>
                    string.Equals(g, modelo.genero.Trim(), StringComparison.OrdinalIgnoreCase));

                if (genero == null)
                {
                    ModelState.AddModelError("genero", "Seleccione un género válido.");
                }
                else
                {
                    modelo.genero = genero;
                }
            }

            ValidarDatosDelExpediente(modelo);

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            try
            {
                string identificacion = prematricula.identificacion;

                // Todas las matrículas del estudiante (cédula comparada normalizada)
                List<PrematriculaEntity> prematriculas = ValidacionExpediente.PrematriculasDe(db, identificacion);

                UsuarioEntity usuario = string.IsNullOrEmpty(entidad.fk_id_usuario)
                    ? null
                    : db.Usuarios.FirstOrDefault(u => u.id_usuario == entidad.fk_id_usuario);

                var antes = new
                {
                    prematricula.nombre,
                    prematricula.apellido1,
                    prematricula.apellido2,
                    prematricula.genero,
                    entidad.correo,
                    entidad.grupo_sanguineo,
                    entidad.alergias,
                    contacto_emergencia = entidad.contactoEmergencia
                };

                string nombre = modelo.nombre.Trim();
                string apellido1 = modelo.apellido1.Trim();
                string apellido2 = ExpedienteMapper.Limpiar(modelo.apellido2);

                foreach (PrematriculaEntity p in prematriculas)
                {
                    p.nombre = nombre;
                    p.apellido1 = apellido1;
                    p.apellido2 = apellido2;
                    p.genero = modelo.genero;
                }

                if (usuario != null)
                {
                    usuario.nombre = nombre;
                    usuario.apellido1 = apellido1;
                    usuario.apellido2 = apellido2;
                }

                ExpedienteMapper.ActualizarEntidad(entidad, modelo);

                Auditoria.Registrar(
                    db,
                    "Editar expediente",
                    "Se modificó el expediente #" + entidad.id_expediente + " del estudiante " + identificacion + ".",
                    antes,
                    new
                    {
                        nombre,
                        apellido1,
                        apellido2,
                        modelo.genero,
                        entidad.correo,
                        entidad.grupo_sanguineo,
                        entidad.alergias,
                        contacto_emergencia = entidad.contactoEmergencia
                    });

                db.SaveChanges();

                TempData["Mensaje"] = "Los cambios del expediente se guardaron correctamente.";
                return AlExpediente(idUsuarioExpediente, "medica");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Error editando expediente: " + ex);
                ModelState.AddModelError("", "No fue posible guardar los cambios del expediente. Intente de nuevo.");
                return View(modelo);
            }
        }

        // =========================================
        // CONSULTA DE EXPEDIENTES DESDE LA BASE DE DATOS
        // expediente_estudiantes + prematricula (datos personales)
        // + usuarios (correo del estudiante si el expediente no tiene uno)
        // =========================================

        private List<ExpedienteDto> Consultar(int? idExpediente)
        {
            var consulta = from e in db.Expedientes
                           join p in db.Prematriculas on e.fk_id_matricula equals p.id_prematricula into ps
                           from p in ps.DefaultIfEmpty()
                           join u in db.Usuarios on e.fk_id_usuario equals u.id_usuario into us
                           from u in us.DefaultIfEmpty()
                           select new { e, p, email = u != null ? u.email : null };

            if (idExpediente.HasValue)
            {
                int id = idExpediente.Value;
                consulta = consulta.Where(x => x.e.id_expediente == id);
            }

            var filas = consulta.ToList();

            // El grado y los datos más recientes salen de la última matrícula aprobada del estudiante.
            List<string> identificaciones = filas
                .Where(x => x.p != null)
                .Select(x => x.p.identificacion)
                .Distinct()
                .ToList();

            List<PrematriculaEntity> aprobadas = identificaciones.Any()
                ? db.Prematriculas
                    .Where(p => identificaciones.Contains(p.identificacion) && p.estado == EstadoAprobada)
                    .ToList()
                : new List<PrematriculaEntity>();

            Dictionary<int, string> grados = db.Grados.ToList()
                .ToDictionary(g => g.id_grado, g => g.nombre);

            return filas
                .Select(x =>
                {
                    PrematriculaEntity datos = x.p == null
                        ? null
                        : aprobadas
                            .Where(a => a.identificacion == x.p.identificacion)
                            .OrderByDescending(a => a.fecha_prematricula)
                            .FirstOrDefault() ?? x.p;

                    var dto = new ExpedienteDto
                    {
                        id_expediente = x.e.id_expediente,
                        id_matricula = x.e.fk_id_matricula,
                        id_usuario = x.e.fk_id_usuario,
                        correo = !string.IsNullOrWhiteSpace(x.e.correo) ? x.e.correo : x.email,
                        grupo_sanguineo = x.e.grupo_sanguineo,
                        alergias = x.e.alergias,
                        contactoEmergencia = x.e.contactoEmergencia
                    };

                    CompletarDatosPersonales(dto, datos, grados);
                    return dto;
                })
                .OrderBy(d => d.nombre)
                .ThenBy(d => d.apellido1)
                .ToList();
        }

        private void CompletarDatosPersonales(ExpedienteDto dto, PrematriculaEntity p)
        {
            CompletarDatosPersonales(dto, p, null);
        }

        private void CompletarDatosPersonales(ExpedienteDto dto, PrematriculaEntity p, Dictionary<int, string> grados)
        {
            if (p == null)
            {
                return;
            }

            if (grados == null)
            {
                int idGrado = p.fk_id_grado;
                grados = db.Grados.Where(g => g.id_grado == idGrado).ToList()
                    .ToDictionary(g => g.id_grado, g => g.nombre);
            }

            dto.identificacion = p.identificacion;
            dto.nombre = p.nombre;
            dto.apellido1 = p.apellido1;
            dto.apellido2 = p.apellido2;
            dto.genero = p.genero;
            dto.grado = p.fk_id_grado;
            dto.nombre_grado = grados.ContainsKey(p.fk_id_grado) ? grados[p.fk_id_grado] : "Sin grado";
            dto.telefono = p.telefono;
            dto.fecha_nacimiento = p.fecha_nacimiento;
            dto.tiene_adecuacion = p.tiene_adecuacion;
            dto.tiene_discapacidad = p.tiene_discapacidad;
            dto.direccion = p.direccion;
        }

        // Validaciones de los campos propios del expediente (crear y editar)
        private void ValidarDatosDelExpediente(ExpedienteDto modelo)
        {
            if (!string.IsNullOrWhiteSpace(modelo.grupo_sanguineo))
            {
                string grupo = modelo.grupo_sanguineo.Trim().ToUpperInvariant();

                if (!ValidacionExpediente.GruposSanguineos.Contains(grupo))
                {
                    ModelState.AddModelError("grupo_sanguineo", "Debe seleccionar un grupo sanguíneo válido.");
                }
                else
                {
                    modelo.grupo_sanguineo = grupo;
                }
            }

            if (modelo.correo != null && modelo.correo.Trim().Length == 0)
            {
                modelo.correo = null;
            }
        }

        // =========================================
        // CARGAR PREMATRÍCULAS
        // Matrículas aprobadas de estudiantes que todavía no tienen
        // expediente (ni por matrícula ni por usuario). Una por estudiante.
        // =========================================

        private void CargarPrematriculas(int seleccionada)
        {
            List<string> identificacionesConExpediente =
                (from e in db.Expedientes
                 join p in db.Prematriculas on e.fk_id_matricula equals p.id_prematricula
                 select p.identificacion)
                .Distinct()
                .ToList();

            List<string> usuariosConExpediente = db.Expedientes
                .Where(e => e.fk_id_usuario != null)
                .Select(e => e.fk_id_usuario)
                .Distinct()
                .ToList();

            if (usuariosConExpediente.Any())
            {
                identificacionesConExpediente.AddRange(db.Usuarios
                    .Where(u => usuariosConExpediente.Contains(u.id_usuario) && u.identificacion != null)
                    .Select(u => u.identificacion)
                    .ToList());
            }

            var excluidas = new HashSet<string>(identificacionesConExpediente
                .Select(c => Cedula.Normalizar(c) ?? (c ?? "").Trim()));

            Dictionary<int, string> grados = db.Grados.ToList()
                .ToDictionary(g => g.id_grado, g => g.nombre);

            var opciones = db.Prematriculas
                .Where(p => p.estado == EstadoAprobada)
                .ToList()
                .Where(p => !excluidas.Contains(Cedula.Normalizar(p.identificacion) ?? (p.identificacion ?? "").Trim()))
                .GroupBy(p => Cedula.Normalizar(p.identificacion) ?? (p.identificacion ?? "").Trim())
                .Select(g => g.OrderByDescending(p => p.fecha_prematricula).First())
                .OrderBy(p => p.nombre)
                .ThenBy(p => p.apellido1)
                .Select(p => new SelectListItem
                {
                    Value = p.id_prematricula.ToString(),
                    Text = (Cedula.Normalizar(p.identificacion) ?? p.identificacion) + " - " +
                           string.Join(" ", new[] { p.nombre, p.apellido1, p.apellido2 }
                               .Where(x => !string.IsNullOrWhiteSpace(x))) +
                           (grados.ContainsKey(p.fk_id_grado) ? " (" + grados[p.fk_id_grado] + ")" : "")
                })
                .ToList();

            ViewBag.Matriculas = new SelectList(
                opciones,
                "Value",
                "Text",
                seleccionada > 0 ? seleccionada.ToString() : null
            );
        }

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
