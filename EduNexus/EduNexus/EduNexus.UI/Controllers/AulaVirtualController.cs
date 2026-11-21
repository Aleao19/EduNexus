using EduNexus.Abstracciones.ModelosParaUI.Asignaciones;
using EduNexus.Abstracciones.ModelosParaUI.Materias;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // Aula virtual: Docente (sus clases), Estudiante (las de sus secciones),
    // Administrador y Director (todas, solo consulta).
    [AutorizarRoles(RolesSistema.Administrador, RolesSistema.Director, RolesSistema.Docente, RolesSistema.Estudiante)]
    public class AulaVirtualController : Controller
    {
        // GET: AulaVirtual
        public ActionResult AulaVirtual()
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                List<SeccionMateriaEntity> clases = ObtenerClasesVisibles(db, usuario);

                Dictionary<int, string> secciones = HorarioServicio.EtiquetasDeSecciones(db);

                List<int> idsMaterias = clases.Select(c => c.fk_id_materia).Distinct().ToList();
                Dictionary<int, MateriaEntity> materias = db.Materias
                    .Where(m => idsMaterias.Contains(m.id_materia)).ToList()
                    .ToDictionary(m => m.id_materia);

                List<string> idsProfesores = clases.Select(c => c.fk_id_profesor).Distinct().ToList();
                Dictionary<string, UsuarioEntity> profesores = db.Usuarios
                    .Where(u => idsProfesores.Contains(u.id_usuario)).ToList()
                    .ToDictionary(u => u.id_usuario);

                List<MateriasDto> modelo = clases.Where(c => materias.ContainsKey(c.fk_id_materia)).Select(c =>
                {
                    MateriaEntity materia = materias[c.fk_id_materia];
                    UsuarioEntity profesor;
                    if (c.fk_id_profesor == null || !profesores.TryGetValue(c.fk_id_profesor, out profesor)) profesor = null;
                    string seccion;
                    secciones.TryGetValue(c.fk_id_seccion, out seccion);

                    return new MateriasDto
                    {
                        id_materia = materia.id_materia,
                        nombre = materia.nombre,
                        profesor = profesor != null ? HorarioServicio.NombreCompleto(profesor) : "",
                        id_seccion = c.fk_id_seccion,
                        seccion = seccion
                    };
                })
                .OrderBy(x => x.nombre).ThenBy(x => x.seccion)
                .ToList();

                return View(modelo);
            }
        }

        // Clases (materia + sección) del periodo actual que el usuario puede ver:
        // - Docente: las clases donde él es el docente (secciones_materias.fk_id_profesor).
        // - Estudiante: las materias de las secciones donde está matriculado.
        // - Administrador y Director: todas (solo consulta).
        // - Cualquier otro rol: ninguna.
        private static List<SeccionMateriaEntity> ObtenerClasesVisibles(EduNexusDbContext db, UsuarioDto usuario)
        {
            bool esDocente = RolesSistema.Es(usuario, RolesSistema.Docente);
            bool esEstudiante = RolesSistema.Es(usuario, RolesSistema.Estudiante);
            bool veTodas = RolesSistema.Es(usuario, RolesSistema.Administrador, RolesSistema.Director);

            if (!esDocente && !esEstudiante && !veTodas)
            {
                return new List<SeccionMateriaEntity>();
            }

            DateTime hoy = DateTime.Today;

            List<int> idsCalendario = db.Calendarios
                .Where(c => c.fecha_inicio <= hoy && c.fecha_fin >= hoy)
                .Select(c => c.id_calendario).ToList();

            List<int> idsSecciones = db.Secciones
                .Where(s => idsCalendario.Contains(s.fk_id_calendario))
                .Select(s => s.id_seccion).ToList();

            IQueryable<SeccionMateriaEntity> consulta = db.SeccionesMaterias
                .Where(sm => idsSecciones.Contains(sm.fk_id_seccion));

            string idUsuario = usuario.id_usuario;

            if (esDocente)
            {
                consulta = consulta.Where(sm => sm.fk_id_profesor == idUsuario);
            }
            else if (esEstudiante)
            {
                List<int> susSecciones = db.SeccionesEstudiantes
                    .Where(se => se.fk_id_usuario == idUsuario)
                    .Select(se => se.fk_id_seccion).ToList();

                consulta = consulta.Where(sm => susSecciones.Contains(sm.fk_id_seccion));
            }

            return consulta.ToList();
        }

        // GET: AulaVirtual/Materia/5?seccion=2
        // Vista de una clase: asignaciones reales (solo lectura), estudiantes de la sección
        // y, para el docente, la pestaña de asistencia.
        public ActionResult Materia(int? id, int? seccion)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                SeccionMateriaEntity clase = id.HasValue
                    ? ObtenerClasesVisibles(db, usuario).FirstOrDefault(c =>
                        c.fk_id_materia == id.Value && (!seccion.HasValue || c.fk_id_seccion == seccion.Value))
                    : null;

                if (clase == null)
                {
                    TempData["Error"] = "No tiene acceso a esta clase o la clase no existe en el periodo actual.";
                    return RedirectToAction("AulaVirtual");
                }

                int idSeccion = clase.fk_id_seccion;
                int idMateria = clase.fk_id_materia;
                bool esEstudiante = RolesSistema.Es(usuario, RolesSistema.Estudiante);

                MateriaEntity materia = db.Materias.FirstOrDefault(m => m.id_materia == idMateria);
                string etiquetaSeccion;
                HorarioServicio.EtiquetasDeSecciones(db).TryGetValue(idSeccion, out etiquetaSeccion);

                // Estudiantes reales de la sección
                List<string> idsEstudiantes = db.SeccionesEstudiantes
                    .Where(se => se.fk_id_seccion == idSeccion)
                    .Select(se => se.fk_id_usuario).ToList();
                Dictionary<string, UsuarioEntity> estudiantes = db.Usuarios
                    .Where(u => idsEstudiantes.Contains(u.id_usuario)).ToList()
                    .ToDictionary(u => u.id_usuario);

                // Asignaciones reales de la clase (el estudiante solo ve las suyas)
                IQueryable<AsignacionEntity> consulta = db.Asignaciones
                    .Where(a => a.fk_id_seccion == idSeccion && a.fk_id_materia == idMateria);
                if (esEstudiante)
                {
                    string idUsuario = usuario.id_usuario;
                    consulta = consulta.Where(a => a.fk_id_usuario == idUsuario);
                }
                List<AsignacionEntity> registros = consulta.OrderBy(a => a.nombre).ToList();

                List<AsignacionesDto> asignaciones = registros.Select(a => new AsignacionesDto
                {
                    id_asignacion = a.id_asignacion,
                    nombre = a.nombre,
                    descripcion = a.descripcion,
                    porcentaje_nota = (float)a.porcentaje_nota,
                    calificacion = (float)a.calificacion,
                    estado = a.estado
                }).ToList();

                Dictionary<int, string> estudiantePorAsignacion = registros.ToDictionary(
                    a => a.id_asignacion,
                    a =>
                    {
                        UsuarioEntity est;
                        return a.fk_id_usuario != null && estudiantes.TryGetValue(a.fk_id_usuario, out est)
                            ? HorarioServicio.NombreCompleto(est)
                            : "";
                    });

                ViewBag.IdMateria = idMateria;
                ViewBag.IdSeccion = idSeccion;
                ViewBag.NombreMateria = materia != null ? materia.nombre : "Materia";
                ViewBag.NombreSeccion = etiquetaSeccion ?? "";
                ViewBag.EsDocente = RolesSistema.Es(usuario, RolesSistema.Docente);
                ViewBag.EsEstudiante = esEstudiante;
                ViewBag.EstudiantePorAsignacion = estudiantePorAsignacion;
                ViewBag.Estudiantes = estudiantes.Values
                    .Select(u => HorarioServicio.NombreCompleto(u))
                    .OrderBy(n => n)
                    .ToList();

                return View(asignaciones);
            }
        }
    }
}
