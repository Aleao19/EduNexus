using EduNexus.Abstracciones.ModelosParaUI.Horarios;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;

namespace EduNexus.UI.Models
{
    public static class HorarioServicio
    {
        public const string RolDocente = "Docente";
        private static readonly string[] OrdenDias = { "lunes", "martes", "miercoles", "jueves", "viernes", "sabado", "domingo" };

        public static string Hora(TimeSpan t)
        {
            return t.ToString("hh\\:mm");
        }

        public static int OrdenDia(string dia)
        {
            string d = (dia ?? "").Trim().ToLowerInvariant().Replace("é", "e").Replace("a", "a");
            int posicion = Array.IndexOf(OrdenDias, d);
            return posicion < 0 ? 99 : posicion;
        }
        public static string SinTildes(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return "";
            }

            string descompuesto = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
        }
        public static string NombreCompleto(UsuarioEntity u)
        {
            return string.Join(" ", new[] { u.nombre, u.apellido1, u.apellido2 }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        public static List<UsuarioEntity> ObtenerDocentes(EduNexusDbContext db)
        {
            string idRol = db.Roles.Where(r => r.nombre == RolDocente).Select(r => r.id_rol).FirstOrDefault();
            if (idRol == null)
            {
                return new List<UsuarioEntity>();
            }
            return db.Usuarios.Where(u => u.rol == idRol).OrderBy(u => u.nombre).ToList();
        }

        // "Tercer Grado - 3-A (2026)"
        public static Dictionary<int, string> EtiquetasDeSecciones(EduNexusDbContext db)
        {
            Dictionary<int, string> grados = db.Grados.ToList().ToDictionary(g => g.id_grado, g => g.nombre);
            Dictionary<int, int> anios = db.Calendarios.ToList().ToDictionary(c => c.id_calendario, c => c.fecha_inicio.Year);

            return db.Secciones.ToList().ToDictionary(
                s => s.id_seccion,
                s => (grados.ContainsKey(s.fk_id_grado) ? grados[s.fk_id_grado] : "Grado")
                     + " - " + s.nombre
                     + (anios.ContainsKey(s.fk_id_calendario) ? " (" + anios[s.fk_id_calendario] + ")" : ""));
        }

        // Docente de una clase (sección + materia)
        public static string DocenteDeClase(EduNexusDbContext db, int idSeccion, int idMateria)
        {
            string idProfesor = db.SeccionesMaterias
                .Where(sm => sm.fk_id_seccion == idSeccion && sm.fk_id_materia == idMateria)
                .Select(sm => sm.fk_id_profesor).FirstOrDefault();

            if (idProfesor != null)
            {
                return idProfesor;
            }

            // Si la materia no está asignada a la sección, se usa su profesor responsable
            return db.Materias.Where(m => m.id_materia == idMateria)
                .Select(m => m.profesor).FirstOrDefault();
        }

        // Clases (sección + materia) que da un docente
        public static List<SeccionMateriaEntity> ClasesDelDocente(EduNexusDbContext db, string idDocente)
        {
            return db.SeccionesMaterias.Where(sm => sm.fk_id_profesor == idDocente).ToList();
        }

        public static List<HorarioItemDto> ConstruirItems(EduNexusDbContext db, List<HorarioEntity> horarios)
        {
            Dictionary<int, string> secciones = EtiquetasDeSecciones(db);

            List<int> idsMaterias = horarios.Select(h => h.fk_materia).Distinct().ToList();
            Dictionary<int, MateriaEntity> materias = db.Materias
                .Where(m => idsMaterias.Contains(m.id_materia)).ToList()
                .ToDictionary(m => m.id_materia);

            // Docente de cada clase (sección + materia)
            List<int> idsSecciones = horarios.Select(h => h.fk_seccion).Distinct().ToList();
            List<SeccionMateriaEntity> clases = db.SeccionesMaterias
                .Where(sm => idsSecciones.Contains(sm.fk_id_seccion)).ToList();

            Func<HorarioEntity, string> docenteDe = h =>
            {
                SeccionMateriaEntity clase = clases.FirstOrDefault(c =>
                    c.fk_id_seccion == h.fk_seccion && c.fk_id_materia == h.fk_materia);
                return clase != null ? clase.fk_id_profesor : materias[h.fk_materia].profesor;
            };

            List<string> idsProfesores = horarios.Select(docenteDe).Distinct().ToList();
            Dictionary<string, UsuarioEntity> profesores = db.Usuarios
                .Where(u => idsProfesores.Contains(u.id_usuario)).ToList()
                .ToDictionary(u => u.id_usuario);

            return horarios.Select(h =>
            {
                MateriaEntity materia = materias[h.fk_materia];
                string idProfesor = docenteDe(h);
                UsuarioEntity profesor;
                profesores.TryGetValue(idProfesor, out profesor);
                string seccion;
                secciones.TryGetValue(h.fk_seccion, out seccion);

                return new HorarioItemDto
                {
                    id_horario = h.id_horario,
                    dia_semana = h.dia_semana,
                    hora_inicio = h.hora_inicio,
                    hora_fin = h.hora_fin,
                    inicio = Hora(h.hora_inicio),
                    fin = Hora(h.hora_fin),
                    id_materia = materia.id_materia,
                    materia = materia.nombre,
                    id_profesor = idProfesor,
                    profesor = profesor != null ? NombreCompleto(profesor) : "",
                    id_seccion = h.fk_seccion,
                    seccion = seccion ?? ""
                };
            })
            .OrderBy(x => OrdenDia(x.dia_semana))
            .ThenBy(x => x.hora_inicio)
            .ToList();
        }

        // Devuelve el mensaje del conflicto encontrado, o null si no hay conflicto.
        // idExcluir = id del horario que se está modificando (0 al registrar).
        public static string BuscarConflicto(EduNexusDbContext db, int idSeccion, int idMateria,
            string dia, TimeSpan inicio, TimeSpan fin, int idExcluir)
        {
            // Conflicto por sección: la sección ya tiene una materia en ese bloque
            HorarioEntity enSeccion = db.Horarios
                .Where(h => h.fk_seccion == idSeccion && h.dia_semana == dia && h.id_horario != idExcluir)
                .ToList()
                .FirstOrDefault(h => h.hora_inicio < fin && h.hora_fin > inicio);

            if (enSeccion != null)
            {
                int idMateriaOcupante = enSeccion.fk_materia;
                string ocupante = db.Materias.Where(m => m.id_materia == idMateriaOcupante)
                    .Select(m => m.nombre).FirstOrDefault();

                return "Conflicto: la sección ya tiene la materia " + ocupante + " asignada el "
                    + enSeccion.dia_semana + " de " + Hora(enSeccion.hora_inicio) + " a " + Hora(enSeccion.hora_fin) + ".";
            }

            // Conflicto por docente: el docente de esta clase ya da otra en ese bloque
            string idProfesor = DocenteDeClase(db, idSeccion, idMateria);
            if (idProfesor == null)
            {
                return null;
            }

            List<SeccionMateriaEntity> clasesDelDocente = ClasesDelDocente(db, idProfesor);

            HorarioEntity enDocente = db.Horarios
                .Where(h => h.dia_semana == dia && h.id_horario != idExcluir)
                .ToList()
                .FirstOrDefault(h => h.hora_inicio < fin && h.hora_fin > inicio
                    && clasesDelDocente.Any(c => c.fk_id_seccion == h.fk_seccion && c.fk_id_materia == h.fk_materia));

            if (enDocente != null)
            {
                string seccion;
                EtiquetasDeSecciones(db).TryGetValue(enDocente.fk_seccion, out seccion);
                UsuarioEntity docente = db.Usuarios.FirstOrDefault(u => u.id_usuario == idProfesor);

                return "Conflicto: el docente " + (docente != null ? NombreCompleto(docente) : "")
                    + " ya tiene una lección asignada el " + enDocente.dia_semana
                    + " de " + Hora(enDocente.hora_inicio) + " a " + Hora(enDocente.hora_fin)
                    + " en la sección " + seccion + ".";
            }

            return null;
        }
    }
}