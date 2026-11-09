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
        private const int TamanoPagina = 10;

        // RHOR-09-002: consulta por sección, docente o texto, con paginación (id = id_seccion)
        public ActionResult ListadoDeHorarios(int? id, string docente, string q, int pagina = 1)
        {
            using (var db = new EduNexusDbContext())
            {
                Dictionary<int, string> secciones = HorarioServicio.EtiquetasDeSecciones(db);
                ViewBag.Secciones = new SelectList(secciones.OrderBy(x => x.Value).ToList(), "Key", "Value", id);
                ViewBag.Docentes = new SelectList(
                    HorarioServicio.ObtenerDocentes(db)
                        .Select(u => new { u.id_usuario, nombre = HorarioServicio.NombreCompleto(u) }).ToList(),
                    "id_usuario", "nombre", docente);

                IQueryable<HorarioEntity> consulta = db.Horarios;

                if (id.HasValue)
                {
                    int idSeccion = id.Value;
                    consulta = consulta.Where(h => h.fk_seccion == idSeccion);
                }


                List<HorarioItemDto> items = HorarioServicio.ConstruirItems(db, consulta.ToList());

                if (!string.IsNullOrEmpty(docente))
                {
                    items = items.Where(x => x.id_profesor == docente).ToList();
                }

                // Búsqueda por texto: materia, docente, sección o día
                if (!string.IsNullOrWhiteSpace(q))
                {
                    string buscado = HorarioServicio.SinTildes(q);
                    items = items.Where(x => HorarioServicio.SinTildes(
                        x.materia + " " + x.profesor + " " + x.seccion + " " + x.dia_semana).Contains(buscado)).ToList();
                }

                int total = items.Count;
                int totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));
                pagina = Math.Min(Math.Max(1, pagina), totalPaginas);

                var modelo = new HorarioConsultaDto
                {
                    id_seccion = id,
                    id_docente = docente,
                    q = q,
                    consultado = true,
                    pagina = pagina,
                    totalPaginas = totalPaginas,
                    totalRegistros = total,
                    tamanoPagina = TamanoPagina,
                    items = items.Skip((pagina - 1) * TamanoPagina).Take(TamanoPagina).ToList()
                };

                if (total == 0)
                {
                    modelo.mensaje = (id.HasValue || !string.IsNullOrEmpty(docente) || !string.IsNullOrWhiteSpace(q))
                        ? "No hay horario registrado para la selección realizada."
                        : "Aún no hay horarios registrados.";
                }

                return View(modelo);
            }
        }

        // AJAX: sugerencias para el buscador del listado
        [HttpGet]
        public JsonResult SugerenciasHorarios(string term)
        {
            var sugerencias = new List<object>();
            string buscado = HorarioServicio.SinTildes(term);

            if (buscado.Length < 2)
            {
                return Json(sugerencias, JsonRequestBehavior.AllowGet);
            }

            using (var db = new EduNexusDbContext())
            {
                List<HorarioItemDto> items = HorarioServicio.ConstruirItems(db, db.Horarios.ToList());

                sugerencias.AddRange(items.Select(x => x.materia).Distinct()
                    .Where(t => HorarioServicio.SinTildes(t).Contains(buscado))
                    .Take(4).Select(t => new { texto = t, tipo = "Materia" }));

                sugerencias.AddRange(items.Select(x => x.profesor).Where(t => !string.IsNullOrEmpty(t)).Distinct()
                    .Where(t => HorarioServicio.SinTildes(t).Contains(buscado))
                    .Take(4).Select(t => new { texto = t, tipo = "Docente" }));

                sugerencias.AddRange(items.Select(x => x.seccion).Where(t => !string.IsNullOrEmpty(t)).Distinct()
                    .Where(t => HorarioServicio.SinTildes(t).Contains(buscado))
                    .Take(4).Select(t => new { texto = t, tipo = "Sección" }));
            }

            return Json(sugerencias, JsonRequestBehavior.AllowGet);
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
                    modelo.mensaje = "No hay horario registrado para esta sección.";
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
                TempData["Error"] = "Seleccione primero una sección para registrar su horario.";
                return RedirectToAction("ListadoDeHorarios");
            }

            using (var db = new EduNexusDbContext())
            {
                if (!db.Secciones.Any(s => s.id_seccion == id.Value))
                {
                    return HttpNotFound();
                }
                if (!db.SeccionesMaterias.Any(sm => sm.fk_id_seccion == id.Value))
                {
                    TempData["Error"] = "Esta sección aún no tiene materias asignadas. Asígnelas primero en Materias por sección.";
                    return RedirectToAction("ListadoDeHorarios", new { id = id.Value });
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

                // Escenario 3 y RHOR-09-005: conflicto por sección o docente
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

                string etiqueta;
                HorarioServicio.EtiquetasDeSecciones(db).TryGetValue(horario.seccion, out etiqueta);
                Auditoria.Registrar(db, "Registro de horario",
                    "Horario de la sección " + etiqueta + ": " + horario.dia_semana + " "
                        + HorarioServicio.Hora(horario.hora_inicio) + "-" + HorarioServicio.Hora(horario.hora_fin),
                    null, new
                    {
                        horario.dia_semana,
                        hora_inicio = HorarioServicio.Hora(horario.hora_inicio),
                        hora_fin = HorarioServicio.Hora(horario.hora_fin),
                        id_materia = horario.materia,
                        id_seccion = horario.seccion
                    });
                db.SaveChanges();
            }

            TempData["Mensaje"] = "El horario se registró correctamente y quedó asociado a la sección y al docente de la materia. No se detectaron conflictos.";
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
                    // Nombre oficial del día para que el combo lo preseleccione ("Miercoles" -> "Miércoles")
                    dia_semana = HorarioServicio.NombreDia(h.dia_semana) ?? h.dia_semana,
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

                // Escenario 1: se registra la fecha y el usuario que modificó (bitácora)
                Auditoria.Registrar(db, "Modificación de horario", "Horario #" + existente.id_horario,
                    antes, new
                    {
                        existente.dia_semana,
                        hora_inicio = HorarioServicio.Hora(existente.hora_inicio),
                        hora_fin = HorarioServicio.Hora(existente.hora_fin),
                        id_materia = existente.fk_materia,
                        id_seccion = existente.fk_seccion
                    });

                db.SaveChanges();
            }

            TempData["Mensaje"] = "El horario se actualizó correctamente.";
            return RedirectToAction("ListadoDeHorarios", new { id = horario.seccion });
        }

        // Validaciones comunes de Create y Edit
        private static string ValidarDatos(EduNexusDbContext db, HorariosDto horario)
        {
            // Se acepta el día con o sin tilde y se guarda con su nombre oficial ("Miércoles")
            string dia = Dias.FirstOrDefault(d => HorarioServicio.MismoDia(d, horario.dia_semana));
            if (dia == null)
            {
                return "El día seleccionado no es válido.";
            }
            horario.dia_semana = dia;
            if (horario.hora_fin <= horario.hora_inicio)
            {
                return "La hora de fin debe ser posterior a la hora de inicio.";
            }
            if (!db.Secciones.Any(s => s.id_seccion == horario.seccion))
            {
                return "La sección especificada no existe.";
            }
            if (!db.Materias.Any(m => m.id_materia == horario.materia))
            {
                return "La materia especificada no existe.";
            }
            if (!db.SeccionesMaterias.Any(sm => sm.fk_id_seccion == horario.seccion && sm.fk_id_materia == horario.materia))
            {
                return "La materia no está asignada a esta sección. Asígnela primero en Materias por sección, con su docente.";
            }
            return null;
        }

        private ActionResult VistaCrear(EduNexusDbContext db, HorariosDto horario)
        {
            string etiqueta;
            HorarioServicio.EtiquetasDeSecciones(db).TryGetValue(horario.seccion, out etiqueta);
            ViewBag.NombreSeccion = etiqueta;
            ViewBag.Materias = ListaMaterias(db, horario.seccion);
            return View("CrearHorario", horario);
        }

        private ActionResult VistaEditar(EduNexusDbContext db, HorariosDto horario)
        {
            string etiqueta;
            HorarioServicio.EtiquetasDeSecciones(db).TryGetValue(horario.seccion, out etiqueta);
            ViewBag.NombreSeccion = etiqueta;
            ViewBag.Dias = new SelectList(Dias);
            ViewBag.Materias = ListaMaterias(db, horario.seccion);
            return View("EditarHorario", horario);
        }

        // Materias asignadas a la sección, con el docente de esa sección: "Matemáticas - Ana Mora"
        private static SelectList ListaMaterias(EduNexusDbContext db, int idSeccion)
        {
            List<SeccionMateriaEntity> clases = db.SeccionesMaterias
                .Where(sm => sm.fk_id_seccion == idSeccion).ToList();

            List<int> idsMaterias = clases.Select(c => c.fk_id_materia).ToList();
            Dictionary<int, string> nombres = db.Materias
                .Where(m => idsMaterias.Contains(m.id_materia)).ToList()
                .ToDictionary(m => m.id_materia, m => m.nombre);

            List<string> idsDocentes = clases.Select(c => c.fk_id_profesor).Where(x => x != null).Distinct().ToList();
            Dictionary<string, UsuarioEntity> docentes = db.Usuarios
                .Where(u => idsDocentes.Contains(u.id_usuario)).ToList()
                .ToDictionary(u => u.id_usuario);

            var opciones = clases.Select(c => new
            {
                id_materia = c.fk_id_materia,
                texto = (nombres.ContainsKey(c.fk_id_materia) ? nombres[c.fk_id_materia] : "Materia " + c.fk_id_materia)
                    + " - " + (c.fk_id_profesor != null && docentes.ContainsKey(c.fk_id_profesor)
                        ? HorarioServicio.NombreCompleto(docentes[c.fk_id_profesor]) : "Sin docente")
            }).OrderBy(o => o.texto).ToList();

            return new SelectList(opciones, "id_materia", "texto");
        }
    }
}