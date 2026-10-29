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
    // Tablas: usuarios (rol Docente), materia (profesor), horarios, secciones, grados, calendario
    [AutorizarAdministrador]
    public class HorarioDocentesController : Controller
    {
        private const string RolDocente = "Docente";
        private static readonly string[] Dias = { "lunes", "martes", "miercoles", "jueves", "viernes", "sabado", "domingo" };

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
                        IdUsuario = u.id_usuario,
                        Nombre = NombreCompleto(u),
                        Identificacion = u.identificacion,
                        Email = u.email,
                        Estado = u.estado ? "Activo" : "Inactivo"
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

                // Las materias del docente (materia.profesor) y sus horarios
                Dictionary<int, MateriaEntity> materias = db.Materias
                    .Where(m => m.profesor == id).ToList()
                    .ToDictionary(m => m.id_materia);

                List<int> idsMaterias = materias.Keys.ToList();
                List<HorarioEntity> horarios = db.Horarios
                    .Where(h => idsMaterias.Contains(h.fk_materia)).ToList();

                Dictionary<int, string> etiquetas = EtiquetasDeSecciones(db);

                var modelo = new HorarioDocenteDto
                {
                    IdUsuario = docente.id_usuario,
                    Docente = NombreCompleto(docente),
                    Identificacion = docente.identificacion,
                    Email = docente.email,

                    // Escenario 1: horario completo (día, hora, materia y sección)
                    // Escenario 2: si la lista queda vacía, la vista muestra el aviso y la opción de asignar
                    Horarios = horarios
                        .OrderBy(h => OrdenDia(h.dia_semana))
                        .ThenBy(h => h.hora_inicio)
                        .Select(h =>
                        {
                            string seccion;
                            etiquetas.TryGetValue(h.fk_seccion, out seccion);
                            MateriaEntity materia = materias[h.fk_materia];

                            return new HorarioDocenteItemDto
                            {
                                DiaSemana = h.dia_semana,
                                HoraInicio = h.hora_inicio.ToString("HH:mm"),
                                HoraFin = h.hora_fin.ToString("HH:mm"),
                                Materia = materia.id_materia + " - " + materia.nombre,
                                Seccion = seccion ?? ""
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

        // Etiqueta de cada sección: "Tercer Grado - 3-A (2026)"
        private static Dictionary<int, string> EtiquetasDeSecciones(EduNexusDbContext db)
        {
            Dictionary<int, string> grados = db.Grados.ToList().ToDictionary(g => g.id_grado, g => g.nombre);
            Dictionary<int, int> anios = db.Calendarios.ToList().ToDictionary(c => c.id_calendario, c => c.fecha_inicio.Year);

            return db.Secciones.ToList().ToDictionary(
                s => s.id_seccion,
                s => (grados.ContainsKey(s.fk_id_grado) ? grados[s.fk_id_grado] : "Grado")
                     + " - " + s.nombre
                     + (anios.ContainsKey(s.fk_id_calendario) ? " (" + anios[s.fk_id_calendario] + ")" : ""));
        }

        private static int OrdenDia(string dia)
        {
            string d = Normalizar(dia).Replace("á", "a").Replace("é", "e");
            int posicion = Array.IndexOf(Dias, d);
            return posicion < 0 ? 99 : posicion;
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