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

        // =========================================
        // LISTADO DE EXPEDIENTES
        // =========================================

        public ActionResult ListadoDeExpedientes()
        {
            return View(Consultar(null));
        }

        // =========================================
        // DETALLE DE EXPEDIENTE
        // =========================================

        public ActionResult DetalleDeExpediente(int? id)
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

            return View(expediente);
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
                return RedirectToAction("DetalleDeExpediente", new { id = entidad.id_expediente });
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

            if (string.IsNullOrEmpty(expediente.identificacion))
            {
                TempData["Error"] = "La prematrícula ligada a este expediente no existe; no se pueden editar los datos personales.";
                return RedirectToAction("DetalleDeExpediente", new { id = id.Value });
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

            if (prematricula == null)
            {
                TempData["Error"] = "La prematrícula ligada a este expediente no existe; no se pueden editar los datos personales.";
                return RedirectToAction("DetalleDeExpediente", new { id = entidad.id_expediente });
            }

            // La identificación se muestra siempre desde la base de datos.
            modelo.identificacion = prematricula.identificacion;
            modelo.id_matricula = entidad.fk_id_matricula;

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

                List<PrematriculaEntity> prematriculas = db.Prematriculas
                    .Where(p => p.identificacion == identificacion)
                    .ToList();

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
                return RedirectToAction("DetalleDeExpediente", new { id = entidad.id_expediente });
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

            var excluidas = new HashSet<string>(identificacionesConExpediente);

            Dictionary<int, string> grados = db.Grados.ToList()
                .ToDictionary(g => g.id_grado, g => g.nombre);

            var opciones = db.Prematriculas
                .Where(p => p.estado == EstadoAprobada)
                .ToList()
                .Where(p => !excluidas.Contains(p.identificacion))
                .GroupBy(p => p.identificacion)
                .Select(g => g.OrderByDescending(p => p.fecha_prematricula).First())
                .OrderBy(p => p.nombre)
                .ThenBy(p => p.apellido1)
                .Select(p => new SelectListItem
                {
                    Value = p.id_prematricula.ToString(),
                    Text = p.identificacion + " - " +
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
