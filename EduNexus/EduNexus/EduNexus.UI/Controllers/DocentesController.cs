using EduNexus.Abstracciones.ModelosParaUI.AsignacionesDocentes;
using EduNexus.Abstracciones.ModelosParaUI.Docentes;
using EduNexus.Abstracciones.ModelosParaUI.Materias;
using EduNexus.Abstracciones.ModelosParaUI.Secciones;
using EduNexus.Abstracciones.ModelosParaUI.Horarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System;

namespace EduNexus.UI.Controllers
{
    // Gestión de docentes (usuarios con rol Docente) y su carga académica (secciones_materias.fk_id_profesor)
    [AutorizarAdministrador]
    public class DocentesController : Controller
    {
        // Dominio institucional del MEP exigido para docentes (mismo criterio que UsuariosController)
        private const string DominioInstitucional = "@mep.go.cr";
        private const int LongitudMinimaContrasenna = 8;

        // GET: Docentes
        public ActionResult ListadoDeDocentes()
        {
            List<DocentesDto> docentes = ObtenerDocentes();
            return View(docentes);
        }

        // GET: Docentes/CrearDocente
        public ActionResult CrearDocente()
        {
            return View(new DocentesDto());
        }

        // POST: Docentes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DocentesDto docente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(docente.contrasenna_temporal))
                {
                    ModelState.AddModelError("contrasenna_temporal", "La contraseña temporal es obligatoria.");
                }
                else if (docente.contrasenna_temporal.Length < LongitudMinimaContrasenna)
                {
                    ModelState.AddModelError("contrasenna_temporal", "La contraseña temporal debe tener al menos " + LongitudMinimaContrasenna + " caracteres.");
                }

                if (!ModelState.IsValid)
                {
                    return VistaCrear(docente);
                }

                using (var db = new EduNexusDbContext())
                {
                    string idRolDocente = IdRolDocente(db);
                    if (idRolDocente == null)
                    {
                        ModelState.AddModelError("", "No existe el rol de Docente en el sistema.");
                        return VistaCrear(docente);
                    }

                    ValidarDatosDocente(db, docente, null);
                    if (!ModelState.IsValid)
                    {
                        return VistaCrear(docente);
                    }

                    var nuevoDocente = new UsuarioEntity
                    {
                        id_usuario = Guid.NewGuid().ToString(),
                        identificacion = docente.identificacion.Trim(),
                        nombre = docente.nombre.Trim(),
                        apellido1 = docente.apellido1.Trim(),
                        apellido2 = string.IsNullOrWhiteSpace(docente.apellido2) ? null : docente.apellido2.Trim(),
                        email = docente.email.Trim(),
                        telefono = string.IsNullOrWhiteSpace(docente.telefono) ? null : docente.telefono.Trim(),
                        rol = idRolDocente,
                        estado = true,
                        email_confirmado = false,
                        password_hash = ContrasennaHasher.Cifrar(docente.contrasenna_temporal),
                        security_stamp = Guid.NewGuid().ToString()
                    };

                    db.Usuarios.Add(nuevoDocente);
                    Auditoria.Registrar(db, "Crear docente",
                        "Se registró el docente " + HorarioServicio.NombreCompleto(nuevoDocente) + " (" + nuevoDocente.identificacion + ").",
                        null, DatosAuditoria(nuevoDocente));
                    db.SaveChanges();

                    TempData["Mensaje"] = "El docente " + HorarioServicio.NombreCompleto(nuevoDocente)
                        + " se registró correctamente. Comuníquele su contraseña temporal para el primer ingreso.";
                }

                return RedirectToAction("ListadoDeDocentes");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar el docente: " + ex.Message);
                return VistaCrear(docente);
            }
        }

        // GET: Docentes/EditarDocente/{id}
        public ActionResult EditarDocente(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(400);
            }

            DocentesDto docente = ObtenerDocentes().FirstOrDefault(x => x.id_docente == id);
            if (docente == null)
            {
                return HttpNotFound();
            }
            return View(docente);
        }

        // POST: Docentes/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, DocentesDto docente)
        {
            // El id llega en la ruta; si no, se toma del campo oculto del formulario
            id = string.IsNullOrWhiteSpace(id) ? docente.id_docente : id;
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(400);
            }
            docente.id_docente = id;

            // La contraseña no se cambia desde esta pantalla
            ModelState.Remove("contrasenna_temporal");

            if (docente.estado != "Activo" && docente.estado != "Inactivo")
            {
                ModelState.AddModelError("estado", "Seleccione un estado válido.");
            }

            try
            {
                using (var db = new EduNexusDbContext())
                {
                    string idRolDocente = IdRolDocente(db);
                    var docenteExistente = db.Usuarios.FirstOrDefault(u => u.id_usuario == id);

                    // Solo se editan usuarios con rol Docente
                    if (docenteExistente == null || idRolDocente == null || docenteExistente.rol != idRolDocente)
                    {
                        return HttpNotFound();
                    }

                    if (!ModelState.IsValid)
                    {
                        return View("EditarDocente", docente);
                    }

                    ValidarDatosDocente(db, docente, id);
                    if (!ModelState.IsValid)
                    {
                        return View("EditarDocente", docente);
                    }

                    var antes = DatosAuditoria(docenteExistente);

                    docenteExistente.identificacion = docente.identificacion.Trim();
                    docenteExistente.nombre = docente.nombre.Trim();
                    docenteExistente.apellido1 = docente.apellido1.Trim();
                    docenteExistente.apellido2 = string.IsNullOrWhiteSpace(docente.apellido2) ? null : docente.apellido2.Trim();
                    docenteExistente.email = docente.email.Trim();
                    docenteExistente.telefono = string.IsNullOrWhiteSpace(docente.telefono) ? null : docente.telefono.Trim();
                    docenteExistente.estado = docente.estado == "Activo";

                    Auditoria.Registrar(db, "Editar docente",
                        "Se modificó el docente " + HorarioServicio.NombreCompleto(docenteExistente) + " (" + docenteExistente.identificacion + ").",
                        antes, DatosAuditoria(docenteExistente));
                    db.SaveChanges();

                    TempData["Mensaje"] = "Los datos del docente " + HorarioServicio.NombreCompleto(docenteExistente) + " se actualizaron correctamente.";
                }

                return RedirectToAction("ListadoDeDocentes");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar el docente: " + ex.Message);
                return View("EditarDocente", docente);
            }
        }

        // GET: Docentes/CargaAcademicaDocente/{id}
        public ActionResult CargaAcademicaDocente(string id)
        {
            DocentesDto docente = ObtenerDocentes().FirstOrDefault(x => x.id_docente == id);
            if (docente == null)
            {
                return HttpNotFound();
            }

            List<AsignacionDocenteDto> asignaciones = ObtenerAsignaciones().Where(x => x.id_docente == id).ToList();
            ViewBag.IdDocente = id;
            ViewBag.NombreDocente = NombreCompleto(docente);
            ViewBag.Materias = ObtenerMaterias();
            ViewBag.Secciones = ObtenerSecciones();
            return View(asignaciones);
        }

        // GET: Docentes/AsignarDocente/{id}
        public ActionResult AsignarDocente(string id)
        {
            using (var db = new EduNexusDbContext())
            {
                if (BuscarDocente(db, id) == null)
                {
                    return HttpNotFound();
                }
                CargarDatosAsignacion(db, id, null, null);
            }
            return View(new AsignacionDocenteDto { id_docente = id });
        }

        // POST: Docentes/AsignarDocente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarDocente(AsignacionDocenteDto asignacion)
        {
            // Los combos vacíos se validan abajo con mensajes propios
            ModelState.Remove("id_materia");
            ModelState.Remove("id_seccion");

            using (var db = new EduNexusDbContext())
            {
                try
                {
                    UsuarioEntity docente = BuscarDocente(db, asignacion.id_docente);
                    if (docente == null)
                    {
                        return HttpNotFound();
                    }

                    if (!docente.estado)
                    {
                        ModelState.AddModelError("", "El docente está inactivo; actívelo antes de asignarle materias.");
                    }

                    MateriaEntity materia = asignacion.id_materia > 0
                        ? db.Materias.FirstOrDefault(m => m.id_materia == asignacion.id_materia)
                        : null;
                    if (materia == null)
                    {
                        ModelState.AddModelError("id_materia", "Seleccione una materia válida.");
                    }

                    SeccionEntity seccion = asignacion.id_seccion > 0
                        ? db.Secciones.FirstOrDefault(s => s.id_seccion == asignacion.id_seccion)
                        : null;
                    if (seccion == null)
                    {
                        ModelState.AddModelError("id_seccion", "Seleccione una sección válida.");
                    }

                    if (!ModelState.IsValid)
                    {
                        CargarDatosAsignacion(db, asignacion.id_docente, asignacion.id_materia, asignacion.id_seccion);
                        return View(asignacion);
                    }

                    SeccionMateriaEntity clase = db.SeccionesMaterias.FirstOrDefault(a =>
                        a.fk_id_materia == asignacion.id_materia && a.fk_id_seccion == asignacion.id_seccion);

                    if (clase != null && clase.fk_id_profesor == asignacion.id_docente)
                    {
                        ModelState.AddModelError("", "Esta materia y sección ya está asignada a este docente.");
                        CargarDatosAsignacion(db, asignacion.id_docente, asignacion.id_materia, asignacion.id_seccion);
                        return View(asignacion);
                    }

                    // El docente no puede tener choque con los horarios ya definidos para esta clase
                    string choque = HorarioServicio.BuscarChoqueDocente(db, asignacion.id_docente, asignacion.id_seccion, asignacion.id_materia);
                    if (choque != null)
                    {
                        ModelState.AddModelError("", choque);
                        CargarDatosAsignacion(db, asignacion.id_docente, asignacion.id_materia, asignacion.id_seccion);
                        return View(asignacion);
                    }

                    var etiquetas = HorarioServicio.EtiquetasDeSecciones(db);
                    string etiqueta = etiquetas.ContainsKey(seccion.id_seccion) ? etiquetas[seccion.id_seccion] : seccion.nombre;
                    string anterior = clase != null ? clase.fk_id_profesor : null;

                    if (clase == null)
                    {
                        db.SeccionesMaterias.Add(new SeccionMateriaEntity
                        {
                            fk_id_materia = asignacion.id_materia,
                            fk_id_seccion = asignacion.id_seccion,
                            fk_id_profesor = asignacion.id_docente,
                            fecha_asignacion = DateTime.Now
                        });
                    }
                    else
                    {
                        clase.fk_id_profesor = asignacion.id_docente;
                    }

                    Auditoria.Registrar(db, "Asignación de docente",
                        "Docente " + HorarioServicio.NombreCompleto(docente) + " asignado a " + materia.nombre + " en " + etiqueta + ".",
                        anterior == null ? null : new { id_seccion = seccion.id_seccion, id_materia = materia.id_materia, id_profesor = anterior },
                        new { id_seccion = seccion.id_seccion, seccion = etiqueta, id_materia = materia.id_materia, materia = materia.nombre, id_profesor = asignacion.id_docente });
                    db.SaveChanges();

                    TempData["Mensaje"] = anterior == null
                        ? "Se asignó " + materia.nombre + " en " + etiqueta + " al docente."
                        : "Se reasignó " + materia.nombre + " en " + etiqueta + " a este docente (reemplaza al docente anterior).";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al asignar materia: " + ex.Message);
                    CargarDatosAsignacion(db, asignacion.id_docente, asignacion.id_materia, asignacion.id_seccion);
                    return View(asignacion);
                }
            }

            return RedirectToAction("CargaAcademicaDocente", new { id = asignacion.id_docente });
        }

        private ActionResult VistaCrear(DocentesDto docente)
        {
            // La contraseña nunca se devuelve a la vista
            docente.contrasenna_temporal = null;
            return View("CrearDocente", docente);
        }

        // Identificación y correo únicos (comparación en memoria, sin mayúsculas) y correo institucional del MEP
        private void ValidarDatosDocente(EduNexusDbContext db, DocentesDto docente, string idExcluido)
        {
            var otros = db.Usuarios
                .Where(u => u.id_usuario != idExcluido)
                .Select(u => new { u.identificacion, u.email })
                .ToList();

            if (!string.IsNullOrWhiteSpace(docente.identificacion))
            {
                string identificacion = docente.identificacion.Trim();
                if (otros.Any(u => u.identificacion != null && u.identificacion.Trim().Equals(identificacion, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("identificacion", "Esta identificación ya está registrada.");
                }
            }

            if (!string.IsNullOrWhiteSpace(docente.email))
            {
                string correo = docente.email.Trim();
                if (otros.Any(u => u.email != null && u.email.Trim().Equals(correo, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("email", "El correo electrónico ya está en uso.");
                }
                if (!correo.EndsWith(DominioInstitucional, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("email", "Debe usarse un correo institucional del MEP (" + DominioInstitucional + ") para el rol Docente.");
                }
            }
        }

        private static object DatosAuditoria(UsuarioEntity u)
        {
            return new
            {
                u.id_usuario,
                u.identificacion,
                u.nombre,
                u.apellido1,
                u.apellido2,
                u.email,
                u.telefono,
                estado = u.estado ? "Activo" : "Inactivo"
            };
        }

        private static string IdRolDocente(EduNexusDbContext db)
        {
            return db.Roles.ToList()
                .Where(r => RolesSistema.Es(r.nombre, RolesSistema.Docente))
                .Select(r => r.id_rol)
                .FirstOrDefault();
        }

        // Usuario con rol Docente, o null
        private static UsuarioEntity BuscarDocente(EduNexusDbContext db, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }
            string idRol = IdRolDocente(db);
            return idRol == null ? null : db.Usuarios.FirstOrDefault(u => u.id_usuario == id && u.rol == idRol);
        }

        private static string NombreCompleto(DocentesDto d)
        {
            return string.Join(" ", new[] { d.nombre, d.apellido1, d.apellido2 }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        // Combos de materia y sección, y horarios ya registrados de cada clase (para el paso 3 de la vista)
        private void CargarDatosAsignacion(EduNexusDbContext db, string idDocente, int? idMateria, int? idSeccion)
        {
            UsuarioEntity docente = BuscarDocente(db, idDocente);
            ViewBag.NombreDocente = docente != null ? HorarioServicio.NombreCompleto(docente) : "";

            Dictionary<int, string> materias = db.Materias.OrderBy(m => m.nombre).ToList()
                .ToDictionary(m => m.id_materia, m => m.nombre);
            Dictionary<int, string> secciones = HorarioServicio.EtiquetasDeSecciones(db);

            ViewBag.Materias = new SelectList(materias.ToList(), "Key", "Value", idMateria);
            ViewBag.Secciones = new SelectList(secciones.OrderBy(s => s.Value).ToList(), "Key", "Value", idSeccion);
            ViewBag.NombresMaterias = materias;
            ViewBag.NombresSecciones = secciones;

            ViewBag.HorariosDisponibles = db.Horarios.ToList()
                .OrderBy(h => HorarioServicio.OrdenDia(h.dia_semana))
                .ThenBy(h => h.hora_inicio)
                .Select(h => new HorariosDto
                {
                    id_horario = h.id_horario,
                    dia_semana = h.dia_semana,
                    hora_inicio = h.hora_inicio,
                    hora_fin = h.hora_fin,
                    materia = h.fk_materia,
                    seccion = h.fk_seccion
                })
                .ToList();
        }

        private List<DocentesDto> ObtenerDocentes()
        {
            using (var db = new EduNexusDbContext())
            {
                string idRolDocente = IdRolDocente(db);

                if (idRolDocente == null)
                {
                    return new List<DocentesDto>();
                }

                var usuarios = db.Usuarios
                    .Where(u => u.rol == idRolDocente)
                    .OrderBy(u => u.nombre)
                    .ThenBy(u => u.apellido1)
                    .ToList();

                return usuarios.Select(u => new DocentesDto
                {
                    id_docente = u.id_usuario,
                    identificacion = u.identificacion,
                    nombre = u.nombre,
                    apellido1 = u.apellido1,
                    apellido2 = u.apellido2,
                    email = u.email,
                    telefono = u.telefono,
                    estado = u.estado ? "Activo" : "Inactivo"
                }).ToList();
            }
        }

        private List<AsignacionDocenteDto> ObtenerAsignaciones()
        {
            using (var db = new EduNexusDbContext())
            {
                return db.SeccionesMaterias.ToList()
                    .Select(a => new AsignacionDocenteDto
                    {
                        id_asignacion_docente = a.id_seccion_materia,
                        id_docente = a.fk_id_profesor,
                        id_materia = a.fk_id_materia,
                        id_seccion = a.fk_id_seccion
                    })
                    .ToList();
            }
        }

        private List<MateriasDto> ObtenerMaterias()
        {
            using (var db = new EduNexusDbContext())
            {
                return db.Materias
                    .OrderBy(m => m.nombre)
                    .ToList()
                    .Select(m => new MateriasDto
                    {
                        id_materia = m.id_materia,
                        nombre = m.nombre,
                        id_profesor = m.profesor
                    })
                    .ToList();
            }
        }

        private List<SeccionesDto> ObtenerSecciones()
        {
            using (var db = new EduNexusDbContext())
            {
                var grados = db.Grados.ToList().ToDictionary(g => g.id_grado, g => g.nombre);
                var calendarios = db.Calendarios.ToList().ToDictionary(c => c.id_calendario, c => c.fecha_inicio.Year);
                var conteo = db.SeccionesEstudiantes
                    .GroupBy(se => se.fk_id_seccion)
                    .Select(g => new { id = g.Key, total = g.Count() })
                    .ToList()
                    .ToDictionary(x => x.id, x => x.total);

                return db.Secciones
                    .ToList()
                    .Select(s => new SeccionesDto
                    {
                        id_seccion = s.id_seccion,
                        nombre = s.nombre,
                        grado = s.fk_id_grado,
                        nombre_grado = grados.ContainsKey(s.fk_id_grado) ? grados[s.fk_id_grado] : string.Empty,
                        anio = calendarios.ContainsKey(s.fk_id_calendario) ? (int?)calendarios[s.fk_id_calendario] : null,
                        id_calendario = s.fk_id_calendario,
                        cupo = s.cupo,
                        estudiantes_matriculados = conteo.ContainsKey(s.id_seccion) ? conteo[s.id_seccion] : 0,
                        fecha_modificacion = null,
                        usuario_modificacion = string.Empty
                    })
                    .ToList();
            }
        }
    }
}
