using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    [AutorizarRoles(RolesSistema.Administrador, RolesSistema.Director, RolesSistema.Docente, RolesSistema.Estudiante)]
    public class AsignacionesController : Controller
    {
        public const int TamanoMaximoMb = 10;
        private const int FilasPorPaginaEntregas = 10;
        private const int FilasPorPaginaHistorial = 15;

        private static readonly string[] ExtensionesPermitidas =
            { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".txt", ".jpg", ".jpeg", ".png", ".zip" };

        private static readonly string[] FormatosFecha = { "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss" };

        // =====================================================================
        // RAV-12-002: ASIGNAR TAREAS
        // =====================================================================

        // GET: Asignaciones/CrearAsignacion?idMateria=5&idSeccion=2
        public ActionResult CrearAsignacion(int? idMateria, int? idSeccion)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                if (!EsDocenteDeLaClase(db, usuario, idMateria, idSeccion))
                {
                    TempData["Error"] = "No tiene acceso a esta materia.";
                    return RedirectToAction("AulaVirtual", "AulaVirtual");
                }

                var modelo = new CrearAsignacionVm
                {
                    idMateria = idMateria.Value,
                    idSeccion = idSeccion.Value,
                    fechaApertura = DateTime.Now.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
                    fechaCierre = DateTime.Today.AddDays(7).AddHours(23).AddMinutes(59)
                                      .ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
                };
                CompletarDatos(db, modelo);
                return View(modelo);
            }
        }

        // POST: Asignaciones/CrearAsignacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearAsignacion(CrearAsignacionVm modelo)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                if (!EsDocenteDeLaClase(db, usuario, modelo.idMateria, modelo.idSeccion))
                {
                    TempData["Error"] = "No tiene acceso a esta materia.";
                    return RedirectToAction("AulaVirtual", "AulaVirtual");
                }

                CompletarDatos(db, modelo);

                string nombre = (modelo.nombre ?? "").Trim();
                string descripcion = (modelo.descripcion ?? "").Trim();

                if (nombre == "")
                    ModelState.AddModelError("nombre", "Ingrese el nombre de la asignación.");
                else if (nombre.Length > 256)
                    ModelState.AddModelError("nombre", "El nombre no puede superar los 256 caracteres.");

                if (descripcion.Length > 512)
                    ModelState.AddModelError("descripcion", "La descripción no puede superar los 512 caracteres.");

                decimal porcentaje;
                if (!TryParseDecimal(modelo.porcentaje, out porcentaje) || porcentaje <= 0 || porcentaje > 100)
                {
                    ModelState.AddModelError("porcentaje", "Ingrese un porcentaje entre 0.01 y 100.");
                }
                else if (porcentaje > modelo.porcentajeDisponible)
                {
                    ModelState.AddModelError("porcentaje",
                        "El porcentaje excede el 100 % de la materia. Disponible: " +
                        modelo.porcentajeDisponible.ToString("0.##", CultureInfo.InvariantCulture) + " %.");
                }

                DateTime apertura, cierre;
                bool aperturaOk = DateTime.TryParseExact(modelo.fechaApertura ?? "", FormatosFecha,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out apertura);
                bool cierreOk = DateTime.TryParseExact(modelo.fechaCierre ?? "", FormatosFecha,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out cierre);

                if (!aperturaOk)
                    ModelState.AddModelError("fechaApertura", "Ingrese la fecha de apertura.");
                if (!cierreOk)
                    ModelState.AddModelError("fechaCierre", "Ingrese la fecha de cierre.");
                if (aperturaOk && cierreOk && cierre <= apertura)
                    ModelState.AddModelError("fechaCierre", "La fecha de cierre debe ser posterior a la apertura.");

                List<string> idsEstudiantes = db.SeccionesEstudiantes
                    .Where(se => se.fk_id_seccion == modelo.idSeccion)
                    .Select(se => se.fk_id_usuario).ToList();

                if (idsEstudiantes.Count == 0)
                    ModelState.AddModelError("", "La sección no tiene estudiantes asignados.");

                if (!ModelState.IsValid)
                {
                    return View(modelo);
                }

                string grupo = Guid.NewGuid().ToString();
                porcentaje = Math.Round(porcentaje, 2);

                var filas = idsEstudiantes.Select(idEstudiante => new AsignacionEntity
                {
                    nombre = nombre,
                    descripcion = descripcion,
                    porcentaje_nota = porcentaje,
                    calificacion = 0,
                    estado = "Pendiente",
                    fk_id_usuario = idEstudiante,
                    fk_id_seccion = modelo.idSeccion,
                    fk_id_materia = modelo.idMateria,
                    fecha_apertura = apertura,
                    fecha_cierre = cierre,
                    id_grupo = grupo,
                    fk_id_docente = usuario.id_usuario
                }).ToList();

                db.Asignaciones.AddRange(filas);

                db.Bitacora.Add(new BitacoraEntity
                {
                    tipo_de_evento = "Creación de asignación",
                    descripcion_evento = Recortar("Asignación \"" + nombre + "\" creada para " + modelo.etiquetaSeccion, 250),
                    fecha = DateTime.Now,
                    datos_anteriores = JsonConvert.SerializeObject(new { }),
                    datos_posteriores = JsonConvert.SerializeObject(new
                    {
                        nombre,
                        porcentaje,
                        apertura,
                        cierre,
                        id_materia = modelo.idMateria,
                        id_seccion = modelo.idSeccion,
                        estudiantes = idsEstudiantes.Count
                    }),
                    fk_id_usuario = usuario.id_usuario
                });

                db.SaveChanges();

                TempData["Mensaje"] = "La asignación se creó y se publicó para " + idsEstudiantes.Count + " estudiantes.";
            }

            return Redirect(Url.Action("Materia", "AulaVirtual", new { id = modelo.idMateria, seccion = modelo.idSeccion }));
        }

        // =====================================================================
        // RAV-12-003: RECIBIR ENTREGAS (estudiante)
        // =====================================================================

        // GET: Asignaciones/Entregar/5
        public ActionResult Entregar(int id)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                AsignacionEntity fila = ObtenerFilaDelEstudiante(db, usuario, id);
                if (fila == null)
                {
                    TempData["Error"] = "No tiene acceso a esta asignación.";
                    return RedirectToAction("AulaVirtual", "AulaVirtual");
                }

                return View(ConstruirEntregarVm(db, fila));
            }
        }

        // POST: Asignaciones/Entregar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Entregar(int id, HttpPostedFileBase archivo)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                AsignacionEntity fila = ObtenerFilaDelEstudiante(db, usuario, id);
                if (fila == null)
                {
                    TempData["Error"] = "No tiene acceso a esta asignación.";
                    return RedirectToAction("AulaVirtual", "AulaVirtual");
                }

                // Escenario 3: fuera de plazo (también aún no abierta o ya calificada)
                string bloqueo = MotivoBloqueo(fila);
                if (bloqueo != null)
                {
                    TempData["Error"] = bloqueo;
                    return RedirectToAction("Entregar", new { id = id });
                }

                // Escenario 4: sin archivo
                if (archivo == null || archivo.ContentLength == 0)
                {
                    TempData["Error"] = "Debe adjuntar un archivo.";
                    return RedirectToAction("Entregar", new { id = id });
                }

                string nombreOriginal = Path.GetFileName(archivo.FileName) ?? "";
                string extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();

                if (!ExtensionesPermitidas.Contains(extension))
                {
                    TempData["Error"] = "Formato no permitido. Formatos válidos: " + FormatosPermitidos() + ".";
                    return RedirectToAction("Entregar", new { id = id });
                }

                if (archivo.ContentLength > TamanoMaximoMb * 1024 * 1024)
                {
                    TempData["Error"] = "El archivo supera el tamaño máximo de " + TamanoMaximoMb + " MB.";
                    return RedirectToAction("Entregar", new { id = id });
                }

                if (nombreOriginal.Length > 255)
                {
                    nombreOriginal = nombreOriginal.Substring(nombreOriginal.Length - 255);
                }

                // Escenarios 1 y 2: entrega nueva o reemplazo
                string carpeta = Server.MapPath("~/App_Data/Entregas/" + fila.id_asignacion);
                Directory.CreateDirectory(carpeta);

                string nombreGuardado = Guid.NewGuid().ToString("N") + extension;
                string rutaCompleta = Path.Combine(carpeta, nombreGuardado);
                archivo.SaveAs(rutaCompleta);

                string archivoAnterior = fila.archivo_ruta;
                bool eraReemplazo = !string.IsNullOrEmpty(archivoAnterior);

                fila.archivo_nombre = nombreOriginal;
                fila.archivo_ruta = nombreGuardado;
                fila.fecha_entrega = DateTime.Now;
                fila.estado = "Entregada";

                try
                {
                    db.SaveChanges();
                }
                catch
                {
                    if (System.IO.File.Exists(rutaCompleta)) { System.IO.File.Delete(rutaCompleta); }
                    throw;
                }

                if (eraReemplazo)
                {
                    try
                    {
                        string anterior = Path.Combine(carpeta, archivoAnterior);
                        if (System.IO.File.Exists(anterior)) { System.IO.File.Delete(anterior); }
                    }
                    catch { /* no es crítico */ }
                }

                TempData["Mensaje"] = (eraReemplazo ? "Su entrega se actualizó el " : "Su tarea se entregó correctamente el ")
                    + fila.fecha_entrega.Value.ToString("dd/MM/yyyy 'a las' HH:mm") + ".";
            }

            return RedirectToAction("Entregar", new { id = id });
        }

        // GET: Asignaciones/DescargarEntrega/5  (dueño de la entrega o docente de la clase)
        public ActionResult DescargarEntrega(int id)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                var fila = db.Asignaciones.FirstOrDefault(a => a.id_asignacion == id);
                if (fila == null || string.IsNullOrEmpty(fila.archivo_ruta))
                {
                    TempData["Error"] = "El archivo no está disponible.";
                    return RedirectToAction("AulaVirtual", "AulaVirtual");
                }

                bool esDueno = usuario != null && fila.fk_id_usuario == usuario.id_usuario;
                bool esDocente = EsDocenteDeLaClase(db, usuario, fila.fk_id_materia, fila.fk_id_seccion);

                if (!esDueno && !esDocente)
                {
                    TempData["Error"] = "No tiene acceso a este archivo.";
                    return RedirectToAction("AulaVirtual", "AulaVirtual");
                }

                string ruta = Path.Combine(Server.MapPath("~/App_Data/Entregas/" + fila.id_asignacion), fila.archivo_ruta);
                if (!System.IO.File.Exists(ruta))
                {
                    TempData["Error"] = "El archivo no está disponible.";
                    return esDueno
                        ? RedirectToAction("Entregar", new { id = id })
                        : RedirectToAction("Materia", "AulaVirtual", new { id = fila.fk_id_materia, seccion = fila.fk_id_seccion });
                }

                string nombre = string.IsNullOrEmpty(fila.archivo_nombre) ? fila.archivo_ruta : fila.archivo_nombre;
                return File(ruta, MimeMapping.GetMimeMapping(nombre), nombre);
            }
        }

        // =====================================================================
        // RAV-12-003 (esc. 5) y RAV-12-004: ENTREGAS DE UNA ASIGNACIÓN Y CALIFICACIÓN (docente)
        // =====================================================================

        // GET: Asignaciones/Detalles/5?q=ana&estado=noentregaron&pagina=2
        // El id puede ser el de cualquier fila de la asignación (todas comparten id_grupo).
        public ActionResult Detalles(int id, string q, string estado, int pagina = 1)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            if (EsEstudiante(usuario))
            {
                return RedirectToAction("Entregar", new { id = id });
            }

            using (var db = new EduNexusDbContext())
            {
                AsignacionEntity referencia = db.Asignaciones.FirstOrDefault(a => a.id_asignacion == id);
                if (referencia == null)
                {
                    TempData["Error"] = "La asignación no existe.";
                    return RedirectToAction("AulaVirtual", "AulaVirtual");
                }

                int idMateria = referencia.fk_id_materia ?? 0;
                int idSeccion = referencia.fk_id_seccion;

                if (!TieneAccesoALaClase(db, usuario, idMateria, idSeccion))
                {
                    TempData["Error"] = "No tiene acceso a esta materia.";
                    return RedirectToAction("AulaVirtual", "AulaVirtual");
                }

                List<AsignacionEntity> filas = FilasDeLaAsignacion(db, referencia).ToList();

                List<string> idsUsuarios = filas.Select(f => f.fk_id_usuario).Distinct().ToList();
                Dictionary<string, UsuarioEntity> usuarios = db.Usuarios
                    .Where(u => idsUsuarios.Contains(u.id_usuario)).ToList()
                    .ToDictionary(u => u.id_usuario);

                List<EstudianteEntregaVm> todos = filas.Select(f =>
                {
                    UsuarioEntity u;
                    usuarios.TryGetValue(f.fk_id_usuario, out u);
                    return new EstudianteEntregaVm
                    {
                        id_asignacion = f.id_asignacion,
                        nombre = u != null ? HorarioServicio.NombreCompleto(u) : f.fk_id_usuario,
                        identificacion = u != null ? u.identificacion : "",
                        estado = EstadoVisible(f),
                        entrego = HayEntrega(f),
                        calificada = EsCalificada(f),
                        tieneArchivo = !string.IsNullOrEmpty(f.archivo_ruta),
                        archivoNombre = f.archivo_nombre,
                        fechaEntrega = f.fecha_entrega,
                        calificacion = f.calificacion
                    };
                }).ToList();

                // Filtros de búsqueda
                string busqueda = (q ?? "").Trim();
                string filtro = (estado ?? "todos").Trim().ToLowerInvariant();

                IEnumerable<EstudianteEntregaVm> consulta = todos;

                if (busqueda != "")
                {
                    consulta = consulta.Where(e =>
                        (e.nombre ?? "").IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (e.identificacion ?? "").IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                switch (filtro)
                {
                    case "entregaron": consulta = consulta.Where(e => e.entrego); break;
                    case "noentregaron": consulta = consulta.Where(e => !e.entrego); break;
                    case "porcalificar": consulta = consulta.Where(e => e.entrego && !e.calificada); break;
                    case "calificadas": consulta = consulta.Where(e => e.calificada); break;
                    default: filtro = "todos"; break;
                }

                List<EstudianteEntregaVm> filtrados = consulta.OrderBy(e => e.nombre).ToList();

                // Paginación
                var parametros = new Dictionary<string, string>
                {
                    { "q", busqueda },
                    { "estado", filtro == "todos" ? "" : filtro }
                };
                PaginacionVm paginacion = Paginar(filtrados.Count, FilasPorPaginaEntregas, ref pagina,
                    Url.Action("Detalles", new { id = id }), parametros);

                List<EstudianteEntregaVm> visibles = filtrados
                    .Skip((pagina - 1) * FilasPorPaginaEntregas)
                    .Take(FilasPorPaginaEntregas)
                    .ToList();

                MateriaEntity materia = db.Materias.FirstOrDefault(m => m.id_materia == idMateria);
                string etiquetaSeccion;
                HorarioServicio.EtiquetasDeSecciones(db).TryGetValue(idSeccion, out etiquetaSeccion);

                var modelo = new DetalleAsignacionVm
                {
                    id = id,
                    nombre = referencia.nombre,
                    descripcion = referencia.descripcion,
                    porcentaje = referencia.porcentaje_nota,
                    apertura = referencia.fecha_apertura,
                    cierre = referencia.fecha_cierre,
                    idMateria = idMateria,
                    idSeccion = idSeccion,
                    nombreMateria = materia != null ? materia.nombre : "",
                    etiquetaSeccion = etiquetaSeccion ?? "",
                    puedeCalificar = EsDocenteDeLaClase(db, usuario, idMateria, idSeccion),
                    total = todos.Count,
                    entregaron = todos.Count(e => e.entrego),
                    calificadas = todos.Count(e => e.calificada),
                    q = busqueda,
                    estado = filtro,
                    filtrados = filtrados.Count,
                    items = visibles,
                    paginacion = paginacion
                };

                return View(modelo);
            }
        }

        // POST: Asignaciones/Calificar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Calificar(int id, int idDetalle, string calificacion, string q, string estado, int pagina = 1)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                AsignacionEntity fila = db.Asignaciones.FirstOrDefault(a => a.id_asignacion == id);
                if (fila == null)
                {
                    TempData["Error"] = "La asignación no existe.";
                    return RedirectToAction("AulaVirtual", "AulaVirtual");
                }

                // Solo el docente de esa clase califica
                if (!EsDocenteDeLaClase(db, usuario, fila.fk_id_materia, fila.fk_id_seccion))
                {
                    TempData["Error"] = "No tiene acceso a esta materia.";
                    return RedirectToAction("AulaVirtual", "AulaVirtual");
                }

                var volver = RedirectToAction("Detalles", new { id = idDetalle, q = q, estado = estado, pagina = pagina });

                // Escenario 3: sin entrega
                if (!HayEntrega(fila))
                {
                    TempData["Error"] = "El estudiante aún no tiene una entrega para calificar.";
                    return volver;
                }

                // Escenario 2: calificación fuera de rango o no numérica
                decimal nueva;
                if (!TryParseDecimal(calificacion, out nueva) || nueva < 0 || nueva > 100)
                {
                    TempData["Error"] = "La calificación debe ser un número entre 0 y 100.";
                    return volver;
                }
                nueva = Math.Round(nueva, 2);

                bool yaCalificada = EsCalificada(fila);
                decimal anterior = fila.calificacion;
                string estadoAnterior = fila.estado;

                if (yaCalificada && anterior == nueva)
                {
                    TempData["Mensaje"] = "La calificación no cambió.";
                    return volver;
                }

                UsuarioEntity estudiante = db.Usuarios.FirstOrDefault(u => u.id_usuario == fila.fk_id_usuario);
                string nombreEstudiante = estudiante != null ? HorarioServicio.NombreCompleto(estudiante) : fila.fk_id_usuario;

                // Escenarios 1 y 4: se guarda la nota y queda en bitácora (nota anterior, nueva, usuario y fecha)
                fila.calificacion = nueva;
                fila.estado = "Calificada";

                db.Bitacora.Add(new BitacoraEntity
                {
                    tipo_de_evento = yaCalificada ? "Modificación de calificación" : "Calificación de asignación",
                    descripcion_evento = Recortar("\"" + fila.nombre + "\" - " + nombreEstudiante, 250),
                    fecha = DateTime.Now,
                    datos_anteriores = JsonConvert.SerializeObject(new
                    {
                        calificacion = yaCalificada ? (decimal?)anterior : null,
                        estado = estadoAnterior
                    }),
                    datos_posteriores = JsonConvert.SerializeObject(new
                    {
                        calificacion = nueva,
                        estado = "Calificada"
                    }),
                    fk_id_usuario = usuario.id_usuario
                });

                db.SaveChanges();

                TempData["Mensaje"] = (yaCalificada ? "Calificación actualizada: " : "Calificación guardada: ")
                    + nueva.ToString("0.##", CultureInfo.InvariantCulture) + " para " + nombreEstudiante + ".";

                return volver;
            }
        }

        // =====================================================================
        // RAV-12-005: HISTORIAL
        // =====================================================================

        // GET: Asignaciones/Historial?idMateria=5&idSeccion=2&estado=calificadas
        public ActionResult Historial(int? idMateria, int? idSeccion, string idEstudiante, int? idAsignacion, string estado, int pagina = 1)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                // Escenario 5: acceso restringido
                SeccionMateriaEntity clase = (idMateria.HasValue && idSeccion.HasValue)
                    ? AulaVirtualController.ObtenerClasesVisibles(db, usuario)
                        .FirstOrDefault(c => c.fk_id_materia == idMateria.Value && c.fk_id_seccion == idSeccion.Value)
                    : null;

                if (clase == null)
                {
                    TempData["Error"] = "No tiene acceso a esta materia.";
                    return RedirectToAction("AulaVirtual", "AulaVirtual");
                }

                int mat = clase.fk_id_materia;
                int sec = clase.fk_id_seccion;
                bool esEstudiante = EsEstudiante(usuario);
                bool esDocente = RolesSistema.Es(usuario, RolesSistema.Docente);

                List<AsignacionEntity> todas = db.Asignaciones
                    .Where(a => a.fk_id_materia == mat && a.fk_id_seccion == sec)
                    .ToList();

                // Escenario 1: el estudiante solo ve su propio historial
                if (esEstudiante)
                {
                    string yo = usuario.id_usuario;
                    todas = todas.Where(a => a.fk_id_usuario == yo).ToList();
                }

                List<string> idsUsuarios = todas.Select(a => a.fk_id_usuario).Distinct().ToList();
                Dictionary<string, string> nombres = db.Usuarios
                    .Where(u => idsUsuarios.Contains(u.id_usuario)).ToList()
                    .ToDictionary(u => u.id_usuario, u => HorarioServicio.NombreCompleto(u));

                Func<string, string> nombreDe = i => (i != null && nombres.ContainsKey(i)) ? nombres[i] : (i ?? "");

                // Escenario 4: no hay ninguna asignación en el alcance del usuario
                bool hayRegistros = todas.Any();

                // Selectores del docente (escenario 2)
                var selEstudiantes = new List<SelectListItem>();
                var selAsignaciones = new List<SelectListItem>();
                if (esDocente)
                {
                    selEstudiantes = todas.Select(a => a.fk_id_usuario).Distinct()
                        .Select(i => new SelectListItem { Value = i, Text = nombreDe(i), Selected = i == idEstudiante })
                        .OrderBy(s => s.Text).ToList();

                    selAsignaciones = todas.GroupBy(a => GrupoClave(a)).Select(g => g.First())
                        .OrderBy(a => a.nombre)
                        .Select(a => new SelectListItem
                        {
                            Value = a.id_asignacion.ToString(),
                            Text = a.nombre,
                            Selected = idAsignacion.HasValue && idAsignacion.Value == a.id_asignacion
                        }).ToList();
                }

                // Filtros
                IEnumerable<AsignacionEntity> consulta = todas;

                if (esDocente && !string.IsNullOrEmpty(idEstudiante))
                {
                    consulta = consulta.Where(a => a.fk_id_usuario == idEstudiante);
                }

                if (esDocente && idAsignacion.HasValue)
                {
                    AsignacionEntity rep = todas.FirstOrDefault(a => a.id_asignacion == idAsignacion.Value);
                    if (rep != null)
                    {
                        string clave = GrupoClave(rep);
                        consulta = consulta.Where(a => GrupoClave(a) == clave);
                    }
                }

                // Escenario 3: filtro por estado
                string filtro = (estado ?? "todas").Trim().ToLowerInvariant();
                switch (filtro)
                {
                    case "pendientes": consulta = consulta.Where(a => !HayEntrega(a)); break;
                    case "entregadas": consulta = consulta.Where(a => HayEntrega(a) && !EsCalificada(a)); break;
                    case "calificadas": consulta = consulta.Where(a => EsCalificada(a)); break;
                    default: filtro = "todas"; break;
                }

                // Ordenadas por fecha (apertura, la más reciente primero)
                List<AsignacionEntity> ordenadas = consulta
                    .OrderByDescending(a => a.fecha_apertura ?? DateTime.MinValue)
                    .ThenBy(a => a.nombre)
                    .ThenBy(a => nombreDe(a.fk_id_usuario))
                    .ToList();

                var parametros = new Dictionary<string, string>
                {
                    { "idMateria", mat.ToString() },
                    { "idSeccion", sec.ToString() },
                    { "idEstudiante", esDocente ? idEstudiante : "" },
                    { "idAsignacion", esDocente && idAsignacion.HasValue ? idAsignacion.Value.ToString() : "" },
                    { "estado", filtro == "todas" ? "" : filtro }
                };
                PaginacionVm paginacion = Paginar(ordenadas.Count, FilasPorPaginaHistorial, ref pagina,
                    Url.Action("Historial"), parametros);

                List<HistorialItemVm> items = ordenadas
                    .Skip((pagina - 1) * FilasPorPaginaHistorial)
                    .Take(FilasPorPaginaHistorial)
                    .Select(a => new HistorialItemVm
                    {
                        id_asignacion = a.id_asignacion,
                        estudiante = nombreDe(a.fk_id_usuario),
                        asignacion = a.nombre,
                        apertura = a.fecha_apertura,
                        cierre = a.fecha_cierre,
                        estado = EstadoVisible(a),
                        fechaEntrega = a.fecha_entrega,
                        porcentaje = a.porcentaje_nota,
                        calificacion = a.calificacion,
                        calificada = EsCalificada(a),
                        entrego = HayEntrega(a)
                    }).ToList();

                MateriaEntity materia = db.Materias.FirstOrDefault(m => m.id_materia == mat);
                string etiquetaSeccion;
                HorarioServicio.EtiquetasDeSecciones(db).TryGetValue(sec, out etiquetaSeccion);

                var modelo = new HistorialVm
                {
                    idMateria = mat,
                    idSeccion = sec,
                    nombreMateria = materia != null ? materia.nombre : "",
                    etiquetaSeccion = etiquetaSeccion ?? "",
                    esDocente = esDocente,
                    esEstudiante = esEstudiante,
                    hayRegistros = hayRegistros,
                    estado = filtro,
                    estudiantes = selEstudiantes,
                    asignaciones = selAsignaciones,
                    total = ordenadas.Count,
                    items = items,
                    paginacion = paginacion
                };

                return View(modelo);
            }
        }

        // =====================================================================
        // AUXILIARES
        // =====================================================================

        public static bool EsCalificada(AsignacionEntity a)
        {
            return (a.estado ?? "").StartsWith("calificad", StringComparison.OrdinalIgnoreCase);
        }

        public static bool HayEntrega(AsignacionEntity a)
        {
            string estado = a.estado ?? "";
            return !string.IsNullOrEmpty(a.archivo_ruta)
                || estado.StartsWith("entreg", StringComparison.OrdinalIgnoreCase)
                || estado.StartsWith("calificad", StringComparison.OrdinalIgnoreCase);
        }

        // "Pendiente" con la fecha de cierre vencida se muestra como "Atrasada"
        public static string EstadoVisible(AsignacionEntity a)
        {
            string estado = string.IsNullOrEmpty(a.estado) ? "Pendiente" : a.estado;

            if (string.Equals(estado, "Pendiente", StringComparison.OrdinalIgnoreCase) &&
                a.fecha_cierre.HasValue && a.fecha_cierre.Value < DateTime.Now)
            {
                return "Atrasada";
            }
            return estado;
        }

        private static bool EsEstudiante(UsuarioDto usuario)
        {
            return RolesSistema.Es(usuario, RolesSistema.Estudiante);
        }

        private static bool EsDocenteDeLaClase(EduNexusDbContext db, UsuarioDto usuario, int? idMateria, int? idSeccion)
        {
            if (usuario == null || !idMateria.HasValue || !idSeccion.HasValue) { return false; }
            if (!RolesSistema.Es(usuario, RolesSistema.Docente)) { return false; }

            return db.SeccionesMaterias.Any(sm =>
                sm.fk_id_materia == idMateria.Value &&
                sm.fk_id_seccion == idSeccion.Value &&
                sm.fk_id_profesor == usuario.id_usuario);
        }

        // Docente de la clase, o Director/Administrador (solo consulta)
        private static bool TieneAccesoALaClase(EduNexusDbContext db, UsuarioDto usuario, int idMateria, int idSeccion)
        {
            return AulaVirtualController.ObtenerClasesVisibles(db, usuario)
                .Any(c => c.fk_id_materia == idMateria && c.fk_id_seccion == idSeccion);
        }

        private static string GrupoClave(AsignacionEntity a)
        {
            return !string.IsNullOrEmpty(a.id_grupo) ? a.id_grupo : "n:" + a.nombre;
        }

        // Todas las filas (una por estudiante) de la misma asignación
        private static IQueryable<AsignacionEntity> FilasDeLaAsignacion(EduNexusDbContext db, AsignacionEntity referencia)
        {
            if (!string.IsNullOrEmpty(referencia.id_grupo))
            {
                string grupo = referencia.id_grupo;
                return db.Asignaciones.Where(a => a.id_grupo == grupo);
            }

            int seccion = referencia.fk_id_seccion;
            int? materia = referencia.fk_id_materia;
            string nombre = referencia.nombre;
            return db.Asignaciones.Where(a =>
                (a.id_grupo == null || a.id_grupo == "") &&
                a.fk_id_seccion == seccion && a.fk_id_materia == materia && a.nombre == nombre);
        }

        private static AsignacionEntity ObtenerFilaDelEstudiante(EduNexusDbContext db, UsuarioDto usuario, int idAsignacion)
        {
            if (!EsEstudiante(usuario)) { return null; }
            return db.Asignaciones.FirstOrDefault(a =>
                a.id_asignacion == idAsignacion && a.fk_id_usuario == usuario.id_usuario);
        }

        // null = se puede entregar; si no, el motivo
        private static string MotivoBloqueo(AsignacionEntity a)
        {
            DateTime ahora = DateTime.Now;

            if (EsCalificada(a))
                return "La asignación ya fue calificada; no se puede modificar la entrega.";

            if (a.fecha_apertura.HasValue && ahora < a.fecha_apertura.Value)
                return "La asignación aún no está abierta para entregas.";

            if (a.fecha_cierre.HasValue && ahora > a.fecha_cierre.Value)
                return "La fecha de cierre ya pasó; no se reciben más entregas.";

            return null;
        }

        private static string FormatosPermitidos()
        {
            return string.Join(", ", ExtensionesPermitidas.Select(e => e.TrimStart('.').ToUpperInvariant()));
        }

        private static EntregarVm ConstruirEntregarVm(EduNexusDbContext db, AsignacionEntity a)
        {
            int idMateria = a.fk_id_materia ?? 0;
            var materia = db.Materias.FirstOrDefault(m => m.id_materia == idMateria);
            string motivo = MotivoBloqueo(a);

            return new EntregarVm
            {
                id_asignacion = a.id_asignacion,
                idMateria = idMateria,
                idSeccion = a.fk_id_seccion,
                nombreMateria = materia != null ? materia.nombre : "",
                nombre = a.nombre,
                descripcion = a.descripcion,
                porcentaje = a.porcentaje_nota,
                calificacion = a.calificacion,
                apertura = a.fecha_apertura,
                cierre = a.fecha_cierre,
                fechaEntrega = a.fecha_entrega,
                estado = EstadoVisible(a),
                archivoNombre = a.archivo_nombre,
                yaEntrego = !string.IsNullOrEmpty(a.archivo_ruta),
                puedeEntregar = motivo == null,
                motivoBloqueo = motivo,
                formatos = FormatosPermitidos(),
                tamanoMaxMb = TamanoMaximoMb
            };
        }

        private static void CompletarDatos(EduNexusDbContext db, CrearAsignacionVm modelo)
        {
            var materia = db.Materias.FirstOrDefault(m => m.id_materia == modelo.idMateria);
            modelo.nombreMateria = materia != null ? materia.nombre : "";

            Dictionary<int, string> etiquetas = HorarioServicio.EtiquetasDeSecciones(db);
            string etiqueta;
            modelo.etiquetaSeccion = etiquetas.TryGetValue(modelo.idSeccion, out etiqueta) ? etiqueta : "";

            modelo.cantidadEstudiantes = db.SeccionesEstudiantes.Count(se => se.fk_id_seccion == modelo.idSeccion);

            // Porcentaje ya usado: se cuenta una vez por asignación (no una vez por estudiante)
            var filas = db.Asignaciones
                .Where(a => a.fk_id_materia == modelo.idMateria && a.fk_id_seccion == modelo.idSeccion)
                .Select(a => new { a.id_grupo, a.nombre, a.porcentaje_nota })
                .ToList();

            decimal usado = filas
                .GroupBy(f => !string.IsNullOrEmpty(f.id_grupo) ? f.id_grupo : "n:" + f.nombre)
                .Sum(g => g.First().porcentaje_nota);

            modelo.porcentajeDisponible = Math.Max(0m, 100m - usado);
        }

        private static bool TryParseDecimal(string texto, out decimal valor)
        {
            return decimal.TryParse((texto ?? "").Trim().Replace(',', '.'),
                NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
        }

        private static string Recortar(string texto, int max)
        {
            texto = texto ?? "";
            return texto.Length <= max ? texto : texto.Substring(0, max);
        }

        private static PaginacionVm Paginar(int total, int porPagina, ref int pagina, string rutaBase, Dictionary<string, string> parametros)
        {
            int totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)porPagina));
            pagina = Math.Min(Math.Max(1, pagina), totalPaginas);

            return new PaginacionVm
            {
                pagina = pagina,
                totalPaginas = totalPaginas,
                totalRegistros = total,
                rutaBase = rutaBase,
                parametros = parametros
            };
        }
    }

    // =========================================================================
    // MODELOS DE VISTA
    // =========================================================================

    public class CrearAsignacionVm
    {
        public int idMateria { get; set; }
        public int idSeccion { get; set; }

        public string nombre { get; set; }
        public string descripcion { get; set; }
        public string porcentaje { get; set; }
        public string fechaApertura { get; set; }
        public string fechaCierre { get; set; }

        public string nombreMateria { get; set; }
        public string etiquetaSeccion { get; set; }
        public int cantidadEstudiantes { get; set; }
        public decimal porcentajeDisponible { get; set; }
    }

    public class EntregarVm
    {
        public int id_asignacion { get; set; }
        public int idMateria { get; set; }
        public int idSeccion { get; set; }
        public string nombreMateria { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public decimal porcentaje { get; set; }
        public decimal calificacion { get; set; }
        public DateTime? apertura { get; set; }
        public DateTime? cierre { get; set; }
        public DateTime? fechaEntrega { get; set; }
        public string estado { get; set; }
        public string archivoNombre { get; set; }
        public bool yaEntrego { get; set; }
        public bool puedeEntregar { get; set; }
        public string motivoBloqueo { get; set; }
        public string formatos { get; set; }
        public int tamanoMaxMb { get; set; }
    }

    public class ResumenAsignacionVm
    {
        public int total { get; set; }
        public int entregadas { get; set; }
        public int calificadas { get; set; }
    }

    public class PaginacionVm
    {
        public int pagina { get; set; }
        public int totalPaginas { get; set; }
        public int totalRegistros { get; set; }
        public string rutaBase { get; set; }
        public Dictionary<string, string> parametros { get; set; }
    }

    public class EstudianteEntregaVm
    {
        public int id_asignacion { get; set; }
        public string nombre { get; set; }
        public string identificacion { get; set; }
        public string estado { get; set; }
        public bool entrego { get; set; }
        public bool calificada { get; set; }
        public bool tieneArchivo { get; set; }
        public string archivoNombre { get; set; }
        public DateTime? fechaEntrega { get; set; }
        public decimal calificacion { get; set; }
    }

    public class DetalleAsignacionVm
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public decimal porcentaje { get; set; }
        public DateTime? apertura { get; set; }
        public DateTime? cierre { get; set; }
        public int idMateria { get; set; }
        public int idSeccion { get; set; }
        public string nombreMateria { get; set; }
        public string etiquetaSeccion { get; set; }
        public bool puedeCalificar { get; set; }
        public int total { get; set; }
        public int entregaron { get; set; }
        public int calificadas { get; set; }
        public string q { get; set; }
        public string estado { get; set; }
        public int filtrados { get; set; }
        public List<EstudianteEntregaVm> items { get; set; }
        public PaginacionVm paginacion { get; set; }
    }

    public class HistorialItemVm
    {
        public int id_asignacion { get; set; }
        public string estudiante { get; set; }
        public string asignacion { get; set; }
        public DateTime? apertura { get; set; }
        public DateTime? cierre { get; set; }
        public string estado { get; set; }
        public DateTime? fechaEntrega { get; set; }
        public decimal porcentaje { get; set; }
        public decimal calificacion { get; set; }
        public bool calificada { get; set; }
        public bool entrego { get; set; }
    }

    public class HistorialVm
    {
        public int idMateria { get; set; }
        public int idSeccion { get; set; }
        public string nombreMateria { get; set; }
        public string etiquetaSeccion { get; set; }
        public bool esDocente { get; set; }
        public bool esEstudiante { get; set; }
        public bool hayRegistros { get; set; }
        public string estado { get; set; }
        public List<SelectListItem> estudiantes { get; set; }
        public List<SelectListItem> asignaciones { get; set; }
        public int total { get; set; }
        public List<HistorialItemVm> items { get; set; }
        public PaginacionVm paginacion { get; set; }
    }
}