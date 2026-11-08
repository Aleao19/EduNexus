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

        public static List<HorarioItemDto> ConstruirItems(EduNexusDbContext db, List<HorarioEntity> horarios)
        {
            Dictionary<int, string> secciones = EtiquetasDeSecciones(db);

            List<int> idsMaterias = horarios.Select(h => h.fk_materia).Distinct().ToList();
            Dictionary<int, MateriaEntity> materias = db.Materias
                .Where(m => idsMaterias.Contains(m.id_materia)).ToList()
                .ToDictionary(m => m.id_materia);

            List<string> idsProfesores = materias.Values.Select(m => m.profesor).Distinct().ToList();
            Dictionary<string, UsuarioEntity> profesores = db.Usuarios
                .Where(u => idsProfesores.Contains(u.id_usuario)).ToList()
                .ToDictionary(u => u.id_usuario);

            return horarios.Select(h =>
            {
                MateriaEntity materia = materias[h.fk_materia];
                UsuarioEntity profesor;
                profesores.TryGetValue(materia.profesor, out profesor);
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
                    profesor = profesor != null ? NombreCompleto(profesor) : "",
                    id_seccion = h.fk_seccion,
                    seccion = seccion ?? ""
                };
            })
            .OrderBy(x => OrdenDia(x.dia_semana))
            .ThenBy(x => x.hora_inicio)
            .ToList();
        }

        // Devuelve el mensaje del conflicto encontrado, o null si no hay conflicto
        // idExcluir = id del horario que se esta modificando (0 al registrar)
        public static string BuscarConflicto(EduNexusDbContext db, int idSeccion, int idMateria,
            string dia, TimeSpan inicio, TimeSpan fin, int idExcluir)
        {
            MateriaEntity materia = db.Materias.FirstOrDefault(m => m.id_materia == idMateria);
            if (materia == null)
            {
                return null;
            }

            // Conflicto por seccion: la seccion ya tiene una materia en ese bloque
            HorarioEntity enSeccion = db.Horarios
                .Where(h => h.fk_seccion == idSeccion && h.dia_semana == dia && h.id_horario != idExcluir)
                .ToList()
                .FirstOrDefault(h => h.hora_inicio < fin && h.hora_fin > inicio);

            if (enSeccion != null)
            {
                int idMateriaOcupante = enSeccion.fk_materia;
                string ocupante = db.Materias.Where(m => m.id_materia == idMateriaOcupante)
                    .Select(m => m.nombre).FirstOrDefault();

                return "Conflicto: la seccion ya tiene la materia " + ocupante + " asignada el "
                    + enSeccion.dia_semana + " de " + Hora(enSeccion.hora_inicio) + " a " + Hora(enSeccion.hora_fin) + ".";
            }

            // Conflicto por docente: el profesor de la materia ya tiene otra leccion en ese bloque
            List<int> materiasDelDocente = db.Materias
                .Where(m => m.profesor == materia.profesor)
                .Select(m => m.id_materia).ToList();

            HorarioEntity enDocente = db.Horarios
                .Where(h => materiasDelDocente.Contains(h.fk_materia) && h.dia_semana == dia && h.id_horario != idExcluir)
                .ToList()
                .FirstOrDefault(h => h.hora_inicio < fin && h.hora_fin > inicio);

            if (enDocente != null)
            {
                string seccion;
                EtiquetasDeSecciones(db).TryGetValue(enDocente.fk_seccion, out seccion);
                string idProfesor = materia.profesor;
                UsuarioEntity docente = db.Usuarios.FirstOrDefault(u => u.id_usuario == idProfesor);

                return "Conflicto: el docente " + (docente != null ? NombreCompleto(docente) : "")
                    + " ya tiene una leccion asignada el " + enDocente.dia_semana
                    + " de " + Hora(enDocente.hora_inicio) + " a " + Hora(enDocente.hora_fin)
                    + " con la seccion " + seccion + ".";
            }

            return null;
        }
    }
}