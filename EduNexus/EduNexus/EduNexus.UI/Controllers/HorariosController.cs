using EduNexus.Abstracciones.ModelosParaUI.Horarios;
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
    // RHOR-09-001 a 005: registrar, consultar, modificar horarios y detectar conflictos
    [AutorizarRoles("Administrador", "Coordinador Academico")]
    public class HorariosController : Controller
    {
        private static readonly string[] Dias = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" };

        // RHOR-09-002: consulta por seccion y/o docente (id = id_seccion)
        public ActionResult ListadoDeHorarios(int? id, string docente)
        {
            using (var db = new EduNexusDbContext())
            {
                Dictionary<int, string> secciones = HorarioServicio.EtiquetasDeSecciones(db);
                ViewBag.Secciones = new SelectList(secciones.OrderBy(x => x.Value).ToList(), "Key", "Value", id);
                ViewBag.Docentes = new SelectList(
                    HorarioServicio.ObtenerDocentes(db)
                        .Select(u => new { u.id_usuario, nombre = HorarioServicio.NombreCompleto(u) }).ToList(),
                    "id_usuario", "nombre", docente);

                var modelo = new HorarioConsultaDto { id_seccion = id, id_docente = docente };

                if (id.HasValue || !string.IsNullOrEmpty(docente))
                {
                    IQueryable<HorarioEntity> consulta = db.Horarios;

                    if (id.HasValue)
                    {
                        int idSeccion = id.Value;
                        consulta = consulta.Where(h => h.fk_seccion == idSeccion);
                    }

                    if (!string.IsNullOrEmpty(docente))
                    {
                        List<int> idsMaterias = db.Materias
                            .Where(m => m.profesor == docente)
                            .Select(m => m.id_materia).ToList();
                        consulta = consulta.Where(h => idsMaterias.Contains(h.fk_materia));
                    }

                    modelo.items = HorarioServicio.ConstruirItems(db, consulta.ToList());
                    modelo.consultado = true;

                    // Escenario 2: consulta sin horarios registrados
                    if (!modelo.items.Any())
                    {
                        modelo.mensaje = "No hay horario registrado para la seleccion realizada.";
                    }
                }

                return View(modelo);
            }
        }

        // Horario semanal de una seccion (id = id_seccion)
        public ActionResult VisualizarHorario(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("ListadoDeHorarios");
            }

            using (var db = new EduNexusDbContext())
            {
                string etiqueta;
                if (!HorarioServicio.EtiquetasDeSecciones(db).TryGetValue(id.Value, out etiqueta))
                {
                    return HttpNotFound();
                }

                int idSeccion = id.Value;
                var modelo = new HorarioConsultaDto
                {
                    id_seccion = idSeccion,
                    titulo = etiqueta,
                    items = HorarioServicio.ConstruirItems(db, db.Horarios.Where(h => h.fk_seccion == idSeccion).ToList())
                };

                if (!modelo.items.Any())
                {
                    modelo.mensaje = "No hay horario registrado para esta seccion.";
                }
                return View(modelo);
            }
        }

        // RHOR-09-004: horario de la seccion con cupos y estudiantes asignados (id = id_seccion)
        public ActionResult HorarioParaAsignacion(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("ListadoDeHorarios");
            }

            using (var db = new EduNexusDbContext())
            {
                int idSeccion = id.Value;
                SeccionEntity seccion = db.Secciones.FirstOrDefault(s => s.id_seccion == idSeccion);
                if (seccion == null)
                {
                    return HttpNotFound();
                }

                string etiqueta;
                HorarioServicio.EtiquetasDeSecciones(db).TryGetValue(idSeccion, out etiqueta);

                List<string> idsEstudiantes = db.SeccionesEstudiantes
                    .Where(se => se.fk_id_seccion == idSeccion)
                    .Select(se => se.fk_id_usuario).ToList();

                List<string> nombres = db.Usuarios
                    .Where(u => idsEstudiantes.Contains(u.id_usuario)).ToList()
                    .Select(HorarioServicio.NombreCompleto)
                    .OrderBy(n => n).ToList();

                var modelo = new HorarioAsignacionDto
                {
                    id_seccion = idSeccion,
                    seccion = etiqueta,
                    cupo = seccion.cupo,
                    matriculados = idsEstudiantes.Count,
                    cupos_disponibles = Math.Max(0, seccion.cupo - idsEstudiantes.Count),
                    estudiantes = nombres,
                    items = HorarioServicio.ConstruirItems(db, db.Horarios.Where(h => h.fk_seccion == idSeccion).ToList())
                };

                return View(modelo);
            }
        }

        // GET: Horarios/CrearHorario/5 (5 = id_seccion)
        public ActionResult CrearHorario(int? id)
        {
            if (!id.HasValue)
            {
                TempData["Error"] = "Seleccione primero una seccion para registrar su horario.";
                return RedirectToAction("ListadoDeHorarios");
            }

            using (var db = new EduNexusDbContext())
            {
                if (!db.Secciones.Any(s => s.id_seccion == id.Value))
                {
                    return HttpNotFound();
                }

                var horario = new HorariosDto { seccion = id.Value };
                return VistaCrear(db, horario);
            }
        }

        // POST: Horarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(HorariosDto horario)
        {
            using (var db = new EduNexusDbContext())
            {
                // Escenario 2: datos obligatorios incompletos
                if (!ModelState.IsValid)
                {
                    return VistaCrear(db, horario);
                }

                string error = ValidarDatos(db, horario);
                if (error != null)
                {
                    ModelState.AddModelError("", error);
                    return VistaCrear(db, horario);
                }

                // Escenario 3 y RHOR-09-005: conflicto por seccion o docente
                string conflicto = HorarioServicio.BuscarConflicto(db, horario.seccion, horario.materia,
                    horario.dia_semana, horario.hora_inicio, horario.hora_fin, 0);

                if (conflicto != null)
                {
                    ModelState.AddModelError("", conflicto);
                    return VistaCrear(db, horario);
                }

                // Escenario 1: registro exitoso (la materia ya lleva al docente responsable)
                db.Horarios.Add(new HorarioEntity
                {
                    dia_semana = horario.dia_semana,
                    hora_inicio = horario.hora_inicio,
                    hora_fin = horario.hora_fin,
                    fk_materia = horario.materia,
                    fk_seccion = horario.seccion
                });
                db.SaveChanges();
            }

            TempData["Mensaje"] = "El horario se registro correctamente y quedo asociado a la seccion y al docente de la materia. No se detectaron conflictos.";
            return RedirectToAction("ListadoDeHorarios", new { id = horario.seccion });
        }

        // GET: Horarios/EditarHorario/5 (5 = id_horario)
        public ActionResult EditarHorario(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(400);
            }

            using (var db = new EduNexusDbContext())
            {
                HorarioEntity h = db.Horarios.FirstOrDefault(x => x.id_horario == id.Value);
                if (h == null)
                {
                    return HttpNotFound();
                }

                var horario = new HorariosDto
                {
                    id_horario = h.id_horario,
                    dia_semana = h.dia_semana,
                    hora_inicio = h.hora_inicio,
                    hora_fin = h.hora_fin,
                    seccion = h.fk_seccion,
                    materia = h.fk_materia
                };
                return VistaEditar(db, horario);
            }
        }

        // POST: Horarios/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(HorariosDto horario)
        {
            using (var db = new EduNexusDbContext())
            {
                if (!ModelState.IsValid)
                {
                    return VistaEditar(db, horario);
                }

                HorarioEntity existente = db.Horarios.FirstOrDefault(h => h.id_horario == horario.id_horario);
                if (existente == null)
                {
                    return HttpNotFound();
                }

                string error = ValidarDatos(db, horario);
                if (error != null)
                {
                    ModelState.AddModelError("", error);
                    return VistaEditar(db, horario);
                }

                // Escenario 2: conflicto con otro horario (se excluye el propio horario)
                string conflicto = HorarioServicio.BuscarConflicto(db, horario.seccion, horario.materia,
                    horario.dia_semana, horario.hora_inicio, horario.hora_fin, existente.id_horario);

                if (conflicto != null)
                {
                    ModelState.AddModelError("", conflicto);
                    return VistaEditar(db, horario);
                }

                var antes = new
                {
                    existente.dia_semana,
                    hora_inicio = HorarioServicio.Hora(existente.hora_inicio),
                    hora_fin = HorarioServicio.Hora(existente.hora_fin),
                    id_materia = existente.fk_materia,
                    id_seccion = existente.fk_seccion
                };

                existente.dia_semana = horario.dia_semana;
                existente.hora_inicio = horario.hora_inicio;
                existente.hora_fin = horario.hora_fin;
                existente.fk_materia = horario.materia;
                existente.fk_seccion = horario.seccion;

                // Escenario 1: se registra la fecha y el usuario que modifico (bitacora)
                var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;
                if (usuario != null)
                {
                    db.Bitacora.Add(new BitacoraEntity
                    {
                        tipo_de_evento = "Modificacion de horario",
                        descripcion_evento = "Horario #" + existente.id_horario,
                        fecha = DateTime.Now,
                        datos_anteriores = JsonConvert.SerializeObject(antes),
                        datos_posteriores = JsonConvert.SerializeObject(new
                        {
                            existente.dia_semana,
                            hora_inicio = HorarioServicio.Hora(existente.hora_inicio),
                            hora_fin = HorarioServicio.Hora(existente.hora_fin),
                            id_materia = existente.fk_materia,
                            id_seccion = existente.fk_seccion
                        }),
                        fk_id_usuario = usuario.id_usuario
                    });
                }

                db.SaveChanges();
            }

            TempData["Mensaje"] = "El horario se actualizo correctamente.";
            return RedirectToAction("ListadoDeHorarios", new { id = horario.seccion });
        }

        // Validaciones comunes de Create y Edit
        private static string ValidarDatos(EduNexusDbContext db, HorariosDto horario)
        {
            if (!Dias.Contains(horario.dia_semana))
            {
                return "El día seleccionado no es valido.";
            }
            if (horario.hora_fin <= horario.hora_inicio)
            {
                return "La hora de fin debe ser posterior a la hora de inicio.";
            }
            if (!db.Secciones.Any(s => s.id_seccion == horario.seccion))
            {
                return "La seccion especificada no existe.";
            }
            if (!db.Materias.Any(m => m.id_materia == horario.materia))
            {
                return "La materia especificada no existe.";
            }
            return null;
        }

        private ActionResult VistaCrear(EduNexusDbContext db, HorariosDto horario)
        {
            string etiqueta;
            HorarioServicio.EtiquetasDeSecciones(db).TryGetValue(horario.seccion, out etiqueta);
            ViewBag.NombreSeccion = etiqueta;
            ViewBag.Materias = ListaMaterias(db);
            return View("CrearHorario", horario);
        }

        private ActionResult VistaEditar(EduNexusDbContext db, HorariosDto horario)
        {
            ViewBag.Dias = new SelectList(Dias);
            ViewBag.Materias = ListaMaterias(db);
            ViewBag.Secciones = new SelectList(
                HorarioServicio.EtiquetasDeSecciones(db).OrderBy(x => x.Value).ToList(), "Key", "Value");
            return View("EditarHorario", horario);
        }

        // Materias con su docente responsable: "Matematicas - Ana Mora"
        private static SelectList ListaMaterias(EduNexusDbContext db)
        {
            List<MateriaEntity> materias = db.Materias.OrderBy(m => m.nombre).ToList();
            List<string> ids = materias.Select(m => m.profesor).Distinct().ToList();
            Dictionary<string, UsuarioEntity> docentes = db.Usuarios
                .Where(u => ids.Contains(u.id_usuario)).ToList()
                .ToDictionary(u => u.id_usuario);

            var opciones = materias.Select(m => new
            {
                m.id_materia,
                texto = m.nombre + (docentes.ContainsKey(m.profesor) ? " - " + HorarioServicio.NombreCompleto(docentes[m.profesor]) : "")
            }).ToList();

            return new SelectList(opciones, "id_materia", "texto");
        }
    }
}