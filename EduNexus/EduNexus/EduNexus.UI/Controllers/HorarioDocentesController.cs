using EduNexus.Abstracciones.ModelosParaUI.Docentes;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RDOC-07-001 Como administrador quiero consultar el horario de un docente
    // para facilitar su asignación a materias y secciones
    // Tablas: usuarios (rol Docente), secciones_materias (fk_id_profesor), materia, horarios, secciones, grados, calendario
    [AutorizarAdministrador]
    public class HorarioDocentesController : Controller
    {
        private const string RolDocente = "Docente";

        // Búsqueda de docentes por nombre, identificación o correo
        public ActionResult BuscarDocente(string q)
        {
            using (var db = new EduNexusDbContext())
            {
                string buscado = Normalizar(q);

                List<DocenteBusquedaDto> resultados = ObtenerDocentes(db)
                    .Where(u => buscado == "" ||
                                Normalizar(NombreCompleto(u) + " " + u.identificacion + " " + u.email).Contains(buscado))
                    .Select(u => new DocenteBusquedaDto
                    {
                        id_usuario = u.id_usuario,
                        nombre = NombreCompleto(u),
                        identificacion = u.identificacion,
                        email = u.email,
                        estado = u.estado ? "Activo" : "Inactivo"
                    })
                    .ToList();

                ViewBag.Busqueda = q;
                return View(resultados);
            }
        }

        public ActionResult HorarioDeDocente(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(400);
            }

            using (var db = new EduNexusDbContext())
            {
                UsuarioEntity docente = ObtenerDocentes(db).FirstOrDefault(u => u.id_usuario == id);
                if (docente == null)
                {
                    return HttpNotFound();
                }

                // Clases del docente (secciones_materias.fk_id_profesor) y sus horarios
                List<SeccionMateriaEntity> clases = HorarioServicio.ClasesDelDocente(db, id);
                List<int> idsSecciones = clases.Select(c => c.fk_id_seccion).Distinct().ToList();
                List<int> idsMaterias = clases.Select(c => c.fk_id_materia).Distinct().ToList();

                Dictionary<int, MateriaEntity> materias = db.Materias
                    .Where(m => idsMaterias.Contains(m.id_materia)).ToList()
                    .ToDictionary(m => m.id_materia);

                List<HorarioEntity> horarios = db.Horarios
                    .Where(h => idsSecciones.Contains(h.fk_seccion) && idsMaterias.Contains(h.fk_materia)).ToList()
                    .Where(h => clases.Any(c => c.fk_id_seccion == h.fk_seccion && c.fk_id_materia == h.fk_materia))
                    .ToList();

                Dictionary<int, string> etiquetas = HorarioServicio.EtiquetasDeSecciones(db);
                ViewBag.TieneClases = clases.Any();

                var modelo = new HorarioDocenteDto
                {
                    id_usuario = docente.id_usuario,
                    docente = NombreCompleto(docente),
                    identificacion = docente.identificacion,
                    email = docente.email,

                    // Escenario 1: horario completo (día, hora, materia y sección)
                    // Escenario 2: si la lista queda vacía, la vista muestra el aviso y la opción de asignar
                    horarios = horarios
                        .OrderBy(h => HorarioServicio.OrdenDia(h.dia_semana))
                        .ThenBy(h => h.hora_inicio)
                        .Select(h =>
                        {
                            string seccion;
                            etiquetas.TryGetValue(h.fk_seccion, out seccion);
                            MateriaEntity materia;
                            materias.TryGetValue(h.fk_materia, out materia);

                            return new HorarioDocenteItemDto
                            {
                                dia_semana = h.dia_semana,
                                hora_inicio = HorarioServicio.Hora(h.hora_inicio),
                                hora_fin = HorarioServicio.Hora(h.hora_fin),
                                materia = materia != null ? materia.nombre : "Materia " + h.fk_materia,
                                seccion = seccion ?? ""
                            };
                        })
                        .ToList()
                };

                return View(modelo);
            }
        }

        private static List<UsuarioEntity> ObtenerDocentes(EduNexusDbContext db)
        {
            string idRolDocente = db.Roles
                .Where(r => r.nombre == RolDocente)
                .Select(r => r.id_rol)
                .FirstOrDefault();

            if (idRolDocente == null)
            {
                return new List<UsuarioEntity>();
            }

            return db.Usuarios.Where(u => u.rol == idRolDocente).OrderBy(u => u.nombre).ToList();
        }

        private static string NombreCompleto(UsuarioEntity u)
        {
            return string.Join(" ", new[] { u.nombre, u.apellido1, u.apellido2 }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return "";
            }
            return string.Join(" ", texto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        }
    }
}