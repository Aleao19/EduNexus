using EduNexus.Abstracciones.ModelosParaUI.AsignacionesDocentes;
using EduNexus.Abstracciones.ModelosParaUI.Docentes;
using EduNexus.Abstracciones.ModelosParaUI.Horarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System;

namespace EduNexus.UI.Controllers
{
    // Gestión de docentes (usuarios con rol Docente) y su carga académica (secciones_materias.fk_id_profesor).
    // Flujo único: los docentes se registran aquí (no en Seguridad → Usuarios) y su ficha
    // (CargaAcademicaDocente) es el lugar para asignarle o quitarle materias y secciones.
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

                // Cédula con formato estándar 0-0000-0000
                string cedula = Cedula.Normalizar(docente.identificacion);
                if (!string.IsNullOrWhiteSpace(docente.identificacion) && cedula == null)
                {
                    ModelState.AddModelError("identificacion", Cedula.MensajeFormato);
                }
                else if (cedula != null)
                {
                    docente.identificacion = cedula;
                }

                if (!ModelState.IsValid)
                {
                    return VistaCrear(docente);
                }

                string idNuevo;
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
                        identificacion = cedula,
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
                    idNuevo = nuevoDocente.id_usuario;

                    TempData["Mensaje"] = "El docente " + HorarioServicio.NombreCompleto(nuevoDocente)
                        + " se registró correctamente. Comuníquele su contraseña temporal para el primer ingreso"
                        + " y asígnele sus materias y secciones desde esta ficha.";
                }

                // Se continúa en la ficha del docente para asignarle su carga académica
                return RedirectToAction("CargaAcademicaDocente", new { id = idNuevo });
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

            // La contraseña no se cambia desde esta pantalla y la cédula no es modificable
            ModelState.Remove("contrasenna_temporal");
            ModelState.Remove("identificacion");

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

                    // Cualquier valor de cédula enviado se ignora: se muestra siempre la guardada
                    docente.identificacion = Cedula.Normalizar(docenteExistente.identificacion) ?? docenteExistente.identificacion;

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

                    docenteExistente.nombre = docente.nombre.Trim();
                    docenteExistente.apellido1 = docente.apellido1.Trim();
                    docenteExistente.apellido2 = string.IsNullOrWhiteSpace(docente.apellido2) ? null : docente.apellido2.Trim();
                    docenteExistente.email = docente.email.Trim();
                    docenteExistente.telefono = string.IsNullOrWhiteSpace(docente.telefono) ? null : docente.telefono.Trim();
                    docenteExistente.estado = docente.estado == "Activo";

                    Auditoria.Registrar(db, "Editar docente",
                        "Se modificó el docente " + HorarioServicio.NombreCompleto(docenteExistente) + " (" + docente.identificacion + ").",
                        antes, DatosAuditoria(docenteExistente));
                    db.SaveChanges();

                    TempData["Mensaje"] = "Los datos del docente " + HorarioServicio.NombreCompleto(docenteExistente) + " se actualizaron correctamente.";
                }

                return RedirectToAction("CargaAcademicaDocente", new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar el docente: " + ex.Message);
                return View("EditarDocente", docente);
            }
        }

        // GET: Docentes/CargaAcademicaDocente/{id}
        // Ficha del docente: sus datos y las materias/secciones que tiene a cargo (secciones_materias.fk_id_profesor)
        public ActionResult CargaAcademicaDocente(string id)
        {
            DocentesDto docente = ObtenerDocentes().FirstOrDefault(x => x.id_docente == id);
            if (docente == null)
            {
                return HttpNotFound();
            }

            List<AsignacionDocenteDto> asignaciones;
            using (var db = new EduNexusDbContext())
            {
                List<SeccionMateriaEntity> clases = HorarioServicio.ClasesDelDocente(db, id);
                List<int> idsSecciones = clases.Select(c => c.fk_id_seccion).Distinct().ToList();
                List<int> idsMaterias = clases.Select(c => c.fk_id_materia).Distinct().ToList();

                Dictionary<int, string> materias = db.Materias
                    .Where(m => idsMaterias.Contains(m.id_materia)).ToList()
                    .ToDictionary(m => m.id_materia, m => m.nombre);
                Dictionary<int, SeccionEntity> secciones = db.Secciones
                    .Where(s => idsSecciones.Contains(s.id_seccion)).ToList()
                    .ToDictionary(s => s.id_seccion);
                Dictionary<int, string> etiquetas = HorarioServicio.EtiquetasDeSecciones(db);
                Dictionary<int, int> matriculados = db.SeccionesEstudiantes
                    .Where(se => idsSecciones.Contains(se.fk_id_seccion))
                    .GroupBy(se => se.fk_id_seccion)
                    .Select(g => new { id = g.Key, total = g.Count() })
                    .ToList()
                    .ToDictionary(x => x.id, x => x.total);
                List<HorarioEntity> horarios = db.Horarios
                    .Where(h => idsSecciones.Contains(h.fk_seccion)).ToList();

                asignaciones = clases.Select(c => new AsignacionDocenteDto
                {
                    id_asignacion_docente = c.id_seccion_materia,
                    id_docente = c.fk_id_profesor,
                    id_materia = c.fk_id_materia,
                    id_seccion = c.fk_id_seccion,
                    materia = materias.ContainsKey(c.fk_id_materia) ? materias[c.fk_id_materia] : "Materia " + c.fk_id_materia,
                    seccion = etiquetas.ContainsKey(c.fk_id_seccion) ? etiquetas[c.fk_id_seccion] : "Sección " + c.fk_id_seccion,
                    estudiantes_matriculados = matriculados.ContainsKey(c.fk_id_seccion) ? matriculados[c.fk_id_seccion] : 0,
                    cupo = secciones.ContainsKey(c.fk_id_seccion) ? secciones[c.fk_id_seccion].cupo : 0,
                    bloques_horario = horarios.Count(h => h.fk_seccion == c.fk_id_seccion && h.fk_materia == c.fk_id_materia),
                    fecha_asignacion = c.fecha_asignacion
                })
                .OrderBy(a => a.seccion)
                .ThenBy(a => a.materia)
                .ToList();
            }

            ViewBag.Docente = docente;
            ViewBag.IdDocente = id;
            ViewBag.NombreDocente = NombreCompleto(docente);
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

        // POST: Docentes/QuitarAsignacion
        // Deja la clase (sección + materia) sin este docente. Como secciones_materias.fk_id_profesor es
        // obligatorio, quitar al docente elimina la fila, pero solo si la clase no tiene horarios,
        // asistencia ni asignaciones (notas); si los tiene, debe reasignarse otro docente en su lugar.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QuitarAsignacion(int id, string idDocente)
        {
            if (string.IsNullOrWhiteSpace(idDocente))
            {
                return RedirectToAction("ListadoDeDocentes");
            }

            using (var db = new EduNexusDbContext())
            {
                SeccionMateriaEntity clase = db.SeccionesMaterias.FirstOrDefault(x => x.id_seccion_materia == id);
                if (clase == null || clase.fk_id_profesor != idDocente)
                {
                    TempData["Error"] = "La asignación no existe o ya no pertenece a este docente.";
                    return RedirectToAction("CargaAcademicaDocente", new { id = idDocente });
                }

                int idSeccion = clase.fk_id_seccion;
                int idMateria = clase.fk_id_materia;
                string nombreMateria = db.Materias.Where(m => m.id_materia == idMateria).Select(m => m.nombre).FirstOrDefault()
                                       ?? "Materia " + idMateria;
                string etiqueta;
                HorarioServicio.EtiquetasDeSecciones(db).TryGetValue(idSeccion, out etiqueta);
                etiqueta = etiqueta ?? "Sección " + idSeccion;

                int horarios = db.Horarios.Count(h => h.fk_seccion == idSeccion && h.fk_materia == idMateria);
                int asistencia = db.Asistencias.Count(a => a.fk_id_seccion == idSeccion && a.fk_id_materia == idMateria);
                int tareas = db.Asignaciones.Count(a => a.fk_id_seccion == idSeccion && a.fk_id_materia == idMateria);

                if (horarios + asistencia + tareas > 0)
                {
                    var detalle = new List<string>();
                    if (horarios > 0) detalle.Add(horarios + " bloque(s) de horario");
                    if (asistencia > 0) detalle.Add(asistencia + " registro(s) de asistencia");
                    if (tareas > 0) detalle.Add(tareas + " asignación(es) o nota(s)");

                    TempData["Error"] = "No se puede quitar al docente de " + nombreMateria + " en " + etiqueta
                        + " porque la clase ya tiene " + string.Join(", ", detalle)
                        + ". Reasigne otro docente en su lugar (desde la ficha del otro docente, \"Asignar materia y sección\").";
                    return RedirectToAction("CargaAcademicaDocente", new { id = idDocente });
                }

                UsuarioEntity docente = db.Usuarios.FirstOrDefault(u => u.id_usuario == idDocente);
                string nombreDocente = docente != null ? HorarioServicio.NombreCompleto(docente) : idDocente;

                db.SeccionesMaterias.Remove(clase);
                Auditoria.Registrar(db, "Quitar asignación de docente",
                    "Se quitó al docente " + nombreDocente + " de " + nombreMateria + " en " + etiqueta + " (la materia queda sin asignar en esa sección).",
                    new { id_seccion_materia = id, id_seccion = idSeccion, seccion = etiqueta, id_materia = idMateria, materia = nombreMateria, id_profesor = idDocente },
                    null);
                db.SaveChanges();

                TempData["Mensaje"] = "Se quitó " + nombreMateria + " en " + etiqueta + " de la carga del docente.";
            }

            return RedirectToAction("CargaAcademicaDocente", new { id = idDocente });
        }

        private ActionResult VistaCrear(DocentesDto docente)
        {
            // La contraseña nunca se devuelve a la vista
            docente.contrasenna_temporal = null;
            return View("CrearDocente", docente);
        }

        // Cédula (solo al registrar) y correo únicos (comparación en memoria) y correo institucional del MEP
        private void ValidarDatosDocente(EduNexusDbContext db, DocentesDto docente, string idExcluido)
        {
            var otros = db.Usuarios
                .Where(u => u.id_usuario != idExcluido)
                .Select(u => new { u.identificacion, u.email })
                .ToList();

            // La cédula solo se valida al registrar: después de creada no se modifica
            if (idExcluido == null && !string.IsNullOrWhiteSpace(docente.identificacion))
            {
                if (otros.Any(u => u.identificacion != null && Cedula.Iguales(u.identificacion, docente.identificacion)))
                {
                    ModelState.AddModelError("identificacion", "Esta cédula ya está registrada.");
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
                    // Se muestra siempre con formato 0-0000-0000 (datos viejos pueden venir sin guiones)
                    identificacion = Cedula.Normalizar(u.identificacion) ?? u.identificacion,
                    nombre = u.nombre,
                    apellido1 = u.apellido1,
                    apellido2 = u.apellido2,
                    email = u.email,
                    telefono = u.telefono,
                    estado = u.estado ? "Activo" : "Inactivo"
                }).ToList();
            }
        }
    }
}
