using EduNexus.Abstracciones.ModelosParaUI.Horarios;
using EduNexus.Abstracciones.ModelosParaUI.Materias;
using EduNexus.Abstracciones.ModelosParaUI.Secciones;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class HorariosController : Controller
    {
        // GET: Horarios/ListadoDeHorarios/5 (5 = id_seccion)
        public ActionResult ListadoDeHorarios(int? id)
        {
            List<HorariosDto> horarios = ObtenerHorarios().Where(x => x.seccion == id).ToList();
            ViewBag.IdSeccion = id;
            ViewBag.NombreSeccion = ObtenerSecciones().FirstOrDefault(x => x.id_seccion == id)?.nombre;
            ViewBag.Materias = ObtenerMaterias();
            return View(horarios);
        }

        // GET: Horarios/VisualizarHorario/5 (5 = id_seccion)
        public ActionResult VisualizarHorario(int? id)
        {
            List<HorariosDto> horarios = ObtenerHorarios().Where(x => x.seccion == id).ToList();
            ViewBag.IdSeccion = id;
            ViewBag.NombreSeccion = ObtenerSecciones().FirstOrDefault(x => x.id_seccion == id)?.nombre;
            ViewBag.Materias = ObtenerMaterias();
            return View(horarios);
        }

        // GET: Horarios/CrearHorario/5 (5 = id_seccion)
        public ActionResult CrearHorario(int? id)
        {
            ViewBag.NombreSeccion = ObtenerSecciones().FirstOrDefault(x => x.id_seccion == id)?.nombre;
            ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
            ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
            return View(new HorariosDto { seccion = id ?? 0 });
        }

        // POST: Horarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(HorariosDto horario, FormCollection collection)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                    ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                    return View("CrearHorario", horario);
                }

                using (var db = new EduNexusDbContext())
                {
                    // Validar que la sección exista
                    var seccion = db.Secciones.FirstOrDefault(s => s.id_seccion == horario.seccion);
                    if (seccion == null)
                    {
                        ModelState.AddModelError("", "La sección especificada no existe.");
                        ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                        ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                        return View("CrearHorario", horario);
                    }

                    // Validar que la materia exista
                    var materia = db.Materias.FirstOrDefault(m => m.id_materia == horario.materia);
                    if (materia == null)
                    {
                        ModelState.AddModelError("", "La materia especificada no existe.");
                        ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                        ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                        return View("CrearHorario", horario);
                    }

                    // Validar no haya conflicto de horarios en la misma sección
                    bool conflicto = db.Horarios.Any(h =>
                        h.fk_seccion == horario.seccion &&
                        h.dia_semana == horario.dia_semana &&
                        ((h.hora_inicio < horario.hora_fin && h.hora_fin > horario.hora_inicio)));

                    if (conflicto)
                    {
                        ModelState.AddModelError("", "Existe conflicto de horarios en esta sección.");
                        ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                        ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                        return View("CrearHorario", horario);
                    }

                    // Crear nuevo horario
                    var nuevoHorario = new HorarioEntity
                    {
                        dia_semana = horario.dia_semana,
                        hora_inicio = horario.hora_inicio,
                        hora_fin = horario.hora_fin,
                        fk_materia = horario.materia,
                        fk_seccion = horario.seccion
                    };

                    db.Horarios.Add(nuevoHorario);
                    db.SaveChanges();
                }

                return RedirectToAction("ListadoDeHorarios", new { id = horario.seccion });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar el horario: " + ex.Message);
                ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                return View("CrearHorario", horario);
            }
        }

        // GET: Horarios/EditarHorario/5 (5 = id_horario)
        public ActionResult EditarHorario(int? id)
        {
            HorariosDto horario = ObtenerHorarios().FirstOrDefault(x => x.id_horario == id);
            ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
            ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
            return View(horario);
        }

        // POST: Horarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, HorariosDto horario, FormCollection collection)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                    ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                    return View("EditarHorario", horario);
                }

                using (var db = new EduNexusDbContext())
                {
                    // Obtener el horario existente
                    var horarioExistente = db.Horarios.FirstOrDefault(h => h.id_horario == id);
                    if (horarioExistente == null)
                    {
                        return HttpNotFound();
                    }

                    // Validar que la sección exista
                    var seccion = db.Secciones.FirstOrDefault(s => s.id_seccion == horario.seccion);
                    if (seccion == null)
                    {
                        ModelState.AddModelError("", "La sección especificada no existe.");
                        ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                        ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                        return View("EditarHorario", horario);
                    }

                    // Validar que la materia exista
                    var materia = db.Materias.FirstOrDefault(m => m.id_materia == horario.materia);
                    if (materia == null)
                    {
                        ModelState.AddModelError("", "La materia especificada no existe.");
                        ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                        ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                        return View("EditarHorario", horario);
                    }

                    // Validar no haya conflicto de horarios en la misma sección (excluyendo este horario)
                    bool conflicto = db.Horarios.Any(h =>
                        h.id_horario != id &&
                        h.fk_seccion == horario.seccion &&
                        h.dia_semana == horario.dia_semana &&
                        ((h.hora_inicio < horario.hora_fin && h.hora_fin > horario.hora_inicio)));

                    if (conflicto)
                    {
                        ModelState.AddModelError("", "Existe conflicto de horarios en esta sección.");
                        ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                        ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                        return View("EditarHorario", horario);
                    }

                    // Actualizar horario
                    horarioExistente.dia_semana = horario.dia_semana;
                    horarioExistente.hora_inicio = horario.hora_inicio;
                    horarioExistente.hora_fin = horario.hora_fin;
                    horarioExistente.fk_materia = horario.materia;
                    horarioExistente.fk_seccion = horario.seccion;

                    db.SaveChanges();
                }

                return RedirectToAction("ListadoDeHorarios", new { id = horario.seccion });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar el horario: " + ex.Message);
                ViewBag.Materias = new SelectList(ObtenerMaterias(), "id_materia", "nombre");
                ViewBag.Dias = new SelectList(new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" });
                return View("EditarHorario", horario);
            }
        }

        private List<HorariosDto> ObtenerHorarios()
        {
            using (var db = new EduNexusDbContext())
            {
                var grados = db.Grados.ToList().ToDictionary(g => g.id_grado, g => g.nombre);
                var materias = db.Materias.ToList().ToDictionary(m => m.id_materia, m => m.nombre);
                var secciones = db.Secciones.ToList().ToDictionary(s => s.id_seccion, s => s.nombre);

                return db.Horarios
                    .ToList()
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
        }

        private List<MateriasDto> ObtenerMaterias()
        {
            using (var db = new EduNexusDbContext())
            {
                var usuarios = db.Usuarios.ToList().ToDictionary(u => u.id_usuario);

                return db.Materias
                    .ToList()
                    .Select(m => new MateriasDto
                    {
                        id_materia = m.id_materia,
                        nombre = m.nombre,
                        profesor = (m.profesor != null && usuarios.ContainsKey(m.profesor))
                                    ? usuarios[m.profesor].nombre + " " + usuarios[m.profesor].apellido1
                                    : string.Empty
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