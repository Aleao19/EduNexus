using EduNexus.Abstracciones.ModelosParaUI.Asistencia;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RASI-10-001 Como docente necesito registrar la asistencia por presente, tardía,
    // ausencia justificada o no justificada
    // Se usa en la página del menú Asistencia y en la pestaña Asistencia del Aula Virtual.
    public class AsistenciaController : Controller
    {
        private class ClaseDocente
        {
            public int id_seccion { get; set; }
            public int id_materia { get; set; }
            public string valor { get; set; }
            public string texto { get; set; }
            public DateTime? inicio_periodo { get; set; }
            public DateTime? fin_periodo { get; set; }
        }

        // Solo el rol Docente puede registrar asistencia
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;
            if (!RolesSistema.Es(usuario, RolesSistema.Docente))
            {
                TempData["MensajeError"] = "Esta funcionalidad es exclusiva de los docentes.";
                filterContext.Result = RedirectToAction("Index", "Home");
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // ===================== Página del menú Asistencia =====================

        public ActionResult Registrar(string clase, DateTime? fecha)
        {
            return MostrarFormulario(clase, fecha, false);
        }

        public ActionResult Editar(string clase, DateTime? fecha)
        {
            return MostrarFormulario(clase, fecha, true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Guardar(AsistenciaFormDto modelo)
        {
            if (string.IsNullOrEmpty(modelo.clase))
            {
                return RedirectToAction("Registrar");
            }

            if (ProcesarGuardado(modelo))
            {
                TempData["Mensaje"] = MensajeExito(modelo);
                return RedirectToAction("Registrar", new { clase = modelo.clase, fecha = modelo.fecha.Value.ToString("yyyy-MM-dd") });
            }

            AplicarUrls(modelo, false);
            return View("Registrar", modelo);
        }

        // ===================== Pestaña Asistencia del Aula Virtual =====================

        [HttpGet]
        public ActionResult Lista(string clase, DateTime? fecha, bool editar = false)
        {
            using (var db = new EduNexusDbContext())
            {
                var modelo = new AsistenciaFormDto
                {
                    clase = clase,
                    fecha = (fecha ?? DateTime.Today).Date,
                    esEdicion = editar
                };

                if (!string.IsNullOrEmpty(clase))
                {
                    CargarLista(db, modelo, null);
                }

                AplicarUrls(modelo, true);
                return PartialView("_ListaAsistencia", modelo);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarLista(AsistenciaFormDto modelo)
        {
            if (string.IsNullOrEmpty(modelo.clase))
            {
                return PartialView("_ListaAsistencia", new AsistenciaFormDto { mensajeError = "Seleccione una clase." });
            }

            if (ProcesarGuardado(modelo))
            {
                using (var db = new EduNexusDbContext())
                {
                    // Se vuelve a mostrar la lista guardada, ya en modo consulta
                    modelo.esEdicion = false;
                    CargarLista(db, modelo, null);
                    modelo.mensajeExito = MensajeExito(modelo);
                }
            }

            AplicarUrls(modelo, true);
            return PartialView("_ListaAsistencia", modelo);
        }

        // ===================== Lógica compartida =====================

        private ActionResult MostrarFormulario(string clase, DateTime? fecha, bool esEdicion)
        {
            using (var db = new EduNexusDbContext())
            {
                var modelo = new AsistenciaFormDto
                {
                    clase = clase,
                    fecha = (fecha ?? DateTime.Today).Date,
                    esEdicion = esEdicion
                };

                CargarClases(db, modelo);

                if (!string.IsNullOrEmpty(clase))
                {
                    CargarLista(db, modelo, null);
                }

                AplicarUrls(modelo, false);
                return View("Registrar", modelo);
            }
        }

        // Valida y guarda. Devuelve true si se guardó. Si no, deja los errores en ModelState.
        private bool ProcesarGuardado(AsistenciaFormDto modelo)
        {
            using (var db = new EduNexusDbContext())
            {
                modelo.fecha = (modelo.fecha ?? DateTime.Today).Date;

                Dictionary<string, string> enviados = (modelo.estudiantes ?? new List<AsistenciaEstudianteDto>())
                    .Where(e => e.id_usuario != null)
                    .GroupBy(e => e.id_usuario)
                    .ToDictionary(g => g.Key, g => g.First().estado);

                // Se reconstruye la lista desde la base de datos, no desde lo que envió el navegador
                ModelState.Clear();
                CargarClases(db, modelo);
                CargarLista(db, modelo, enviados);

                if (!string.IsNullOrEmpty(modelo.mensajeError))
                {
                    return false;
                }

                if (!modelo.estudiantes.Any())
                {
                    ModelState.AddModelError("", "La sección no tiene estudiantes asignados.");
                    return false;
                }

                // Escenario 3: la fecha ya tiene lista guardada, no se permite crear otra
                if (modelo.yaRegistrado && !modelo.esEdicion)
                {
                    CargarLista(db, modelo, null);   // se muestran los estados que ya estaban guardados
                    return false;
                }

                // Escenario 2: estudiantes sin estado asignado
                foreach (AsistenciaEstudianteDto est in modelo.estudiantes)
                {
                    est.sinMarcar = string.IsNullOrEmpty(est.estado) || !AsistenciaFormDto.Estados.Contains(est.estado);
                }

                if (modelo.estudiantes.Any(e => e.sinMarcar))
                {
                    ModelState.AddModelError("", "Debe asignar un estado a todos los estudiantes antes de guardar. Los pendientes están resaltados.");
                    return false;
                }

                // Escenario 1: se guarda la lista completa (o se actualiza si se está editando)
                string[] partes = modelo.clase.Split('-');
                int idSeccion = int.Parse(partes[0]);
                int idMateria = int.Parse(partes[1]);
                DateTime fecha = modelo.fecha.Value;
                string idDocente = IdDocente();

                List<AsistenciaEntity> existentes = db.Asistencias
                    .Where(a => a.fk_id_seccion == idSeccion && a.fk_id_materia == idMateria && a.fecha == fecha)
                    .ToList();

                Dictionary<string, AsistenciaEstudianteDto> porEstudiante = modelo.estudiantes
                    .ToDictionary(e => e.id_usuario, e => e);

                // Bitácora: resumen por estudiante antes y después del cambio
                var antes = existentes.Any()
                    ? existentes.OrderBy(x => x.fk_id_usuario).Select(x => new
                    {
                        estudiante = porEstudiante.ContainsKey(x.fk_id_usuario) ? porEstudiante[x.fk_id_usuario].identificacion : x.fk_id_usuario,
                        x.estado
                    }).ToList()
                    : null;

                foreach (AsistenciaEstudianteDto est in modelo.estudiantes)
                {
                    AsistenciaEntity fila = existentes.FirstOrDefault(x => x.fk_id_usuario == est.id_usuario);

                    if (fila == null)
                    {
                        db.Asistencias.Add(new AsistenciaEntity
                        {
                            fecha = fecha,
                            estado = est.estado,
                            fk_id_seccion = idSeccion,
                            fk_id_materia = idMateria,
                            fk_id_usuario = est.id_usuario,
                            fk_id_registrado_por = idDocente,
                            fecha_registro = DateTime.Now
                        });
                    }
                    else
                    {
                        fila.estado = est.estado;
                        fila.fk_id_registrado_por = idDocente;
                        fila.fecha_registro = DateTime.Now;
                    }
                }

                bool esEdicion = existentes.Any();

                Auditoria.Registrar(
                    db,
                    esEdicion ? "Editar asistencia" : "Registrar asistencia",
                    (esEdicion ? "Se modificó" : "Se registró") + " la asistencia de " + modelo.titulo
                        + " del " + fecha.ToString("dd/MM/yyyy") + " (" + modelo.estudiantes.Count + " estudiantes).",
                    antes,
                    modelo.estudiantes.OrderBy(e => e.id_usuario).Select(e => new
                    {
                        estudiante = e.identificacion ?? e.id_usuario,
                        e.estado
                    }).ToList());

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    // Otro registro de la misma lista se guardó al mismo tiempo
                    // (índice único fecha + sección + materia + estudiante).
                    System.Diagnostics.Trace.TraceError("Error guardando asistencia: " + ex);
                    ModelState.AddModelError("", "No se pudo guardar la asistencia porque ya existe un registro para alguno de los estudiantes en esta fecha. Recargue la lista e intente de nuevo.");
                    return false;
                }

                return true;
            }
        }

        private static string MensajeExito(AsistenciaFormDto modelo)
        {
            return "La asistencia del " + modelo.fecha.Value.ToString("dd/MM/yyyy")
                + " se guardó correctamente para " + modelo.estudiantes.Count + " estudiantes.";
        }

        // Direcciones de guardar, editar y cancelar según dónde se muestre la lista
        private void AplicarUrls(AsistenciaFormDto modelo, bool ajax)
        {
            string fecha = modelo.fecha.HasValue ? modelo.fecha.Value.ToString("yyyy-MM-dd") : "";
            modelo.modoAjax = ajax;

            if (ajax)
            {
                modelo.urlGuardar = Url.Action("GuardarLista", "Asistencia");
                modelo.urlEditar = Url.Action("Lista", "Asistencia", new { clase = modelo.clase, fecha = fecha, editar = true });
                modelo.urlCancelar = Url.Action("Lista", "Asistencia", new { clase = modelo.clase, fecha = fecha });
            }
            else
            {
                modelo.urlGuardar = Url.Action("Guardar", "Asistencia");
                modelo.urlEditar = Url.Action("Editar", "Asistencia", new { clase = modelo.clase, fecha = fecha });
                modelo.urlCancelar = Url.Action("Registrar", "Asistencia", new { clase = modelo.clase, fecha = fecha });
            }
        }

        private void CargarClases(EduNexusDbContext db, AsistenciaFormDto modelo)
        {
            List<ClaseDocente> clases = ObtenerClases(db, IdDocente());
            ViewBag.Clases = new SelectList(clases.Select(c => new { c.valor, c.texto }).ToList(), "valor", "texto", modelo.clase);
            modelo.sinClases = !clases.Any();
        }

        // Carga los estudiantes de la sección. "enviados" trae lo marcado por el docente al guardar.
        // Si no se envía nada y la fecha ya tiene lista, se cargan los estados guardados.
        private void CargarLista(EduNexusDbContext db, AsistenciaFormDto modelo, Dictionary<string, string> enviados)
        {
            ClaseDocente clase = ObtenerClases(db, IdDocente()).FirstOrDefault(c => c.valor == modelo.clase);
            if (clase == null)
            {
                modelo.mensajeError = "La sección seleccionada no está asignada a usted.";
                return;
            }

            modelo.titulo = clase.texto;

            // La fecha no puede ser futura y debe estar dentro del período lectivo de la sección
            DateTime fechaLista = modelo.fecha.Value.Date;
            if (fechaLista > DateTime.Today)
            {
                modelo.mensajeError = "No se puede registrar asistencia de una fecha futura.";
                return;
            }
            if (clase.inicio_periodo.HasValue && clase.fin_periodo.HasValue &&
                (fechaLista < clase.inicio_periodo.Value.Date || fechaLista > clase.fin_periodo.Value.Date))
            {
                modelo.mensajeError = "La fecha debe estar dentro del período lectivo de la sección ("
                    + clase.inicio_periodo.Value.ToString("dd/MM/yyyy") + " al "
                    + clase.fin_periodo.Value.ToString("dd/MM/yyyy") + ").";
                return;
            }

            int idSeccion = clase.id_seccion;
            int idMateria = clase.id_materia;
            DateTime fecha = modelo.fecha.Value.Date;

            List<string> idsEstudiantes = db.SeccionesEstudiantes
                .Where(se => se.fk_id_seccion == idSeccion)
                .Select(se => se.fk_id_usuario).ToList();

            List<UsuarioEntity> estudiantes = db.Usuarios
                .Where(u => idsEstudiantes.Contains(u.id_usuario)).ToList()
                .OrderBy(u => u.apellido1).ThenBy(u => u.nombre).ToList();

            List<AsistenciaEntity> existentes = db.Asistencias
                .Where(a => a.fk_id_seccion == idSeccion && a.fk_id_materia == idMateria && a.fecha == fecha)
                .ToList();

            modelo.yaRegistrado = existentes.Any();

            modelo.estudiantes = estudiantes.Select(u =>
            {
                string estado = null;

                if (enviados != null)
                {
                    enviados.TryGetValue(u.id_usuario, out estado);
                }
                else
                {
                    AsistenciaEntity fila = existentes.FirstOrDefault(x => x.fk_id_usuario == u.id_usuario);
                    estado = fila != null ? fila.estado : null;
                }

                return new AsistenciaEstudianteDto
                {
                    id_usuario = u.id_usuario,
                    nombre = HorarioServicio.NombreCompleto(u),
                    identificacion = u.identificacion,
                    estado = estado
                };
            }).ToList();
        }

        // Clases (sección + materia) del docente en el período lectivo actual
        private List<ClaseDocente> ObtenerClases(EduNexusDbContext db, string idDocente)
        {
            DateTime hoy = DateTime.Today;

            Dictionary<int, CalendarioEntity> calendarios = db.Calendarios
                .Where(c => c.fecha_inicio <= hoy && c.fecha_fin >= hoy)
                .ToList()
                .ToDictionary(c => c.id_calendario, c => c);

            List<int> idsCalendario = calendarios.Keys.ToList();

            Dictionary<int, int> calendarioDeSeccion = db.Secciones
                .Where(s => idsCalendario.Contains(s.fk_id_calendario))
                .ToList()
                .ToDictionary(s => s.id_seccion, s => s.fk_id_calendario);

            List<int> idsSecciones = calendarioDeSeccion.Keys.ToList();

            // Solo las clases donde él es el docente de esa sección
            List<SeccionMateriaEntity> asignadas = db.SeccionesMaterias
                .Where(sm => sm.fk_id_profesor == idDocente && idsSecciones.Contains(sm.fk_id_seccion))
                .ToList();

            List<int> idsMaterias = asignadas.Select(sm => sm.fk_id_materia).Distinct().ToList();

            Dictionary<int, string> etiquetas = HorarioServicio.EtiquetasDeSecciones(db);
            Dictionary<int, string> nombresMaterias = db.Materias
                .Where(m => idsMaterias.Contains(m.id_materia)).ToList()
                .ToDictionary(m => m.id_materia, m => m.nombre);

            return asignadas.Select(sm => new ClaseDocente
            {
                id_seccion = sm.fk_id_seccion,
                id_materia = sm.fk_id_materia,
                valor = sm.fk_id_seccion + "-" + sm.fk_id_materia,
                texto = (etiquetas.ContainsKey(sm.fk_id_seccion) ? etiquetas[sm.fk_id_seccion] : "Sección")
                        + " - " + (nombresMaterias.ContainsKey(sm.fk_id_materia) ? nombresMaterias[sm.fk_id_materia] : "Materia"),
                inicio_periodo = calendarios[calendarioDeSeccion[sm.fk_id_seccion]].fecha_inicio,
                fin_periodo = calendarios[calendarioDeSeccion[sm.fk_id_seccion]].fecha_fin
            })
            .OrderBy(c => c.texto)
            .ToList();
        }

        private string IdDocente()
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;
            return usuario != null ? usuario.id_usuario : null;
        }
    }
}