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

        // Nombres de los días como se muestran y se guardan en la tabla horarios
        public static readonly string[] DiasSemana = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

        public static string Hora(TimeSpan t)
        {
            return t.ToString("hh\\:mm");
        }

        public static int OrdenDia(string dia)
        {
            int posicion = Array.IndexOf(OrdenDias, SinTildes(dia));
            return posicion < 0 ? 99 : posicion;
        }

        // Nombre oficial del día ("miercoles" -> "Miércoles"), o null si no es un día válido
        public static string NombreDia(string dia)
        {
            int posicion = OrdenDia(dia);
            return posicion < DiasSemana.Length ? DiasSemana[posicion] : null;
        }

        // Compara dos días sin importar tildes ni mayúsculas ("Miércoles" == "miercoles")
        public static bool MismoDia(string a, string b)
        {
            int pa = OrdenDia(a);
            return pa != 99 ? pa == OrdenDia(b) : SinTildes(a) == SinTildes(b);
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
                if (clase != null)
                {
                    return clase.fk_id_profesor;
                }
                MateriaEntity m;
                return materias.TryGetValue(h.fk_materia, out m) ? m.profesor : null;
            };

            List<string> idsProfesores = horarios.Select(docenteDe).Where(x => x != null).Distinct().ToList();
            Dictionary<string, UsuarioEntity> profesores = db.Usuarios
                .Where(u => idsProfesores.Contains(u.id_usuario)).ToList()
                .ToDictionary(u => u.id_usuario);

            return horarios.Select(h =>
            {
                MateriaEntity materia;
                materias.TryGetValue(h.fk_materia, out materia);
                string idProfesor = docenteDe(h);
                UsuarioEntity profesor = null;
                if (idProfesor != null)
                {
                    profesores.TryGetValue(idProfesor, out profesor);
                }
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
                    id_materia = h.fk_materia,
                    materia = materia != null ? materia.nombre : "",
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

        // Choque de horario al asignar un docente a una clase (sección + materia) que ya tiene horarios:
        // devuelve el mensaje si el docente ya da otra clase en alguno de esos bloques, o null.
        public static string BuscarChoqueDocente(EduNexusDbContext db, string idDocente, int idSeccion, int idMateria)
        {
            List<HorarioEntity> horariosClase = db.Horarios
                .Where(h => h.fk_seccion == idSeccion && h.fk_materia == idMateria).ToList();
            if (!horariosClase.Any())
            {
                return null;
            }

            List<SeccionMateriaEntity> otrasClases = ClasesDelDocente(db, idDocente)
                .Where(c => !(c.fk_id_seccion == idSeccion && c.fk_id_materia == idMateria)).ToList();
            if (!otrasClases.Any())
            {
                return null;
            }

            List<int> idsSecciones = otrasClases.Select(c => c.fk_id_seccion).Distinct().ToList();
            List<HorarioEntity> horariosDocente = db.Horarios
                .Where(h => idsSecciones.Contains(h.fk_seccion)).ToList()
                .Where(h => otrasClases.Any(c => c.fk_id_seccion == h.fk_seccion && c.fk_id_materia == h.fk_materia))
                .ToList();

            foreach (HorarioEntity hc in horariosClase.OrderBy(h => OrdenDia(h.dia_semana)).ThenBy(h => h.hora_inicio))
            {
                HorarioEntity choque = horariosDocente.FirstOrDefault(o => MismoDia(o.dia_semana, hc.dia_semana)
                    && o.hora_inicio < hc.hora_fin && o.hora_fin > hc.hora_inicio);
                if (choque != null)
                {
                    string seccion;
                    EtiquetasDeSecciones(db).TryGetValue(choque.fk_seccion, out seccion);
                    string materia = db.Materias.Where(m => m.id_materia == choque.fk_materia)
                        .Select(m => m.nombre).FirstOrDefault();
                    return "Choque de horario: el docente ya imparte " + materia + " en la sección " + seccion
                        + " el " + choque.dia_semana + " de " + Hora(choque.hora_inicio) + " a " + Hora(choque.hora_fin)
                        + ", que coincide con el horario de esta clase (" + hc.dia_semana + " de "
                        + Hora(hc.hora_inicio) + " a " + Hora(hc.hora_fin) + ").";
                }
            }
            return null;
        }

        // Devuelve el mensaje del conflicto encontrado, o null si no hay conflicto.
        // idExcluir = id del horario que se está modificando (0 al registrar).
        public static string BuscarConflicto(EduNexusDbContext db, int idSeccion, int idMateria,
            string dia, TimeSpan inicio, TimeSpan fin, int idExcluir)
        {
            // Conflicto por sección: la sección ya tiene una materia en ese bloque
            // El día se compara en memoria para tolerar tildes ("Miércoles" / "Miercoles")
            HorarioEntity enSeccion = db.Horarios
                .Where(h => h.fk_seccion == idSeccion && h.id_horario != idExcluir)
                .ToList()
                .FirstOrDefault(h => MismoDia(h.dia_semana, dia) && h.hora_inicio < fin && h.hora_fin > inicio);

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
                .Where(h => h.id_horario != idExcluir)
                .ToList()
                .FirstOrDefault(h => MismoDia(h.dia_semana, dia) && h.hora_inicio < fin && h.hora_fin > inicio
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