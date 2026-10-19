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
    public class DocentesController : Controller
    {
        // GET: Docentes
        public ActionResult ListadoDeDocentes()
        {
            List<DocentesDto> docentes = ObtenerDocentes();
            return View(docentes);
        }

        // GET: Docentes/Create
        public ActionResult CrearDocente()
        {
            return View();
        }

        // POST: Docentes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DocentesDto docente)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("CrearDocente", docente);
                }

                using (var db = new EduNexusDbContext())
                {
                    // Obtener el ID del rol de Docente
                    string idRolDocente = db.Roles
                        .Where(r => r.nombre == "Docente")
                        .Select(r => r.id_rol)
                        .FirstOrDefault();

                    if (idRolDocente == null)
                    {
                        ModelState.AddModelError("", "No existe el rol de Docente en el sistema.");
                        return View("CrearDocente", docente);
                    }

                    // Validar que la identificación sea única
                    var docenteExistente = db.Usuarios
                        .FirstOrDefault(u => u.identificacion == docente.identificacion);

                    if (docenteExistente != null)
                    {
                        ModelState.AddModelError("identificacion", "Esta identificación ya está registrada.");
                        return View("CrearDocente", docente);
                    }

                    // Crear nuevo usuario con rol de Docente
                    var nuevoDocente = new UsuarioEntity
                    {
                        id_usuario = Guid.NewGuid().ToString(),
                        identificacion = docente.identificacion,
                        nombre = docente.nombre,
                        apellido1 = docente.apellido1,
                        apellido2 = docente.apellido2,
                        email = docente.email,
                        telefono = docente.telefono,
                        rol = idRolDocente,
                        estado = true,
                        email_confirmado = false,
                        password_hash = "pendiente", // Implementar generación de contraseña
                        security_stamp = Guid.NewGuid().ToString()
                    };

                    db.Usuarios.Add(nuevoDocente);
                    db.SaveChanges();
                }

                return RedirectToAction("ListadoDeDocentes");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar el docente: " + ex.Message);
                return View("CrearDocente", docente);
            }
        }

        // GET: Docentes/Edit/5
        public ActionResult EditarDocente(string id)
        {
            List<DocentesDto> docentes = ObtenerDocentes();
            DocentesDto docente = docentes.FirstOrDefault(x => x.id_docente == id);
            return View(docente);
        }

        // POST: Docentes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, DocentesDto docente)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("EditarDocente", docente);
                }

                using (var db = new EduNexusDbContext())
                {
                    // Buscar el usuario existente
                    var docenteExistente = db.Usuarios.FirstOrDefault(u => u.id_usuario == id);

                    if (docenteExistente == null)
                    {
                        return HttpNotFound();
                    }

                    // Validar que la identificación sea única (si cambió)
                    if (docenteExistente.identificacion != docente.identificacion)
                    {
                        var identificacionDuplicada = db.Usuarios
                            .FirstOrDefault(u => u.identificacion == docente.identificacion && u.id_usuario != id);

                        if (identificacionDuplicada != null)
                        {
                            ModelState.AddModelError("identificacion", "Esta identificación ya está registrada.");
                            return View("EditarDocente", docente);
                        }
                    }

                    // Actualizar datos
                    docenteExistente.identificacion = docente.identificacion;
                    docenteExistente.nombre = docente.nombre;
                    docenteExistente.apellido1 = docente.apellido1;
                    docenteExistente.apellido2 = docente.apellido2;
                    docenteExistente.email = docente.email;
                    docenteExistente.telefono = docente.telefono;
                    docenteExistente.estado = docente.estado == "Activo";

                    db.SaveChanges();
                }

                return RedirectToAction("ListadoDeDocentes");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar el docente: " + ex.Message);
                return View("EditarDocente", docente);
            }
        }

        // GET: Docentes/CargaAcademicaDocente/5
        public ActionResult CargaAcademicaDocente(string id)
        {
            List<AsignacionDocenteDto> asignaciones = ObtenerAsignaciones().Where(x => x.id_docente == id).ToList();
            ViewBag.IdDocente = id;
            ViewBag.NombreDocente = ObtenerDocentes().FirstOrDefault(x => x.id_docente == id)?.nombre;
            ViewBag.Materias = ObtenerMaterias();
            ViewBag.Secciones = ObtenerSecciones();
            return View(asignaciones);
        }

        // GET: Docentes/AsignarDocente/5  
        public ActionResult AsignarDocente(string id)
        {
            ViewBag.NombreDocente = ObtenerDocentes().FirstOrDefault(x => x.id_docente == id)?.nombre;
            ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
            ViewBag.Secciones = new SelectList(ObtenerSecciones(), "id_seccion", "nombre");
            return View(new AsignacionDocenteDto { id_docente = id ?? string.Empty });
        }

        // POST: Docentes/AsignarDocente/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarDocente(AsignacionDocenteDto asignacion, FormCollection collection)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.NombreDocente = ObtenerDocentes().FirstOrDefault(x => x.id_docente == asignacion.id_docente)?.nombre;
                    ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                    ViewBag.Secciones = new SelectList(ObtenerSecciones(), "id_seccion", "nombre");
                    return View(asignacion);
                }

                using (var db = new EduNexusDbContext())
                {
                    // Validar que la materia y sección no estén ya asignadas al docente
                    var asignacionExistente = db.SeccionesMaterias
                        .FirstOrDefault(a => a.fk_id_materia == asignacion.id_materia && a.fk_id_seccion == asignacion.id_seccion);

                    if (asignacionExistente != null)
                    {
                        // Verificar si el profesor de esa materia es el docente actual
                        var materia = db.Materias.FirstOrDefault(m => m.id_materia == asignacion.id_materia);
                        if (materia?.profesor == asignacion.id_docente)
                        {
                            ModelState.AddModelError("", "Esta materia y sección ya está asignada a este docente.");
                            ViewBag.NombreDocente = ObtenerDocentes().FirstOrDefault(x => x.id_docente == asignacion.id_docente)?.nombre;
                            ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                            ViewBag.Secciones = new SelectList(ObtenerSecciones(), "id_seccion", "nombre");
                            return View(asignacion);
                        }
                    }

                    // Actualizar la materia para asignarla al docente
                    var materia2 = db.Materias.FirstOrDefault(m => m.id_materia == asignacion.id_materia);
                    if (materia2 != null)
                    {
                        materia2.profesor = asignacion.id_docente;
                        db.SaveChanges();
                    }
                }

                return RedirectToAction("CargaAcademicaDocente", new { id = asignacion.id_docente });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al asignar materia: " + ex.Message);
                ViewBag.NombreDocente = ObtenerDocentes().FirstOrDefault(x => x.id_docente == asignacion.id_docente)?.nombre;
                ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                ViewBag.Secciones = new SelectList(ObtenerSecciones(), "id_seccion", "nombre");
                return View(asignacion);
            }
        }

        private List<DocentesDto> ObtenerDocentes()
        {
            using (var db = new EduNexusDbContext())
            {
                string idRolDocente = db.Roles
                    .Where(r => r.nombre == "Docente")
                    .Select(r => r.id_rol)
                    .FirstOrDefault();

                if (idRolDocente == null)
                {
                    return new List<DocentesDto>();
                }

                var usuarios = db.Usuarios
                    .Where(u => u.rol == idRolDocente)
                    .OrderBy(u => u.nombre)
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
                var materias = db.Materias.ToList().ToDictionary(m => m.id_materia);
                var asignaciones = db.SeccionesMaterias.ToList();

                return asignaciones
                    .Where(a => materias.ContainsKey(a.fk_id_materia) && !string.IsNullOrEmpty(materias[a.fk_id_materia].profesor))
                    .Select(a => new AsignacionDocenteDto
                    {
                        id_asignacion_docente = a.id_seccion_materia,
                        id_docente = materias[a.fk_id_materia].profesor,
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
                        estudiantes_matriculados = db.SeccionesEstudiantes.Count(se => se.fk_id_seccion == s.id_seccion),
                        fecha_modificacion = null,
                        usuario_modificacion = string.Empty
                    })
                    .ToList();
            }
        }
    }
}