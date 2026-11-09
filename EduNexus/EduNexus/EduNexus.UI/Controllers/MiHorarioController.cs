using EduNexus.Abstracciones.ModelosParaUI.Horarios;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RHOR-09-006 (docente) y RHOR-09-007 (estudiante): consultar mi horario
    public class MiHorarioController : Controller
    {
        public ActionResult MiHorario()
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;
            if (usuario == null)
            {
                return RedirectToAction("Login", "Home");
            }

            bool esDocente = string.Equals(usuario.nombreRol, "Docente", StringComparison.OrdinalIgnoreCase);
            bool esEstudiante = string.Equals(usuario.nombreRol, "Estudiante", StringComparison.OrdinalIgnoreCase);

            if (!esDocente && !esEstudiante)
            {
                TempData["MensajeError"] = "Esta pantalla es solo para docentes y estudiantes.";
                return RedirectToAction("Index", "Home");
            }

            string idUsuario = usuario.id_usuario;
            DateTime hoy = DateTime.Today;

            using (var db = new EduNexusDbContext())
            {
                // Secciones del periodo lectivo actual
                List<int> idsCalendario = db.Calendarios
                    .Where(c => c.fecha_inicio <= hoy && c.fecha_fin >= hoy)
                    .Select(c => c.id_calendario).ToList();

                List<int> idsSecciones = db.Secciones
                    .Where(s => idsCalendario.Contains(s.fk_id_calendario))
                    .Select(s => s.id_seccion).ToList();

                var modelo = new HorarioConsultaDto { titulo = "Mi horario semanal" };
                List<HorarioEntity> horarios = new List<HorarioEntity>();

                if (esDocente)
                {
                    List<SeccionMateriaEntity> clasesDelDocente = HorarioServicio.ClasesDelDocente(db, idUsuario);

                    horarios = db.Horarios.Where(h => idsSecciones.Contains(h.fk_seccion)).ToList()
                        .Where(h => clasesDelDocente.Any(c => c.fk_id_seccion == h.fk_seccion && c.fk_id_materia == h.fk_materia))
                        .ToList();

                    // Escenario 2 (docente): sin horario para el periodo actual
                    if (!horarios.Any())
                    {
                        modelo.mensaje = "Aun no tiene horario asignado para el periodo actual.";
                    }
                }
                else
                {
                    List<int> susSecciones = db.SeccionesEstudiantes
                        .Where(se => se.fk_id_usuario == idUsuario && idsSecciones.Contains(se.fk_id_seccion))
                        .Select(se => se.fk_id_seccion).ToList();

                    // Escenario 2 (estudiante): sin seccion asignada en el periodo actual
                    if (!susSecciones.Any())
                    {
                        modelo.mensaje = "Aun no tiene seccion asignada para el periodo lectivo actual.";
                    }
                    else
                    {
                        horarios = db.Horarios.Where(h => susSecciones.Contains(h.fk_seccion)).ToList();
                        if (!horarios.Any())
                        {
                            modelo.mensaje = "Su seccion aun no tiene horario configurado.";
                        }
                    }
                }

                modelo.items = HorarioServicio.ConstruirItems(db, horarios);
                ViewBag.EsDocente = esDocente;
                return View(modelo);
            }
        }
    }
}